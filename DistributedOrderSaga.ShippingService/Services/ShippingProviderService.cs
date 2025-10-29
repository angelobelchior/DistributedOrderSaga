using DistributedOrderSaga.Contracts.Models.Orders;
using DistributedOrderSaga.ShippingService.Models;

namespace DistributedOrderSaga.ShippingService.Services;

public class ShippingProviderService
{
    private static readonly string[] Carriers = ["Correios", "FedEx", "UPS", "DHL"];
    
    public async Task<ShippingProviderResult> ShipOrderAsync(Order order, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(500, 800), cancellationToken);
        
        var success = Random.Shared.NextDouble() > 0.2; 
        
        if (!success)
            return new ShippingProviderResult(
                ShipmentStatus.Failed,
                Message: "Provedor de entrega recusou o envio (simulação).");

        var carrier = Carriers[Random.Shared.Next(Carriers.Length)];
        var trackingCode = GenerateTrackingCode(carrier);
        
        return new ShippingProviderResult(
            ShipmentStatus.Shipped,
            TrackingCode: trackingCode,
            Message: $"Envio registrado com {carrier}.");
    }

    private static string GenerateTrackingCode(string carrier)
    {
        var prefix = carrier switch
        {
            "Correios" => "BR",
            "FedEx" => "FX",
            "UPS" => "1Z",
            "DHL" => "DH",
            _ => "XX"
        };
        
        var randomNumber = Random.Shared.Next(100000000, 999999999);
        return $"{prefix}{randomNumber}";
    }
}
