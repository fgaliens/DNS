using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Charon.Dns.Lib.Protocol;

namespace Charon.Dns.Lib.Client.RequestResolver
{
    public class NullRequestResolver : IRequestResolver
    {
        public Task<IResponse> Resolve(IRequest request, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new ResponseException("Request failed");
        }
    }
}
