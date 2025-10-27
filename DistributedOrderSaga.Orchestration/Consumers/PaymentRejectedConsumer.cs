using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Orders;
using DistributedOrderSaga.Contracts.Events.Payments;
using DistributedOrderSaga.Contracts.Models.Sagas;
using DistributedOrderSaga.Orchestration.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;

namespace DistributedOrderSaga.Orchestration.Consumers;

public class PaymentRejectedConsumer(
    ILogger<PaymentRejectedConsumer> logger,
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

        _channel.DefaultQueueDeclare("payment_rejected");
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: "PaymentRejectedConsumer",
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var evt = ea.Body.ToMessage<PaymentRejectedEvent>();
                    var saga = sagaStateRepository.Get(evt.Order.Id);
                    if (saga is not { Status: < SagaStatus.CancelledByPayment })
                    {
                        logger.LogWarning("Payment rejection already processed for order {OrderId}",
                            evt.Order.Id);
                        return;
                    }

                    var reason = $"Payment rejected: {evt.Reason}";
                    sagaStateUpdater.UpdatePaymentInfo(saga, evt.Id, false);
                    sagaStateUpdater.SetCancellationReason(saga, reason);
                    sagaStateUpdater.TransitionToStatus(
                        saga,
                        SagaStatus.CancelledByPayment,
                        SagaEvent.PaymentRejected(reason));

                    sagaStateRepository.Update(saga);

                    var cancelOrder = CancelOrderCommand.Create(evt.Order, reason);
                    await publisher.PublishAsync("cancel_order", cancelOrder, ct);
                },
                stoppingToken,
                sendToDlq: false);
        };
        _channel.BasicConsume(queue: "payment_rejected", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}