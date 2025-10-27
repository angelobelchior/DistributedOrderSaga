using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Orders;
using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.OrderService.Repositories;

namespace DistributedOrderSaga.OrderService.Consumers;

public class OrderApprovedConsumer(
    IConnection connection,
    OrderRepository repository,
    BaseMessageConsumer messageConsumer,
    ILogger<OrderApprovedConsumer> logger)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.DefaultQueueDeclare("approve_order");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: nameof(OrderApprovedConsumer),
                deliverEventArgs: ea,
                function: _ =>
                {
                    var approveOrder = ea.Body.ToMessage<ApproveOrderCommand>();
                    var order = repository.Get(approveOrder.Order.Id);
                    if (order is null)
                    {
                        logger.LogWarning("Order {OrderId} not found", approveOrder.Order.Id);
                        return Task.CompletedTask;
                    }

                    order = order.ChangeStatus(OrderStatus.Approved);
                    repository.Update(order);
                    logger.LogInformation("Order {OrderId} approved", approveOrder.Order.Id);

                    return Task.CompletedTask;
                },
                stoppingToken,
                sendToDlq: false);
        };
        _channel.BasicConsume(queue: "approve_order", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}