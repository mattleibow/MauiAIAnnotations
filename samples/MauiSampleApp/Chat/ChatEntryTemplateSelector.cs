namespace MauiSampleApp.Chat;

/// <summary>
/// A <see cref="DataTemplateSelector"/> that picks the correct visual template
/// for each <see cref="ChatEntry"/> based on its <see cref="ChatEntryType"/>.
/// Set the individual template properties in XAML to define the appearance of
/// each entry type.
/// </summary>
public class ChatEntryTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTextTemplate { get; set; }
    public DataTemplate? AssistantTextTemplate { get; set; }
    public DataTemplate? ToolCallTemplate { get; set; }
    public DataTemplate? ToolResultTemplate { get; set; }
    public DataTemplate? ErrorTemplate { get; set; }
    public DataTemplate? FallbackTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ChatEntry entry)
        {
            var template = entry.Type switch
            {
                ChatEntryType.UserText => UserTextTemplate,
                ChatEntryType.AssistantText => AssistantTextTemplate,
                ChatEntryType.ToolCall => ToolCallTemplate,
                ChatEntryType.ToolResult => ToolResultTemplate,
                ChatEntryType.Error => ErrorTemplate,
                _ => FallbackTemplate,
            };
            if (template is not null)
                return template;
        }

        return FallbackTemplate ?? new DataTemplate(() => new Label { Text = item?.ToString() ?? "" });
    }
}
