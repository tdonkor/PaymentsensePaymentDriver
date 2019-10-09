using System.ServiceModel;

namespace Acrelec.Mockingbird.Payment.Contracts
{
    [ServiceContract]
    public interface IPaymentService
    {
        [OperationContract]
        Result Init(RuntimeConfiguration configuration);

        [OperationContract]
        Result Test();

        [OperationContract]
        Result<PaymentData> Pay(int amount);

        [OperationContract]
        void Shutdown();
    }
}
