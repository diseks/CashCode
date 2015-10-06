using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Threading;

using SharpLib;

namespace CashCode.Net
{
    #region Класс CashCodeBillValidator
    public sealed class CashCodeBillValidator
    {
        #region Константы
        /// <summary>
        /// Тайм-аут ожидания ответа от считывателя
        /// </summary>
        private const int POLL_TIMEOUT = 200;   
        /// <summary>
        /// Тайм-аут ожидания снятия блокировки
        /// </summary>
        private const int EVENT_WAIT_HANDLER_TIMEOUT = 2000; 
        #endregion Константы

        #region Поля
        private readonly byte[] ENABLE_BILL_TYPES_WITH_ESCROW    = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        private EventWaitHandle _syncSerial;     // Переменная синхронизации отправки и считывания данных с ком порта
        private List<byte>      _receivedBytes;  // Полученные байты
        private CashCodeError   _lastError;
        private object          _locker;
        private SerialPort      _serial;
        private bool            _isStartGetMoney;
        #endregion Поля

        #region События
        public event CashCodeHandler OnEvent;
        #endregion События

        #region Конструктор
        public CashCodeBillValidator()
        {
            this._locker = new object();

            // Из спецификации:
            //      Baud Rate:	9600 bps/19200 bps (no negotiation, hardware selectable)
            //      Start bit:	1
            //      Data bit:	8 (bit 0 = LSB, bit 0 sent first)
            //      Parity:		Parity none 
            //      Stop bit:	1
            this._serial = new SerialPort();
            this._serial.DataBits = 8;
            this._serial.Parity = Parity.None;
            this._serial.StopBits = StopBits.One;
            this._serial.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);

            this._receivedBytes = new List<byte>();
            this._syncSerial = new EventWaitHandle(false, EventResetMode.AutoReset);

            // 
            _isStartGetMoney = false;
        }
        #endregion Конструктор

        #region Свойства
        public bool IsConnected
        {
            get { return _serial.IsOpen; }
        }
        public string PortName
        {
            set { _serial.PortName = value; }
            get { return _serial.PortName; }
        }
        public int Baudrate
        {
            set { _serial.BaudRate = value; }
            get { return _serial.BaudRate; }
        }
        #endregion

        #region Обработчики событий
        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Пауза для ожидания примема всех данных (100 мс)
            Thread.Sleep(100);

            int size = _serial.BytesToRead;
            if (size == 0) return;

            Byte[] data = new Byte[size];
            _serial.Read(data, 0, size);

            this._receivedBytes = data.ToList();
            this._syncSerial.Set();
        }
        private void RaiseEvent(CashCodeError error, Object data = null)
        {
            if (OnEvent != null)
            {
                OnEvent(error, data);
            }
        }
        #endregion

        #region Вспомогательные методы
        private void WriteToSerial(Byte[] data)
        {
            _serial.Write(data, 0, data.Length);
        }
        public void SendAck()
        {
            Byte[] data = CashCodeMessage.CreateMessage(BillValidatorCommands.Ack);

            WriteToSerial(data);
        }
        public void SendNak()
        {
            Byte[] data = CashCodeMessage.CreateMessage(BillValidatorCommands.Nak);

            WriteToSerial(data);
        }
        private CashCodeError SendCommand(BillValidatorCommands cmd, out CashCodeMessage message, Byte[] data = null)
        {
            message = null;
            Byte[] packet = CashCodeMessage.CreateMessage(cmd, data);

            // Передача пакета
            WriteToSerial(packet);

            // Ожидание ответа от устройства
            this._syncSerial.WaitOne(EVENT_WAIT_HANDLER_TIMEOUT);
            this._syncSerial.Reset();

            byte[] answer = this._receivedBytes.ToArray();

            // Данные не получены
            if (answer.Length == 0) return CashCodeError.Timeout;

            // Анализ ответа
            CashCodeError error = ProcessAnswer(answer, out message);

            return error;
        }
        private CashCodeError ProcessAnswer(Byte[] data, out CashCodeMessage message)
        {
            CashCodeError error = CashCodeMessage.DecodeMessage(data, out message);

            if (error == CashCodeError.Crc || error == CashCodeError.DataSize)
            {
                SendNak();
            }

            return error;
        }
        #endregion Вспомогательные методы

        #region Управление устройством
        public CashCodeError Open()
        {
            this._lastError = CashCodeError.Ok;

            try
            {
                this._serial.Open();
            }
            catch
            {
                this._lastError = CashCodeError.SerialOpen;
            }

            return this._lastError;
        }
        public CashCodeError Close()
        {
            this._serial.Close();

            return CashCodeError.Ok;
        }
        public CashCodeError Reset()
        {
            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.Reset, out message);

            return error;
        }
        public CashCodeError Identification(out String partNumber, out String serialNumber, out String assetNumber)
        {
            partNumber   = ""; 
            serialNumber = "";
            assetNumber  = "";

            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.Identification, out message);

            if (error == CashCodeError.Ok)
            {
                
                // Описание ответа
                //
                // Z1-Z15  Part Number   – 15 bytes, ASCII characters
	            // Z16-Z27 Serial Number – 12 bytes Factory assigned serial number, ASCII characters
	            // Z28-Z34 Asset Number  – 7 bytes, unique to every Bill Validator, binary data

                partNumber   = System.Text.Encoding.ASCII.GetString(message.Data, 0, 14);  partNumber   = partNumber.TrimEnd(' ');
                serialNumber = System.Text.Encoding.ASCII.GetString(message.Data, 15, 11); serialNumber = serialNumber.TrimEnd(' ');
                assetNumber  = Mem.Clone(message.Data, 27, 12).ToAsciiEx("-");
            }

            return error;
        }
        public CashCodeError GetStatus(out CashCodeNominal nominal, out CashCodeNominal security)
        {
            nominal  = new CashCodeNominal();
            security = new CashCodeNominal();
            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.GetStatus, out message);

            if (error == CashCodeError.Ok)
            {
                // Заполнение поля принимаемых купюр (рубли хранятся в 3-м байте)
                // Заполнение поля купюр с повышенным контролем при приеме (рубли хранятся в 6-м байте)
                nominal.ByteValue  = message.Data[2];
                security.ByteValue = message.Data[5];
            }

            return error;
        }
        public CashCodeError EnableBillTypes(CashCodeNominal nominal)
        {
            Byte[] data = new Byte[6]
            {
                0x00, 0x00, nominal.ByteValue, 0x00, 0x00, 0x00
            };

            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.EnableBillTypes, out message, data);

            if (error == CashCodeError.Ok)
            {
                SendAck();
                CashCodeStatus status;
                Poll(out status);
            }

            return error;
        }
        public CashCodeError SetSecurity(CashCodeNominal nominal)
        {
            Byte[] data = new Byte[3]
            {
                0x00, 0x00, nominal.ByteValue
            };

            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.SetSecurity, out message, data);

            if (error == CashCodeError.Ok)
            {
                SendAck();
            }

            return error;
        }
        public CashCodeError Poll(out CashCodeStatus status)
        {
            status = null;
            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.Poll, out message);

            if (error == CashCodeError.Ok)
            {
                status = new CashCodeStatus();
                status.Z1 = message.Data[0];
                status.Z2 = (message.Data.Length > 1) ? message.Data[1] : (Byte)0x00;
            }

            return error;
        }
        public CashCodeError Stack()
        {
            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.Stack, out message);

            return error;
        }
        public CashCodeError Return()
        {
            CashCodeMessage message;
            CashCodeError   error = SendCommand(BillValidatorCommands.Return, out message);

            return error;
        }
        #endregion Управление устройством

        #region Расширенное управление устройством
        public CashCodeError ResetSoftware()
        {
            CashCodeError   error;
            CashCodeStatus  status;
            
            // RESET
            error = Reset();
            if (error != CashCodeError.Ok) return error;
            // POLL (INITIALIZE)
            error = Poll(out status);
            if (error != CashCodeError.Ok) return error;
            //
            if (status.IsError == true) 
            {
                SendNak(); return CashCodeError.GenericError;
            }
            else
            {
                SendAck();
            }
            // POLL (UNIT DISABLED)
            error = Poll(out status);
            if (error != CashCodeError.Ok) return error;
            //
            if (status.IsError == true) 
            {
                SendNak(); return CashCodeError.GenericError;
            }
            else
            {
                SendAck();
            }

            return error;
        }
        public CashCodeError Enable()
        {
            CashCodeNominal nominal = new CashCodeNominal();
            nominal.SetAll();

            CashCodeError error = this.EnableBillTypes(nominal);

            return error;
        }
        public CashCodeError Disable()
        {
            CashCodeNominal nominal = new CashCodeNominal();
            nominal.ClearAll();

            CashCodeError error = this.EnableBillTypes(nominal);

            return error;
        }
        private void PollGetMoney(int waitSumm, int waitTime)
        {
            // Включение купюроприемника
            Enable();

            DateTime timeStart = DateTime.Now;
            Boolean  isRun     = true;
            int      summ      = 0;

            CashCodeStatus status;
            CashCodeError  error;

            while (isRun)
            {
                error = Poll(out status);

                if (error != CashCodeError.Ok)
                {
                    RaiseEvent(CashCodeError.GenericError, error);
                    isRun = false; break;
                }

                switch (status.Z1)
                {
                    // Ожидание приема купюры
                    case CashCodeStatus.IDLING: break;
                    // Анализ купюры
                    case CashCodeStatus.ACCEPTING: break;
                    // Купюра готова к добавлению
                    case CashCodeStatus.STACKING: break;
                    // Подмена купюры
                    case CashCodeStatus.CHEATED: RaiseEvent(CashCodeError.StatusError, status); break;
                    // Отказ приема
                    case CashCodeStatus.REJECT: RaiseEvent(CashCodeError.StatusError, status); break;

                    // Включение питания модуля
                    case CashCodeStatus.POWER_UP:  
                    case CashCodeStatus.POWER_UP_STACKER:  
                    case CashCodeStatus.POWER_UP_VALIDATOR:  
                        {
                            RaiseEvent(CashCodeError.PowerUp);
                            isRun = false;
                        }
                        break;

                    case CashCodeStatus.CASSETTE_FULL:
                    case CashCodeStatus.CASSETTE_OUT:
                    case CashCodeStatus.CASSETTE_JUMMED:
                    case CashCodeStatus.VALIDATOR_JUMMED:
                        {
                            RaiseEvent(CashCodeError.StatusError, status);

                            // Reset();
                            isRun = false;
                        }
                        break;

                    case CashCodeStatus.BILL_STACKED:
                        {
                            int nominal = 0;
                            switch (status.Z2)    
                            {
                                case 2: nominal = 10; break;
                                case 3: nominal = 50; break;
                                case 4: nominal = 100; break;
                                case 5: nominal = 500; break;
                                case 6: nominal = 1000; break;
                                case 7: nominal = 5000; break;
                            }

                            if (nominal == 0) break;

                            RaiseEvent(CashCodeError.BillReceived, nominal);

                            // Сброс времени ожидания следующей купюры
                            timeStart = DateTime.Now;
                            // Отправка подтверждения оборудованию
                            SendAck();
                            // Увеличение принятой суммы
                            summ += nominal;

                            // Проверка окончания приема денег
                            if (summ > waitSumm)
                            {
                                RaiseEvent(CashCodeError.SummReceived, summ);

                                isRun = false;
                            }
                        }
                        break;

                    // Неизвестное состояние
                    default:
                        {
                            RaiseEvent(CashCodeError.StatusError, status);
                            isRun = false;
                        }
                        break;
                } // end switch (анализ кода статуса)

                // Проверка 
                //   + истечения тайм-аута ожидания
                //   + внешнее прекращение процесса приема денег
                if (_isStartGetMoney == false || ((DateTime.Now - timeStart).TotalSeconds > waitTime))
                {
                    RaiseEvent(CashCodeError.BillTimeout);

                    isRun = false;
                }
            } // end while (прием денег в течении определенного премени)


            // Выключение купюроприемника
            Disable();
        }
        public void StartGetMoney(int waitSumm, int timeoutSec)
        {
            if (_isStartGetMoney == false)
            {
                _isStartGetMoney = true;

                Thread thread = new Thread(() =>
                {
                    PollGetMoney(waitSumm, timeoutSec);
                    _isStartGetMoney = false;
                    RaiseEvent(CashCodeError.SummReceivedEnd);
                });

                thread.Start();
            }
        }
        public void StopGetMoney()
        {
            if (_isStartGetMoney == true)
            {
                _isStartGetMoney = false;
            }
        }
        #endregion Расширенное управление устройством
    }
    #endregion Класс CashCodeBillValidator

}
