using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyConverter.Web;

public sealed class FrankfurterClientOptions
{
    public string BaseUrl { get; init; }
}

public interface IFrankfurterClient
{
    Task<HttpResult<FrankfurterResponse>> GetLatestAsync(string currencyCode, CancellationToken cToken);
    Task<HttpResult<FrankfurterResponse>> ConvertAsync(string baseCurrencyCode, string targetCurrencyCode, decimal amount, CancellationToken cToken);
    Task<HttpResult<FrankfurterSeriesResponse>> GetHistoryAsync(DateOnly beginDate, DateOnly endDate, string currencyCode, CancellationToken cToken);
}

public class FrankfurterClient : IFrankfurterClient
{
    private const string LatestEndpointTemplate = "/latest?from={0}";
    private const string ConvertEndpointTemplate = $"{LatestEndpointTemplate}&to={{1}}&amount={{2}}";
    private const string SeriesEndpointTemplate = "/{0}..{1}?from={2}";

    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterClient> _logger;

    public FrankfurterClient(HttpClient httpClient, ILogger<FrankfurterClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HttpResult<FrankfurterResponse>> GetLatestAsync(string currencyCode, CancellationToken cToken)
    {
        using var httpResponseMessage = await _httpClient.GetAsync(string.Format(LatestEndpointTemplate, currencyCode), cToken);
        var contentString = await httpResponseMessage.Content.ReadAsStringAsync(cToken);
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("[Frankfurter] Error fetching latest rate for {CurrencyCode}: [{Code}] {Message}",
                currencyCode, httpResponseMessage.StatusCode, contentString);

            return HttpResult<FrankfurterResponse>.Failure(httpResponseMessage.StatusCode);
        }

        try
        {
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<FrankfurterResponse>(cToken);
            if (response != null)
                return HttpResult<FrankfurterResponse>.Success(response);

            _logger.LogError("[Frankfurter] GetLatest response is null: {Content}", contentString);
            return HttpResult<FrankfurterResponse>.InternalError();
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "[Frankfurter] Error parsing JSON response: {Content}", contentString);
            return HttpResult<FrankfurterResponse>.InternalError();
        }
    }

    public async Task<HttpResult<FrankfurterResponse>> ConvertAsync(string baseCurrencyCode, string targetCurrencyCode, decimal amount, CancellationToken cToken)
    {
        using var httpResponseMessage = await _httpClient.GetAsync(string.Format(ConvertEndpointTemplate, baseCurrencyCode, targetCurrencyCode, amount), cToken);
        var contentString = await httpResponseMessage.Content.ReadAsStringAsync(cToken);
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("[Frankfurter] Error converting {Amount} {BaseCurrencyCode} to {TargetCurrencyCode}: [{Code}] {Message}",
                amount, baseCurrencyCode, targetCurrencyCode, httpResponseMessage.StatusCode, contentString);

            return HttpResult<FrankfurterResponse>.Failure(httpResponseMessage.StatusCode);
        }

        try
        {
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<FrankfurterResponse>(cToken);
            if (response != null)
                return HttpResult<FrankfurterResponse>.Success(response);

            _logger.LogError("[Frankfurter] Convert response is null: {Content}", contentString);
            return HttpResult<FrankfurterResponse>.InternalError();
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "[Frankfurter] Error parsing JSON response: {Content}", contentString);
            return HttpResult<FrankfurterResponse>.InternalError();
        }
    }

    public async Task<HttpResult<FrankfurterSeriesResponse>> GetHistoryAsync(DateOnly beginDate, DateOnly endDate, string currencyCode, CancellationToken cToken)
    {
        using var httpResponseMessage = await _httpClient.GetAsync(string.Format(SeriesEndpointTemplate, beginDate.ToString("O"), endDate.ToString("O"), currencyCode), cToken);
        var contentString = await httpResponseMessage.Content.ReadAsStringAsync(cToken);
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("[Frankfurter] Error getting series for {CurrencyCode} from {BeginDate} to {EndDate}: [{Code}] {Message}",
                currencyCode, beginDate, endDate, httpResponseMessage.StatusCode, contentString);

            return HttpResult<FrankfurterSeriesResponse>.Failure(httpResponseMessage.StatusCode);
        }

        try
        {
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<FrankfurterSeriesResponse>(cToken);
            if (response != null)
                return HttpResult<FrankfurterSeriesResponse>.Success(response);

            _logger.LogError("[Frankfurter] Convert response is null: {Content}", contentString);
            return HttpResult<FrankfurterSeriesResponse>.InternalError();
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "[Frankfurter] Error parsing JSON response: {Content}", contentString);
            return HttpResult<FrankfurterSeriesResponse>.InternalError();
        }
    }
}

public abstract record FrankfurterResponseBase(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("base")] string Base);

public record FrankfurterResponse(
    decimal Amount,
    string Base,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("rates")] Dictionary<string, decimal> Rates)
    : FrankfurterResponseBase(Amount, Base);

public record FrankfurterSeriesResponse(
    decimal Amount,
    string Base,
    [property: JsonPropertyName("start_date")] string BeginDate,
    [property: JsonPropertyName("end_date")] string EndDate,
    [property: JsonPropertyName("rates")] Dictionary<string, Dictionary<string, decimal>> Rates)
    : FrankfurterResponseBase(Amount, Base);

public readonly struct HttpResult<TResult>(TResult? result, int statusCode)
{
    public static HttpResult<TResult> Success(TResult result) => new(result, StatusCodes.Status200OK);
    public static HttpResult<TResult> Failure(int statusCode) => new(default, statusCode);
    public static HttpResult<TResult> Failure(HttpStatusCode statusCode) => Failure((int)statusCode);
    public static HttpResult<TResult> InternalError() => Failure(StatusCodes.Status500InternalServerError);

    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(StatusCode))]
    public bool IsSuccess => StatusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;
    public TResult? Result { get; init; } = result;
    public int StatusCode { get; init; } = statusCode;
}