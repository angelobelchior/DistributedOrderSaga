using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Orders;
using DistributedOrderSaga.Contracts.Events.Shipping;
using DistributedOrderSaga.Contracts.Models.Sagas;
using DistributedOrderSaga.Orchestration.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;

namespace DistributedOrderSaga.Orchestration.Consumers;

public class OrderShippedConsumer(
    IConnection connection,
    BaseMessageConsumer messageConsumer,
    Publisher publisher,
    SagaStateRepository sagaStateRepository,
    SagaStateUpdater sagaStateUpdater)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.DefaultQueueDeclare("order_shipped");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: "OrderShippedConsumer",
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var orderShipped = ea.Body.ToMessage<OrderShippedEvent>();
                    var saga = sagaStateRepository.Get(orderShipped.Order.Id);
                    
                    if (saga == null || saga.Status == SagaStatus.Completed)
                        return;

                    sagaStateUpdater.TransitionToStatus(
                        saga,
                        SagaStatus.Completed, 
                        SagaEvent.OrderShipped);
                    
                    sagaStateRepository.Update(saga);

                    var approveOrder = ApproveOrderCommand.Create(orderShipped.Order);
                    await publisher.PublishAsync("approve_order", approveOrder, ct);
                },
                stoppingToken,
                sendToDlq: false);
        };

        _channel.BasicConsume(queue: "order_shipped", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}