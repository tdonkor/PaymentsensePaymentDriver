using System.Runtime.Serialization;

namespace Acrelec.Mockingbird.Payment.Contracts
{
    [DataContract]
    public class RuntimeConfiguration
    {
        static RuntimeConfiguration()
        {
            Instance = new RuntimeConfiguration();
        }

        ///// <summary>
        ///// IP Address
        ///// </summary>
        //[DataMember]
        //public string IpAddress { get; set; }

        /// <summary>
        /// Transaction timeout
        /// </summary>
        [DataMember]
        public uint TransactionTimeout { get; set; }

        /// <summary>
        /// POS Number
        /// </summary>
        [DataMember]
        public int PosNumber { get; set; }


        public static RuntimeConfiguration Instance { get; set; }
    }
}
