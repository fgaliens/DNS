using Charon.Dns.Lib.AsyncEvents;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Lib.Tracing;
using Charon.Dns.Net;
using Charon.Dns.RequestResolving;
using Charon.Dns.Routing;

namespace Charon.Dns.Interceptors
{
    public class ResponseInterceptor(
        IHostNameAnalyzer hostNameAnalyzer,
        IRouteManager<IpV4Network> ipV4NetworkManager,
        IRouteManager<IpV6Network> ipV6NetworkManager) 
        : IResponseInterceptor
    {
        public async Task Handle(
            IRequest request, 
            IResponse response,
            RequestTrace trace,
            CancellationToken token = default)
        {
            var logger = trace.Logger;

            if (response.Truncated)
            {
                logger.Warning("Response {@Response} for request {@Request} has been truncated", response, request);
            }
            
            var previousHostNameWasSecured = false;
            Domain? previousHostName = null;
            SecuredConnectionParams? connectionParams = null;
            List<Task>? addRouteTasks = null;
            
            foreach (var answer in response.AnswerRecords)
            {
                var shouldBeSecured = false;

                if (answer.Name.Equals(previousHostName))
                {
                    shouldBeSecured = previousHostNameWasSecured;
                }
                else
                {
                    shouldBeSecured = hostNameAnalyzer.ShouldBeSecured(
                        answer.Name.ToString(),
                        trace,
                        out connectionParams);
                    previousHostName = answer.Name;
                    previousHostNameWasSecured = shouldBeSecured;
                }
                
                if (shouldBeSecured)
                {
                    addRouteTasks ??= new(response.AnswerRecords.Count);
                    if (answer.Type is RecordType.A)
                    {
                        var ipV4Network = new IpV4Network(answer.Data, connectionParams!.IpV4RoutingSubnet);
                        var addRouteTask = ipV4NetworkManager.AddRoute(
                            ipV4Network,
                            connectionParams.InterfaceName,
                            trace);
                        addRouteTasks.Add(addRouteTask);
                    }
                    else if (answer.Type is RecordType.AAAA)
                    {
                        var ipV6Network = new IpV6Network(answer.Data, connectionParams!.IpV6RoutingSubnet);
                        var addRouteTask = ipV6NetworkManager.AddRoute(
                            ipV6Network,
                            connectionParams.InterfaceName,
                            trace);
                        addRouteTasks.Add(addRouteTask);
                    }
                }
            }

            if (addRouteTasks is not null)
            {
                await Task.WhenAll(addRouteTasks);
            }
        }

        async Task IAsyncObserver<OnResponseEventArgs>.OnEvent(OnResponseEventArgs eventArgs)
        {
            await Handle(eventArgs.Request, eventArgs.Response, eventArgs.Trace);
        }

        Task IAsyncObserver<OnResponseEventArgs>.OnCompleted()
        {
            return Task.CompletedTask;
        }
    }
}
