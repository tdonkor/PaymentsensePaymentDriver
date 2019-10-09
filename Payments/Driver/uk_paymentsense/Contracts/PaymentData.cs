using System.Runtime.Serialization;

namespace Acrelec.Mockingbird.Payment.Contracts
{
    [DataContract]
    public class PaymentData
    {
        [DataMember]
        public PaymentResult Result { get; set; } 

        [DataMember]
        public int PaidAmount { get; set; }

        [DataMember]
        public string TenderMediaDetails { get; set; }

        [DataMember]
        public bool HasClientReceipt { get; set; }

        [DataMember]
        public bool HasMerchantReceipt { get; set; }
    }
}
