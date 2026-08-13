using gud.Server.Middleware;
using gud.Server.Repositories.Implementations;
using gud.Server.Repositories.Interfaces;
using gud.Server.Services.Implementations;
using gud.Server.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IRepoRepository, RepoRepository>();
builder.Services.AddScoped<IRepoService, RepoService>();
builder.Services.AddScoped<IRefService, RefService>();
builder.Services.AddScoped<IObjectService, ObjectService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();