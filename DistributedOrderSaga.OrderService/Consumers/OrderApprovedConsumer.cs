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
                function: async ct =>
                {
                    var approveOrder = ea.Body.ToMessage<ApproveOrderCommand>();
                    var order = await repository.GetAsync(approveOrder.Order.Id, ct);
                    if (order is null)
                    {
                        logger.LogWarning("Order {OrderId} not found", approveOrder.Order.Id);
                        return;
                    }

                    order = order.ChangeStatus(OrderStatus.Approved);
                    await repository.UpdateAsync(order, ct);
                    logger.LogInformation("Order {OrderId} approved", approveOrder.Order.Id);
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