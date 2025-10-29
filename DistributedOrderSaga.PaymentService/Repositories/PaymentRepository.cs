using System.Collections.Concurrent;
using DistributedOrderSaga.PaymentService.Models;

namespace DistributedOrderSaga.PaymentService.Repositories;

public class PaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public async Task<Payment> InsertAsync(Payment payment, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        _payments[payment.Id] = payment;
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        _payments[payment.Id] = payment;
        return payment;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 500), cancellationToken);
        return _payments.Values.FirstOrDefault(o => o.OrderId == orderId);
    }
}