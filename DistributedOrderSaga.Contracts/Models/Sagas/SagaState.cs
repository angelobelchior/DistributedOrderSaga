using DistributedOrderSaga.Contracts.Models.Orders;

namespace DistributedOrderSaga.Contracts.Models.Sagas;

public class SagaState
{
    public Order Order { get; set; } = null!;
    public SagaStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public bool? PaymentApproved { get; set; }
    public string? CancellationReason { get; set; }
    public List<SagaStateTransition> History { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    
    public static SagaState CreateFromOrder(Order order)
    {
        return new SagaState
        {
            Order = order,
            Status = SagaStatus.Started,
            StartedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            History =
            [
                new()
                {
                    Timestamp = DateTime.UtcNow,
                    FromStatus = SagaStatus.Started,
                    ToStatus = SagaStatus.Started,
                    Event = SagaEvent.OrderCreated
                }
            ]
        };
    }
}