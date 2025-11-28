using System.Security.Claims;
using EvoAITest.ApiService.Models;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.ApiService.Endpoints;

/// <summary>
/// Extension methods for registering automation task endpoints.
/// </summary>
public static class TaskEndpoints
{
    /// <summary>
    /// Maps all automation task endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .WithOpenApi();

        // POST /api/tasks - Create new task
        group.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .WithSummary("Create a new automation task")
            .WithDescription("Creates a new automation task for the authenticated user")
            .Produces<TaskResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET /api/tasks - Get all tasks for user
        group.MapGet("/", GetTasks)
            .WithName("GetTasks")
            .WithSummary("Get all tasks for the current user")
            .WithDescription("Retrieves all automation tasks for the authenticated user, optionally filtered by status")
            .Produces<List<TaskResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET /api/tasks/{id} - Get task by ID
        group.MapGet("/{id:guid}", GetTaskById)
            .WithName("GetTaskById")
            .WithSummary("Get a specific task by ID")
            .WithDescription("Retrieves a specific automation task by its unique identifier")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        // PUT /api/tasks/{id} - Update task
        group.MapPut("/{id:guid}", UpdateTask)
            .WithName("UpdateTask")
            .WithSummary("Update an existing task")
            .WithDescription("Updates an existing automation task")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        // DELETE /api/tasks/{id} - Delete task
        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask")
            .WithSummary("Delete a task")
            .WithDescription("Deletes an automation task and all related execution history")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        // GET /api/tasks/{id}/history - Get execution history
        group.MapGet("/{id:guid}/history", GetExecutionHistory)
            .WithName("GetExecutionHistory")
            .WithSummary("Get execution history for a task")
            .WithDescription("Retrieves all execution history records for a specific task")
            .Produces<List<ExecutionHistoryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    /// <summary>
    /// Creates a new automation task.
    /// </summary>
    private static async Task<IResult> CreateTask(
        [FromBody] CreateTaskRequest request,
        IAutomationTaskRepository repository,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from claims (or use a default for development)
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? "anonymous-user"; // Fallback for development

            logger.LogInformation("Creating task '{TaskName}' for user {UserId}", request.Name, userId);

            // Create task entity
            var task = new AutomationTask
            {
                UserId = userId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                NaturalLanguagePrompt = request.NaturalLanguagePrompt,
                Status = TaskStatus.Pending,
                CreatedBy = user.Identity?.Name ?? "Unknown"
            };

            // Save to database
            var createdTask = await repository.CreateAsync(task, cancellationToken);

            logger.LogInformation("Task {TaskId} created successfully", createdTask.Id);

            // Return 201 Created
            var response = TaskResponse.FromEntity(createdTask);
            return Results.Created($"/api/tasks/{createdTask.Id}", response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating task");
            return Results.Problem(
                title: "Error creating task",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets all tasks for the authenticated user.
    /// </summary>
    private static async Task<IResult> GetTasks(
        IAutomationTaskRepository repository,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        [FromQuery] TaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? "anonymous-user";

            logger.LogInformation("Retrieving tasks for user {UserId} with status filter {Status}", userId, status?.ToString() ?? "None");

            List<AutomationTask> tasks;

            if (status.HasValue)
            {
                // Filter by status
                tasks = await repository.GetByUserIdAndStatusAsync(userId, status.Value, cancellationToken);
            }
            else
            {
                // Get all tasks for user
                tasks = await repository.GetByUserIdAsync(userId, cancellationToken);
            }

            logger.LogDebug("Retrieved {TaskCount} tasks for user {UserId}", tasks.Count, userId);

            var response = tasks.Select(TaskResponse.FromEntity).ToList();
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tasks");
            return Results.Problem(
                title: "Error retrieving tasks",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    private static async Task<IResult> GetTaskById(
        Guid id,
        IAutomationTaskRepository repository,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? "anonymous-user";

            logger.LogInformation("Retrieving task {TaskId} for user {UserId}", id, userId);

            var task = await repository.GetByIdAsync(id, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Task {TaskId} not found", id);
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"Task {id} not found",
                    Code = "TASK_NOT_FOUND"
                });
            }

            // Authorization check: Ensure user owns the task
            if (task.UserId != userId)
            {
                logger.LogWarning("User {UserId} attempted to access task {TaskId} owned by {OwnerId}",
                    userId, id, task.UserId);
                return Results.Forbid();
            }

            var response = TaskResponse.FromEntity(task);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving task {TaskId}", id);
            return Results.Problem(
                title: "Error retrieving task",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    private static async Task<IResult> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        IAutomationTaskRepository repository,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? "anonymous-user";

            logger.LogInformation("Updating task {TaskId} for user {UserId}", id, userId);

            var task = await repository.GetByIdAsync(id, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Task {TaskId} not found", id);
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"Task {id} not found",
                    Code = "TASK_NOT_FOUND"
                });
            }

            // Authorization check
            if (task.UserId != userId)
            {
                logger.LogWarning("User {UserId} attempted to update task {TaskId} owned by {OwnerId}",
                    userId, id, task.UserId);
                return Results.Forbid();
            }

            // Apply updates
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                task.Name = request.Name;
            }

            if (request.Description != null)
            {
                task.Description = request.Description;
            }

            if (request.Status.HasValue)
            {
                task.UpdateStatus(request.Status.Value);
            }

            // Save changes
            await repository.UpdateAsync(task, cancellationToken);

            logger.LogInformation("Task {TaskId} updated successfully", id);

            var response = TaskResponse.FromEntity(task);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency"))
        {
            logger.LogWarning(ex, "Concurrency conflict updating task {TaskId}", id);
            return Results.Conflict(new ErrorResponse
            {
                Message = "Task was modified by another process. Please refresh and try again.",
                Code = "CONCURRENCY_CONFLICT"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating task {TaskId}", id);
            return Results.Problem(
                title: "Error updating task",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    private static async Task<IResult> DeleteTask(
        Guid id,
        IAutomationTaskRepository repository,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? "anonymous-user";

            logger.LogInformation("Deleting task {TaskId} for user {UserId}", id, userId);

            var task = await repository.GetByIdAsync(id, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Task {TaskId} not found", id);
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"Task {id} not found",
                    Code = "TASK_NOT_FOUND"
                });
            }

            // Authorization check
            if (task.UserId != userId)
            {
                logger.LogWarning("User {UserId} attempted to delete task {TaskId} owned by {OwnerId}",
                    userId, id, task.UserId);
                return Results.Forbid();
            }

            // Delete task (cascade deletes execution history)
            await repository.DeleteAsync(id, cancellationToken);

            logger.LogInformation("Task {TaskId} deleted successfully", id);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting task {TaskId}", id);
            return Results.Problem(
                title: "Error deleting task",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets execution history for a task.
    /// </summary>
    private static async Task<IResult> GetExecutionHistory(
        Guid id,
        IAutomationTaskRepository repository,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? "anonymous-user";

            logger.LogInformation("Retrieving execution history for task {TaskId}", id);

            // First check if task exists and user owns it
            var task = await repository.GetByIdAsync(id, cancellationToken);

            if (task == null)
            {
                logger.LogWarning("Task {TaskId} not found", id);
                return Results.NotFound(new ErrorResponse
                {
                    Message = $"Task {id} not found",
                    Code = "TASK_NOT_FOUND"
                });
            }

            // Authorization check
            if (task.UserId != userId)
            {
                logger.LogWarning("User {UserId} attempted to access history for task {TaskId} owned by {OwnerId}",
                    userId, id, task.UserId);
                return Results.Forbid();
            }

            // Get execution history
            var history = await repository.GetExecutionHistoryAsync(id, cancellationToken);

            logger.LogDebug("Retrieved {HistoryCount} execution records for task {TaskId}",
                history.Count, id);

            var response = history.Select(ExecutionHistoryResponse.FromEntity).ToList();
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving execution history for task {TaskId}", id);
            return Results.Problem(
                title: "Error retrieving execution history",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
