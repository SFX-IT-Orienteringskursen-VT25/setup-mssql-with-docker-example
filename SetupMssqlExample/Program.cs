using SetupMssqlExample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDatabase, Database>();

var app = builder.Build();

await DockerStarter.StartDockerContainerAsync();

var database = app.Services.GetRequiredService<IDatabase>();
database.Setup();
database.InsertValue(DateTime.UtcNow.DayOfWeek.ToString());
database.Select();
database.DeleteAll();
database.Select();

await app.RunAsync();