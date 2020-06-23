using System;
using System.Net;
using MassTransit;
using MassTransit.Saga;
using Messaging.Activities;
using Messaging.Consumers;
using Messaging.Contracts;
using Messaging.StateMachines;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var hostConfig = hostContext.Configuration;
                    services.AddLogging();
                    services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) =>
                        module.IncludeDiagnosticSourceActivities.Add("MassTransit"));
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                        x.AddBus(context => Bus.Factory.CreateUsingRabbitMq(
                            configurator =>
                            {
                                var uri = new Uri(hostConfig["RabbitMQ"]);
                                var usernameAndPassword = uri.UserInfo.Split(":");

                                configurator.Host(uri,
                                    h =>
                                    {
                                        h.Username(WebUtility.UrlDecode(usernameAndPassword[0]));
                                        h.Password(WebUtility.UrlDecode(usernameAndPassword[1]));
                                    });

                                configurator.ReceiveEndpoint("submit-order",
                                    e => { e.Consumer<SubmitOrderConsumer>(); });

                                configurator.ReceiveEndpoint("order-observer",
                                    e => { e.Consumer<OrderSubmittedConsumer>(); });

                                configurator.ReceiveEndpoint("order-state", e =>
                                {
                                    var machine = new OrderStateMachine();
                                    var repository = new InMemorySagaRepository<OrderState>();

                                    e.StateMachineSaga(machine, repository);

                                    EndpointConvention.Map<OrderProcessed>(e.InputAddress);
                                });

                                configurator.ReceiveEndpoint("execute-process-order", e =>
                                {
                                    e.ExecuteActivityHost<ProcessOrderActivity, ProcessOrderArguments>();

                                    EndpointConvention.Map<ProcessOrderArguments>(e.InputAddress);
                                });
                            }));
                    });

                    services.AddHostedService<Worker>();
                });
    }
}