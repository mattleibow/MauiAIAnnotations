namespace MauiSampleApp.Controls;

public partial class WeatherControl : ContentView
{
    public static readonly BindableProperty DateProperty =
        BindableProperty.Create(nameof(Date), typeof(string), typeof(WeatherControl), string.Empty);

    public static readonly BindableProperty EmojiProperty =
        BindableProperty.Create(nameof(Emoji), typeof(string), typeof(WeatherControl), "☁️");

    public static readonly BindableProperty TemperatureProperty =
        BindableProperty.Create(nameof(Temperature), typeof(double), typeof(WeatherControl), 0.0);

    public static readonly BindableProperty DescriptionProperty =
        BindableProperty.Create(nameof(Description), typeof(string), typeof(WeatherControl), string.Empty);

    public string Date
    {
        get => (string)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public string Emoji
    {
        get => (string)GetValue(EmojiProperty);
        set => SetValue(EmojiProperty, value);
    }

    public double Temperature
    {
        get => (double)GetValue(TemperatureProperty);
        set => SetValue(TemperatureProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public WeatherControl()
    {
        InitializeComponent();
    }
}
