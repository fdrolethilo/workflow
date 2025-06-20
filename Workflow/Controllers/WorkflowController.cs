using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Workflow.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IDurableClient _durableClient;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(IDurableClient durableClient, ILogger<WorkflowController> logger)
    {
        _durableClient = durableClient;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new workflow orchestration
    /// </summary>
    /// <param name="request">The workflow start request</param>
    /// <returns>Workflow instance details</returns>
    [HttpPost("start")]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Input))
            {
                return BadRequest("Input is required to start the workflow");
            }

            // Start the orchestration
            string instanceId = await _durableClient.StartNewAsync(
                orchestratorFunctionName: nameof(Functions.RunOrchestrator),
                instanceId: request.InstanceId, // Optional: if null, a GUID will be generated
                input: request.Input);

            _logger.LogInformation("Started workflow orchestration with instance ID: {InstanceId}", instanceId);

            var response = new StartWorkflowResponse
            {
                InstanceId = instanceId,
                Input = request.Input,
                StartedAt = DateTime.UtcNow,
                StatusQueryGetUri = Url.Action(nameof(GetWorkflowStatus), new { instanceId }),
                TerminatePostUri = Url.Action(nameof(TerminateWorkflow), new { instanceId })
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start workflow with input: {Input}", request.Input);
            return StatusCode(500, "An error occurred while starting the workflow");
        }
    }

    /// <summary>
    /// Gets the status of a workflow orchestration
    /// </summary>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <returns>Workflow status details</returns>
    [HttpGet("status/{instanceId}")]
    public async Task<IActionResult> GetWorkflowStatus(string instanceId)
    {
        try
        {
            var status = await _durableClient.GetStatusAsync(instanceId);
            
            if (status == null)
            {
                return NotFound($"Workflow instance '{instanceId}' not found");
            }

            var response = new WorkflowStatusResponse
            {
                InstanceId = status.InstanceId,
                RuntimeStatus = status.RuntimeStatus.ToString(),
                Input = status.Input,
                Output = status.Output,
                CreatedTime = status.CreatedTime,
                LastUpdatedTime = status.LastUpdatedTime,
                CustomStatus = status.CustomStatus
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow status for instance: {InstanceId}", instanceId);
            return StatusCode(500, "An error occurred while retrieving workflow status");
        }
    }

    /// <summary>
    /// Terminates a running workflow orchestration
    /// </summary>
    /// <param name="instanceId">The workflow instance ID</param>
    /// <param name="reason">Reason for termination</param>
    /// <returns>Confirmation of termination</returns>
    [HttpPost("terminate/{instanceId}")]
    public async Task<IActionResult> TerminateWorkflow(string instanceId, [FromBody] TerminateWorkflowRequest request)
    {
        try
        {
            await _durableClient.TerminateAsync(instanceId, request.Reason ?? "Terminated via API");
            
            _logger.LogInformation("Terminated workflow instance: {InstanceId}, Reason: {Reason}", 
                instanceId, request.Reason);

            return Ok(new { Message = $"Workflow instance '{instanceId}' has been terminated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate workflow instance: {InstanceId}", instanceId);
            return StatusCode(500, "An error occurred while terminating the workflow");
        }
    }

    /// <summary>
    /// Lists all workflow instances with optional filtering
    /// </summary>
    /// <param name="runtimeStatus">Optional status filter</param>
    /// <param name="createdTimeFrom">Optional start time filter</param>
    /// <param name="createdTimeTo">Optional end time filter</param>
    /// <param name="top">Maximum number of instances to return</param>
    /// <returns>List of workflow instances</returns>
    [HttpGet("instances")]
    public async Task<IActionResult> GetWorkflowInstances(
        [FromQuery] string? runtimeStatus = null,
        [FromQuery] DateTime? createdTimeFrom = null,
        [FromQuery] DateTime? createdTimeTo = null,
        [FromQuery] int top = 50)
    {
        try
        {
            var condition = new OrchestrationStatusQueryCondition
            {
                PageSize = Math.Min(top, 100) // Limit to prevent performance issues
            };

            if (!string.IsNullOrEmpty(runtimeStatus) && 
                Enum.TryParse<OrchestrationRuntimeStatus>(runtimeStatus, true, out var status))
            {
                condition.RuntimeStatus = [status];
            }

            if (createdTimeFrom.HasValue)
            {
                condition.CreatedTimeFrom = createdTimeFrom.Value;
            }

            if (createdTimeTo.HasValue)
            {
                condition.CreatedTimeTo = createdTimeTo.Value;
            }

            var queryResult = await _durableClient.ListInstancesAsync(condition, CancellationToken.None);
            
            var instances = queryResult.DurableOrchestrationState.Select(state => new WorkflowInstanceSummary
            {
                InstanceId = state.InstanceId,
                Name = state.Name,
                RuntimeStatus = state.RuntimeStatus.ToString(),
                CreatedTime = state.CreatedTime,
                LastUpdatedTime = state.LastUpdatedTime,
                Input = state.Input
            }).ToList();

            return Ok(new
            {
                Instances = instances,
                ContinuationToken = queryResult.ContinuationToken,
                Count = instances.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow instances");
            return StatusCode(500, "An error occurred while retrieving workflow instances");
        }
    }
}

// Request/Response DTOs
public class StartWorkflowRequest
{
    public string Input { get; set; } = string.Empty;
    public string? InstanceId { get; set; } // Optional: if provided, must be unique
}

public class StartWorkflowResponse
{
    public string InstanceId { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string? StatusQueryGetUri { get; set; }
    public string? TerminatePostUri { get; set; }
}

public class WorkflowStatusResponse
{
    public string InstanceId { get; set; } = string.Empty;
    public string RuntimeStatus { get; set; } = string.Empty;
    public object? Input { get; set; }
    public object? Output { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
    public object? CustomStatus { get; set; }
}

public class TerminateWorkflowRequest
{
    public string? Reason { get; set; }
}

public class WorkflowInstanceSummary
{
    public string InstanceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RuntimeStatus { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime LastUpdatedTime { get; set; }
    public object? Input { get; set; }
}
