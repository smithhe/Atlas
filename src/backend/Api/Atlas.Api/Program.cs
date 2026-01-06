var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    // Match frontend-friendly JSON (enums as strings).
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Atlas.Application.Features.Tasks.CreateTask.CreateTaskCommand>();
});

builder.Services.AddValidatorsFromAssemblyContaining<Atlas.Application.Features.Tasks.CreateTask.CreateTaskCommand>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Persistence (SQLite for now; swap to Postgres later).
var connectionString = builder.Configuration.GetConnectionString("AtlasDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Data Source=atlas-dev.db";
}

builder.Services.AddDbContext<AtlasDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddAtlasPersistence();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();

    // For local/dev: create tables without migrations.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseFastEndpoints();

app.Run();