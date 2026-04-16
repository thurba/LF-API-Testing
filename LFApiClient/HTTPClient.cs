namespace LFApiClient;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class HTTPClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<APISettings> _apiOptions;
    private readonly ILogger<HTTPClient> _logger;

    public HTTPClient(ILogger<HTTPClient> logger, IHttpClientFactory httpClientFactory, IOptions<APISettings> apiOptions)
    {
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _logger = logger;
        _logger.LogInformation("HTTPClient initialized with API server: {APIServer}", _apiOptions.Value.BaseUrl);
    }

    public async Task RefreshToken(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                grant_type = "password",
                username = _apiOptions.Value.Username,
                password = _apiOptions.Value.Password
                
            }),
            Encoding.UTF8,
            "application/json");

//TODO: add correct token request url and handle response to extract token and set it for future requests
        var response = await client.PostAsync(_apiOptions.Value.APIServer, content, cancellationToken);
    }


}