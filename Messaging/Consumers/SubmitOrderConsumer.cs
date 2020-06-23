using System.Net.Http;

namespace Messaging.Consumers
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Context;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;

    public class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            LogContext.Info?.Log("Submitting Order: {OrderId}", context.Message.OrderId);
            using (var client = new HttpClient())
                await client.GetAsync("https://www.google.com");
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            if (!EndpointConvention.TryGetDestinationAddress<ProcessOrderArguments>(out var activityAddress))
                throw new ConfigurationException("No endpoint address for activity");

            builder.AddActivity("Process", activityAddress);

            if (!EndpointConvention.TryGetDestinationAddress<OrderProcessed>(out var eventAddress))
                throw new ConfigurationException("No endpoint address for activity");

            await builder.AddSubscription(eventAddress, RoutingSlipEvents.Completed, endpoint =>
                endpoint.Send<OrderProcessed>(context.Message));

            await context.Execute(builder.Build());

            await context.Publish<OrderSubmitted>(context.Message, x => x.ResponseAddress = context.ResponseAddress);
        }
    }
}