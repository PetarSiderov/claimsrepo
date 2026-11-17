using Claims.Auditing;
using Claims.Data;
using Claims.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "claims";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<ClaimsContext>(options =>
{
    var mongoClient = new MongoClient(mongoConnectionString);
    options.UseMongoDB(mongoClient, mongoDatabaseName);
});

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlite(builder.Configuration["AuditDb:ConnectionString"] ?? "Data Source=claims-audit.db"));

builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<ICoverService, CoverService>();
builder.Services.AddScoped<PremiumCalculator>();
builder.Services.AddSingleton<IAuditQueue, BackgroundAuditQueue>();
builder.Services.AddHostedService(sp => (BackgroundAuditQueue)sp.GetRequiredService<IAuditQueue>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();
    auditContext.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
