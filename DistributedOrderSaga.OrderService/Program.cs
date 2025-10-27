using System.Diagnostics;
using DistributedOrderSaga.Contracts.Events.Orders;
using DistributedOrderSaga.Messaging;
using DistributedOrderSaga.OrderService.Consumers;
using DistributedOrderSaga.OrderService.Models;
using DistributedOrderSaga.OrderService.Repositories;
using DistributedOrderSaga.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient(connectionName: "messaging");
builder.Services.AddRabbitMQExtensions();
builder.Services.AddHostedService<OrderCancelledConsumer>();
builder.Services.AddHostedService<OrderApprovedConsumer>();
builder.Services.AddSingleton<OrderRepository>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseHttpsRedirection();

var activitySource = new ActivitySource("OrderSagaDemo.DistributedOrderSaga.OrderService");

app.MapGet("/", () =>
    Results.Ok("Hello from Orders Service!"));

app.MapPost("/orders", async (
    [FromServices] Publisher publisher,
    [FromServices] OrderRepository repository,
    [FromBody] OrderViewModel viewModel,
    CancellationToken cancellationToken) =>
{
    using var activity = activitySource.StartActivity(nameof(OrderCreatedEvent), ActivityKind.Producer);

    var order = viewModel.ToOrder();
    await repository.InsertAsync(order, cancellationToken);

    var orderCreated = OrderCreatedEvent.Create(order);
    await publisher.PublishAsync("order_created", orderCreated, cancellationToken);
    return Results.Accepted($"/orders/{order.Id}", new { orderId = order.Id });
});

app.MapGet("/orders", async (
    [FromServices] OrderRepository repository,
    CancellationToken cancellationToken) =>
{
    var orders = await repository.ListAsync(cancellationToken);
    return Results.Ok(orders);
});

app.MapGet("/orders/{id:guid}", async (
    Guid id,
    [FromServices] OrderRepository repository,
    CancellationToken cancellationToken) =>
{
    var order = await repository.GetAsync(id, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();