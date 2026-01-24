using TimeSeriesAnalytics.Api.Models;
using TimeSeriesAnalytics.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure ClickHouse settings
builder.Services.Configure<ClickHouseSettings>(
    builder.Configuration.GetSection("ClickHouse"));

// Register services
builder.Services.AddSingleton<IClickHouseService, ClickHouseService>();
builder.Services.AddScoped<IClickHouseInitializer, ClickHouseInitializer>();
builder.Services.AddScoped<IDataGeneratorService, DataGeneratorService>();
builder.Services.AddScoped<IQueryBuilderService, QueryBuilderService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:4200" };

        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Initialize database schema on startup
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IClickHouseInitializer>();
    await initializer.InitializeDatabaseAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
