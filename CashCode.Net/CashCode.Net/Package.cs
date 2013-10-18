using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CashCode.Net
{
    public enum ResponseType { ACK, NAK };

    public sealed class Package
    {
        #region Поля
        
        private const int POLYNOMIAL =  0x08408;     // Необходима для расчета CRC
        private const byte _Sync =      0x02;        // Бит синхронизации (фиксированный)
        private const byte _Adr =       0x03;        // Переферийный адрес оборования. Для купюропиемника из документации равен 0x03

        private byte _Cmd;
        private byte[] _Data;

        #endregion

        #region Конструктор класса

        public Package()
        {}

        public Package(byte cmd, byte[] data)
        {
            this._Cmd = cmd;
            this.Data = data;
        }

        #endregion

        #region Свойства

        public byte Cmd
        {
            get { return _Cmd; }
            set { _Cmd = value; }
        }

        public byte[] Data
        {
            get { return _Data; }
            set 
            {
                if (value.Length + 5 > 250)
                {

                }
                else
                {
                    _Data = new byte[value.Length];
                    _Data = value;
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
            Buff.Add(_Sync);

            // Байт 2: адрес устройства
            Buff.Add(_Adr);

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
            Buff.Add(this._Cmd);

            // Байт 4: Команда
            if (this._Data != null)
            {
                for (int i = 0; i < _Data.Length; i++)
                { Buff.Add(this._Data[i]); }
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
            return (this._Data == null ? 0 : this._Data.Length) + 6;
        }

        // Расчет контрольной суммы
        private static int GetCRC16(byte[] BufData, int SizeData)
        {
            int TmpCRC, CRC;
            CRC = 0;

            for (int i = 0; i < SizeData; i++)
            {
                TmpCRC = CRC ^ BufData[i];

                for (byte j = 0; j < 8; j++)
                {
                    if ((TmpCRC & 0x0001) != 0) { TmpCRC >>= 1; TmpCRC ^= POLYNOMIAL; }
                    else { TmpCRC >>= 1; }
                }

                CRC = TmpCRC;
            }

            return CRC;
        }

        public static bool CheckCRC(byte[] Buff)
        {
            bool result = true;

            byte[] OldCRC = new byte[] { Buff[Buff.Length - 2], Buff[Buff.Length - 1]};

            // Два последних байта в длине убираем, так как это исходная CRC
            byte[] NewCRC = BitConverter.GetBytes(GetCRC16(Buff, Buff.Length - 2));

            for (int i = 0; i < 2; i++)
            {
                if (OldCRC[i] != NewCRC[i])
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public static byte[] CreateResponse(ResponseType type)
        {
            // Буффер пакета (без 2-х байт CRC). Первые четыре байта это SYNC, ADR, LNG, CMD
            List<byte> Buff = new List<byte>();

            // Байт 1: Флаг синхронизации
            Buff.Add(_Sync);

            // Байт 2: адрес устройства
            Buff.Add(_Adr);

            // Байт 3: длина пакета, всегда 6
            Buff.Add(0x06);
           
            // Байт 4: Данные
            if (type == ResponseType.ACK) { Buff.Add(0x00); }
            else if (type == ResponseType.NAK) { Buff.Add(0xFF); }

            // Последний байт - CRC
            byte[] CRC = BitConverter.GetBytes(GetCRC16(Buff.ToArray(), Buff.Count));

            byte[] package = new byte[Buff.Count + CRC.Length];
            Buff.ToArray().CopyTo(package, 0);
            CRC.CopyTo(package, Buff.Count);

            return package;
        }

        #endregion
    }
}
