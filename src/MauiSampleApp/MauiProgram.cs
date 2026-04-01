using System.ClientModel;
using System.Reflection;
using Azure.AI.OpenAI;
using MauiDevFlow.Agent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MauiSampleApp.Core.Services;
using MauiSampleApp.ViewModels;

namespace MauiSampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            return CreateMauiAppCore();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "maui_crash.log");
            File.AppendAllText(logPath,
                $"[{DateTime.Now}] CreateMauiApp CRASH: {ex}\n\n");
            throw;
        }
    }

    private static MauiApp CreateMauiAppCore()
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
        builder.Services.AddTransient<MainPage>();

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
            // AI not configured — register a no-op stub so app still starts
            builder.Services.AddSingleton<IChatClient>(new NoOpChatClient());
            return builder;
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey));
        var chatClient = azureClient.GetChatClient(deploymentName);

        builder.Services.AddSingleton<IChatClient>(provider =>
        {
            var lf = provider.GetRequiredService<ILoggerFactory>();
            return chatClient.AsIChatClient()
                .AsBuilder()
                .UseLogging(lf)
                .UseFunctionInvocation()
                .Build();
        });

        return builder;
    }
}
