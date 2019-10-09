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

        /// <summary>
        /// POS Number
        /// </summary>
        [DataMember]
        public int PosNumber { get; set; }

        ///// <summary>
        ///// COM Port
        ///// </summary>
        //[DataMember]
        //public string Port { get; set; }

        /// <summary>
        /// Force online transaction
        /// </summary>
        [DataMember]
        public bool ForceOnline { get; set; }

        ///// <summary>
        ///// IP Address
        ///// </summary>
        //[DataMember]
        //public string IpAddress { get; set; }

        public static RuntimeConfiguration Instance { get; set; }
    }
}
