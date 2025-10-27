using System.Collections.Concurrent;
using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.OrderService.Repositories;

public class OrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public async Task<Order> InsertAsync(Order order, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(1000, 2500), cancellationToken);
        _orders[order.Id] = order;
        return order;
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(1000, 2500), cancellationToken);
        _orders[order.Id] = order;
        return order;
    }

    public async Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(1000, 2500), cancellationToken);
        _orders.TryGetValue(orderId, out var order);
        return order;
    }

    public async Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(1000, 2500), cancellationToken);
        return _orders.Values.ToList();
    }
}