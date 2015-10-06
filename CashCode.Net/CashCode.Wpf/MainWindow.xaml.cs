using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using CashCode.Net;

using SharpLib;

namespace CashCode.Wpf
{

    #region Класс MainWindow

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Поля

        private Boolean _isOpened;

        #endregion

        #region Свойства

        public CashCodeBillValidator Validator { get; set; }

        public bool IsOpened
        {
            get { return _isOpened; }
            set { SetViewOpened(value); }
        }

        public bool IsClosed
        {
            get { return (_isOpened == false); }
            set { SetViewOpened(value == false); }
        }

        public CashCodeNominal Nominal
        {
            get
            {
                CashCodeNominal nominal = new CashCodeNominal();
                nominal.B10 = (PART_checkBoxB10.IsChecked == true);
                nominal.B50 = (PART_checkBoxB50.IsChecked == true);
                nominal.B100 = (PART_checkBoxB1000.IsChecked == true);
                nominal.B500 = (PART_checkBoxB500.IsChecked == true);
                nominal.B1000 = (PART_checkBoxB1000.IsChecked == true);
                nominal.B5000 = (PART_checkBoxB5000.IsChecked == true);

                return nominal;
            }
        }

        #endregion

        #region События

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Конструктор

        public MainWindow()
        {
            InitializeComponent();

            // Load port settings
            InitPortSettings();
            // Create validator object
            Validator = new CashCodeBillValidator();
            Validator.OnEvent += ValidatorOnEvent;

            // Init variables
            DataContext = this;
            IsOpened = false;
        }

        #endregion

        #region Методы

        #region Инициализация

        private void InitPortSettings()
        {
            for (int i = 1; i < 32; i++)
            {
                PART_comboBoxSerial.Items.Add("COM" + i);
            }
            PART_comboBoxBaudrate.Items.Add("9600");
            PART_comboBoxBaudrate.Items.Add("19200");

            PART_comboBoxSerial.SelectedIndex = 0;
            PART_comboBoxBaudrate.SelectedIndex = 0;
        }

        #endregion Инициализация

        #endregion

        #region Вспомогательные методы

        private void SetViewOpened(Boolean value)
        {
            _isOpened = value;
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("IsOpened"));
                PropertyChanged(this, new PropertyChangedEventArgs("IsClosed"));
            }
        }

        protected void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void AddText(String text, bool error = false)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            PART_memo.AddLine(text, error ? Brushes.Red : Brushes.Black);
        }

        private void AddTextError(CashCodeError error)
        {
            AddText(error.ToStringEx(), error != CashCodeError.Ok);
        }

        #endregion

        #region Управление портом

        private void ButtonSerialOpen(object sender, RoutedEventArgs e)
        {
            var name = PART_comboBoxSerial.SelectedItem as String;
            var baudrate = (string)PART_comboBoxBaudrate.SelectedItem;

            Validator.PortName = name;
            Validator.Baudrate = baudrate.ToIntEx();
            CashCodeError error = Validator.Open();
            if (error == CashCodeError.Ok)
            {
                IsOpened = true;
            }

            if (IsOpened)
            {
                AddText(String.Format("Port {0} opened", name));
            }
            else
            {
                AddText(String.Format("Error opening port {0}", name), true);
            }
        }

        private void ButtonSerialClose(object sender, RoutedEventArgs e)
        {
            Validator.Close();

            IsOpened = false;

            AddText("Port closed");
        }

        #endregion Управление портом

        #region Управление устройством

        private void ButtonReset(object sender, RoutedEventArgs e)
        {
            AddText("Execute 'Reset sowtware'");
            CashCodeError error = Validator.ResetSoftware();
            AddTextError(error);
        }

        private void ButtonIdentification(object sender, RoutedEventArgs e)
        {
            AddText("Send 'Identification'");

            String partNumber;
            String serialNumber;
            String assetNumber;

            CashCodeError error = Validator.Identification(out partNumber, out serialNumber, out assetNumber);

            if (error != CashCodeError.Ok)
            {
                AddTextError(error);
            }
            else
            {
                AddText(String.Format("Part number={0}, Serial={1}, Id={2}", partNumber, serialNumber, assetNumber));
            }
        }

        private void ButtonStatus(object sender, RoutedEventArgs e)
        {
            AddText("Send 'Get Status'");

            CashCodeStatus status;
            Validator.Poll(out status);

            CashCodeNominal nominal;
            CashCodeNominal security;

            CashCodeError error = Validator.GetStatus(out nominal, out security);

            if (error != CashCodeError.Ok)
            {
                AddTextError(error);
            }
            else
            {
                AddText("Receiving:");
                if (nominal.B10)
                {
                    AddText("10 RUB");
                }
                if (nominal.B50)
                {
                    AddText("50 RUB");
                }
                if (nominal.B100)
                {
                    AddText("100 RUB");
                }
                if (nominal.B500)
                {
                    AddText("500 RUB");
                }
                if (nominal.B1000)
                {
                    AddText("1000 RUB");
                }
                if (nominal.B5000)
                {
                    AddText("5000 RUB");
                }
                AddText("Повышеный контроль для купюр:");
                if (security.B10)
                {
                    AddText("10 RUB");
                }
                if (security.B50)
                {
                    AddText("50 RUB");
                }
                if (security.B100)
                {
                    AddText("100 RUB");
                }
                if (security.B500)
                {
                    AddText("500 RUB");
                }
                if (security.B1000)
                {
                    AddText("1000 RUB");
                }
                if (security.B5000)
                {
                    AddText("5000 RUB");
                }
            }
        }

        private void ButtonEnable(object sender, RoutedEventArgs e)
        {
            AddText("Enable validator");

            CashCodeError error = Validator.Enable();

            AddTextError(error);
        }

        private void ButtonDisable(object sender, RoutedEventArgs e)
        {
            AddText("Disable validator");

            CashCodeError error = Validator.Disable();

            AddTextError(error);
        }

        private void ButtonEnableBillTypes(object sender, RoutedEventArgs e)
        {
            AddText("Send 'EnableBillTypes'");

            CashCodeNominal nominal = new CashCodeNominal();
            nominal.B10 = (PART_checkBoxB10.IsChecked == true);
            nominal.B50 = (PART_checkBoxB50.IsChecked == true);
            nominal.B100 = (PART_checkBoxB1000.IsChecked == true);
            nominal.B500 = (PART_checkBoxB500.IsChecked == true);
            nominal.B1000 = (PART_checkBoxB1000.IsChecked == true);
            nominal.B5000 = (PART_checkBoxB5000.IsChecked == true);

            CashCodeError error = Validator.EnableBillTypes(nominal);

            AddTextError(error);
        }

        private void ButtonSetSecurity(object sender, RoutedEventArgs e)
        {
            AddText("Send 'SetSecurity'");

            CashCodeError error = Validator.SetSecurity(Nominal);

            AddTextError(error);
        }

        private void ButtonPoll(object sender, RoutedEventArgs e)
        {
            AddText("Send 'POLL'");

            CashCodeStatus status;
            CashCodeError error = Validator.Poll(out status);

            if (error == CashCodeError.Ok)
            {
                AddText("Status = " + status.Text);
            }
            else
            {
                AddTextError(error);
            }
        }

        private void ButtonStack(object sender, RoutedEventArgs e)
        {
            AddText("Send 'STACK'");

            CashCodeError error = Validator.Stack();

            AddTextError(error);
        }

        private void ButtonReturn(object sender, RoutedEventArgs e)
        {
            AddText("Send 'RETURN'");

            CashCodeError error = Validator.Return();

            AddTextError(error);
        }

        private void ButtonStartGetValue(object sender, RoutedEventArgs e)
        {
            int timeout = 120;
            int summ = PART_textEditDenomination.Text.ToIntEx();
            AddText(String.Format("Start process receive (wait={0} RUB, timeout={1} sec)", summ, timeout));
            Validator.StartGetMoney(0, 20);
        }

        private void ButtonStopGetValue(object sender, RoutedEventArgs e)
        {
            Validator.StopGetMoney();
        }

        private void ValidatorOnEvent(CashCodeError error, object data)
        {
            switch (error)
            {
                case CashCodeError.GenericError:
                    AddText(((CashCodeError)data).ToStringEx());
                    break;
                case CashCodeError.SummReceivedEnd:
                    AddText(error.ToStringEx());
                    break;
                case CashCodeError.StatusError:
                    AddText(((CashCodeStatus)data).Text);
                    break;
                case CashCodeError.BillReceived:
                    AddText(((int)data) + " RUB");
                    break;
            }
        }

        private void ButtonSendAck(object sender, RoutedEventArgs e)
        {
            Validator.SendAck();
        }

        private void ButtonSendNak(object sender, RoutedEventArgs e)
        {
            Validator.SendNak();
        }

        #endregion Управление устройством
    }

    #endregion Класс MainWindow
}