using System.Net;
using Charon.Dns.Lib.AsyncEvents;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Net;
using Charon.Dns.RequestResolving;
using Charon.Dns.Routing;
using Serilog;

namespace Charon.Dns.Interceptors
{
    public class ResponseInterceptor(
        IHostNameAnalyzer hostNameAnalyzer,
        IRouteManager<IpV4Network> ipV4NetworkManager,
        IRouteManager<IpV6Network> ipV6NetworkManager,
        ILogger logger) 
        : IResponseInterceptor
    {
        public async Task Handle(
            IRequest request, 
            IResponse response,
            IPEndPoint remoteEndPoint,
            CancellationToken token = default)
        {
            logger.Debug("Intercepting request from {RemoteIp}", remoteEndPoint);

            if (response.Truncated)
            {
                logger.Warning("Response {@Response} for request {@Request} has been truncated", response, request);
            }
            
            var previousHostNameWasSecured = false;
            Domain? previousHostName = null;
            SecuredConnectionParams? connectionParams = null;
            
            foreach (var answer in response.AnswerRecords)
            {
                var shouldBeSecured = false;

                if (answer.Name.Equals(previousHostName))
                {
                    shouldBeSecured = previousHostNameWasSecured;
                }
                else
                {
                    shouldBeSecured = hostNameAnalyzer.ShouldBeSecured(answer.Name.ToString(), out connectionParams);
                    previousHostName = answer.Name;
                    previousHostNameWasSecured = shouldBeSecured;
                }
                
                if (shouldBeSecured)
                {
                    if (answer.Type is RecordType.A)
                    {
                        var ipV4Network = new IpV4Network(answer.Data, connectionParams!.IpV4RoutingSubnet);
                        await ipV4NetworkManager.AddRoute(ipV4Network, connectionParams.InterfaceName);
                    }
                    else if (answer.Type is RecordType.AAAA)
                    {
                        var ipV6Network = new IpV6Network(answer.Data, connectionParams!.IpV6RoutingSubnet);
                        await ipV6NetworkManager.AddRoute(ipV6Network, connectionParams.InterfaceName);
                    }
                }
            }
        }

        async Task IAsyncObserver<OnResponseEventArgs>.OnEvent(OnResponseEventArgs eventArgs)
        {
            await Handle(eventArgs.Request, eventArgs.Response, eventArgs.Remote);
        }

        Task IAsyncObserver<OnResponseEventArgs>.OnCompleted()
        {
            return Task.CompletedTask;
        }
    }
}
