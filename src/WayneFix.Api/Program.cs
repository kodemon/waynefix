using FluentValidation;
using FluentValidation.AspNetCore;
using WayneFix.Api.Validators;
using WayneFix.Application.Services;
using WayneFix.Domain.Interfaces;
using WayneFix.Infrastructure.Email;
using WayneFix.Infrastructure.Persistence;
using WayneFix.Infrastructure.Persistence.Repositories;
using Workers;

var builder = WebApplication.CreateBuilder(args);

/*
 |--------------------------------------------------------------------------------
 | Dependencies
 |--------------------------------------------------------------------------------
 */

builder.Services.AddSingleton<IEmailService, ConsoleEmailService>();

builder.Services.AddScoped(sp => new DbContext(
    builder.Configuration.GetConnectionString("DefaultConnection")!
));
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<IReportRepository, DapperReportRepository>();

/*
 |--------------------------------------------------------------------------------
 | Controllers
 |--------------------------------------------------------------------------------
 */

builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<CreateReportDTOValidator>();
builder.Services.AddFluentValidationAutoValidation();

/*
 |--------------------------------------------------------------------------------
 | Workers
 |--------------------------------------------------------------------------------
 */

builder.Services.AddHostedService<OutboxProcessorWorker>();

/*
 |--------------------------------------------------------------------------------
 | OpenAPI
 |--------------------------------------------------------------------------------
 */

builder.Services.AddOpenApi();

/*
 |--------------------------------------------------------------------------------
 | Application
 |--------------------------------------------------------------------------------
 */

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ### Migrate Database

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContext>();
    await db.InitializeAsync();
}

// ### Start Application

app.Run();
