using gud.Server.Data;
using gud.Server.Middleware;
using gud.Server.Repositories.Implementations;
using gud.Server.Repositories.Interfaces;
using gud.Server.Services.Implementations;
using gud.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IRepoRepository, RepoRepository>();
builder.Services.AddScoped<IRepoService, RepoService>();
builder.Services.AddScoped<IRefService, RefService>();
builder.Services.AddScoped<IObjectService, ObjectService>();

builder.Services.AddDbContext<GudDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=gud.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GudDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed on startup");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();