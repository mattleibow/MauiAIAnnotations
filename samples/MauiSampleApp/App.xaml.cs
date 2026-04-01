namespace MauiSampleApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        var debugButton = new Button
        {
            Text = "⚙",
            FontSize = 16,
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Center,
        };
        debugButton.Clicked += async (_, _) =>
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("Debug");
        };

        window.TitleBar = new TitleBar
        {
            Title = "Garden Helper",
            Subtitle = "Your AI-powered plant companion",
            ForegroundColor = Colors.White,
            BackgroundColor = Color.FromArgb("#5B8C5A"),
            HeightRequest = 40,
            TrailingContent = debugButton,
        };

        return window;
    }
}