namespace DistributedOrderSaga.ShippingService.Models;

public record ShippingProviderResult(
    ShipmentStatus Status,
    string? TrackingCode = null,
    string? Message = null);
