using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Shippings;
using DistributedOrderSaga.Contracts.Events.Payments;
using DistributedOrderSaga.Contracts.Models.Sagas;
using DistributedOrderSaga.Orchestration.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;

namespace DistributedOrderSaga.Orchestration.Consumers;

public class PaymentApprovedConsumer(
    ILogger<PaymentApprovedConsumer> logger,
    Publisher publisher,
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

        _channel.DefaultQueueDeclare("payment_approved");
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: "PaymentApprovedConsumer",
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var evt = ea.Body.ToMessage<PaymentApprovedEvent>();

                    var saga = sagaStateRepository.Get(evt.Order.Id);
                    if (saga == null)
                    {
                        logger.LogWarning("Saga not found for order {OrderId}", evt.Order.Id);
                        return;
                    }

                    if (saga.Status >= SagaStatus.Shipping)
                    {
                        logger.LogWarning("Payment already processed for order {OrderId}", evt.Order.Id);
                        return;
                    }

                    sagaStateUpdater.TransitionToStatus(
                        saga,
                        SagaStatus.PaymentApproved,
                        SagaEvent.PaymentApproved);

                    sagaStateUpdater.TransitionToStatus(
                        saga,
                        SagaStatus.Shipping,
                        SagaEvent.ShipOrder);
                    
                    sagaStateUpdater.UpdatePaymentInfo(saga, evt.Id, true);
                    
                    sagaStateRepository.Update(saga);

                    var shipOrder = ShipOrderCommand.Create(evt.Order);
                    await publisher.PublishAsync("ship_order", shipOrder, ct);
                },
                stoppingToken,
                sendToDlq: false);
        };
        _channel.BasicConsume(queue: "payment_approved", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}