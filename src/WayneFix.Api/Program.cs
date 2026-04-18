using WayneFix.Application.Services;
using WayneFix.Domain.Interfaces;
using WayneFix.Infrastructure.Email;
using WayneFix.Infrastructure.Persistence;
using Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEmailService, ConsoleEmailService>();
builder.Services.AddScoped(sp => new DbContext(
    builder.Configuration.GetConnectionString("DefaultConnection")!
));
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<IReportRepository, DapperReportRepository>();

builder.Services.AddControllers();

builder.Services.AddHostedService<OutboxProcessorWorker>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContext>();
    await db.InitializeAsync();
}

app.Run();
