using System.Collections.Concurrent;
using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.OrderService.Repositories;

public class OrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Order Insert(Order order)
    {
        _orders[order.Id] = order;
        return order;
    }

    public Order Update(Order order)
    {
        _orders[order.Id] = order;
        return order;
    }

    public Order? Get(Guid orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return order;
    }

    public IReadOnlyList<Order> List()
        => _orders.Values.ToList();
}