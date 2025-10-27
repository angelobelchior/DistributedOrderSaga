using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Orders;
using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.OrderService.Repositories;

namespace DistributedOrderSaga.OrderService.Consumers;

public class OrderCancelledConsumer(
    IConnection connection,
    OrderRepository repository,
    BaseMessageConsumer messageConsumer,
    ILogger<OrderCancelledConsumer> logger)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        //TODO: Deixar prefetchCount configurável
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.DefaultQueueDeclare("cancel_order");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: nameof(OrderCancelledConsumer),
                deliverEventArgs: ea,
                function: _ =>
                {
                    var cancelOrder = ea.Body.ToMessage<CancelOrderCommand>();
                    var order = repository.Get(cancelOrder.Order.Id);
                    if (order is null)
                    {
                        logger.LogWarning("Order {OrderId} not found", cancelOrder.Order.Id);
                        return Task.CompletedTask;
                    }

                    order = order.ChangeStatus(OrderStatus.Canceled);
                    repository.Update(order);
                    logger.LogInformation("Order {OrderId} canceled", cancelOrder.Order.Id);

                    return Task.CompletedTask;
                },
                stoppingToken,
                sendToDlq: false);
        };
        _channel.BasicConsume(queue: "cancel_order", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}