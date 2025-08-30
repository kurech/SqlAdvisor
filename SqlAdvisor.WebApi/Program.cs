using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlAdvisor.Application.Services;
using SqlAdvisor.Domain.Interfaces;
using SqlAdvisor.Infrastructure.Postgres;
using SqlAdvisor.Infrastructure.Rules;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("Pg")!;

builder.Services.AddSingleton<IQueryAnalyzer>(_ => new PostgresQueryAnalyzer(conn));
builder.Services.AddSingleton<IStatisticsProvider>(_ => new PgSettingsProvider(conn));
builder.Services.AddSingleton<IRuleEngine, SimpleRuleEngine>();

builder.Services.AddScoped<AnalyzeQueryUseCase>();

//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/analyze", async (AnalyzeRequest req, AnalyzeQueryUseCase usecase) =>
{
    if (string.IsNullOrWhiteSpace(req.Sql)) return Results.BadRequest("SQL is empty");

    try
    {
        if (req.Sql.Contains(';') && !req.Sql.TrimEnd().EndsWith(';'))
            return Results.BadRequest("Многооператорный SQL не поддерживается для анализа");

        var result = await usecase.ExecuteAsync(req.Sql);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error analyzing SQL: {ex.Message}", statusCode: 500);
    }
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

app.Run();

public sealed record AnalyzeRequest(string Sql);