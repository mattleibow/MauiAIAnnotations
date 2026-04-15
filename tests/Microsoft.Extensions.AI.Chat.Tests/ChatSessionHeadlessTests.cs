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

    [Fact]
    public async Task Sending_new_message_while_approval_pending_auto_rejects_and_continues()
    {
        var request = new ToolApprovalRequestContent(
            "approval-auto-reject-1",
            new FunctionCallContent(
                "call-auto-reject-1",
                "add_plant",
                new Dictionary<string, object?> { ["nickname"] = "Fern" }));

        var innerClient = new SequenceChatClient(
            [new ChatResponseUpdate(ChatRole.Assistant, [request])],
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Got it, moving on.")])]);

        var session = new ChatSession([], innerClient);

        await session.SendAsync("Add a fern");
        Assert.True(session.HasPendingApprovals);

        // Send a new message while approval is still pending — should auto-reject
        await session.SendAsync("Never mind, do something else");

        Assert.False(session.HasPendingApprovals);
        Assert.Equal(ToolApprovalState.Rejected, session.Messages.Single(m => m.Role == ContentRole.Approval).ApprovalState);

        // The rejection response should have been added to the history sent to the inner client
        var lastHistory = innerClient.ReceivedMessages[^1];
        Assert.Contains(lastHistory, static m => m.Contents.OfType<ToolApprovalResponseContent>().Any(r => !r.Approved));
    }

    [Fact]
    public async Task Follow_up_message_after_approved_tool_call_does_not_crash()
    {
        // Reproduces the exact crash scenario: Send → Approve → tool executes → Send again
        // The FunctionInvokingChatClient middleware sets InformationalOnly=true on the
        // response's FCC but not the request's FCC, causing a validation mismatch
        // on the next turn if resolved approval content remains in the history.

        var plantsDb = new List<string>();

        var addPlantTool = AIFunctionFactory.Create(
            (string nickname) => { plantsDb.Add(nickname); return $"Added {nickname}"; },
            new AIFunctionFactoryOptions
            {
                Name = "add_plant",
            });
        var addPlantApproval = new ApprovalRequiredAIFunction(addPlantTool);

        var getPlantsTool = AIFunctionFactory.Create(
            () => plantsDb.Count > 0 ? string.Join(", ", plantsDb) : "No plants yet",
            new AIFunctionFactoryOptions
            {
                Name = "get_plants",
            });

        var tools = new AITool[] { addPlantApproval, getPlantsTool };

        int innerCallCount = 0;
        var innerClient = new CallbackChatClient(updates =>
        {
            innerCallCount++;
            return innerCallCount switch
            {
                // Turn 1: AI decides to call add_plant
                1 => [new ChatResponseUpdate(ChatRole.Assistant, [
                    new FunctionCallContent("call-1", "add_plant",
                        new Dictionary<string, object?> { ["nickname"] = "Fern" })])],
                // Turn 2 (after approval + execution): AI responds with text
                2 => [new ChatResponseUpdate(ChatRole.Assistant, [
                    new TextContent("Done! I've added Fern to your garden.")])],
                // Turn 3: AI decides to call get_plants
                3 => [new ChatResponseUpdate(ChatRole.Assistant, [
                    new FunctionCallContent("call-2", "get_plants")])],
                // Turn 4 (after get_plants executes): AI responds with text
                4 => [new ChatResponseUpdate(ChatRole.Assistant, [
                    new TextContent("You have: Fern")])],
                _ => [new ChatResponseUpdate(ChatRole.Assistant, [
                    new TextContent("...")])],
            };
        });

        using var pipeline = new ChatClientBuilder(innerClient)
            .UseFunctionInvocation()
            .Build();

        var session = new ChatSession(tools, pipeline);

        // Step 1: Send initial message — middleware intercepts add_plant, yields approval request
        await session.SendAsync("Add a fern plant");
        Assert.True(session.HasPendingApprovals, "Should have pending approval for add_plant");

        // Step 2: Approve the tool call
        var pending = session.PendingApprovals.Single();
        var request = (ToolApprovalRequestContent)pending.Content;
        await session.SubmitApprovalAsync(request.CreateResponse(approved: true));

        Assert.False(session.HasPendingApprovals);
        Assert.Contains("Fern", plantsDb);

        // Step 3: Send a follow-up message — THIS PREVIOUSLY CRASHED with:
        // "ToolApprovalRequestContent found with FunctionCall.CallId(s) '...'
        //  that have no matching ToolApprovalResponseContent"
        await session.SendAsync("What plants do I have?");

        // Verify the follow-up worked
        Assert.Contains(session.Messages,
            static m => m.Content is TextContent { Text: "You have: Fern" });
    }
}
