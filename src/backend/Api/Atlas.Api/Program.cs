using Microsoft.Data.Sqlite;
using Atlas.Persistence.Seeding;

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

// Persistence (SQLite for now; swap to Postgres later).
// Development default: in-memory SQLite (relational behavior, no on-disk db file).
var useInMemorySqlite = builder.Environment.IsDevelopment() &&
                        builder.Configuration.GetValue("UseInMemorySqlite", true);

if (useInMemorySqlite)
{
    // Keep the connection open for the life of the app; otherwise the in-memory database is lost.
    var keepAliveConnection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
    keepAliveConnection.Open();
    builder.Services.AddSingleton(keepAliveConnection);

    builder.Services.AddDbContext<AtlasDbContext>((sp, options) =>
        options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("AtlasDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Data Source=atlas-dev.db";
    }

    builder.Services.AddDbContext<AtlasDbContext>(options =>
        options.UseSqlite(connectionString));
}

builder.Services.AddAtlasPersistence();

var app = builder.Build();

// Dispose the keep-alive SQLite connection on shutdown.
if (useInMemorySqlite)
{
    var conn = app.Services.GetRequiredService<SqliteConnection>();
    app.Lifetime.ApplicationStopping.Register(() => conn.Dispose());
}


app.UseCors("DevCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();

    // For local/dev: create tables without migrations.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
    db.Database.EnsureCreated();

    await DevDatabaseSeeder.SeedAsync(db);
}
else {
    app.UseHttpsRedirection();
}


app.UseFastEndpoints();

app.Run();