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

            Errors.Add(100000, CashCode.Net.Properties.Resource.CashCodeErroList_UnknownError);

            Errors.Add(100010, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorOpeningComPort);
            Errors.Add(100020, CashCode.Net.Properties.Resource.CashCodeErroList_ComPortNotOpen);
            Errors.Add(100030, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorOffspringOfTheCommandToEnableTheBillValidator);
            Errors.Add(100040, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorOffspringOfTheCommandToEnableTheBillValidatorNoPOWERUPCommandWasReceivedFromTheBillValidator);
            Errors.Add(100050, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorOffspringOfTheCommandToEnableTheBillValidatorNoACKCommandWasReceivedFromTheBillValidator);
            Errors.Add(100060, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorOffspringOfTheCommandToEnableTheBillValidatorNoINITIALIZECommandWasReceivedFromTheBillValidator);
            Errors.Add(100070, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorCheckingOfStatusOfTheBillValidatorStackerSDown);
            Errors.Add(100080, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorCheckingOfStatusOfTheBillValidatorStackerIsFull);
            Errors.Add(100090, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorCheckingOfStatusOfTheBillValidatorThereSABillStuckInTheValidator);
            Errors.Add(100100, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorCheckingOfStatusOfTheBillValidatorThereSABillStuckInTheStacker);
            Errors.Add(100110, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorCheckingOfStatusOfTheBillValidatorFakeBill);
            Errors.Add(100120, CashCode.Net.Properties.Resource.CashCodeErroList_ErrorCheckingOfStatusOfTheBillValidatorThePreviousBillHasNotYetHitTheStackAndIsInTheRecognitionMechanism);

            Errors.Add(100130, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorStackerMechanismError);
            Errors.Add(100140, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorStackerTransmissionSpeedError);
            Errors.Add(100150, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorSendBillsToStackerError);
            Errors.Add(100160, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorAlignmentBillsMechanismError);
            Errors.Add(100170, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorStackerError);
            Errors.Add(100180, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorOpticalSensorsError);
            Errors.Add(100190, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorInductanceChannelError);
            Errors.Add(100200, CashCode.Net.Properties.Resource.CashCodeErroList_BillValidatorErrorStackerOccupancyCheckChannelError);

            // Ошибки распознования купюры
            Errors.Add(0x60, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToInsertion);
            Errors.Add(0x61, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToMagnetic);
            Errors.Add(0x62, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToRemainedBillInHead);
            Errors.Add(0x63, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToMultiplying);
            Errors.Add(0x64, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToConveying);
            Errors.Add(0x65, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToIdentification1);
            Errors.Add(0x66, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToVerification);
            Errors.Add(0x67, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToOptic);
            Errors.Add(0x68, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToInhibit);
            Errors.Add(0x69, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToCapacity);
            Errors.Add(0x6A, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToOperation);
            Errors.Add(0x6C, CashCode.Net.Properties.Resource.CashCodeErroList_RejectingDueToLength);
        }
    }
}
