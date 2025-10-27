using System.Collections.Concurrent;
using DistributedOrderSaga.ShippingService.Models;

namespace DistributedOrderSaga.ShippingService.Repositories;

public class ShippingRepository
{
    private readonly ConcurrentDictionary<Guid, Shipment> _shipments = new();
    
    public Shipment Insert(Shipment shipment)
    {
        _shipments[shipment.Id] = shipment;
        return shipment;
    }
    
    public Shipment Update(Shipment shipment)
    {
        _shipments[shipment.Id] = shipment;
        return shipment;
    }
    
    public Shipment? GetByOrderId(Guid orderId)
        => _shipments.Values.FirstOrDefault(s => s.OrderId == orderId);
}
