using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; private set; }
        public string? Message { get; private set; }
        public T? Data { get; private set; }

        public static ServiceResult<T> Ok(T data, string? message = null)
        {
            return new ServiceResult<T> { IsSuccess = true, Data = data, Message = message };
        }

        public static ServiceResult<T> Fail(string message)
        {
            return new ServiceResult<T> { IsSuccess = false, Message = message };
        }
    }
}
