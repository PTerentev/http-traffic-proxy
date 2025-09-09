using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpTrafficProxy.Domain.Exceptions;

public class HttpProxyException : Exception
{
    public HttpProxyException(string message) : base(message)
    {
    }
}
