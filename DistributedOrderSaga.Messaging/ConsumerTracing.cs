using System.Diagnostics;
using System.Text;
using RabbitMQ.Client.Events;

namespace DistributedOrderSaga.Messaging;

public static class ConsumerTracing
{
    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string consumerName)
    {
        string? traceParent = null;

        if (ea.BasicProperties?.Headers != null &&
            ea.BasicProperties.Headers.TryGetValue("traceparent", out var headerValue))
        {
            traceParent = headerValue switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                ReadOnlyMemory<byte> mem => Encoding.UTF8.GetString(mem.ToArray()),
                string s => s,
                _ => null
            };
        }

    var context = traceParent != null ? ActivityContext.Parse(traceParent, null) : default;
        var activity = Constants.ActivitySource.StartActivity($"{consumerName} Consume", ActivityKind.Consumer, context);
        return activity;
    }
}