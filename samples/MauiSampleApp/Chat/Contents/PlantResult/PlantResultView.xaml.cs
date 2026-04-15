using Microsoft.Extensions.AI.Maui.Chat;

namespace MauiSampleApp.Chat;

public partial class PlantResultView : ContentContextView
{
    private readonly PlantResultViewModel _vm = new();
    public PlantResultViewModel ViewModel => _vm;

    public PlantResultView()
    {
        InitializeComponent();
    }

    protected override void RefreshFromContentContext()
    {
        if (ContentContext is not null)
            _vm.ApplyContentContext(ContentContext);
    }
}
