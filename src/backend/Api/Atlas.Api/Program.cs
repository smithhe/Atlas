using Microsoft.EntityFrameworkCore;
using Atlas.AzureDevOps;
using Atlas.Api.Time;
using Atlas.Application.Abstractions.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    // Match frontend-friendly JSON (enums as strings).
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Local UI (Vite/Electron) -> API calls.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Atlas.Application.Features.Tasks.CreateTask.CreateTaskCommand>();
});

builder.Services.AddValidatorsFromAssemblyContaining<Atlas.Application.Features.Tasks.CreateTask.CreateTaskCommand>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Persistence (Postgres).
var connectionString = builder.Configuration.GetConnectionString("AtlasDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'AtlasDb' is required.");
}

builder.Services.AddDbContext<AtlasDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddAtlasPersistence();
builder.Services.AddAzureDevOps();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

var app = builder.Build();


app.UseCors("DevCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();

    // For local/dev: create tables without migrations.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    db.Database.EnsureCreated();
}
else {
    app.UseHttpsRedirection();
}


app.UseFastEndpoints();

app.Run();