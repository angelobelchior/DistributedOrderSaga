using DistributedOrderSaga.ServiceDefaults;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.ShippingService.Consumers;
using DistributedOrderSaga.ShippingService.Repositories;
using DistributedOrderSaga.ShippingService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient(connectionName: "messaging");
builder.Services.AddRabbitMQExtensions();

builder.Services.AddSingleton<ShippingRepository>();
builder.Services.AddSingleton<ShippingProviderService>();

builder.Services.AddHostedService<ShipOrderConsumer>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapGet("/", () =>
    Results.Ok("Hello from Shipping Service!"));

app.MapGet("/shipments/{orderId:guid}", async
(
    Guid orderId,
    ShippingRepository repository,
    CancellationToken cancellationToken) =>
{
    var shipment = await repository.GetByOrderIdAsync(orderId, cancellationToken);
    return shipment is not null
        ? Results.Ok(shipment)
        : Results.NotFound();
});

app.Run();