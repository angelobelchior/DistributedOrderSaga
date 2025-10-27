using System.Collections.Concurrent;
using DistributedOrderSaga.PaymentService.Models;

namespace DistributedOrderSaga.PaymentService.Repositories;

public class PaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    
    public Payment Insert(Payment payment)
    {
        _payments[payment.Id] = payment;
        return payment;
    }
    
    public Payment Update(Payment payment)
    {
        _payments[payment.Id] = payment;
        return payment;
    }
    
    public Payment? GetByOrderId(Guid orderId)
        => _payments.Values.FirstOrDefault(o => o.OrderId == orderId);
}