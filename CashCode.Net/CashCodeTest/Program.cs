using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CashCode.Net;

namespace CashCodeTest
{
    class Program
    {
        static int Sum = 0;

        static void Main(string[] args)
        {
            try
            {
                using (CashCodeBillValidator c = new CashCodeBillValidator(Properties.Settings.Default.Port, 9600))
                {
                    c.BillReceived += new BillReceivedHandler(c_BillReceived);
                    c.BillStacking += new BillStackingHandler(c_BillStacking);
                    c.BillCassetteStatusEvent += new BillCassetteHandler(c_BillCassetteStatusEvent);
                    c.ConnectBillValidator();

                    if (c.IsConnected)
                    {
                        c.PowerUpBillValidator();
                        c.StartListening();


                        c.EnableBillValidator();
                        Console.ReadKey();
                        c.DisableBillValidator();
                        Console.ReadKey();
                        c.EnableBillValidator();
                        Console.ReadKey();
                        c.StopListening();
                    }

                    c.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void c_BillCassetteStatusEvent(object Sender, BillCassetteEventArgs e)
        {
            Console.WriteLine(e.Status.ToString());
        }

        static void c_BillStacking(object Sender, System.ComponentModel.CancelEventArgs e)
        {
            Console.WriteLine("Купюра в стеке");
            if (Sum > 100)
            {
                //e.Cancel = true;
                Console.WriteLine("Превышен лимит единовременной оплаты");
            }
        }

        static void c_BillReceived(object Sender, BillReceivedEventArgs e)
        {
            if (e.Status == BillRecievedStatus.Rejected)
            {
                Console.WriteLine(e.RejectedReason);
            }
            else if (e.Status == BillRecievedStatus.Accepted)
            {
                Sum += e.Value;
                Console.WriteLine("Bill accepted! " + e.Value + " руб. Общая сумму: " + Sum.ToString());
            }
        }


    }
}
