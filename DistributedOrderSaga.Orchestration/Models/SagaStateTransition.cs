namespace DistributedOrderSaga.Orchestration.Models;

public class SagaStateTransition
{
    public DateTime Timestamp { get; set; }
    public SagaStatus FromStatus { get; set; }
    public SagaStatus ToStatus { get; set; }
    public SagaEvent Event { get; set; } = SagaEvent.Empty;
}