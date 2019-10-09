
namespace Acrelec.Mockingbird.Payment.Contracts
{
    public enum ResultCode
    {
        GenericError = -1,
        Success = 0,
        CannotOpenPort = 1,
        PortNotOpen = 2,
        WrongParameter = 3,
        CorruptedData = 4,
        SendFailed = 5,
        SendAborted = 6,
        ReceiveFailed = 7,
        TimeoutError = 8,
        TransactionCancelled = 16
    }
}