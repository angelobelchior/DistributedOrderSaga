using DistributedOrderSaga.Contracts.Models;
using DistributedOrderSaga.ShippingService.Models;

namespace DistributedOrderSaga.ShippingService.Services;

public class ShippingProviderService
{
    private static readonly string[] Carriers = ["Correios", "FedEx", "UPS", "DHL"];
    
    public ShippingProviderResult ShipOrder(Order order)
    {
        var random = new Random();
        var success = random.NextDouble() > 0.2; 
        
        if (!success)
            return new ShippingProviderResult(
                ShipmentStatus.Failed,
                Message: "Provedor de entrega recusou o envio (simulação).");

        var carrier = Carriers[random.Next(Carriers.Length)];
        var trackingCode = GenerateTrackingCode(carrier);
        
        return new ShippingProviderResult(
            ShipmentStatus.Shipped,
            TrackingCode: trackingCode,
            Message: $"Envio registrado com {carrier}.");
    }

    public ShippingProviderResult CancelShipment(string trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
            return new ShippingProviderResult(
                ShipmentStatus.Failed,
                Message: "Código de rastreamento não fornecido.");

        var success = Random.Shared.NextDouble() <= 0.85; 
        return success
            ? new ShippingProviderResult(
                ShipmentStatus.Cancelled,
                TrackingCode: trackingCode,
                Message: "Envio cancelado com sucesso.")
            : new ShippingProviderResult(
                ShipmentStatus.Failed,
                TrackingCode: trackingCode,
                Message: "Falha ao cancelar envio no provedor (simulação).");
    }

    public ShippingProviderResult TrackShipment(string trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
            return new ShippingProviderResult(
                ShipmentStatus.Failed,
                Message: "Código de rastreamento não fornecido.");

        var statuses = new[] 
        { 
            ShipmentStatus.Shipped, 
            ShipmentStatus.InTransit, 
            ShipmentStatus.Delivered 
        };
        
        var randomStatus = statuses[Random.Shared.Next(statuses.Length)];
        
        return new ShippingProviderResult(
            randomStatus,
            TrackingCode: trackingCode,
            Message: $"Status atualizado: {randomStatus}");
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
