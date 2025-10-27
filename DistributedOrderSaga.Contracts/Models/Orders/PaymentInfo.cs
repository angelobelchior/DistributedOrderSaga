namespace DistributedOrderSaga.Contracts.Models.Orders;

public record PaymentInfo(
    string CardNumber, 
    string CardHolder, 
    DateTime ExpiryDate, 
    string Cvv);