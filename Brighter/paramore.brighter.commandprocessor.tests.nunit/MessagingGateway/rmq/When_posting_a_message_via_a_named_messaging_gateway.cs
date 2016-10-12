#region Licence
/* The MIT License (MIT)
Copyright � 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the �Software�), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED �AS IS�, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using System.Collections.Generic;
using nUnitShouldAdapter;
using NUnit.Framework;
using NUnit.Specifications;
using paramore.brighter.commandprocessor.Logging;
using paramore.brighter.commandprocessor.messaginggateway.rmq;
using paramore.brighter.commandprocessor.messaginggateway.rmq.MessagingGatewayConfiguration;

namespace paramore.brighter.commandprocessor.tests.nunit.MessagingGateway.rmq
{
    [Subject("Messaging Gateway")]
    [Category("RMQ")]
    [Property("Requires", "RabbitMQ")]
    public class When_posting_a_message_via_a_named_messaging_gateway : ContextSpecification
    {
        private static IAmAMessageProducer s_messageProducer;
        private static IAmAMessageConsumer s_messageConsumer;
        private static Message s_message;
        private static TestRMQListener s_client;
        private static string s_messageBody;
        private static IDictionary<string, object> s_messageHeaders;

        private Establish _context = () =>
        {
            s_message = new Message(header: new MessageHeader(Guid.NewGuid(), "test1", MessageType.MT_COMMAND), body: new MessageBody("test content"));

            var rmqConnection = new RmqMessagingGatewayConnection
            {
                AmpqUri = new AmqpUriSpecification(uri: new Uri("amqp://guest:guest@localhost:5672/%2f")),
                Exchange = new Exchange("paramore.brighter.exchange")
            };

            s_messageProducer = new RmqMessageProducer(rmqConnection);

            var logger = LogProvider.For<RmqMessageConsumer>();
            s_messageConsumer = new RmqMessageConsumer(rmqConnection, s_message.Header.Topic, s_message.Header.Topic, false, 1, false, logger);
            s_messageConsumer.Purge();

            s_client = new TestRMQListener(rmqConnection, s_message.Header.Topic);
        };

        private Because _of = () =>
        {
            s_messageProducer.Send(s_message);

            var result = s_client.Listen();
            s_messageBody = result.GetBody();
            s_messageHeaders = result.GetHeaders();
        };

        private It _should_send_a_message_via_rmq_with_the_matching_body = () => s_messageBody.ShouldEqual(s_message.Body.Value);
        private It _should_send_a_message_via_rmq_without_delay_header = () => s_messageHeaders.Keys.ShouldNotContain(HeaderNames.DELAY_MILLISECONDS);
        private It _should_received_a_message_via_rmq_without_delayed_header = () => s_messageHeaders.Keys.ShouldNotContain(HeaderNames.DELAYED_MILLISECONDS);

        private Cleanup _tearDown = () =>
        {
            s_messageConsumer.Purge();
            s_messageProducer.Dispose();
        };
    }
}