using DistributedOrderSaga.Contracts.Models.Sagas;
using DistributedOrderSaga.Orchestration.Consumers;
using DistributedOrderSaga.Orchestration.Repositories;
using DistributedOrderSaga.ServiceDefaults;
using DistributedOrderSaga.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddRabbitMQClient(connectionName: "messaging");
builder.Services.AddRabbitMQExtensions();

builder.Services.AddSingleton<SagaStateRepository>();
builder.Services.AddSingleton<SagaStateUpdater>();

builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<PaymentApprovedConsumer>();
builder.Services.AddHostedService<PaymentRejectedConsumer>();
builder.Services.AddHostedService<PaymentRefundedConsumer>();
builder.Services.AddHostedService<OrderShippingFailedConsumer>();
builder.Services.AddHostedService<OrderShippedConsumer>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseHttpsRedirection();

app.MapGet("/", () =>
    Results.Ok("Hello from Order Saga Service!"));

app.MapGet("api/v1/saga/{orderId:guid}", (Guid orderId, SagaStateRepository repository) =>
    {
        var state = repository.Get(orderId);
        return state is not null
            ? Results.Ok(state)
            : Results.NotFound(new { message = $"SAGA state not found for OrderId={orderId}" });
    })
    .WithName("GetSagaState")
    .WithDescription("Obtém o estado completo de uma SAGA específica pelo OrderId")
    .WithOpenApi();

app.MapGet("api/v1/saga", (SagaStateRepository repository, SagaStatus? status = null) => Results.Ok(
        (object?)(status is null
            ? repository.GetAll()
            : repository.GetByStatus(status.GetValueOrDefault()))))
    .WithName("GetAllSagas")
    .WithDescription("Lista todas as SAGAs ou filtra por status")
    .WithOpenApi();

app.MapGet("api/v1/saga/statistics", (SagaStateRepository queryService)
        => Results.Ok(queryService.GetStatistics()))
    .WithName("GetSagaStatistics")
    .WithDescription("Obtém estatísticas agregadas de todas as SAGAs")
    .WithOpenApi();

app.Run();