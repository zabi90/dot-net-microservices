using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformService.AsyncDataServices;
using PlatformService.AutoMapperProfile;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("PlatformsConn");
Console.WriteLine($"--> Using SQL Server Db" + connectionString);
var env = builder.Environment;
if (env.IsProduction())
{
    Console.WriteLine("--> Using Production Db");
    builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseSqlServer(connectionString));

}else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("InMem"));
}




builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();
builder.Services.AddScoped<IPlatformRepository, PlatformRepository>();
builder.Services.AddControllers();
// Register AutoMapper (scans all profiles in the assembly)

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<PlatformsProfile>();
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseCors(policy =>
{
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
    policy.AllowAnyOrigin();
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

PrepareDatabase.Prepare(app, env.IsProduction());
Console.WriteLine("Starting up Platform Service...");
Console.WriteLine($"CommandService Endpoint {builder.Configuration["CommandService"]}");
app.Run();
