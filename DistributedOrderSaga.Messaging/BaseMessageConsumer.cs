using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DistributedOrderSaga.Messaging;

public class BaseMessageConsumer(ILogger<BaseMessageConsumer> logger)
{
    public async Task ConsumeAsync(
        IModel channel,
        string consumerName,
        BasicDeliverEventArgs deliverEventArgs,
        Func<CancellationToken, Task> function,
        CancellationToken cancellationToken,
        bool sendToDlq)
    {
        using var activity = ConsumerTracing.StartConsumerActivity(deliverEventArgs, consumerName);
        if (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("[{ConsumerName}] Cancellation requested, skipping message processing", consumerName);
            channel.BasicNack(deliveryTag: deliverEventArgs.DeliveryTag, multiple: false, requeue: true);
            return;
        }

        try
        {
            await function(cancellationToken);
            channel.BasicAck(deliveryTag: deliverEventArgs.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[{ConsumerName}] Operation cancelled during processing", consumerName);
            channel.BasicNack(deliverEventArgs.DeliveryTag, false, requeue: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{ConsumerName}] Error during message processing", consumerName);
            if (sendToDlq)
                channel.BasicNack(deliveryTag: deliverEventArgs.DeliveryTag, multiple: false, requeue: false);
            else
                channel.BasicAck(deliveryTag: deliverEventArgs.DeliveryTag, multiple: false);
        }
    }
}