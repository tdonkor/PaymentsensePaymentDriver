using System.Runtime.Serialization;

namespace Acrelec.Mockingbird.Payment.Contracts
{
    [DataContract]
    public class Result<T> : Result
    {
        public Result(T data)
            : this(ResultCode.Success, null, data)
        {
            
        }

        public Result(Result result)
            : base(result.ResultCode, result.Message)
        {

        }

        public Result(ResultCode resultCode, string message = null, T data = default(T))
            : base(resultCode, message)
        {
            Data = data;
        }

        /// <summary>
        /// The additional response data
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public T Data { get; set; }

        public static implicit operator Result<T>(T data)
        {
            return new Result<T>(data);
        }

        public static implicit operator T(Result<T> data)
        {
            return data.Data;
        }

        public static implicit operator Result<T>(ResultCode code)
        {
            return new Result<T>(code);
        }

        public static implicit operator ResultCode(Result<T> code)
        {
            return code.ResultCode;
        }
    }

    [DataContract]
    public class Result : IResult
    {
        public Result()
            : this(ResultCode.Success)
        {

        }

        public Result(ResultCode resultCode, string message = null)
        {
            ResultCode = resultCode;
            Message = message;
        }

        /// <inheritdoc />
        [DataMember]
        public ResultCode ResultCode { get; set; }

        /// <inheritdoc />
        [DataMember(EmitDefaultValue = false)]
        public string Message { get; set; }

        public static implicit operator Result(ResultCode code)
        {
            return new Result(code);
        }

        public static implicit operator ResultCode(Result code)
        {
            return code.ResultCode;
        }
    }

    public interface IResult
    {
        /// <summary>
        /// When implemented returns a result code of the operation
        /// </summary>
        ResultCode ResultCode { get; }

        /// <summary>
        /// When implemented returns a result message of the operation
        /// </summary>
        string Message { get; }
    }
}