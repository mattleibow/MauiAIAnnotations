using System.ComponentModel;
using System.Windows.Input;
using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Standalone C# ContentView for tool approval requests.
/// Subscribes to ContentContext.PropertyChanged to track
/// Content and ApprovalResolved changes. No XAML backing — styled via ControlTemplate.
/// Custom templates can include a root named <c>PART_Root</c>; if omitted,
/// the view falls back to applying visual states to itself.
/// </summary>
public class ToolApprovalView : ContentView
{
    private const string ActiveApproveAutomationId = "ApproveToolButton";
    private const string ActiveRejectAutomationId = "RejectToolButton";

    public static readonly BindableProperty ToolNameProperty =
        BindableProperty.Create(nameof(ToolName), typeof(string), typeof(ToolApprovalView));

    public string? ToolName
    {
        get => (string?)GetValue(ToolNameProperty);
        set => SetValue(ToolNameProperty, value);
    }

    public static readonly BindableProperty IsPendingProperty =
        BindableProperty.Create(nameof(IsPending), typeof(bool), typeof(ToolApprovalView), true);

    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    public static readonly BindableProperty IsResolvedProperty =
        BindableProperty.Create(nameof(IsResolved), typeof(bool), typeof(ToolApprovalView));

    public bool IsResolved
    {
        get => (bool)GetValue(IsResolvedProperty);
        set => SetValue(IsResolvedProperty, value);
    }

    public static readonly BindableProperty ResolutionTextProperty =
        BindableProperty.Create(nameof(ResolutionText), typeof(string), typeof(ToolApprovalView));

    public string? ResolutionText
    {
        get => (string?)GetValue(ResolutionTextProperty);
        set => SetValue(ResolutionTextProperty, value);
    }

    public static readonly BindableProperty ApproveCommandProperty =
        BindableProperty.Create(nameof(ApproveCommand), typeof(ICommand), typeof(ToolApprovalView));

    public ICommand ApproveCommand
    {
        get => (ICommand)GetValue(ApproveCommandProperty);
        set => SetValue(ApproveCommandProperty, value);
    }

    public static readonly BindableProperty RejectCommandProperty =
        BindableProperty.Create(nameof(RejectCommand), typeof(ICommand), typeof(ToolApprovalView));

    public ICommand RejectCommand
    {
        get => (ICommand)GetValue(RejectCommandProperty);
        set => SetValue(RejectCommandProperty, value);
    }

    /// <summary>
    /// Optional inner content view type. When set, the wrapper resolves this view
    /// from DI (supporting constructor injection) and places it in the content slot.
    /// The view or its BindingContext can implement <see cref="IContentContextAware"/>
    /// to receive the <see cref="ContentContext"/> — like MAUI's IQueryAttributable.
    /// </summary>
    internal Type? InnerContentType { get; set; }

    private ContentContext? _ctx;
    private VisualElement? _stateRoot;
    private VisualElement? _buttonsRow;
    private VisualElement? _resolutionLabel;
    private Button? _approveButton;
    private Button? _rejectButton;

    public ToolApprovalView()
    {
        ApproveCommand = new Command(() => Respond(true));
        RejectCommand = new Command(() => Respond(false));
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_ctx is not null)
            _ctx.PropertyChanged -= OnCtxChanged;
        _ctx = BindingContext as ContentContext;
        if (_ctx is not null)
        {
            _ctx.PropertyChanged += OnCtxChanged;
            Refresh();
        }
    }

    private void OnCtxChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ContentContext.Content))
            Refresh();
        if (e.PropertyName is nameof(ContentContext.ApprovalResolved) or nameof(ContentContext.ApprovalResolutionText))
            RefreshApprovalState();
    }

    private void Refresh()
    {
        RefreshToolName();
        BuildInnerContent();
        RefreshApprovalState();
    }

    private void RefreshToolName()
    {
        if (_ctx?.Content is ToolApprovalRequestContent approval &&
            approval.ToolCall is FunctionCallContent fc)
        {
            ToolName = fc.Name;
        }
    }

    private void RefreshApprovalState()
    {
        IsPending = _ctx is not null && !_ctx.ApprovalResolved;
        IsResolved = _ctx?.ApprovalResolved ?? false;
        ResolutionText = _ctx?.ApprovalResolutionText;

        if (_buttonsRow is not null)
        {
            _buttonsRow.IsVisible = !IsResolved;
            _buttonsRow.IsEnabled = !IsResolved;
        }

        if (_resolutionLabel is not null)
            _resolutionLabel.IsVisible = IsResolved;

        if (Content is View innerView)
            innerView.IsEnabled = !IsResolved;

        ApplyVisualState();
        RefreshAutomationIds();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _stateRoot = GetTemplateChild("PART_Root") as VisualElement;
        _buttonsRow = GetTemplateChild("ButtonsRow") as VisualElement;
        _resolutionLabel = GetTemplateChild("ResolutionLabel") as VisualElement;
        _approveButton = GetTemplateChild("PART_ApproveButton") as Button;
        _rejectButton = GetTemplateChild("PART_RejectButton") as Button;

        if (_ctx is not null)
            Refresh();

        ApplyVisualState();
        RefreshAutomationIds();
    }

    private void BuildInnerContent()
    {
        if (_ctx is null)
            return;

        View innerView;
        if (InnerContentType is not null)
        {
            innerView = ContentTemplate.CreateView(InnerContentType, Handler?.MauiContext?.Services);

            var aware = innerView as IContentContextAware ?? innerView.BindingContext as IContentContextAware;
            aware?.ApplyContentContext(_ctx);
        }
        else
        {
            innerView = BuildDefaultArgsView();
        }

        if (_ctx.ApprovalResolved)
            innerView.IsEnabled = false;

        Content = innerView;
    }

    private void ApplyVisualState()
    {
        VisualStateManager.GoToState(_stateRoot ?? this, IsResolved ? "Resolved" : "Pending");
    }

    private void RefreshAutomationIds()
    {
        if (_approveButton is not null && string.IsNullOrWhiteSpace(_approveButton.AutomationId))
            _approveButton.AutomationId = ActiveApproveAutomationId;

        if (_rejectButton is not null && string.IsNullOrWhiteSpace(_rejectButton.AutomationId))
            _rejectButton.AutomationId = ActiveRejectAutomationId;
    }

    private View BuildDefaultArgsView()
    {
        if (_ctx?.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc ||
            fc.Arguments is null || fc.Arguments.Count == 0)
            return new Label { Text = "(no arguments)", FontSize = 12, TextColor = Colors.Gray };

        var stack = new VerticalStackLayout { Spacing = 4 };
        foreach (var kvp in fc.Arguments)
        {
            var row = new HorizontalStackLayout { Spacing = 6 };
            row.Add(new Label
            {
                Text = $"{kvp.Key}:", FontSize = 12,
                FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#7A7062")
            });
            row.Add(new Label
            {
                Text = kvp.Value?.ToString() ?? "(null)", FontSize = 12,
                TextColor = Color.FromArgb("#2C2416")
            });
            stack.Add(row);
        }
        return stack;
    }

    private void Respond(bool approved)
    {
        if (_ctx is null || _ctx.Content is not ToolApprovalRequestContent request)
            return;

        if (_ctx.ApprovalResponder is null)
            throw new InvalidOperationException(
                "This approval view is not connected to a tool approval coordinator. Ensure the chat client pipeline includes UseMauiToolApproval().");

        var response = ResolveResponseFactory()?.CreateApprovalResponse(request, approved)
            ?? request.CreateResponse(approved, approved ? null : "User rejected");

        if (!_ctx.ApprovalResponder(response))
            return;

        var toolName = ToolName ?? "Tool";
        _ctx.ApprovalResolved = true;
        _ctx.ApprovalResolutionText = response.Approved
            ? $"✅ Approved — {toolName}"
            : $"❌ Rejected — {toolName}";
    }

    private IToolApprovalResponseFactory? ResolveResponseFactory()
    {
        if (Content is IToolApprovalResponseFactory viewFactory)
            return viewFactory;

        return (Content as BindableObject)?.BindingContext as IToolApprovalResponseFactory;
    }
}
