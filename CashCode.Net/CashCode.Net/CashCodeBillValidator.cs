using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace CashCode.Net
{
    public enum BillValidatorCommands { ACK=0x00, NAK=0xFF, POLL=0x33, RESET=0x30, GET_STATUS=0x31, SET_SECURITY=0x32,
                                        IDENTIFICATION=0x37, ENABLE_BILL_TYPES=0x34, STACK=0x35, RETURN=0x36, HOLD=0x38}

    public enum BillRecievedStatus {Accepted, Rejected };

    public enum BillCassetteStatus { Inplace, Removed };

    // Делегат события получения банкноты
    public delegate void BillReceivedHandler(object Sender, BillReceivedEventArgs e);

    // Делегат события для контроля за кассетой
    public delegate void BillCassetteHandler(object Sender, BillCassetteEventArgs e);

    // Делегат события в процессе отправки купюры в стек (Здесь можно делать возврат)
    public delegate void BillStackingHandler(object Sender, BillStackedEventArgs e);

    // Делегат события ошибок
    public delegate void BillExceptionHandler(object Sender, BillExceptionEventArgs e);

    public sealed class CashCodeBillValidator : IDisposable
    {
        #region Закрытые члены
        
        private const int POLL_TIMEOUT = 200;    // Тайм-аут ожидания ответа от считывателя
        private const int EVENT_WAIT_HANDLER_TIMEOUT = 10000; // Тайм-аут ожидания снятия блокировки

        private byte[] ENABLE_BILL_TYPES_WITH_ESCROW = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        private EventWaitHandle _SynchCom;     // Переменная синхронизации отправки и считывания данных с ком порта
        private List<byte> _ReceivedBytes;  // Полученные байты

        private int _LastError;
        private bool _Disposed;
        private string _ComPortName;
        private bool _IsConnected;
        private int _BaudRate;
        private bool _IsPowerUp;
        private bool _IsListening;
        private bool _IsEnableBills;
        private object _Locker;

        private SerialPort _ComPort;
        private CashCodeErroList _ErrorList;

        private System.Timers.Timer _Listener;  // Таймер прослушивания купюроприемника

        bool _ReturnBill;

        private Dictionary<int, int> CashCodeTable;

        BillCassetteStatus _cassettestatus = BillCassetteStatus.Inplace;
        #endregion

        #region Конструкторы

        public CashCodeBillValidator(string PortName, int BaudRate, Dictionary<int, int> cashCodeTable = null)
        {
            if (cashCodeTable == null)
                CashCodeTable = _defaultCashCodeTable;

            this._ErrorList = new CashCodeErroList();
            
            this._Disposed = false;
            this._IsEnableBills = false;
            this._ComPortName = "";
            this._Locker = new object();
            this._IsConnected = this._IsPowerUp = this._IsListening = this._ReturnBill = false;

            // Из спецификации:
            //      Baud Rate:	9600 bps/19200 bps (no negotiation, hardware selectable)
            //      Start bit:	1
            //      Data bit:	8 (bit 0 = LSB, bit 0 sent first)
            //      Parity:		Parity none 
            //      Stop bit:	1
            this._ComPort = new SerialPort();
            this._ComPort.PortName = this._ComPortName = PortName;
            this._ComPort.BaudRate = this._BaudRate = BaudRate;
            this._ComPort.DataBits = 8;
            this._ComPort.Parity = Parity.None;
            this._ComPort.StopBits = StopBits.One;
            this._ComPort.DataReceived += new SerialDataReceivedEventHandler(_ComPort_DataReceived);

            this._ReceivedBytes = new List<byte>();
            this._SynchCom = new EventWaitHandle(false, EventResetMode.AutoReset);

            this._Listener = new System.Timers.Timer();
            this._Listener.Interval = POLL_TIMEOUT;
            this._Listener.Enabled = false;
            this._Listener.Elapsed += new System.Timers.ElapsedEventHandler(_Listener_Elapsed);
        }

        #endregion

        #region Деструктор
        // Реализует интерфейс IDisposable
        public void Dispose()
        {
            Dispose(true);
        }

        // Dispose(bool disposing) выполняется по двум сценариям
        // Если disposing=true, метод Dispose вызывается явно или неявно из кода пользователя
        // Управляемые и неуправляемые ресурсы могут быть освобождены
        // Если disposing=false, то метод может быть вызван runtime из финализатора
        // В таком случае только неуправляемые ресурсы могут быть освобождены.
        private void Dispose(bool disposing)
        {
            // Проверим вызывался ли уже метод Dispose
            if (!this._Disposed)
            {
                // Если disposing=true, освободим все управляемые и неуправляемые ресурсы
                if (disposing)
                {
                    _Listener.Dispose();
                    _IsListening = false;
                    _ComPort.Dispose();
                }

                _Disposed = true;
            }
        }

        #endregion

        #region Свойства
        
        public bool IsConnected
        {
            get { return _IsConnected; }
        }

        #endregion

        #region Открытые методы

        /// <summary>
        /// Начало прослушки купюроприемника
        /// </summary>
        public void StartListening()
        {
            // Если не подключен
            if (!this._IsConnected)
            {
                this._LastError = 100020;
                throw new System.IO.IOException(this._ErrorList.Errors[this._LastError]);
            }

            // Если отсутствует энергия, то включим
            if (!this._IsPowerUp) { this.PowerUpBillValidator(); }

            this._IsListening = true;
            this._Listener.Start();
        }

        /// <summary>
        /// Остановк прослушки купюроприемника
        /// </summary>
        public void StopListening()
        {
            this._IsListening = false;
            this._Listener.Stop();
            this.DisableBillValidator();
        }

        /// <summary>
        /// Открытие Ком-порта для работы с купюроприемником
        /// </summary>
        /// <returns></returns>
        public int ConnectBillValidator()
        {
            try
            {
                this._ComPort.Open();
                this._IsConnected = true;
            }
            catch
            {
                this._IsConnected = false;
               this._LastError = 100010;
            }

            return this._LastError;
        }

        // Включение купюроприемника
        public int PowerUpBillValidator()
        {
            List<byte> ByteResult = null;

            // Если ком-порт не открыт
            if (!this._IsConnected)
            {
                this._LastError = 100020;
                throw new System.IO.IOException(this._ErrorList.Errors[this._LastError]);
            }

            // POWER UP
            ByteResult = this.SendCommand(BillValidatorCommands.POLL).ToList();

            // Проверим результат
            if (CheckPollOnError(ByteResult.ToArray()))
            {
                this.SendCommand(BillValidatorCommands.NAK);
                throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
            }

            // Иначе отправляем сигнал подтверждения
            this.SendCommand(BillValidatorCommands.ACK);

            // RESET
            ByteResult = this.SendCommand(BillValidatorCommands.RESET).ToList();

            //Если не получили от купюроприемника сигнала ACK
            if (ByteResult[3] != 0x00)
            {
                this._LastError = 100050;
                return this._LastError;
            }

            // INITIALIZE
            // Далее снова опрашиваем купюроприемник
            ByteResult = this.SendCommand(BillValidatorCommands.POLL).ToList();

            if (CheckPollOnError(ByteResult.ToArray()))
            {
                this.SendCommand(BillValidatorCommands.NAK);
                throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
            }

            // Иначе отправляем сигнал подтверждения
            this.SendCommand(BillValidatorCommands.ACK);

            // GET STATUS
            ByteResult = this.SendCommand(BillValidatorCommands.GET_STATUS).ToList();

            // Команда GET STATUS возвращает 6 байт ответа. Если все равны 0, то статус ok и можно работать дальше, иначе ошибка
            if (ByteResult[3] != 0x00 || ByteResult[4] != 0x00 || ByteResult[5] != 0x00 ||
                ByteResult[6] != 0x00 || ByteResult[7] != 0x00 || ByteResult[8] != 0x00)
            {
                this._LastError = 100070;
                throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
            }

            this.SendCommand(BillValidatorCommands.ACK);

            // SET_SECURITY (в тестовом примере отправояет 3 байта (0 0 0)
            ByteResult = this.SendCommand(BillValidatorCommands.SET_SECURITY, new byte[3] { 0x00, 0x00, 0x00 }).ToList();

            //Если не получили от купюроприемника сигнала ACK
            if (ByteResult[3] != 0x00)
            {
                this._LastError = 100050;
                return this._LastError;
            }

            // IDENTIFICATION
            ByteResult = this.SendCommand(BillValidatorCommands.IDENTIFICATION).ToList();
            this.SendCommand(BillValidatorCommands.ACK);


            // POLL
            // Далее снова опрашиваем купюроприемник. Должны получить команду INITIALIZE
            ByteResult = this.SendCommand(BillValidatorCommands.POLL).ToList();

            // Проверим результат
            if (CheckPollOnError(ByteResult.ToArray()))
            {
                this.SendCommand(BillValidatorCommands.NAK);
                throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
            }

            // Иначе отправляем сигнал подтверждения
            this.SendCommand(BillValidatorCommands.ACK);

            // POLL
            // Далее снова опрашиваем купюроприемник. Должны получить команду UNIT DISABLE
            ByteResult = this.SendCommand(BillValidatorCommands.POLL).ToList();

            // Проверим результат
            if (CheckPollOnError(ByteResult.ToArray()))
            {
                this.SendCommand(BillValidatorCommands.NAK);
                throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
            }

            // Иначе отправляем сигнал подтверждения
            this.SendCommand(BillValidatorCommands.ACK);

            this._IsPowerUp = true;

            return this._LastError;
        }

        // Включение режима приема купюр
        public int EnableBillValidator()
        {
            List<byte> ByteResult = null;

            // Если ком-порт не открыт
            if (!this._IsConnected)
            {
                this._LastError = 100020;
                throw new System.IO.IOException(this._ErrorList.Errors[this._LastError]);
            }

            try
            {
                if (!_IsListening)
                {
                    throw new InvalidOperationException(CashCode.Net.Properties.Resource.EnableBillValidator_ErrorInTheMethodOfEnablingTheReceptionOfBanknotesYouMustCallTheStartListeningMethod);
                }

                lock (_Locker)
                {
                    _IsEnableBills = true;

                    // отпавить команду ENABLE BILL TYPES (в тестовом примере отправляет 6 байт  (255 255 255 0 0 0) Функция удержания включена (Escrow)
                    ByteResult = this.SendCommand(BillValidatorCommands.ENABLE_BILL_TYPES, ENABLE_BILL_TYPES_WITH_ESCROW).ToList();

                    //Если не получили от купюроприемника сигнала ACK
                    if (ByteResult[3] != 0x00)
                    {
                        this._LastError = 100050;
                        throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
                    }

                    // Далее снова опрашиваем купюроприемник
                    ByteResult = this.SendCommand(BillValidatorCommands.POLL).ToList();

                    // Проверим результат
                    if (CheckPollOnError(ByteResult.ToArray()))
                    {
                        this.SendCommand(BillValidatorCommands.NAK);
                        throw new System.ArgumentException(this._ErrorList.Errors[this._LastError]);
                    }

                    // Иначе отправляем сигнал подтверждения
                    this.SendCommand(BillValidatorCommands.ACK);
                }
            }
            catch
            {
                this._LastError = 100030;
            }

            return this._LastError;
        }

        // Выключение режима приема купюр
        public int DisableBillValidator()
        {
            List<byte> ByteResult = null;

            lock (_Locker)
            {
                // Если ком-порт не открыт
                if (!this._IsConnected)
                {
                    this._LastError = 100020;
                    throw new System.IO.IOException(this._ErrorList.Errors[this._LastError]);
                }

                _IsEnableBills = false;

                // отпавить команду ENABLE BILL TYPES (в тестовом примере отправояет 6 байт (0 0 0 0 0 0)
                ByteResult = this.SendCommand(BillValidatorCommands.ENABLE_BILL_TYPES, new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }).ToList();
            }

            //Если не получили от купюроприемника сигнала ACK
            if (ByteResult[3] != 0x00)
            {
                this._LastError = 100050;
                return this._LastError;
            }

            return this._LastError;
        }

        #endregion

        #region Закрытые методы

        private bool CheckPollOnError(byte[] ByteResult)
        {
            bool IsError = false;

            //Если не получили от купюроприемника третий байт равный 30Н (ILLEGAL COMMAND )
            if (ByteResult[3] == 0x30)
            {
                this._LastError = 100040;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 41Н (Drop Cassette Full)
            else if (ByteResult[3] == 0x41)
            {
                this._LastError = 100080;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 42Н (Drop Cassette out of position)
            else if (ByteResult[3] == 0x42)
            {
                this._LastError = 100070;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 43Н (Validator Jammed)
            else if (ByteResult[3] == 0x43)
            {
                this._LastError = 100090;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 44Н (Drop Cassette Jammed)
            else if (ByteResult[3] == 0x44)
            {
                this._LastError = 100100;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 45Н (Cheated)
            else if (ByteResult[3] == 0x45)
            {
                this._LastError = 100110;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 46Н (Pause)
            else if (ByteResult[3] == 0x46)
            {
                this._LastError = 100120;
                IsError = true;
            }
            //Если не получили от купюроприемника третий байт равный 47Н (Generic Failure codes)
            else if (ByteResult[3] == 0x47)
            {
                if (ByteResult[4] == 0x50) { this._LastError = 100130; }        // Stack Motor Failure
                else if (ByteResult[4] == 0x51) { this._LastError = 100140; }   // Transport Motor Speed Failure
                else if (ByteResult[4] == 0x52) { this._LastError = 100150; }   // Transport Motor Failure
                else if (ByteResult[4] == 0x53) { this._LastError = 100160; }   // Aligning Motor Failure
                else if (ByteResult[4] == 0x54) { this._LastError = 100170; }   // Initial Cassette Status Failure
                else if (ByteResult[4] == 0x55) { this._LastError = 100180; }   // Optic Canal Failure
                else if (ByteResult[4] == 0x56) { this._LastError = 100190; }   // Magnetic Canal Failure
                else if (ByteResult[4] == 0x5F) { this._LastError = 100200; }   // Capacitance Canal Failure
                IsError = true;
            }

            return IsError;
        }

        

        // Отправка команды купюроприемнику
        private byte[] SendCommand(BillValidatorCommands cmd, byte[] data = null)
        {
            if (cmd == BillValidatorCommands.ACK || cmd == BillValidatorCommands.NAK)
            {
                byte[] bytes = null;
                
                if (cmd == BillValidatorCommands.ACK) { bytes = Package.CreateResponse(ResponseType.ACK); }
                if (cmd == BillValidatorCommands.NAK) { bytes = Package.CreateResponse(ResponseType.NAK); }

                if (bytes != null) {this._ComPort.Write(bytes, 0, bytes.Length);}

                return null;
            }
            else
            {
                Package package = new Package();
                package.Cmd = (byte)cmd;

                if (data != null) { package.Data = data; }

                byte[] CmdBytes = package.GetBytes();
                this._ComPort.Write(CmdBytes, 0, CmdBytes.Length);

                // Подождем пока получим данные с ком-порта
                var timeout = !this._SynchCom.WaitOne(EVENT_WAIT_HANDLER_TIMEOUT);
                this._SynchCom.Reset();

                if (timeout)
                    throw new TimeoutException();

                // Если CRC ок, то проверим четвертый бит с результатом
                // Должны уже получить данные с ком-порта, поэтому проверим CRC
                if (_packet == null || !Package.CheckCRC(_packet))
                {
                    throw new ArgumentException(CashCode.Net.Properties.Resource.SendCommand_TheMismatchOfTheChecksumOfTheReceivedMessageTheDeviceMayNotBeConnectedToTheCOMPortCheckYourConnectionSettings);
                }

                var res = _packet;
                _packet = null;

                return res;
            }

        }

        // Таблица кодов валют
        private static Dictionary<int, int> _defaultCashCodeTable = new Dictionary<int, int>()
        {
            { 0x02, 10 },               // 10 р.
            { 0x03, 50 },               // 50 р.
            { 0x04, 100 },              // 100 р.
            { 0x0c, 200 },              // 200 р.
            { 0x05, 500 },              // 500 р.
            { 0x06, 1000 },             // 1000 р.
            { 0x0d, 2000 },             // 2000 р.
            { 0x07, 5000 }              // 5000 р.
        };
        private byte[] _packet;
        private readonly object _dataSyncRoot = new object();

        #endregion

        #region События

        /// <summary>
        /// Событие получения купюры
        /// </summary>
        public event BillReceivedHandler BillReceived;

        private void OnBillReceived(BillReceivedEventArgs e)
        {
            if (BillReceived != null)
            {
                BillReceived(this, new BillReceivedEventArgs(e.Status, e.Value, e.RejectedReason));
            }
        }

        public event BillCassetteHandler BillCassetteStatusEvent;
        private void OnBillCassetteStatus(BillCassetteEventArgs e)
        {
            if (BillCassetteStatusEvent != null)
            {
                BillCassetteStatusEvent(this, new BillCassetteEventArgs(e.Status));
            }
        }


        /// <summary>
        /// Событие процесса отправки купюры в стек (Здесь можно делать возврат)
        /// </summary>
        public event BillStackingHandler BillStacking;

        private void OnBillStacking(BillStackedEventArgs e)
        {
            if (BillStacking != null)
            {
                bool cancel = false;
                foreach (BillStackingHandler subscriber in BillStacking.GetInvocationList())
                {
                    subscriber(this, e);

                    if (e.Cancel)
                    {
                        cancel = true;
                        break;
                    }
                }

                _ReturnBill = cancel;
            }
        }

        public event BillExceptionHandler BillException;

        private void OnBillException(BillExceptionEventArgs e)
        {
            if (BillException != null)
            {
                BillException(this, new BillExceptionEventArgs(e.Message));
            }
        }

        #endregion

        #region Обработчики событий

        // Получение данных с ком-порта
        private void _ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_dataSyncRoot)
            {
                // Читаем байты
                while (_ComPort.BytesToRead > 0)
                {
                    this._ReceivedBytes.Add((byte)_ComPort.ReadByte());
                }

                CheckPacket();
            }
        }

        private void CheckPacket()
        {
            var start = _ReceivedBytes.IndexOf(0x2);
            if (start >= 0)
            {
                var lng = start + 2;
                if (_ReceivedBytes.Count < lng + 1)
                    return;

                int length = _ReceivedBytes[lng];
                if (length == 0)
                {
                    var lngHigh = lng + 2;
                    var lngLow = lng + 3;

                    /// исправить если требуется суб команда

                    length = lngHigh;
                    length = length << 8;
                    length = length | lngLow;
                }

                if (_ReceivedBytes.Count < start + length)
                    return;

                _packet = _ReceivedBytes.Skip(start).Take(length).ToArray();
                _ReceivedBytes.Clear();

                // Снимаем блокировку
                this._SynchCom.Set();
            }
        }

        // Таймер прослушки купюроприемника
        private void _Listener_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this._Listener.Stop();

            try
            {
                lock (_Locker)
                {
                    List<byte> ByteResult = null;

                    // отпавить команду POLL
                    ByteResult = this.SendCommand(BillValidatorCommands.POLL).ToList();

                    // Если четвертый бит не Idling (незанятый), то идем дальше
                    if (ByteResult[3] != 0x14)
                    {
                        // ACCEPTING
                        //Если получили ответ 15H (Accepting)
                        if (ByteResult[3] == 0x15)
                        {
                            // Подтверждаем
                            this.SendCommand(BillValidatorCommands.ACK);
                        }

                        // ESCROW POSITION  
                        // Если четвертый бит 1Сh (Rejecting), то купюроприемник не распознал купюру
                        else if (ByteResult[3] == 0x1C)
                        {
                            // Принялии какую-то купюру
                            this.SendCommand(BillValidatorCommands.ACK);

                            OnBillReceived(new BillReceivedEventArgs(BillRecievedStatus.Rejected, 0, this._ErrorList.Errors[ByteResult[4]]));
                        }

                        // ESCROW POSITION
                        // купюра распознана
                        else if (ByteResult[3] == 0x80)
                        {
                            // Подтветждаем
                            this.SendCommand(BillValidatorCommands.ACK);

                            // Событие, что купюра в процессе отправки в стек
                            if (CashCodeTable.ContainsKey(ByteResult[4]))
                            {
                                OnBillStacking(new BillStackedEventArgs(CashCodeTable[ByteResult[4]]));
                            }
                            else
                            {
                                OnBillStacking(new BillStackedEventArgs(0) { Cancel = true});
                                OnBillReceived(new BillReceivedEventArgs(BillRecievedStatus.Rejected, 0, String.Format(CashCode.Net.Properties.Resource._Listener_Elapsed_Code0NotFoundInBillTable, ByteResult[4])));
                            }

                            // Если программа отвечает возвратом, то на возврат
                            if (this._ReturnBill)
                            {
                                // RETURN
                                // Если программа отказывает принимать купюру, отправим RETURN
                                ByteResult = this.SendCommand(BillValidatorCommands.RETURN).ToList();
                                this._ReturnBill = false;
                            }
                            else
                            {
                                // STACK
                                // Если равпознали, отправим купюру в стек (STACK)
                                ByteResult = this.SendCommand(BillValidatorCommands.STACK).ToList();
                            }
                        }

                        // STACKING
                        // Если четвертый бит 17h, следовательно идет процесс отправки купюры в стек (STACKING)
                        else if (ByteResult[3] == 0x17)
                        {
                            this.SendCommand(BillValidatorCommands.ACK);
                        }

                        // Bill stacked
                        // Если четвертый бит 81h, следовательно, купюра попала в стек
                        else if (ByteResult[3] == 0x81)
                        {
                            // Подтветждаем
                            this.SendCommand(BillValidatorCommands.ACK);
                            OnBillReceived(new BillReceivedEventArgs(BillRecievedStatus.Accepted, CashCodeTable[ByteResult[4]], ""));
                        }

                        // RETURNING
                        // Если четвертый бит 18h, следовательно идет процесс возврата
                        else if (ByteResult[3] == 0x18)
                        {
                            this.SendCommand(BillValidatorCommands.ACK);
                        }

                        // BILL RETURNING
                        // Если четвертый бит 82h, следовательно купюра возвращена
                        else if (ByteResult[3] == 0x82)
                        {
                            this.SendCommand(BillValidatorCommands.ACK);
                        }

                        // Drop Cassette out of position
                        // Снят купюроотстойник
                        else if (ByteResult[3] == 0x42)
                        {
                            if (_cassettestatus != BillCassetteStatus.Removed)
                            {
                                // fire event
                                _cassettestatus = BillCassetteStatus.Removed;
                                OnBillCassetteStatus(new BillCassetteEventArgs(_cassettestatus));

                            }
                        }

                        // Initialize
                        // Кассета вставлена обратно на место
                        else if (ByteResult[3] == 0x13)
                        {
                            if (_cassettestatus == BillCassetteStatus.Removed)
                            {
                                // fire event
                                _cassettestatus = BillCassetteStatus.Inplace;
                                OnBillCassetteStatus(new BillCassetteEventArgs(_cassettestatus));
                            }
                        }
                    }
                }
            }
            catch
            {
                OnBillException(new BillExceptionEventArgs("Связь с купюроприемником потеряна"));
            }
            finally
            {
                // Если таймер выключен, то запускаем
                if (!this._Listener.Enabled && this._IsListening)
                    this._Listener.Start();
            }

        }

        #endregion
    }

    /// <summary>
    /// Класс аргументов события получения купюры в купюроприемнике
    /// </summary>
    public class BillReceivedEventArgs : EventArgs
    {

        public BillRecievedStatus Status { get; private set; }
        public int Value { get; private set; }
        public string RejectedReason { get; private set; }

        public BillReceivedEventArgs(BillRecievedStatus status, int value, string rejectedReason)
        {
            this.Status = status;
            this.Value = value;
            this.RejectedReason = rejectedReason;
        }
    }

    public class BillCassetteEventArgs : EventArgs
    {

        public BillCassetteStatus Status { get; private set; }

        public BillCassetteEventArgs(BillCassetteStatus status)
        {
            this.Status = status;
        }
    }

    public class BillStackedEventArgs : CancelEventArgs
    {
        public int Value { get; private set; }

        public BillStackedEventArgs(int value)
        {
            this.Value = value;
            this.Cancel = false;
        }
    }

    public class BillExceptionEventArgs : EventArgs
    {
        public string Message { get; set; }

        public BillExceptionEventArgs(string message)
        {
            Message = message;
        }
    }
}

