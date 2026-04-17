namespace LFApiClient;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System;

public class HTTPClient
{
    private class TokenResponse
    {
        public string? access_token { get; set; }
        public string? token_type { get; set; }
        public int expires_in { get; set; }
    }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<APISettings> _apiOptions;
    private readonly ILogger<HTTPClient> _logger;
    private string _accessToken = "";
    private DateTime _tokenExpiration = DateTime.MinValue; 

    public HTTPClient(ILogger<HTTPClient> logger, IHttpClientFactory httpClientFactory, IOptions<APISettings> apiOptions)
    {
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _logger = logger;
        _logger.LogInformation("HTTPClient initialized with API server: {APIServer}", _apiOptions.Value.BaseUrl);
       
    }
            
    private async Task<bool> UpdateAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_tokenExpiration > DateTime.UtcNow.AddMinutes(1))
        {
            _logger.LogInformation("Access token is still valid. Expiration: {Expiration    }", _tokenExpiration);
            return true;     
        }
        
        var client = _httpClientFactory.CreateClient();
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = _apiOptions.Value.Username,
            ["password"] = _apiOptions.Value.Password
        };
        var content = new FormUrlEncodedContent(formData);
        var endpoint = $"{_apiOptions.Value.BaseUrl}/LFRepositoryAPI/v1/Repositories/{_apiOptions.Value.RepositoryId}/Token";
        _logger.LogInformation("Sending refresh token request to {Endpoint}", endpoint);
        
        var response = await client.PostAsync(endpoint, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Token refreshed successfully. Response: {Response}", responseContent);

            var tokenData = JsonSerializer.Deserialize<TokenResponse>(responseContent);
            _accessToken = tokenData.access_token;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenData.expires_in);
            _logger.LogInformation("Access token updated. Expires at: {Expiration}", _tokenExpiration);
            return true;
        }
        else
        {
            _logger.LogError("Failed to refresh token. Status Code: {StatusCode}, Response: {Response}", response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            return false;
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        if (!await UpdateAccessTokenAsync(cancellationToken))
        {
            throw new Exception("Unable to obtain access token.");
        }

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(method, $"{_apiOptions.Value.BaseUrl}/LFRepositoryAPI/v1/Repositories/{_apiOptions.Value.RepositoryId}/{endpoint}")
        {
            Content = content
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        _logger.LogInformation("Sending {Method} request to {Endpoint}", method, endpoint);
        var response = await client.SendAsync(request, cancellationToken);
        _logger.LogInformation("Received response with status code: {StatusCode}", response.StatusCode);
        return response;
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Get, endpoint, null, cancellationToken);
    }


}