using DistributedOrderSaga.ServiceDefaults;
using DistributedOrderSaga.UI.Components;
using DistributedOrderSaga.UI.ExternalServices;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<OrchestrationClient>(client =>
{
    client.BaseAddress = new Uri("https+http://orchestration");
});

var app = builder.Build();
app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseHsts();
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();