using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Lib.Protocol.Utils;

namespace Charon.Dns.Lib.Client.RequestResolver
{
    public class UdpRequestResolver : IRequestResolver
    {
        private readonly int _timeout;
        private readonly IRequestResolver _fallback;
        private readonly IPEndPoint _dns;

        public UdpRequestResolver(IPEndPoint dns, IRequestResolver fallback, int timeout = 5000)
        {
            _dns = dns;
            _fallback = fallback;
            _timeout = timeout;
        }

        public UdpRequestResolver(IPEndPoint dns, int timeout = 5000)
        {
            _dns = dns;
            _fallback = new NullRequestResolver();
            _timeout = timeout;
        }

        public async Task<IResponse> Resolve(IRequest request, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (UdpClient udp = new UdpClient(_dns.AddressFamily))
            {
                await udp
                    .SendAsync(request.ToArray(), request.Size, _dns)
                    .WithCancellationTimeout(TimeSpan.FromMilliseconds(_timeout), cancellationToken).ConfigureAwait(false);

                UdpReceiveResult result = await udp
                    .ReceiveAsync()
                    .WithCancellationTimeout(TimeSpan.FromMilliseconds(_timeout), cancellationToken).ConfigureAwait(false);

                if (!result.RemoteEndPoint.Equals(_dns)) throw new IOException("Remote endpoint mismatch");
                byte[] buffer = result.Buffer;
                Response response = Response.FromArray(buffer);

                if (response.Truncated)
                {
                    return await _fallback.Resolve(request, remoteEndPoint, cancellationToken).ConfigureAwait(false);
                }

                return new ClientResponse(request, response, buffer);
            }
        }
    }
}
