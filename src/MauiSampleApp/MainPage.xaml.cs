using MauiSampleApp.ViewModels;

namespace MauiSampleApp;

public partial class MainPage : ContentPage
{
	public MainPage(WeatherViewModel weather, ChatViewModel chat)
	{
		InitializeComponent();
		BindingContext = new MainPageViewModel(weather, chat);
	}
}

public class MainPageViewModel(WeatherViewModel weather, ChatViewModel chat)
{
	public WeatherViewModel Weather { get; } = weather;
	public ChatViewModel Chat { get; } = chat;
}
