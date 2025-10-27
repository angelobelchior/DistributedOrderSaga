namespace DistributedOrderSaga.Contracts.Models;

public record PaymentInfo(
    string CardNumber, 
    string CardHolder, 
    DateTime ExpiryDate, 
    string Cvv);