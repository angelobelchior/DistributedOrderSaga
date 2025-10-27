namespace DistributedOrderSaga.Contracts.Models.Sagas;

public record SagaEvent(string Name, string Description)
{
    public static readonly SagaEvent Empty = new(string.Empty, string.Empty);
    
    public static readonly SagaEvent OrderCreated = new("OrderCreated", "Pedido criado. SAGA iniciada");
    public static readonly SagaEvent ProcessPayment = new("ProcessPayment", "Comando de pagamento enviado");
    public static readonly SagaEvent PaymentApproved = new("PaymentApproved", "Pagamento aprovado");
    public static readonly SagaEvent ShipOrder = new("ShipOrder", "Comando de envio enviado");
    public static readonly SagaEvent PaymentRefunded = new("PaymentRefunded", "Pagamento estornado com sucesso");
    public static readonly SagaEvent ShippingFailed = new("ShippingFailed", "Iniciando compensação (rollback)");
    public static readonly SagaEvent OrderShipped = new("OrderShipped", "Envio concluído com sucesso");

    public static SagaEvent PaymentRejected(string reason)
        => new("PaymentRejected", reason);
}