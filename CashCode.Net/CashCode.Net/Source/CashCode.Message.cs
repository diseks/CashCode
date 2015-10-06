using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpLib;

namespace CashCode.Net
{
    public enum ResponseType 
    { 
        Ack, 
        Nak,
        Command
    }

    public sealed class CashCodeMessage
    {
        #region Константы
        private const int POLYNOMIAL = 0x08408;     // Необходима для расчета CRC
        private const byte SYNC      = 0x02;        // Бит синхронизации (фиксированный)
        private const byte ADDR      = 0x03;        // Переферийный адрес оборования. Для купюропиемника из документации равен 0x03
        public  const byte ACK       = 0x00;        // Код ACK-команды
        public  const byte NAK       = 0xFF;        // Код NAK-команды
        #endregion Константы

        #region Поля

        private byte[] _data;
        #endregion

        #region Конструктор класса

        public CashCodeMessage()
        {

        }
        public CashCodeMessage(byte cmd, byte[] data)
        {
            this.Cmd = cmd;
            this.Data = data;
        }

        #endregion

        #region Свойства

        public byte Cmd { get; set; }

        public byte[] Data
        {
            get { return _data; }
            set 
            {
                if (value.Length + 5 > 250)
                {

                }
                else
                {
                    _data = new byte[value.Length];
                    _data = value;
                }
            }
        }

        #endregion

        #region Методы

        // Возвращает массив байтов пакета
        public byte[] GetBytes()
        {
            // Буффер пакета (без 2-х байт CRC). Первые четыре байта это SYNC, ADR, LNG, CMD
            List<byte> Buff = new List<byte>();
            
            // Байт 1: Флаг синхронизации
            Buff.Add(SYNC);

            // Байт 2: адрес устройства
            Buff.Add(ADDR);

            // Байт 3: длина пакета
            // рассчитаем длину пакета
            int result = this.GetLength();

            // Если длина пакета вместе с байтами SYNC, ADR, LNG, CRC, CMD  больше 250
            if (result > 250)
            {
                // то делаем байт длины равный 0, а действительная длина сообщения будет в DATA
                Buff.Add(0);
            }
            else
            {
                Buff.Add(Convert.ToByte(result));
            }

            // Байт 4: Команда
            Buff.Add(this.Cmd);

            // Байт 4: Команда
            if (this._data != null)
            {
                for (int i = 0; i < _data.Length; i++)
                { Buff.Add(this._data[i]); }
            }

            // Последний байт - CRC
            byte[] CRC = BitConverter.GetBytes(GetCRC16(Buff.ToArray(), Buff.Count));

            byte[] package = new byte[Buff.Count + CRC.Length];
            Buff.ToArray().CopyTo(package, 0);
            CRC.CopyTo(package, Buff.Count);

            return package;
        }

        // Возвращает строку шестнадцатиричного представления байтов пакета
        public string GetBytesHex()
        {
            byte[] package = GetBytes();

            StringBuilder hexString = new StringBuilder(package.Length);
            for (int i = 0; i < package.Length; i++)
            {
                hexString.Append(package[i].ToString("X2"));
            }

            return "0x" + hexString.ToString();
        }

        // Длина пакета
        public int GetLength()
        {
            return (this._data == null ? 0 : this._data.Length) + 6;
        }

        /// <summary>
        /// Расчет контрольной суммы
        /// </summary>
        private static int GetCRC16(byte[] data, int size)
        {
            int crc = 0;

            for (int i = 0; i < size; i++)
            {
                int tempCrc = crc ^ data[i];

                for (byte j = 0; j < 8; j++)
                {
                    if ((tempCrc & 0x0001) != 0) { tempCrc >>= 1; tempCrc ^= POLYNOMIAL; }
                    else { tempCrc >>= 1; }
                }

                crc = tempCrc;
            }

            return crc;
        }

        public static bool CheckCRC(byte[] data)
        {
            int crc_0 = ((int)data[data.Length - 2] << 0) + ((int)data[data.Length - 1] << 8);
            int crc_1 = GetCRC16(data, data.Length - 2);

            return (crc_0 == crc_1);
        }

        public static byte[] CreateMessage(BillValidatorCommands cmd, Byte[] data = null)
        {
            int    length = 6 + (data == null ? 0 : data.Length);
            Byte[] buffer = new Byte[length];

            buffer[0] = SYNC;          // Байт 1: Флаг синхронизации
            buffer[1] = ADDR;          // Байт 2: Адрес устройства
            buffer[2] = (Byte)length;  // Байт 3: Длина пакета
            buffer[3] = (Byte)cmd;     // Байт 4: Код команды

            // Добавление данных
            if (data != null)
                Mem.Copy(buffer, 4, data);

            // Расчет контрольной суммы
            int crc = GetCRC16(buffer, length - 2);
            buffer[length - 2] = (Byte)(crc >> 0);
            buffer[length - 1] = (Byte)(crc >> 8);

            return buffer;
        }
        public static CashCodeError DecodeMessage(Byte[] buffer, out CashCodeMessage message)
        {
            message = null;
            int size = buffer.Length;

            // Минимальный размер ответа 6 байт
            if (size < 5) return CashCodeError.Length;

            // Проверка контрольной суммы
            bool isCrcValid = CashCodeMessage.CheckCRC(buffer);
            if (isCrcValid == false) return CashCodeError.Crc;

            // Разбор пакета по полям
            // ===========================
            // buffer[0] = SYNC;          // Байт 1: Флаг синхронизации
            // buffer[1] = ADDR;          // Байт 2: Адрес устройства
            // buffer[2] = (Byte)length;  // Байт 3: Длина пакета
            // buffer[x] = data;          // Байт x: Данные
            // buffer[x] = crcLow;        // Байт x: Контрольная сумма
            // buffer[x] = crcHi;         // Байт x: 2 байта
                    
            // 5 это 1 байт синхронизации, 1 байт адресс девайса, 1 байт - длина данных, 2 байт - CRC
            int length = buffer[2];
                    
            // Проверка размера принятых данных и размера данных в пакете
            if (length != size) return CashCodeError.DataSize;

            // Выделение блока данных
            Byte[] data = Mem.Clone(buffer, 3, length - (3 + 2));

            message      = new CashCodeMessage();
            message.Data = data;

            return CashCodeError.Ok;
        }

        #endregion
    }
}
