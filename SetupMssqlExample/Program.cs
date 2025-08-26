using SetupMssqlExample;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

await DockerStarter.StartDockerContainerAsync();

Database.Setup();
Database.InsertValue(DateTime.UtcNow.DayOfWeek.ToString());
Database.Select();
Database.DeleteAll();
Database.Select();

await app.RunAsync();