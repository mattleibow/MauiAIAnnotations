using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MauiSampleApp.Core.Services;
using MauiSampleApp.ViewModels;
#if DEBUG
using MauiDevFlow.Agent;
#endif

namespace MauiSampleApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		try
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			// Load user secrets from embedded resource
			builder.Configuration.AddUserSecrets();

			// Register services
			builder.Services.AddHttpClient<WeatherService>();
			builder.Services.AddHttpClient<GeocodingService>();
			builder.Services.AddSingleton<WeatherViewModel>();
			builder.Services.AddSingleton<ChatViewModel>();
			builder.Services.AddTransient<MainPage>();

			// Register AI services
			builder.AddOpenAIServices();

#if DEBUG
			builder.Logging.AddDebug();
			builder.AddMauiDevFlowAgent();
#endif

			return builder.Build();
		}
		catch (Exception ex)
		{
			System.IO.File.WriteAllText(
				System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mauisampleapp-crash.txt"),
				$"{DateTime.Now}: {ex}\n");
			throw;
		}
	}

	private static Stream? AddUserSecrets(this ConfigurationManager manager)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var stream = assembly.GetManifestResourceStream("MauiSampleApp.secrets.json");
		if (stream is not null)
		{
			manager.AddJsonStream(stream);
		}
		return stream;
	}

	private static MauiAppBuilder AddOpenAIServices(this MauiAppBuilder builder)
	{
		var aiSection = builder.Configuration.GetSection("AI");
		var apiKey = aiSection["ApiKey"];
		var endpointStr = aiSection["Endpoint"];
		var deploymentName = aiSection["DeploymentName"];

		if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpointStr) || string.IsNullOrEmpty(deploymentName))
		{
			// AI services not configured - skip registration
			return builder;
		}

		var endpoint = new Uri(endpointStr);
		var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
			endpoint,
			new Azure.AzureKeyCredential(apiKey));
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
