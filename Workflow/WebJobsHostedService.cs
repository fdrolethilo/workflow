using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Workflow;

public class WebJobsHostedService(IHost webJobsHost) : IHostedService
{
    private IDurableOrchestrationClient? _durableOrchestrationClient;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await webJobsHost.StartAsync(cancellationToken);
        // Get the durable orchestration client after the host has started
        _durableOrchestrationClient = webJobsHost.Services.GetService<IDurableOrchestrationClient>();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await webJobsHost.StopAsync(cancellationToken);
        webJobsHost.Dispose();
    }

    public IDurableOrchestrationClient GetDurableOrchestrationClient()
    {
        return _durableOrchestrationClient ?? throw new InvalidOperationException("WebJobs host has not been started yet or IDurableOrchestrationClient is not available");
    }
}