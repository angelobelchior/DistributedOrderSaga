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

app.MapGet("api/v1/saga/{orderId:guid}",
        async (
            Guid orderId,
            SagaStateRepository repository, CancellationToken cancellationToken) =>
        {
            var state = await repository.GetAsync(orderId, cancellationToken);
            return state is not null
                ? Results.Ok(state)
                : Results.NotFound(new { message = $"SAGA state not found for OrderId={orderId}" });
        })
    .WithName("GetSagaState")
    .WithDescription("Obtém o estado completo de uma SAGA específica pelo OrderId")
    .WithOpenApi();

app.MapGet("api/v1/saga", async (
        SagaStateRepository repository, 
        SagaStatus? status,
        CancellationToken cancellationToken) => Results.Ok(
        status is null
            ? await repository.GetAllAsync(cancellationToken)
            : await repository.GetByStatusAsync(status.GetValueOrDefault(), cancellationToken)))
    .WithName("GetAllSagas")
    .WithDescription("Lista todas as SAGAs ou filtra por status")
    .WithOpenApi();

app.MapGet("api/v1/saga/statistics", async (SagaStateRepository queryService, CancellationToken cancellationToken)
        => Results.Ok(await queryService.GetStatisticsAsync(cancellationToken)))
    .WithName("GetSagaStatistics")
    .WithDescription("Obtém estatísticas agregadas de todas as SAGAs")
    .WithOpenApi();

app.Run();