using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Charon.Dns.Lib.AsyncEvents;
using Charon.Dns.Lib.Client;
using Charon.Dns.Lib.Client.RequestResolver;
using Charon.Dns.Lib.Protocol;

namespace Charon.Dns.Lib.Server
{
    public class DnsServer 
        : IAsyncObservable<OnRequestEventArgs>, 
            IAsyncObservable<OnResponseEventArgs>, 
            IAsyncObservable<OnExceptionEventArgs>, 
            IAsyncObservable<OnListeningEventArgs>
    {
        private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

        private const int DefaultPort = 53;
        private const int MaxUdpRequestSize = 512;

        private readonly IRequestResolver _resolver;
        private readonly AsyncObservable<OnRequestEventArgs> _requestEventObservable = new();
        private readonly AsyncObservable<OnResponseEventArgs> _responseEventObservable = new();
        private readonly AsyncObservable<OnExceptionEventArgs> _exceptionEventObservable = new();
        private readonly AsyncObservable<OnListeningEventArgs> _listeningEventObservable = new();

        public DnsServer(IRequestResolver resolver, IPEndPoint endServer) :
            this(new FallbackRequestResolver(resolver, new UdpRequestResolver(endServer)))
        { }

        public DnsServer(IRequestResolver resolver, IPAddress endServer, int port = DefaultPort) :
            this(resolver, new IPEndPoint(endServer, port))
        { }

        public DnsServer(IRequestResolver resolver, string endServer, int port = DefaultPort) :
            this(resolver, IPAddress.Parse(endServer), port)
        { }

        public DnsServer(IPEndPoint endServer) :
            this(new UdpRequestResolver(endServer))
        { }

        public DnsServer(IPAddress endServer, int port = DefaultPort) :
            this(new IPEndPoint(endServer, port))
        { }

        public DnsServer(string endServer, int port = DefaultPort) :
            this(IPAddress.Parse(endServer), port)
        { }

        public DnsServer(IRequestResolver resolver)
        {
            _resolver = resolver;
        }

        public Task Listen(int port = DefaultPort, IPAddress ip = null, CancellationToken cancellationToken = default)
        {
            return Listen(new IPEndPoint(ip ?? IPAddress.Any, port), cancellationToken);
        }

        public async Task Listen(IPEndPoint endpoint, CancellationToken cancellationToken = default)
        {
            using var udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(endpoint);

            while (!cancellationToken.IsCancellationRequested)
            {
                var buffer = ArrayPool.Rent(MaxUdpRequestSize);
                var requestInfo = await udpSocket.ReceiveFromAsync(buffer, endpoint, cancellationToken);
                await HandleRequest(udpSocket, buffer, requestInfo, cancellationToken);
            }
        }

        private async Task OnError(Exception e)
        {
            await _exceptionEventObservable.SendEvent(new OnExceptionEventArgs
            {
                Exception = e,
            });
        }

        private async Task HandleRequest(
            Socket socket, 
            byte[] buffer, 
            SocketReceiveFromResult dataInfo,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            
            var message = buffer[..dataInfo.ReceivedBytes];
            var remote = (IPEndPoint)dataInfo.RemoteEndPoint;
            
            Request request = null;

            try
            {
                request = Request.FromArray(message);

                await _requestEventObservable.SendEvent(new OnRequestEventArgs
                {
                    Request = request,
                    Remote = remote,
                });

                IResponse response = await _resolver.Resolve(request, remote, cancellationToken);

                await _responseEventObservable.SendEvent(new OnResponseEventArgs
                {
                    Request = request,
                    Response = response,
                    Remote = remote,
                });

                await socket.SendToAsync(response.ToArray(), SocketFlags.None, remote, cancellationToken);
            }
            catch (SocketException e) { await OnError(e); }
            catch (ArgumentException e) { await OnError(e); }
            catch (IndexOutOfRangeException e) { await OnError(e); }
            catch (OperationCanceledException e) { await OnError(e); }
            catch (IOException e) { await OnError(e); }
            catch (ObjectDisposedException e) { await OnError(e); }
            catch (ResponseException e)
            {
                IResponse response = e.Response;

                if (response == null)
                {
                    response = Response.FromRequest(request);
                }

                try
                {
                    await socket.SendToAsync(response.ToArray(), SocketFlags.None, remote, cancellationToken);
                }
                catch (SocketException) { }
                catch (OperationCanceledException) { }
                finally
                {
                    await _exceptionEventObservable.SendEvent(new OnExceptionEventArgs
                    {
                        Exception = e,
                    });
                }
            }
            finally
            {
                ArrayPool.Return(buffer);
            }
        }

        public class FallbackRequestResolver : IRequestResolver
        {
            private readonly IRequestResolver[] _resolvers;

            public FallbackRequestResolver(params IRequestResolver[] resolvers)
            {
                _resolvers = resolvers;
            }

            public async Task<IResponse> Resolve(
                IRequest request, 
                IPEndPoint remoteEndPoint, 
                CancellationToken cancellationToken = default)
            {
                IResponse response = null;

                foreach (var resolver in _resolvers)
                {
                    response = await resolver.Resolve(request, remoteEndPoint, cancellationToken);
                    if (response.AnswerRecords.Count > 0)
                    {
                        break;
                    }
                }

                return response;
            }
        }

        public IAsyncDisposable Subscribe(IAsyncObserver<OnResponseEventArgs> observer)
        {
            return _responseEventObservable.Subscribe(observer);
        }

        public IAsyncDisposable Subscribe(IAsyncObserver<OnRequestEventArgs> observer)
        {
            return _requestEventObservable.Subscribe(observer);
        }

        public IAsyncDisposable Subscribe(IAsyncObserver<OnExceptionEventArgs> observer)
        {
            return _exceptionEventObservable.Subscribe(observer);
        }

        public IAsyncDisposable Subscribe(IAsyncObserver<OnListeningEventArgs> observer)
        {
            return _listeningEventObservable.Subscribe(observer);
        }
    }
}
