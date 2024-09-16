using CurrencyConverter.Web;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache();

builder.AddFluentValidationEndpointFilter();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<FrankfurterClientOptions>(builder.Configuration.GetSection("FrankfurterClient"));
builder.Services.AddHttpClient<IFrankfurterClient, FrankfurterClient>()
    .ConfigureHttpClient((provider, client) =>
    {
        var options = provider.GetRequiredService<IOptions<FrankfurterClientOptions>>();
        client.BaseAddress = new Uri(options.Value.BaseUrl);
    })
    .AddStandardResilienceHandler();

var app = builder.Build();

app.UseOutputCache();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/latest/{code:required:regex(^[A-Z]{{3}}$)}", async (string code, IFrankfurterClient frankfurterClient, CancellationToken cToken) =>
    {
        var response = await frankfurterClient.GetLatestAsync(code, cToken);
        return response.IsSuccess
            ? Results.Ok(response.Result)
            : Results.Problem(statusCode: response.StatusCode);
    })
    .WithName("Latest")
    .WithOpenApi()
    .CacheOutput(policyBuilder => policyBuilder.SetVaryByRouteValue("code").Expire(TimeSpan.FromMinutes(10)));

app.MapGet("/convert/{baseCode}/{targetCode}/{amount}",
        async ([AsParameters] ConvertRequest request, IFrankfurterClient frankfurterClient, CancellationToken cToken) =>
        {
            var response = await frankfurterClient.ConvertAsync(request.BaseCode, request.TargetCode, request.Amount, cToken);
            return response.IsSuccess
                ? Results.Ok(response.Result)
                : Results.Problem(statusCode: response.StatusCode);
        })
    .AddFluentValidationFilter()
    .WithName("Convert")
    .WithOpenApi()
    .CacheOutput(policyBuilder => policyBuilder.SetVaryByRouteValue(["baseCode", "targetCode", "amount"]).Expire(TimeSpan.FromMinutes(10)));

app.MapGet("/history/{beginDate}/{endDate}/{code}",
        async ([AsParameters] HistoryRequest request, IFrankfurterClient frankfurterClient, CancellationToken cToken) =>
        {
            var response = await frankfurterClient.GetHistoryAsync(request.BeginDate, request.EndDate, request.Code, cToken);
            return response.IsSuccess
                ? Results.Ok(response.Result)
                : Results.Problem(statusCode: response.StatusCode);
        })
    .AddFluentValidationFilter()
    .WithName("History")
    .WithOpenApi()
    .CacheOutput(policyBuilder => policyBuilder.SetVaryByRouteValue(["beginDate", "endDate", "code"]).Expire(TimeSpan.FromMinutes(10)));

app.Run();

public record ConvertRequest([FromRoute]string BaseCode, [FromRoute]string TargetCode, [FromRoute]decimal Amount);
public record HistoryRequest([FromRoute]DateOnly BeginDate, [FromRoute]DateOnly EndDate, [FromRoute]string Code);

// For integration tests purposes
namespace CurrencyConverter.Web
{
    public partial class Program { }
}