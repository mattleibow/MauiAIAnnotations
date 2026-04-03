using System.ComponentModel;
using System.Windows.Input;
using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Standalone C# ContentView for tool approval requests.
/// Subscribes to ContentContext.PropertyChanged to track
/// Content and ApprovalResolved changes. No XAML backing — styled via ControlTemplate.
/// </summary>
public class ToolApprovalView : ContentView
{
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
            RefreshToolName();
        if (e.PropertyName is nameof(ContentContext.ApprovalResolved) or nameof(ContentContext.ApprovalResolutionText))
            RefreshApprovalState();
    }

    private void Refresh()
    {
        RefreshToolName();
        RefreshApprovalState();
        BuildInnerContent();
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
        if (IsResolved)
            VisualStateManager.GoToState(this, "Resolved");
    }

    private void BuildInnerContent()
    {
        if (_ctx is null)
            return;

        // Find the PART_ContentSlot in the ControlTemplate (if template applied)
        // For now, build inner content and set as Content (visual child)
        if (InnerContentType is not null)
        {
            var services = Handler?.MauiContext?.Services;
            var innerView = services is not null
                ? (View)(services.GetService(InnerContentType) ?? Activator.CreateInstance(InnerContentType)!)
                : (View)Activator.CreateInstance(InnerContentType)!;

            var aware = innerView as IContentContextAware ?? innerView.BindingContext as IContentContextAware;
            aware?.ApplyContentContext(_ctx);

            Content = innerView;
        }
        else
        {
            Content = BuildDefaultArgsView();
        }

        if (_ctx.ApprovalResolved)
        {
            if (Content is View v)
                v.IsEnabled = false;
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

        var args = approved && request.ToolCall is FunctionCallContent fc ? fc.Arguments : null;

        var chatVm = FindChatViewModel();
        chatVm?.RespondToApproval(request, approved, args);
    }

    private ChatViewModel? FindChatViewModel()
    {
        Element? current = this;
        while (current is not null)
        {
            if (current is Controls.ChatPanelControl panel)
                return panel.ChatVM;
            current = current.Parent;
        }
        return null;
    }
}
