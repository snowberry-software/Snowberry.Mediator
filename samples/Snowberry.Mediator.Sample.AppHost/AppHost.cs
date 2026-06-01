var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Snowberry_Mediator_Sample_Worker>("worker");

builder.Build().Run();
