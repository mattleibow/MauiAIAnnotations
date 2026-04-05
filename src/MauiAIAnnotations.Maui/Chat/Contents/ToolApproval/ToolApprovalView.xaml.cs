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

    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }

    /// <summary>
    /// Optional inner content view type. When set, the wrapper resolves this view
    /// from DI (supporting constructor injection) and places it in the content slot.
    /// The view or its BindingContext can implement <see cref="IContentContextAware"/>
    /// to receive the <see cref="ContentContext"/> — like MAUI's IQueryAttributable.
    /// </summary>
    internal Type? InnerContentType { get; set; }

    private ContentContext? _ctx;
    private VisualElement? _stateRoot;
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
        ApplyVisualState();
        RefreshAutomationIds();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _stateRoot = GetTemplateChild("PART_Root") as VisualElement;
        _approveButton = GetTemplateChild("PART_ApproveButton") as Button;
        _rejectButton = GetTemplateChild("PART_RejectButton") as Button;
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

        // ContentPresenter in the ControlTemplate renders this
        Content = innerView;
    }

    private void ApplyVisualState()
    {
        VisualStateManager.GoToState(_stateRoot ?? this, IsResolved ? "Resolved" : "Pending");
    }

    private void RefreshAutomationIds()
    {
        var suffix = GetApprovalAutomationSuffix();

        if (_approveButton is not null)
            _approveButton.AutomationId = IsResolved ? $"{ActiveApproveAutomationId}_{suffix}" : ActiveApproveAutomationId;

        if (_rejectButton is not null)
            _rejectButton.AutomationId = IsResolved ? $"{ActiveRejectAutomationId}_{suffix}" : ActiveRejectAutomationId;

        if (IsResolved && Content is not null)
            SuffixAutomationIds(Content, suffix);
    }

    private string GetApprovalAutomationSuffix()
    {
        if (_ctx?.Content is ToolApprovalRequestContent request && !string.IsNullOrWhiteSpace(request.RequestId))
            return request.RequestId[..Math.Min(8, request.RequestId.Length)];

        return "resolved";
    }

    private static void SuffixAutomationIds(Element element, string suffix)
    {
        if (element is VisualElement visual &&
            !string.IsNullOrWhiteSpace(visual.AutomationId) &&
            !visual.AutomationId.EndsWith($"_{suffix}", StringComparison.Ordinal))
        {
            visual.AutomationId = $"{visual.AutomationId}_{suffix}";
        }

        switch (element)
        {
            case Border border when border.Content is not null:
                SuffixAutomationIds(border.Content, suffix);
                break;

            case ContentView contentView when contentView.Content is not null:
                SuffixAutomationIds(contentView.Content, suffix);
                break;

            case ScrollView scrollView when scrollView.Content is not null:
                SuffixAutomationIds(scrollView.Content, suffix);
                break;

            case Layout layout:
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement)
                        SuffixAutomationIds(childElement, suffix);
                }
                break;
        }
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
                "This approval view is not connected to a chat approval responder. Ensure the ContentContext was created by ChatViewModel.");

        _ctx.ApprovalResponder(request, approved);
    }
}
