namespace MauiSampleApp;

public partial class App : Application
{
	private readonly IServiceProvider _serviceProvider;

	public App(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			var mainPage = _serviceProvider.GetRequiredService<MainPage>();
			return new Window(mainPage);
		}
		catch (Exception ex)
		{
			System.IO.File.WriteAllText(
				System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mauisampleapp-crash.txt"),
				$"{DateTime.Now}: CreateWindow: {ex}\n");
			throw;
		}
	}
}