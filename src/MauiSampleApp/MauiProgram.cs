using System.ClientModel;
using System.Reflection;
using MauiDevFlow.Agent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MauiSampleApp.Core.Services;
using MauiSampleApp.ViewModels;
using OpenAI;

namespace MauiSampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Load user secrets embedded as resource
        builder.Configuration.AddUserSecrets();

        // Register MauiDevFlow
#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        // Register services
        builder.Services.AddHttpClient<WeatherService>();

        // Register AI
        builder.AddOpenAIServices();

        // Register ViewModels and Pages
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<ChatViewModel>();
        builder.Services.AddSingleton<ChatPage>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void AddUserSecrets(this ConfigurationManager manager)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // The embedded resource name is based on the UserSecretsId path
        // Try all manifest resource names to find the secrets.json
        var resourceNames = assembly.GetManifestResourceNames();
        var secretsResource = resourceNames.FirstOrDefault(n => n.EndsWith("secrets.json"));
        if (secretsResource is not null)
        {
            var stream = assembly.GetManifestResourceStream(secretsResource);
            if (stream is not null)
                manager.AddJsonStream(stream);
        }
    }

    private static MauiAppBuilder AddOpenAIServices(this MauiAppBuilder builder)
    {
        var aiSection = builder.Configuration.GetSection("AI");
        var apiKey = aiSection["ApiKey"];
        var endpoint = aiSection["Endpoint"];
        var deploymentName = aiSection["DeploymentName"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
        {
            // AI not configured — register a null/stub so app still starts
            return builder;
        }

        var client = new OpenAI.Chat.ChatClient(
            model: deploymentName,
            credential: new ApiKeyCredential(apiKey),
            options: new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        builder.Services.AddSingleton<IChatClient>(provider =>
        {
            var lf = provider.GetRequiredService<ILoggerFactory>();
            return client.AsIChatClient()
                .AsBuilder()
                .UseLogging(lf)
                .UseFunctionInvocation()
                .Build();
        });

        return builder;
    }
}
