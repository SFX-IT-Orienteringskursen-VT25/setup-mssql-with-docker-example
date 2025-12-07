using SetupMssqlExample;

var builder = WebApplication.CreateBuilder(args);
// adding CORS so frontend can access the resource here
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin() 
                  .AllowAnyMethod()  
                  .AllowAnyHeader();
        });
});
var app = builder.Build();

// run Docker and build database
// await DockerStarter.StartDockerContainerAsync();
Database.Setup(); 

// run CORS
app.UseCors("AllowAll");


// API 1: GET /number
// get sum number when loading page
app.MapGet("/number", () =>
{
    try 
    {
        var numbers = Database.GetAllNumbers();
        return Results.Ok(numbers); 
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// API 2: POST /number
// save new sum number when clicking button
app.MapPost("/number", (NumberInput input) =>
{
    try
    {
        int newSum = input.Value;
        Console.WriteLine($"get input number: {newSum}");

        Database.InsertValue(newSum);

        return Results.Ok("Saved successfully");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();


// defind a simple dataset to receive JSON from frontend
internal record NumberInput(int Value);