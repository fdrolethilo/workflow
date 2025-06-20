using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Workflow;

public class Functions(ServiceBusClient serviceBusClient, ILogger<Functions> logger)
{
    public async Task ProcessTimerJob([TimerTrigger("0 */1 * * * *")] TimerInfo timer)
    {
        logger.LogInformation($"Timer trigger executed at: {DateTime.Now}");
        
        // Example of sending a message to Service Bus
        const string message = "Hello from timer trigger";
        var sender = serviceBusClient.CreateSender("mytopic");
        await sender.SendMessageAsync(new ServiceBusMessage(message));
        logger.LogInformation($"Sent message to Service Bus: {message}");
    }
    
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var outputs = new List<string>();

        // Get input
        var input = context.GetInput<string>();
        
        // Call activities
        outputs.Add(await context.CallActivityAsync<string>("SayHello", input));
        outputs.Add(await context.CallActivityAsync<string>("ProcessData", input));
        outputs.Add(await context.CallActivityAsync<string>("CompleteWorkflow", input));

        return outputs;
    }

    public string SayHello([ActivityTrigger] string name)
    {
        return $"Hello {name}!";
    }
    
    public string ProcessData([ActivityTrigger] string input)
    {
        return $"Processing data: {input}";
    }
    
    public string CompleteWorkflow([ActivityTrigger] string input)
    {
        return $"Workflow completed for: {input}";
    }
}