﻿
namespace NServiceBus.SqlServer.AcceptanceTests.SqlTransportSpecific
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Transports.SQLServer;
    using NUnit.Framework;

    class When_using_multiple_schemas : NServiceBusAcceptanceTest
    {
        [Test]
        [Ignore("Multi-schema support will be implemented together with multi-db support")]
        public async Task Message_should_be_delivered()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<Sender>(b => b.When((bus, c) => bus.SendAsync(new MyMessage
                    {
                        Id = c.Id
                    })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run(TimeSpan.FromSeconds(200));

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>

                    c.UseTransport<SqlServerTransport>()
                     .DefaultSchema("WillBeOverriddenViaConnectionString")
                     .ConnectionString(@"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;Queue Schema=sender")
                    )
                .AddMapping<MyMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                    c.UseTransport<SqlServerTransport>()
                     .DefaultSchema("receiver"));
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public Context Context { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Context.Id != message.Id)
                    return Task.FromResult(0);

                Context.WasCalled = true;
                return Task.FromResult(0);
            }
        }
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}