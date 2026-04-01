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

        // Make titlebar transparent — content extends into titlebar area
        window.TitleBar = new TitleBar
        {
            IsVisible = false
        };

        return window;
    }
}