using TWGARProcessorService;
using Microsoft.Extensions.Options;
using LFApiClient;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        if (OperatingSystem.IsWindows())
        {
            // Enabe Windows support only if on Windows + running as service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "ARProcessorService";
            });

        }
        
        builder.Services.AddHostedService<Worker>();

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<ApiClient>(sp =>
            new ApiClient(
                sp.GetRequiredService<ILogger<ApiClient>>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IOptions<APISettings>>()   
                ));

        builder.Logging.ClearProviders();
        builder.Logging.AddLog4Net("log4net.config");

        builder.Services.Configure<ARProcessorSettings>(builder.Configuration.GetSection("ARProcessor"));
        builder.Services.Configure<APISettings>(builder.Configuration.GetSection("LaserficheAPI"));
        var host = builder.Build();
        host.Run();
    }
}