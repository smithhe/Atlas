using Atlas.AzureDevOps;
using Atlas.Api.Time;
using Atlas.Application.Abstractions.Time;
using Atlas.Api.Ai;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Features.Ai.Context;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

app.UseCors("UiCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();

    // For local/dev: create tables without migrations.
    using IServiceScope scope = app.Services.CreateScope();
    AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    db.Database.EnsureCreated();
}
else
{
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