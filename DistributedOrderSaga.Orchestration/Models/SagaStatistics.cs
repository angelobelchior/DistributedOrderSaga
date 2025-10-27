namespace DistributedOrderSaga.Orchestration.Models;

public class SagaStatistics
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Cancelled { get; set; }
    public int Compensated { get; set; }
    public double AverageCompletionTimeSeconds { get; set; }
}