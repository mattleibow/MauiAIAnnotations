using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Chat;

namespace Microsoft.Extensions.AI.Chat.Tests;

public class ChatSessionHeadlessTests
{
    [Fact]
    public async Task Headless_session_surfaces_messages_and_approvals_without_ui_state_objects()
    {
        var request = new ToolApprovalRequestContent(
            "approval-headless-1",
            new FunctionCallContent(
                "call-headless-1",
                "add_plant",
                new Dictionary<string, object?> { ["nickname"] = "Fern" }));

        var innerClient = new SequenceChatClient(
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Let me check.")])],
            [new ChatResponseUpdate(ChatRole.Assistant, [request])],
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Added Fern.")])]);

        var session = new ChatSession([], innerClient);
        var changes = new List<ChatSessionChangeKind>();
        session.Changed += (_, args) => changes.Add(args.Kind);

        await session.SendAsync("Add a fern");

        Assert.Equal(2, session.Messages.Count);
        Assert.Equal(ContentRole.User, session.Messages[0].Role);
        Assert.Equal(ContentRole.Assistant, session.Messages[1].Role);
        Assert.Equal("Let me check.", ((TextContent)session.Messages[1].Content).Text);
        Assert.Contains(ChatSessionChangeKind.MessageAdded, changes);
        Assert.Contains(ChatSessionChangeKind.StateChanged, changes);

        await session.SendAsync("Please continue with approval");

        Assert.True(session.HasPendingApprovals);
        Assert.Single(session.PendingApprovals);
        Assert.Equal(ToolApprovalState.Pending, session.PendingApprovals.Single().ApprovalState);
        Assert.Equal("add_plant", session.PendingApprovals.Single().ToolName);

        await session.SubmitApprovalAsync(request.CreateResponse(approved: true));

        Assert.False(session.HasPendingApprovals);
        Assert.Equal(ToolApprovalState.Approved, session.Messages.Single(m => m.Role == ContentRole.Approval).ApprovalState);
        Assert.Contains(session.Messages, static message => message.Content is TextContent { Text: "Added Fern." });
    }

    [Fact]
    public async Task Headless_session_does_not_inject_a_default_system_prompt()
    {
        var innerClient = new SequenceChatClient([new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Hello!")])]);
        var session = new ChatSession([], innerClient);

        await session.SendAsync("Hi");

        Assert.Single(innerClient.ReceivedMessages);
        Assert.DoesNotContain(innerClient.ReceivedMessages[0], static message => message.Role == ChatRole.System);
        Assert.Null(session.SystemPrompt);
    }

    [Fact]
    public async Task Headless_session_preserves_edited_tool_call_arguments()
    {
        var originalCall = new FunctionCallContent(
            "call-edit-1",
            "add_plant",
            new Dictionary<string, object?> { ["nickname"] = "Old Name" });
        var request = new ToolApprovalRequestContent("approval-edit-1", originalCall);

        var innerClient = new SequenceChatClient(
            [new ChatResponseUpdate(ChatRole.Assistant, [request])],
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Updated nickname.")])]);

        var session = new ChatSession([], innerClient);
        await session.SendAsync("Add a plant");

        var editedResponse = new ToolApprovalResponseContent(
            request.RequestId,
            approved: true,
            new FunctionCallContent(
                originalCall.CallId,
                originalCall.Name,
                new Dictionary<string, object?> { ["nickname"] = "New Name" }));

        await session.SubmitApprovalAsync(editedResponse);

        var replayedResponse = Assert.IsType<ToolApprovalResponseContent>(
            innerClient.ReceivedMessages[1]
                .SelectMany(static message => message.Contents)
                .Single(static content => content is ToolApprovalResponseContent));

        var replayedCall = Assert.IsType<FunctionCallContent>(replayedResponse.ToolCall);
        Assert.Equal("New Name", replayedCall.Arguments?["nickname"]?.ToString());
    }

    [Fact]
    public async Task Headless_session_rejects_edited_tool_call_identity_changes()
    {
        var originalCall = new FunctionCallContent(
            "call-edit-2",
            "add_plant",
            new Dictionary<string, object?> { ["nickname"] = "Original" });
        var request = new ToolApprovalRequestContent("approval-edit-2", originalCall);

        var innerClient = new SequenceChatClient([new ChatResponseUpdate(ChatRole.Assistant, [request])]);
        var session = new ChatSession([], innerClient);

        await session.SendAsync("Add a plant");

        var invalidResponse = new ToolApprovalResponseContent(
            request.RequestId,
            approved: true,
            new FunctionCallContent(
                originalCall.CallId,
                "remove_plant",
                new Dictionary<string, object?> { ["nickname"] = "Original" }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => session.SubmitApprovalAsync(invalidResponse));

        Assert.Equal("Edited approval responses must preserve the original tool call identity.", ex.Message);
        Assert.True(session.HasPendingApprovals);
        Assert.Equal(ToolApprovalState.Pending, session.PendingApprovals.Single().ApprovalState);
    }

    [Fact]
    public async Task Multiple_headless_sessions_hold_pending_approvals_independently()
    {
        var request1 = new ToolApprovalRequestContent(
            "approval-session-1",
            new FunctionCallContent("call-session-1", "add_plant", new Dictionary<string, object?> { ["nickname"] = "Fern" }));
        var request2 = new ToolApprovalRequestContent(
            "approval-session-2",
            new FunctionCallContent("call-session-2", "add_plant", new Dictionary<string, object?> { ["nickname"] = "Palm" }));

        var session1 = new ChatSession([], new SequenceChatClient([new ChatResponseUpdate(ChatRole.Assistant, [request1])]));
        var session2 = new ChatSession([], new SequenceChatClient([new ChatResponseUpdate(ChatRole.Assistant, [request2])]));

        await session1.SendAsync("Add a fern");
        await session2.SendAsync("Add a palm");

        Assert.True(session1.HasPendingApprovals);
        Assert.True(session2.HasPendingApprovals);
        Assert.Equal("Fern", ((FunctionCallContent)((ToolApprovalRequestContent)session1.PendingApprovals.Single().Content).ToolCall!).Arguments?["nickname"]?.ToString());
        Assert.Equal("Palm", ((FunctionCallContent)((ToolApprovalRequestContent)session2.PendingApprovals.Single().Content).ToolCall!).Arguments?["nickname"]?.ToString());

        session1.Clear();

        Assert.False(session1.HasPendingApprovals);
        Assert.Empty(session1.Messages);
        Assert.True(session2.HasPendingApprovals);
        Assert.Single(session2.PendingApprovals);
    }
}
