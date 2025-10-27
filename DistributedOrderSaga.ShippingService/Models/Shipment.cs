namespace DistributedOrderSaga.ShippingService.Models;

public record Shipment(Guid Id, Guid OrderId, ShipmentStatus Status, string? TrackingCode = null)
{
    public static Shipment Create(Guid orderId, ShipmentStatus status, string? trackingCode = null)
        => new(Guid.NewGuid(), orderId, status, trackingCode);

    public Shipment ChangeStatus(ShipmentStatus newStatus)
        => this with { Status = newStatus };

    public Shipment WithTrackingCode(string trackingCode)
        => this with { TrackingCode = trackingCode };

    public bool IsAlreadyProcessed()
        => Status is ShipmentStatus.Shipped or ShipmentStatus.InTransit or ShipmentStatus.Delivered;
}

public enum ShipmentStatus
{
    Pending,
    Shipped,
    InTransit,
    Delivered,
    Failed,
    Cancelled
}