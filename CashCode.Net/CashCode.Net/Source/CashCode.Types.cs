using System;
using System.ComponentModel;

using SharpLib;

namespace CashCode.Net
{

    #region Перечисление BillValidatorCommands

    public enum BillValidatorCommands
    {
        Ack = 0x00,

        Nak = 0xFF,

        Poll = 0x33,

        Reset = 0x30,

        GetStatus = 0x31,

        SetSecurity = 0x32,

        Identification = 0x37,

        EnableBillTypes = 0x34,

        Stack = 0x35,

        Return = 0x36,

        Hold = 0x38
    }

    #endregion Перечисление BillValidatorCommands

    #region Перечисление BillRecievedStatus

    public enum BillRecievedStatus
    {
        Accepted,

        Rejected
    }

    #endregion Перечисление BillRecievedStatus

    #region Перечисление BillCassetteStatus

    public enum BillCassetteStatus
    {
        Inplace,

        Removed
    }

    #endregion Перечисление BillCassetteStatus

    #region Перечисление CashCodeError

    /// <summary>
    /// Коды ошибок валидатора
    /// </summary>
    public enum CashCodeError
    {
        [Description("Выполнено")]
        Ok = 0,

        [Description("Неизвестная ошибка")]
        Unknow = 100000,

        [Description("Ошибка открытия Com-порта")]
        SerialOpen = 100010,

        [Description("Порт не открыт")]
        SerialNotOpen = 100020,

        [Description("Ошибка передачи команды включения купюроприемника")]
        SendCommandPowerUp = 100030,

        [Description("Ошибка передачи команды включения купюроприемника. От купюроприемника не получена команда POWER UP")]
        NotAckPowerUp = 100040,

        [Description("Ошибка передачи команды включения купюроприемника. От купюроприемника не получена команда ACK")]
        NotAck = 100050,

        [Description("Ошибка передачи команды включения купюроприемника. От купюроприемника не получена команда INITIALIZE")]
        NotAckInit = 100060,

        [Description("Ошибка проверки статуса купюроприемника. Cтекер снят")]
        CasseteNotPresent = 100070,

        [Description("Ошибка проверки статуса купюроприемника. Стекер переполнен")]
        CasseteFill = 100080,

        [Description("Ошибка проверки статуса купюроприемника. В валидаторе застряла купюра")]
        ValidatorError = 100090,

        [Description("Ошибка проверки статуса купюроприемника. В стекере застряла купюра")]
        CassetteError = 100100,

        [Description("Ошибка проверки статуса купюроприемника. Фальшивая купюра")]
        WrongBanknot = 100110,

        [Description("Ошибка проверки статуса купюроприемника. Предыдущая купюра еще не попала в стек и находится в механизме распознавания")]
        PrevBanknowProcess = 100120,

        [Description("Ошибка работы купюроприемника. Сбой при работе механизма стекера")]
        CassetteErrorEngine = 100130,

        [Description("Ошибка работы купюроприемника. Сбой в скорости передачи купюры в стекер")]
        CassetteSpeed = 100140,

        [Description("Ошибка работы купюроприемника. Сбой передачи купюры в стекер")]
        CassetteSend = 100150,

        [Description("Ошибка работы купюроприемника. Сбой механизма выравнивания купюр")]
        CassetteAling = 100160,

        [Description("Ошибка работы купюроприемника. Сбой в работе стекера")]
        CassetteWork = 100170,

        [Description("Ошибка работы купюроприемника. Сбой в работе оптических сенсоров")]
        OpticSensor = 100180,

        [Description("Ошибка работы купюроприемника. Сбой работы канала индуктивности")]
        Indance = 100190,

        [Description("Ошибка работы купюроприемника. Сбой в работе канала проверки заполняемости стекера")]
        CheckCassetteFill = 100200,

        [Description("Ошибка контрольной суммы")]
        Crc = 200000,

        [Description("Неверная длина пакета")]
        Length = 200001,

        [Description("Неверный размер данных")]
        DataSize = 200002,

        [Description("Таймаут ответа")]
        Timeout = 200003,

        [Description("Включение питания после команд")]
        PowerUp = 200004,

        [Description("Ошибка приема")]
        StatusError = 200005,

        [Description("Принята купюра")]
        BillReceived = 200006,

        [Description("Принята сумма")]
        SummReceived = 200007,

        [Description("Истекло время ожидания купюры")]
        BillTimeout = 200008,

        [Description("Окончен процесс приема суммы")]
        SummReceivedEnd = 200009,

        [Description("Общая ошибка")]
        GenericError = 200010,

        [Description("Rejecting due to Insertion")]
        RejectInsert = 0x60,

        [Description("Rejecting due to Magnetic")]
        RejectMagnetic = 0x61,

        [Description("Rejecting due to Remained bill in head")]
        RejectRemained = 0x62,

        [Description("Rejecting due to Multiplying")]
        RejectMulti = 0x63,

        [Description("Rejecting due to Conveying")]
        RejectConv = 0x64,

        [Description("Rejecting due to Identification")]
        RejectIdent = 0x65,

        [Description("Rejecting due to Verification")]
        RejectVerify = 0x66,

        [Description("Rejecting due to Optic")]
        RejectOptic = 0x67,

        [Description("Rejecting due to Inhibit")]
        RejectInhibit = 0x68,

        [Description("Rejecting due to Capacity")]
        RejectCapacity = 0x69,

        [Description("Rejecting due to Operation")]
        RejectOperation = 0x6A,

        [Description("Rejecting due to Length")]
        RejectLength = 0x6C
    }

    #endregion Перечисление CashCodeError

    #region Класс CashCodeNominal

    public class CashCodeNominal
    {
        #region Поля

        public Boolean B10;

        public Boolean B100;

        public Boolean B1000;

        public Boolean B50;

        public Boolean B500;

        public Boolean B5000;

        #endregion

        #region Свойства

        public Byte ByteValue
        {
            get { return ToByte(); }
            set { FromByte(value); }
        }

        public String Text
        {
            get { return ToText(); }
        }

        #endregion

        #region Методы

        public Byte ToByte()
        {
            // 0000 0100 = 2 бит - 10 рублей
            // 0000 1000 = 3 бит - 50 рублей
            // 0001 0000 = 4 бит - 100 рублей
            // 0010 0000 = 5 бит - 500 рублей
            // 0100 0000 = 6 бит - 1000 рублей
            // 1000 0000 = 7 бит - 5000 рублей

            Byte value = 0x00;

            if (B10)
            {
                value |= (1 << 2);
            }
            if (B50)
            {
                value |= (1 << 3);
            }
            if (B100)
            {
                value |= (1 << 4);
            }
            if (B500)
            {
                value |= (1 << 5);
            }
            if (B1000)
            {
                value |= (1 << 6);
            }
            if (B5000)
            {
                value |= (1 << 7);
            }

            return value;
        }

        public void FromByte(Byte value)
        {
            // 0000 0100 = 2 бит - 10 рублей
            // 0000 1000 = 3 бит - 50 рублей
            // 0001 0000 = 4 бит - 100 рублей
            // 0010 0000 = 5 бит - 500 рублей
            // 0100 0000 = 6 бит - 1000 рублей
            // 1000 0000 = 7 бит - 5000 рублей
            B10 = Conv.GetBit(value, 2);
            B50 = Conv.GetBit(value, 3);
            B100 = Conv.GetBit(value, 4);
            B500 = Conv.GetBit(value, 5);
            B1000 = Conv.GetBit(value, 6);
            B5000 = Conv.GetBit(value, 7);
        }

        private String ToText()
        {
            if (B10)
            {
                return "10";
            }
            if (B50)
            {
                return "50";
            }
            if (B100)
            {
                return "100";
            }
            if (B500)
            {
                return "500";
            }
            if (B1000)
            {
                return "1000";
            }
            if (B5000)
            {
                return "5000";
            }

            return "0";
        }

        public void ClearAll()
        {
            ByteValue = 0x00;
        }

        public void SetAll()
        {
            ByteValue = 0xFF;
        }

        #endregion
    }

    #endregion Класс CashCodeNominal

    #region Класс CashCodeStatus

    public class CashCodeStatus
    {
        #region Константы

        public const int ACCEPTING = 0x15;

        public const int BILL_RETURNING = 0x82;

        public const int BILL_STACKED = 0x81;

        public const int CASSETTE_FULL = 0x41;

        public const int CASSETTE_JUMMED = 0x44;

        public const int CASSETTE_OUT = 0x42;

        public const int CHEATED = 0x45;

        public const int DEVICE_BUSY = 0x1B;

        public const int ESCROW = 0x80;

        public const int FAILURE = 0x47;

        public const int HOLDING = 0x1A;

        public const int IDLING = 0x14;

        public const int ILLEGAL_COMMAND = 0x30;

        public const int INIT = 0x13;

        public const int PAUSE = 0x46;

        public const int POWER_UP = 0x10;

        public const int POWER_UP_STACKER = 0x12;

        public const int POWER_UP_VALIDATOR = 0x11;

        public const int REJECT = 0x1C;

        public const int RETURNING = 0x18;

        public const int STACKING = 0x17;

        public const int UNIT_DISABLED = 0x19;

        public const int VALIDATOR_JUMMED = 0x43;

        #endregion

        #region Поля

        /// <summary>
        /// 0-й байт статуса
        /// </summary>
        public Byte Z1;

        /// <summary>
        /// 1-й байт статуса
        /// </summary>
        public Byte Z2;

        #endregion

        #region Свойства

        public int Word
        {
            get
            {
                int word = (Z1 << 8) + Z2;

                return word;
            }
        }

        public String WordText
        {
            get
            {
                String result = ((UInt16)Word).ToStringEx(16);

                return result;
            }
        }

        public String Text
        {
            get { return GetText(); }
        }

        public Boolean IsError
        {
            get { return CheckError(); }
        }

        #endregion

        #region Методы

        private String GetText()
        {
            String result = "Неопределено";

            switch (Z1)
            {
                case POWER_UP:
                case POWER_UP_STACKER:
                case POWER_UP_VALIDATOR:
                    result = "Включение питания после после команд";
                    break;

                case INIT:
                    result = "Инициализация";
                    break;
                case IDLING:
                    result = "Ожидание приема купюры";
                    break;
                case ACCEPTING:
                    result = "Анализ купюры";
                    break;
                case STACKING:
                    result = "Купюра готова к добавлению";
                    break;
                case RETURNING:
                    result = "Возврат купюры";
                    break;
                case UNIT_DISABLED:
                    result = "Выключен";
                    break;
                case HOLDING:
                    result = "Купюра удерживаеся";
                    break;
                case DEVICE_BUSY:
                    result = "Устройство занято";
                    break;
                case REJECT:
                    result = "Отказ от приема";
                    break;
                case CASSETTE_FULL:
                    result = "Полная кассета";
                    break;
                case CASSETTE_OUT:
                    result = "Кассета отстутствует";
                    break;
                case VALIDATOR_JUMMED:
                    result = "Замяло купюру";
                    break;
                case CASSETTE_JUMMED:
                    result = "Замяло кассету";
                    break;
                case CHEATED:
                    result = "Подмена купюры";
                    break;
                case PAUSE:
                    result = "Пауза";
                    break;
                case FAILURE:
                    result = "Сбор оборудования";
                    break;
                case ESCROW:
                    result = "Депонент";
                    break;
                case BILL_STACKED:
                    result = "Добавление купюры";
                    break;
                case BILL_RETURNING:
                    result = "Возврат купюры";
                    break;
            }

            return result;
        }

        private Boolean CheckError()
        {
            switch (Z1)
            {
                case ILLEGAL_COMMAND:
                case CASSETTE_FULL:
                case CASSETTE_OUT:
                case VALIDATOR_JUMMED:
                case CASSETTE_JUMMED:
                case CHEATED:
                case PAUSE:
                case FAILURE:
                    return true;
            }

            return false;
        }

        #endregion
    }

    #endregion Класс CashCodeStatus

    #region Класс CashCodeHandler

    public delegate void CashCodeHandler(CashCodeError error, Object data);

    #endregion Класс CashCodeHandler
}