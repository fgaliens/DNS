using System.Net;
using Charon.Dns.Lib.AsyncEvents;
using Charon.Dns.Lib.Protocol;

namespace Charon.Dns.Interceptors
{
    public interface IResponseInterceptor : IAsyncObserver<OnResponseEventArgs>
    {
        Task Handle(
            IRequest request,
            IResponse response,
            IPEndPoint remoteEndPoint,
            CancellationToken token = default);
    }

}
