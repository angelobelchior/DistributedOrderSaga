using System.Diagnostics;

namespace DistributedOrderSaga.Messaging;

internal static class Constants
{
    public static readonly ActivitySource ActivitySource = new("OrderSagaDemo.RabbitMQ");
}