using System.ClientModel;
using System.Reflection;
using Azure.AI.OpenAI;
using Microsoft.Maui.DevFlow.Agent;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.AI.Chat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MauiSampleApp.Core.Services;
using MauiSampleApp.Chat;
using MauiSampleApp.Pages;
using MauiSampleApp.Services;
using MauiSampleApp.ViewModels;
using Shiny.DocumentDb.Sqlite;

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

        // Register MAUI DevFlow
#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        // Register SQLite document store
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "garden.db");
        builder.Services.AddSqliteDocumentStore($"Data Source={dbPath}");

        // Register services
        builder.Services.AddSingleton<SpeciesService>();
        builder.Services.AddSingleton<PlantDataService>();
        builder.Services.AddSingleton<SeasonsService>();

        // ── AI Tools ────────────────────────────────────────────────
        //
        // 1. Source-generated tools (from [ExportAIFunction] on services)
        //    GardenTools aggregates PlantDataService, SeasonsService, and
        //    SpeciesService tools discovered at compile time.
        builder.Services.AddAITools<GardenTools>();

        // 2. Classic / bespoke tools (hand-crafted with AIFunctionFactory)
        //    These demonstrate the "old school" pattern used in apps like
        //    BaristaNotes — AIFunctionFactory.Create with a delegate.
        //    Both styles coexist: DI aggregates them all into IEnumerable<AITool>.
        builder.Services.AddSingleton<AITool>(
            AIFunctionFactory.Create(
                () => DateTime.Now.ToString("dddd, MMMM d yyyy, h:mm tt"),
                "get_current_datetime",
                "Gets the current date and time. Useful for checking when a plant was last watered relative to now."));

        // ── End AI Tools ────────────────────────────────────────────

        // Register AI
        builder.AddOpenAIServices();

        // Register the headless chat engine; the MAUI controls bind to it as a thin UI layer.
        builder.Services.AddChatSession(ServiceLifetime.Transient);

        // Register ViewModels and Pages
        builder.Services.AddSingleton<HomePageViewModel>();
        builder.Services.AddTransient<PlantDetailViewModel>();
        builder.Services.AddTransient<AddPlantViewModel>();
        builder.Services.AddTransient<DebugViewModel>();
        builder.Services.AddTransient<PlantApprovalViewModel>();
        builder.Services.AddTransient<BatchCareApprovalViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<PlantDetailPage>();
        builder.Services.AddTransient<AddPlantPage>();
        builder.Services.AddTransient<PlantApprovalView>();
        builder.Services.AddTransient<BatchCareApprovalView>();
        builder.Services.AddTransient<DebugPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void AddUserSecrets(this ConfigurationManager manager)
    {
        var assembly = Assembly.GetExecutingAssembly();
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
            throw new InvalidOperationException(
                """
                AI services are not configured. Please set up user secrets by running these commands from the MauiSampleApp project directory:

                  cd samples/MauiSampleApp
                  dotnet user-secrets set "AI:ApiKey" "<your-azure-openai-api-key>"
                  dotnet user-secrets set "AI:Endpoint" "<your-azure-openai-endpoint>"
                  dotnet user-secrets set "AI:DeploymentName" "<your-deployment-name>"

                For more info, see the README.md.
                """);
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
                .Build(provider);
        });

        return builder;
    }
}
