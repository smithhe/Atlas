using Atlas.AzureDevOps;
using Atlas.Api.Time;
using Atlas.Application.Abstractions.Time;
using Atlas.Api.Ai;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai.Context;
using Atlas.Persistence.Seeding;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool seedDemoOnly = args.Any(static argument =>
    string.Equals(argument, "--seed-demo", StringComparison.OrdinalIgnoreCase));

builder.Services.Configure<JsonOptions>(options =>
{
    // Match frontend-friendly JSON (enums as strings).
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
configuredCorsOrigins = configuredCorsOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (configuredCorsOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    configuredCorsOrigins =
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173"
    ];
}

if (configuredCorsOrigins.Length == 0)
{
    throw new InvalidOperationException("At least one CORS origin is required in configuration: Cors:AllowedOrigins");
}

// Browser-hosted atlas.ui -> API calls.
builder.Services.AddCors(options =>
{
    options.AddPolicy("UiCors", policy =>
        policy
            .WithOrigins(configuredCorsOrigins)
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

// Persistence (Postgres). Test hosts register their own DbContext in the Testing environment.
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("AtlasDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'AtlasDb' is required.");
    }

    builder.Services.AddDbContext<AtlasDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
}

builder.Services.AddAtlasPersistence();
builder.Services.AddAzureDevOps();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions._sectionName));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions._sectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiSessionStore, PersistentAiSessionStore>();
builder.Services.AddSingleton(sp =>
{
    AiOptions options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
    return new AiExecutionGate(options.MaxConcurrentSessions);
});
builder.Services.AddSingleton<IAiConversationService, AiConversationService>();
builder.Services.AddScoped<AiOrchestrator>();
builder.Services.AddScoped<AiPromptContextResolver>();
builder.Services.AddScoped<IChatModelClient, OpenAiChatModelClient>();
builder.Services.AddScoped<IAiPromptContextBuilder, DashboardPromptContextBuilder>();
builder.Services.AddScoped<IAiPromptContextBuilder, TasksPromptContextBuilder>();
builder.Services.AddScoped<IAiPromptContextBuilder, TeamPromptContextBuilder>();
builder.Services.AddScoped<IAiPromptContextBuilder, RisksPromptContextBuilder>();
builder.Services.AddScoped<IAiPromptContextBuilder, ProjectsPromptContextBuilder>();
builder.Services.AddScoped<IAiPromptContextBuilder, SettingsPromptContextBuilder>();


WebApplication app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using IServiceScope scope = app.Services.CreateScope();
    AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    ILogger startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Atlas.Api.Startup");

    // Compose healthchecks can report Postgres ready before it accepts app connections.
    const int maxAttempts = 15;
    const int delayMilliseconds = 2000;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            startupLogger.LogInformation(
                "Database schema attempt {Attempt}/{MaxAttempts}…",
                attempt,
                maxAttempts);

            if (app.Environment.IsDevelopment() && !seedDemoOnly)
            {
                // Bare local `dotnet run`: create tables without requiring migrate apply.
                // Docker Compose / non-Development uses Migrate() instead — do not mix on the same DB.
                db.Database.EnsureCreated();
            }
            else
            {
                db.Database.Migrate();
            }

            break;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransientDbStartupException(ex))
        {
            startupLogger.LogWarning(
                ex,
                "Database not ready (attempt {Attempt}/{MaxAttempts}); retrying in {DelayMs}ms.",
                attempt,
                maxAttempts,
                delayMilliseconds);
            await Task.Delay(delayMilliseconds);
        }
    }

    bool seedDemo = seedDemoOnly
        || app.Configuration.GetValue("Atlas:SeedDemo", false)
        || string.Equals(
            Environment.GetEnvironmentVariable("ATLAS_SEED_DEMO"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    if (seedDemo)
    {
        await DevDatabaseSeeder.SeedAsync(db);
    }
}

if (seedDemoOnly)
{
    return;
}

app.UseCors("UiCors");

// Liveness/readiness for Compose — registered before HTTPS redirection so HTTP probes succeed in containers.
app.MapGet("/health", async (AtlasDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        bool canConnect = await db.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new { status = "Healthy" });
    }
    catch
    {
        return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}
else if (!string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase))
{
    // Skip HTTPS redirection inside containers (Compose serves HTTP on 8080).
    app.UseHttpsRedirection();
}

// Surface missing Azure PAT (and similar) as a client-readable JSON body instead of a bare 500.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("AzureDevopsToken", StringComparison.Ordinal) ||
        ex.Message.Contains("Azure DevOps PAT", StringComparison.Ordinal))
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

app.UseFastEndpoints();

app.Run();

static bool IsTransientDbStartupException(Exception exception)
{
    for (Exception? current = exception; current is not null; current = current.InnerException)
    {
        if (current is Npgsql.NpgsqlException npgsql)
        {
            // Timeout / connection refused while Postgres is still coming up.
            if (npgsql.IsTransient || npgsql.InnerException is TimeoutException or System.Net.Sockets.SocketException)
            {
                return true;
            }
        }

        if (current is TimeoutException or System.Net.Sockets.SocketException)
        {
            return true;
        }
    }

    return false;
}
