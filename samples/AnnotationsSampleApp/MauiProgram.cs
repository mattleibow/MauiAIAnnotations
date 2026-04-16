using System.ClientModel;
using System.Reflection;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.DevFlow.Agent;
using AnnotationsSampleApp.Services;

namespace AnnotationsSampleApp;

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

#if DEBUG
        builder.AddMauiDevFlowAgent();
#endif

        // ── Services ────────────────────────────────────────────────
        //
        // Different lifetimes demonstrate how DI scoping interacts with
        // the source-generated tool contexts:

        // Singleton: shared across ALL chat sessions. The plant catalog
        // is static data that never changes, so one instance is enough.
        builder.Services.AddSingleton<PlantCatalogService>();

        // Scoped: one instance per DI scope. Each chat session creates
        // its own scope, so the user's garden resets on "New Chat".
        builder.Services.AddScoped<GardenService>();

        // Transient: a brand-new instance every time it's resolved.
        // WeatherService is stateless, so this is fine.
        builder.Services.AddTransient<WeatherService>();

        // ── AI Tools (source-generated) ─────────────────────────────
        //
        // The source generator discovers [ExportAIFunction] methods at
        // compile time. No runtime reflection is used.

        // 1. Default tool set: all garden tools (catalog + garden + weather)
        //    Resolved via IEnumerable<AITool> from DI.
        builder.Services.AddAITools<AllGardenTools>();

        // 2. Keyed tool set: garden management only
        //    Resolved via sp.GetKeyedServices<AITool>("management")
        builder.Services.AddAITools<GardenManagementTools>("management");

        // 3. Hand-crafted tool (no source generator needed)
        //    Coexists alongside generated tools in IEnumerable<AITool>.
        builder.Services.AddSingleton<AITool>(
            AIFunctionFactory.Create(
                () => DateTime.Now.ToString("dddd, MMMM d yyyy, h:mm tt"),
                "get_current_datetime",
                "Gets the current date and time."));

        // 4. On-demand tool sets (not registered in DI)
        //    CatalogTools.Default.GetTools(sp) can be called at any time
        //    to get a read-only tool set without DI registration.

        // ── AI Client ───────────────────────────────────────────────
        builder.AddOpenAIServices();

        // ── Pages ───────────────────────────────────────────────────
        builder.Services.AddTransient<MainPage>();

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
                AI services are not configured. Set up user secrets:

                  cd samples/AnnotationsSampleApp
                  dotnet user-secrets set "AI:ApiKey" "<your-key>"
                  dotnet user-secrets set "AI:Endpoint" "<your-endpoint>"
                  dotnet user-secrets set "AI:DeploymentName" "<your-deployment>"
                """);
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey));
        var chatClient = azureClient.GetChatClient(deploymentName);

        // Register the raw IChatClient (no middleware).
        // MainPage builds the FunctionInvokingChatClient pipeline per session
        // so each session scope's services are used for tool resolution.
        builder.Services.AddSingleton<IChatClient>(chatClient.AsIChatClient());

        return builder;
    }
}
