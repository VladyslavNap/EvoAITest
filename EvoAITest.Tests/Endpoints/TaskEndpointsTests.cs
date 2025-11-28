using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EvoAITest.ApiService.Models;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;
using ApiProgram = EvoAITest.ApiService.Program;

namespace EvoAITest.Tests.Endpoints;

/// <summary>
/// Integration tests for TaskEndpoints using WebApplicationFactory.
/// Tests CRUD operations, validation, authentication, and authorization flows.
/// </summary>
[TestClass]
public sealed class TaskEndpointsTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _factory = new CustomWebApplicationFactory();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory?.Dispose();
    }

    [TestInitialize]
    public void TestInit()
    {
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client?.Dispose();
    }

    #region Create Task Tests

    [TestMethod]
    public async Task CreateTask_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "Test Task",
            Description = "Test Description",
            NaturalLanguagePrompt = "Navigate to example.com and click the login button"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var taskResponse = await response.Content.ReadFromJsonAsync<TaskResponse>();
        taskResponse.Should().NotBeNull();
        taskResponse!.Name.Should().Be("Test Task");
        taskResponse.Description.Should().Be("Test Description");
        taskResponse.NaturalLanguagePrompt.Should().Be("Navigate to example.com and click the login button");
        taskResponse.Status.Should().Be(TaskStatus.Pending);
        taskResponse.Id.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task CreateTask_WithMissingName_ReturnsBadRequest()
    {
        // Arrange - Create an anonymous object with missing required fields
        var request = new { Description = "Test", NaturalLanguagePrompt = "Valid prompt for testing" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateTask_WithShortPrompt_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "Test Task",
            NaturalLanguagePrompt = "Short" // Less than 10 characters
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("VALIDATION_ERROR");
    }

    [TestMethod]
    public async Task CreateTask_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new { Name = "", NaturalLanguagePrompt = "Valid prompt for testing purposes" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Tasks Tests

    [TestMethod]
    public async Task GetTasks_ReturnsOkWithTaskList()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "List Test Task",
            NaturalLanguagePrompt = "Test prompt for list testing"
        };
        await _client.PostAsJsonAsync("/api/tasks", createRequest);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetTasks_WithStatusFilter_ReturnsFilteredTasks()
    {
        // Arrange - Create tasks with different statuses
        var createRequest = new CreateTaskRequest
        {
            Name = "Pending Task",
            NaturalLanguagePrompt = "Test prompt for status filter testing"
        };
        await _client.PostAsJsonAsync("/api/tasks", createRequest);

        // Act
        var response = await _client.GetAsync("/api/tasks?status=Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        tasks!.Should().AllSatisfy(t => t.Status.Should().Be(TaskStatus.Pending));
    }

    #endregion

    #region Get Task By ID Tests

    [TestMethod]
    public async Task GetTaskById_WithExistingTask_ReturnsOk()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "Get By ID Test Task",
            NaturalLanguagePrompt = "Test prompt for get by id"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act
        var response = await _client.GetAsync($"/api/tasks/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Id.Should().Be(createdTask.Id);
        task.Name.Should().Be("Get By ID Test Task");
    }

    [TestMethod]
    public async Task GetTaskById_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Code.Should().Be("TASK_NOT_FOUND");
    }

    #endregion

    #region Update Task Tests

    [TestMethod]
    public async Task UpdateTask_WithValidRequest_ReturnsOk()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "Original Name",
            NaturalLanguagePrompt = "Test prompt for update test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var updateRequest = new UpdateTaskRequest
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
        updatedTask.Should().NotBeNull();
        updatedTask!.Name.Should().Be("Updated Name");
        updatedTask.Description.Should().Be("Updated Description");
    }

    [TestMethod]
    public async Task UpdateTask_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new UpdateTaskRequest
        {
            Name = "Updated Name"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task UpdateTask_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "Original Name",
            NaturalLanguagePrompt = "Test prompt for validation test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Name is too long (over 500 characters)
        var updateRequest = new UpdateTaskRequest
        {
            Name = new string('a', 501)
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task UpdateTask_WithStatusChange_ReturnsOk()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "Task to Update Status",
            NaturalLanguagePrompt = "Test prompt for status update"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var updateRequest = new UpdateTaskRequest
        {
            Status = TaskStatus.Executing
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().Be(TaskStatus.Executing);
    }

    #endregion

    #region Delete Task Tests

    [TestMethod]
    public async Task DeleteTask_WithExistingTask_ReturnsNoContent()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "Task to Delete",
            NaturalLanguagePrompt = "Test prompt for delete test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify task is deleted
        var getResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task DeleteTask_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Execution History Tests

    [TestMethod]
    public async Task GetExecutionHistory_WithExistingTask_ReturnsOk()
    {
        // Arrange - Create a task first
        var createRequest = new CreateTaskRequest
        {
            Name = "Task with History",
            NaturalLanguagePrompt = "Test prompt for history test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act
        var response = await _client.GetAsync($"/api/tasks/{createdTask!.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<List<ExecutionHistoryResponse>>();
        history.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetExecutionHistory_WithNonExistentTask_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/tasks/{Guid.NewGuid()}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [TestMethod]
    public async Task GetTaskById_WithDifferentUserTask_ReturnsForbidden()
    {
        // Arrange - Create a task with the default user
        var createRequest = new CreateTaskRequest
        {
            Name = "User1 Task",
            NaturalLanguagePrompt = "Test prompt for auth test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Create a client with a different user
        using var otherUserClient = _factory.CreateClientWithUser("other-user");

        // Act
        var response = await otherUserClient.GetAsync($"/api/tasks/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task UpdateTask_WithDifferentUserTask_ReturnsForbidden()
    {
        // Arrange - Create a task with the default user
        var createRequest = new CreateTaskRequest
        {
            Name = "User1 Task for Update",
            NaturalLanguagePrompt = "Test prompt for auth update test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Create a client with a different user
        using var otherUserClient = _factory.CreateClientWithUser("other-user");

        var updateRequest = new UpdateTaskRequest
        {
            Name = "Attempted Update"
        };

        // Act
        var response = await otherUserClient.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task DeleteTask_WithDifferentUserTask_ReturnsForbidden()
    {
        // Arrange - Create a task with the default user
        var createRequest = new CreateTaskRequest
        {
            Name = "User1 Task for Delete",
            NaturalLanguagePrompt = "Test prompt for auth delete test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Create a client with a different user
        using var otherUserClient = _factory.CreateClientWithUser("other-user");

        // Act
        var response = await otherUserClient.DeleteAsync($"/api/tasks/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    /// <summary>
    /// Custom WebApplicationFactory for testing TaskEndpoints.
    /// Configures in-memory database and test authentication.
    /// </summary>
    public sealed class CustomWebApplicationFactory : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Remove the existing DbContext registration
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EvoAIDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Remove the existing DbContext
                var dbContextService = services.SingleOrDefault(
                    d => d.ServiceType == typeof(EvoAIDbContext));
                if (dbContextService != null)
                {
                    services.Remove(dbContextService);
                }

                // Add in-memory database for testing
                services.AddDbContext<EvoAIDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });

                // Configure test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Re-register the repository with the new DbContext
                services.AddScoped<IAutomationTaskRepository, AutomationTaskRepository>();
            });
        }

        /// <summary>
        /// Creates an HttpClient with a specific user identity.
        /// </summary>
        public HttpClient CreateClientWithUser(string userId)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
            return client;
        }
    }

    /// <summary>
    /// Test authentication handler that creates a ClaimsPrincipal from test headers.
    /// </summary>
    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check for test user header
            var userId = Context.Request.Headers["X-Test-User-Id"].FirstOrDefault()
                ?? "test-user-id";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"Test User {userId}"),
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
