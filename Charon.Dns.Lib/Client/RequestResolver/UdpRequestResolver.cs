#nullable enable
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Lib.Tracing;

namespace Charon.Dns.Lib.Client.RequestResolver
{
    public class UdpRequestResolver : IRequestResolver
    {
        private const int MaxUdpMsgSize = 512;
        
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(2);
        private readonly IPEndPoint _dnsEndpoint;
        private readonly IRequestResolver? _fallback;
        private readonly ConcurrentBag<Socket> _availableSockets = new();
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private ulong _returnedSocketsCounter;

        public UdpRequestResolver(
            IPEndPoint dnsEndpoint, 
            IRequestResolver? fallback = null)
        {
            _dnsEndpoint = dnsEndpoint;
            _fallback = fallback;
        }

        public async Task<IResponse> Resolve(
            IRequest request, 
            RequestTrace trace, 
            CancellationToken cancellationToken = default)
        {
            if (!_availableSockets.TryTake(out var socket))
            {
                socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                trace.Logger.Debug($"{nameof(UdpRequestResolver)}: New socket for DNS {{Ip}} created", _dnsEndpoint);
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var isDebugging = false;
#if DEBUG
                isDebugging = Debugger.IsAttached;
#endif
                if (!isDebugging)
                {
                    cts.CancelAfter(_timeout);
                }

                var requestData = request.ToArray();
                await socket.SendToAsync(requestData, SocketFlags.None, _dnsEndpoint, cts.Token);

                var buffer = _arrayPool.Rent(MaxUdpMsgSize);

                IResponse? response = null;
                while (request.Id != response?.Id)
                {
                    if (response is not null)
                    {
                        trace.Logger.Warning($"{nameof(UdpRequestResolver)}: DNS resolver ({{Ip}}) got response with unexpected ID:\n" +
                            "Request: {@Request}\nResponse: {@Response}",
                            _dnsEndpoint, request, response);
                    }

                    var responseInfo = await socket.ReceiveFromAsync(buffer, _dnsEndpoint, cts.Token);
                    var senderIp = (responseInfo.RemoteEndPoint as IPEndPoint)?.Address.MapToIPv6();
                    if (!_dnsEndpoint.Address.MapToIPv6().Equals(senderIp))
                    {
                        throw new IOException($"Remote endpoint mismatch. Expected response from {_dnsEndpoint}, received from {responseInfo.RemoteEndPoint}");
                    }

                    response = Response.FromArray(buffer[..responseInfo.ReceivedBytes]);
                }

                _arrayPool.Return(buffer);

                if (response.Truncated)
                {
                    if (_fallback != null)
                    {
                        return await _fallback.Resolve(request, trace, cts.Token);
                    }
                    trace.Logger.Warning($"{nameof(UdpRequestResolver)}: DNS resolver ({{Ip}}) got truncated response for request: {{@Request}}",
                        _dnsEndpoint, request);
                }
                return response;
            }
            catch (OperationCanceledException operationCanceledException)
            {
                throw new OperationCanceledException($"Request to {request.Questions[0].Name} (DNS: {_dnsEndpoint} was canceled)", operationCanceledException);
            }
            catch (Exception e)
            {
                trace.Logger.Error(e, $"{nameof(UdpRequestResolver)}: DNS resolver ({{Ip}}) got error for request {{@Request}}",
                    _dnsEndpoint, request);
                throw;
            }
            finally
            {
                _availableSockets.Add(socket);
                FreeSocketsIfNeeded(trace);
                
                trace.Logger.Debug($"{nameof(UdpRequestResolver)}: Socket for DNS {{Ip}} returned to pool. Pool size: {{Size}}", 
                    _dnsEndpoint, _availableSockets.Count);
            }
        }

        private void FreeSocketsIfNeeded(RequestTrace trace)
        {
            var counter = Interlocked.Increment(ref _returnedSocketsCounter);
            if (counter % 1000 != 0)
            {
                return;
            }

            Interlocked.Exchange(ref _returnedSocketsCounter, 1);
            
            if (_availableSockets.Count <= 5)
            {
                return;
            }
            
            const double socketsFreeFactor = 0.8;
            
            var targetSocketsCount = (int)(_availableSockets.Count * socketsFreeFactor);
            
            trace.Logger.Debug("Trying to free sockets for DNS '{Dns}'. Current count: {SocketsCount}, target count: {TargetSocketsCount}",
                _dnsEndpoint, _availableSockets.Count, targetSocketsCount);
            
            while (_availableSockets.Count > targetSocketsCount)
            {
                if (_availableSockets.TryTake(out var socket))
                {
                    socket.Dispose();
                }
            }
        }
    }
}
