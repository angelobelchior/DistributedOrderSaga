using System.Collections.Concurrent;
using DistributedOrderSaga.ShippingService.Models;

namespace DistributedOrderSaga.ShippingService.Repositories;

public class ShippingRepository
{
    private readonly ConcurrentDictionary<Guid, Shipment> _shipments = new();

    public async Task<Shipment> InsertAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        _shipments[shipment.Id] = shipment;
        return shipment;
    }

    public async Task<Shipment> UpdateAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        _shipments[shipment.Id] = shipment;
        return shipment;
    }

    public async Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        return _shipments.Values.FirstOrDefault(s => s.OrderId == orderId);
    }
}