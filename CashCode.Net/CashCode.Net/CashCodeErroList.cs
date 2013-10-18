using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CashCode.Net
{
    public sealed class CashCodeErroList
    {
        public Dictionary<int, string> Errors { get; private set; }

        public CashCodeErroList()
        {
            Errors = new Dictionary<int, string>();

            Errors.Add(100000, "Неизвестная ошибка");

            Errors.Add(100010, "Ошибка открытия Com-порта");
            Errors.Add(100020, "Com-порт не открыт");
            Errors.Add(100030, "Ошибка отпраки команды включения купюроприемника.");
            Errors.Add(100040, "Ошибка отпраки команды включения купюроприемника. От купюроприемника не получена команда POWER UP.");
            Errors.Add(100050, "Ошибка отпраки команды включения купюроприемника. От купюроприемника не получена команда ACK.");
            Errors.Add(100060, "Ошибка отпраки команды включения купюроприемника. От купюроприемника не получена команда INITIALIZE.");
            Errors.Add(100070, "Ошибка проверки статуса купюроприемника. Cтекер снят.");
            Errors.Add(100080, "Ошибка проверки статуса купюроприемника. Стекер переполнен.");
            Errors.Add(100090, "Ошибка проверки статуса купюроприемника. В валидаторе застряла купюра.");
            Errors.Add(100100, "Ошибка проверки статуса купюроприемника. В стекере застряла купюра.");
            Errors.Add(100110, "Ошибка проверки статуса купюроприемника. Фальшивая купюра.");
            Errors.Add(100120, "Ошибка проверки статуса купюроприемника. Предыдущая купюра еще не попала в стек и находится в механизме распознавания.");

            Errors.Add(100130, "Ошибка работы купюроприемника. Сбой при работе механизма стекера.");
            Errors.Add(100140, "Ошибка работы купюроприемника. Сбой в скорости передачи купюры в стекер.");
            Errors.Add(100150, "Ошибка работы купюроприемника. Сбой передачи купюры в стекер.");
            Errors.Add(100160, "Ошибка работы купюроприемника. Сбой механизма выравнивания купюр.");
            Errors.Add(100170, "Ошибка работы купюроприемника. Сбой в работе стекера.");
            Errors.Add(100180, "Ошибка работы купюроприемника. Сбой в работе оптических сенсоров.");
            Errors.Add(100190, "Ошибка работы купюроприемника. Сбой работы канала индуктивности.");
            Errors.Add(100200, "Ошибка работы купюроприемника. Сбой в работе канала проверки заполняемости стекера.");

            // Ошибки распознования купюры
            Errors.Add(0x60, "Rejecting due to Insertion");
            Errors.Add(0x61, "Rejecting due to Magnetic");
            Errors.Add(0x62, "Rejecting due to Remained bill in head");
            Errors.Add(0x63, "Rejecting due to Multiplying");
            Errors.Add(0x64, "Rejecting due to Conveying");
            Errors.Add(0x65, "Rejecting due to Identification1");
            Errors.Add(0x66, "Rejecting due to Verification");
            Errors.Add(0x67, "Rejecting due to Optic");
            Errors.Add(0x68, "Rejecting due to Inhibit");
            Errors.Add(0x69, "Rejecting due to Capacity");
            Errors.Add(0x6A, "Rejecting due to Operation");
            Errors.Add(0x6C, "Rejecting due to Length");
        }
    }
}
