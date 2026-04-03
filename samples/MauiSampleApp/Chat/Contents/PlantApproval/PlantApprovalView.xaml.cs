using MauiAIAnnotations.Maui.Chat;

namespace MauiSampleApp.Chat;

/// <summary>
/// Content-only approval view for add_plant.
/// Provides editable fields; the library wrapper handles header + buttons.
/// </summary>
public partial class PlantApprovalView : ContentView, IApprovalContentProvider
{
    private readonly PlantApprovalViewModel _vm = new();

    public PlantApprovalView()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    public void Initialize(IDictionary<string, object?>? arguments)
    {
        if (arguments is null) return;

        _vm.Nickname = arguments.TryGetValue("nickname", out var n) ? n?.ToString() ?? "" : "";
        _vm.Species = arguments.TryGetValue("species", out var s) ? s?.ToString() ?? "" : "";
        _vm.Location = arguments.TryGetValue("location", out var l) ? l?.ToString() ?? "" : "";
        _vm.IsIndoor = arguments.TryGetValue("isIndoor", out var i) && i is true;
    }

    public IDictionary<string, object?> GetArguments() => _vm.BuildArguments();

    public void SetReadOnly(bool readOnly)
    {
        NicknameEntry.IsEnabled = !readOnly;
        SpeciesEntry.IsEnabled = !readOnly;
        LocationEntry.IsEnabled = !readOnly;
        IndoorSwitch.IsEnabled = !readOnly;
    }
}
