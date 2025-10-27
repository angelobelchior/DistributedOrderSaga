namespace DistributedOrderSaga.Contracts.Models.Orders;

public record Address(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);