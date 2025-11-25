var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Add SQL Server for EvoAITest database
var sql = builder.AddSqlServer("sql")
    .AddDatabase("evoaidb");

var apiService = builder.AddProject<Projects.EvoAITest_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(sql)
    .WaitFor(sql);

builder.AddProject<Projects.EvoAITest_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
