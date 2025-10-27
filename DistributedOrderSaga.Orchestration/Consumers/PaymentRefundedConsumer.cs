using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Events.Payments;
using DistributedOrderSaga.Orchestration.Models;
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
                function: _ =>
                {
                    var evt = ea.Body.ToMessage<PaymentRefundedEvent>();

                    var saga = sagaStateRepository.Get(evt.Order.Id);
                    if (saga == null)
                    {
                        logger.LogWarning("Saga not found for order {OrderId}", evt.Order.Id);
                        return Task.CompletedTask;
                    }

                    if (saga.Status == SagaStatus.Compensated)
                    {
                        logger.LogInformation("Refund already processed for order {OrderId}", evt.Order.Id);
                        return Task.CompletedTask;
                    }

                    logger.LogInformation("Payment refunded for order {OrderId}", evt.Order.Id);
                    sagaStateUpdater.TransitionToStatus(saga,
                        SagaStatus.Compensated, 
                        SagaEvent.PaymentRefunded);
                    
                    sagaStateRepository.Update(saga);

                    return Task.CompletedTask;
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