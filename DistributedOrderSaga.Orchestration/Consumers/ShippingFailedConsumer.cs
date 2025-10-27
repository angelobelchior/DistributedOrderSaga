using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Orders;
using DistributedOrderSaga.Contracts.Commands.Payments;
using DistributedOrderSaga.Contracts.Events.Shipping;
using DistributedOrderSaga.Contracts.Models.Sagas;
using DistributedOrderSaga.Orchestration.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;

namespace DistributedOrderSaga.Orchestration.Consumers;

public class OrderShippingFailedConsumer(
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
        _channel.DefaultQueueDeclare("order_shipping_failed");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: "OrderShippingFailedConsumer",
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var evt = ea.Body.ToMessage<OrderShippingFailedEvent>();

                    var saga = sagaStateRepository.Get(evt.Order.Id);
                    if (saga is not { Status: < SagaStatus.Compensating })
                        return;

                    var reason = "Shipping failed: " + evt.Reason;
                    sagaStateUpdater.SetCancellationReason(saga, reason);
                    sagaStateUpdater.TransitionToStatus(
                        saga,
                        SagaStatus.Compensating,
                        SagaEvent.ShippingFailed);

                    var refund = RefundPaymentCommand.Create(evt.Order, evt.Reason);
                    await publisher.PublishAsync("refund_payment", refund, ct);
                    sagaStateUpdater.AddMetadata(saga, "RefundPaymentId", refund.Id);

                    sagaStateRepository.Update(saga);

                    var cancel = CancelOrderCommand.Create(evt.Order, reason);
                    await publisher.PublishAsync("cancel_order", cancel, ct);
                },
                stoppingToken,
                sendToDlq: false);
        };

        _channel.BasicConsume(queue: "order_shipping_failed", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}