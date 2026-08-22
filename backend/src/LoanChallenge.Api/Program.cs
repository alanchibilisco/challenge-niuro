using LoanChallenge.Api.Data;
using LoanChallenge.Api.Options;
using LoanChallenge.Api.Services;
using LoanChallenge.Api.Workers;
using LoanChallenge.Core.Application;
using LoanChallenge.Core.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<BlacklistOptions>(builder.Configuration.GetSection("Blacklist"));
builder.Services.Configure<ExternalServiceOptions>(builder.Configuration.GetSection("ExternalService"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Motor de reglas: cada regla es una clase independiente; agregar una nueva
// regla consiste en crear la clase y registraría aquí como ILoanDenialRule,
// sin tocar las existentes.
builder.Services.AddSingleton<ILoanDenialRule, NyStateRule>();
builder.Services.AddSingleton<ILoanDenialRule, BlacklistedSsnRule>();
builder.Services.AddSingleton<LoanRulesEngine>();
builder.Services.AddSingleton<ILoanBlacklist, ConfigLoanBlacklist>();

builder.Services.AddScoped<ILoanRepository, EfLoanRepository>();
builder.Services.AddScoped<LoanApplicationService>();

builder.Services.AddHttpClient<ExternalServiceClient>((sp, client) =>
{
    ExternalServiceOptions options = sp.GetRequiredService<IOptions<ExternalServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()));

WebApplication app = builder.Build();

#if DEBUG
using (IServiceScope scope = app.Services.CreateScope())
{
    LoanDbContext db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();

    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
}
#endif

using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LoanDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.MapControllers();

app.Run();

public partial class Program;
