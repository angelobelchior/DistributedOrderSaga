using DistributedOrderSaga.Contracts;
using DistributedOrderSaga.Contracts.Commands.Shippings;
using DistributedOrderSaga.Contracts.Events.Shipping;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.ShippingService.Models;
using DistributedOrderSaga.ShippingService.Repositories;
using DistributedOrderSaga.ShippingService.Services;

namespace DistributedOrderSaga.ShippingService.Consumers;

public class ShipOrderConsumer(
    BaseMessageConsumer messageConsumer,
    Publisher publisher,
    ILogger<ShipOrderConsumer> logger,
    IConnection connection,
    ShippingRepository shippingRepository,
    ShippingProviderService shippingProviderService)
    : BackgroundService
{
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateModel();
        //TODO: Deixar prefetchCount configurável
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        _channel.QueueDeclareWithDLQ("ship_order");
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            await messageConsumer.ConsumeAsync(
                channel: _channel,
                consumerName: nameof(ShipOrderConsumer),
                deliverEventArgs: ea,
                function: async ct =>
                {
                    var command = ea.Body.ToMessage<ShipOrderCommand>();
                    logger.LogInformation("[ShippingService] Processing shipment: {@shipOrder}", command);

                    var existingShipment = await shippingRepository.GetByOrderIdAsync(command.Order.Id, ct);
                    if (existingShipment is not null && existingShipment.IsAlreadyProcessed())
                    {
                        logger.LogWarning("[ShippingService] Shipment already processed for order {OrderId}", 
                            command.Order.Id);
                        return;
                    }

                    var result = await shippingProviderService.ShipOrderAsync(command.Order, ct);
                    if (result.Status == ShipmentStatus.Shipped)
                    {
                        var shipment = existingShipment is null
                            ? Shipment.Create(command.Order.Id, result.Status, result.TrackingCode)
                            : existingShipment.ChangeStatus(result.Status).WithTrackingCode(result.TrackingCode!);

                        if (existingShipment is null)
                            await shippingRepository.InsertAsync(shipment, ct);
                        else
                            await shippingRepository.UpdateAsync(shipment, ct);

                        var orderShipped = OrderShippedEvent.Create(command.Order);
                        await publisher.PublishAsync("order_shipped", orderShipped, ct);
                        logger.LogInformation(
                            "[ShippingService] Order shipped successfully: {OrderId} | Tracking: {TrackingCode} | {Message}",
                            command.Order.Id, result.TrackingCode, result.Message);
                    }
                    else
                    {
                        var shipment = existingShipment is null
                            ? Shipment.Create(command.Order.Id, ShipmentStatus.Failed)
                            : existingShipment.ChangeStatus(ShipmentStatus.Failed);

                        if (existingShipment is null)
                            await shippingRepository.InsertAsync(shipment, ct);
                        else
                            await shippingRepository.UpdateAsync(shipment, ct);

                        var reason = result.Message ?? "Shipping provider declined the shipment";
                        var evt = OrderShippingFailedEvent.Create(command.Order, reason);
                        await publisher.PublishAsync("order_shipping_failed", evt, ct);
                        logger.LogWarning("[ShippingService] Shipping failed for order {OrderId}: {Reason}",
                            command.Order.Id, reason);
                    }
                },
                stoppingToken,
                sendToDlq: true);
        };
        _channel.BasicConsume(queue: "ship_order", autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}