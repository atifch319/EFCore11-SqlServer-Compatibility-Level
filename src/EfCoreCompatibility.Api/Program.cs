using System.Text.Json.Serialization;
using EfCoreCompatibility.Api.Configurations;
using EfCoreCompatibility.Api.Data;
using EfCoreCompatibility.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddOptions<SqlServerSettings>()
    .Bind(builder.Configuration.GetSection(SqlServerSettings.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SqlServerSettings>, SqlServerSettingsValidator>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var settings = serviceProvider.GetRequiredService<IOptions<SqlServerSettings>>().Value;
    var connectionString = configuration.GetConnectionString(OrdersConnectionString.Name)
        ?? throw new InvalidOperationException("Connection string 'Orders' is not configured.");

    EfSqlServerConfigurator.UseSqlServer(options, connectionString, settings.CompatibilityLevel);
});

builder.Services.AddScoped<OrderQueryService>();
builder.Services.AddScoped<SqlServerDiagnosticsService>();

var app = builder.Build();

var startupSettings = app.Services.GetRequiredService<IOptions<SqlServerSettings>>().Value;
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
startupLogger.LogInformation(
    "EF Core SQL Server provider compatibility level is {CompatibilityLevel}. The database compatibility level is not modified.",
    startupSettings.CompatibilityLevel);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler(handler =>
    {
        handler.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { title = "An unexpected error occurred." });
        });
    });
}

app.MapControllers();

if (startupSettings.ApplyMigrationsOnStartup)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync();
    startupLogger.LogInformation(
        "EF Core migrations were applied. ALTER DATABASE SET COMPATIBILITY_LEVEL was not executed.");
}

app.Run();

public partial class Program;
