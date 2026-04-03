using MauiAIAnnotations.Maui.Chat;

namespace MauiSampleApp.Chat;

/// <summary>
/// Content-only approval view for batch care events.
/// Shows checkboxes for each care item — user unchecks ones they didn't do.
/// </summary>
public partial class BatchCareApprovalView : ContentView, IApprovalContentProvider
{
    private readonly BatchCareApprovalViewModel _vm = new();

    public BatchCareApprovalView()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    public void Initialize(IDictionary<string, object?>? arguments)
    {
        _vm.LoadFromArguments(arguments);
    }

    public IDictionary<string, object?> GetArguments() => _vm.BuildArguments();

    public void SetReadOnly(bool readOnly)
    {
        IsEnabled = !readOnly;
    }
}
