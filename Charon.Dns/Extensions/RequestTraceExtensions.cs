using Charon.Dns.Lib.Tracing;
using Serilog;
using Serilog.Core;

namespace Charon.Dns.Extensions;

public static class RequestTraceExtensions
{
    extension(RequestTrace? trace)
    {
        public ILogger GetLogger(ILogger? globalLogger = null)
        {
            if (trace is null || trace.Logger == Logger.None)
            {
                return globalLogger ?? Logger.None;
            }
            
            return trace.Logger;
        }
    }
}
