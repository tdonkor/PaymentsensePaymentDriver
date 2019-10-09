using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acrelec.Mockingbird.Payment
{
    public class TransactionDetails
    {
        public int AmountTotal { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationLabel { get; set; }
        public string AuthCode { get; set; }
        public string CardSchemeName { get; set; }
        public string CardholderVerificationMethod { get; set; }
        public string Currency { get; set; }
        public string DateOfExpiry { get; set; }
        public string DateOfStart { get; set; }
        public string PaymentMethod { get; set; }
        public IList<string> Notifications { get; set; }
        public string PrimaryAccountNumber { get; set; }
        public string PrimaryAccountNumberSequence { get; set; }
        public string TransactionId { get; set; }
        public string TransactionNumber { get; set; }
        public string TransactionResult { get; set; }
        public string UserMessage { get; set; } = " PLEASE RETAIN RECEIPT. \nThank you.";
        public string TransactionTime { get; set; }
        public string TransactionType { get; set; }
        public object ReceiptLines { get; set; }


        


        }

    }
}
