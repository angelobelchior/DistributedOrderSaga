using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Events.Payments;
using DistributedOrderSaga.Contracts.Models.Sagas;
using DistributedOrderSaga.Orchestration.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;

namespace DistributedOrderSaga.Orchestration.Consumers;

public class PaymentRefundedConsumer(
    ILogger<PaymentRefundedConsumer> logger,
    BaseMessageConsumer messageConsumer,
    IConnection connection,
    SagaStateRepository sagaStateRepository,
    SagaStateUpdater sagaStateUpdater)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _channel.DefaultQueueDeclare("payment_refunded");
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: "PaymentRefundedConsumer",
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var evt = ea.Body.ToMessage<PaymentRefundedEvent>();

                    var saga = await sagaStateRepository.GetAsync(evt.Order.Id, ct);
                    if (saga == null)
                    {
                        logger.LogWarning("Saga not found for order {OrderId}", evt.Order.Id);
                        return;
                    }

                    if (saga.Status == SagaStatus.Compensated)
                    {
                        logger.LogInformation("Refund already processed for order {OrderId}", evt.Order.Id);
                        return;
                    }

                    logger.LogInformation("Payment refunded for order {OrderId}", evt.Order.Id);
                    sagaStateUpdater.TransitionToStatus(saga,
                        SagaStatus.Compensated, 
                        SagaEvent.PaymentRefunded);
                    
                    await sagaStateRepository.UpdateAsync(saga, ct);
                },
                stoppingToken,
                sendToDlq: false);
        };
        _channel.BasicConsume(queue: "payment_refunded", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}