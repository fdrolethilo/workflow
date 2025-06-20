using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.OpenApi.Models;
using Workflow;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();

// Register WebJobs services
builder.Services.AddSingleton<ServiceBusClient>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("ServiceBus connection string is not configured. Please set ConnectionStrings:ServiceBus in appsettings.json.");
    }
    return new ServiceBusClient(connectionString);
});
builder.Services.AddSingleton<Functions>();

// Register Durable Task services
builder.Services.AddDurableClientFactory();

// Register IDurableClient for dependency injection with proper TaskHub configuration
builder.Services.AddScoped<IDurableClient>(serviceProvider =>
{
    var durableClientFactory = serviceProvider.GetRequiredService<IDurableClientFactory>();
    var hubName = builder.Configuration["DurableTask:HubName"] ?? "WorkflowHub";
    var options = new DurableClientOptions
    {
        TaskHub = hubName // Use configuration or default
    };
    return durableClientFactory.CreateClient(options);
});

// Register other services
builder.Services.AddHealthChecks();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Workflow API", 
        Version = "v1",
        Description = "API for managing Durable Task workflows"
    });
});

// Configure WebJobs
builder.Host.ConfigureWebJobs(b =>
{
    b.AddTimers();
    b.AddDurableTask(options =>
    {
        options.HubName = builder.Configuration["DurableTask:HubName"] ?? "WorkflowHub";
    });
    // Add other WebJobs extensions as needed
});

// Configure logging
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workflow API v1"));
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();