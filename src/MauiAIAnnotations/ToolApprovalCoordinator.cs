using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// Default in-memory coordinator for approval-required tool calls.
/// </summary>
public sealed class ToolApprovalCoordinator : IToolApprovalCoordinator
{
    private readonly object _syncLock = new();
    private PendingApprovalBatch? _pendingBatch;

    /// <inheritdoc />
    public bool HasPendingApprovals
    {
        get
        {
            lock (_syncLock)
            {
                return _pendingBatch is not null;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? PendingApprovalsChanged;

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ToolApprovalResponseContent>> WaitForApprovalAsync(
        IReadOnlyList<ToolApprovalRequestContent> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            return [];
        }

        PendingApprovalBatch batch;
        lock (_syncLock)
        {
            if (_pendingBatch is not null)
            {
                throw new InvalidOperationException(
                    "A tool approval flow is already in progress. Complete or cancel the current approvals before starting a new batch.");
            }

            batch = new PendingApprovalBatch(requests);
            _pendingBatch = batch;
        }

        OnPendingApprovalsChanged();

        try
        {
            using var registration = cancellationToken.Register(static state =>
            {
                ((PendingApprovalBatch)state!).TrySetCanceled();
            }, batch);

            return await batch.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_syncLock)
            {
                if (ReferenceEquals(_pendingBatch, batch))
                {
                    _pendingBatch = null;
                }
            }

            OnPendingApprovalsChanged();
        }
    }

    /// <inheritdoc />
    public bool TrySubmit(ToolApprovalResponseContent response)
    {
        ArgumentNullException.ThrowIfNull(response);

        lock (_syncLock)
        {
            return _pendingBatch?.TryAddResponse(response) ?? false;
        }
    }

    /// <inheritdoc />
    public void CancelPending()
    {
        PendingApprovalBatch? batch;
        lock (_syncLock)
        {
            batch = _pendingBatch;
            _pendingBatch = null;
        }

        batch?.TrySetCanceled();
        OnPendingApprovalsChanged();
    }

    private void OnPendingApprovalsChanged() => PendingApprovalsChanged?.Invoke(this, EventArgs.Empty);

    private sealed class PendingApprovalBatch
    {
        private readonly TaskCompletionSource<IReadOnlyList<ToolApprovalResponseContent>> _taskCompletionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IReadOnlyList<ToolApprovalRequestContent> _requests;
        private readonly Dictionary<string, ToolApprovalRequestContent> _requestsById;
        private readonly Dictionary<string, ToolApprovalResponseContent> _responses;

        public PendingApprovalBatch(IReadOnlyList<ToolApprovalRequestContent> requests)
        {
            _requests = requests.ToArray();
            _requestsById = requests.ToDictionary(static request => request.RequestId, StringComparer.Ordinal);
            _responses = new Dictionary<string, ToolApprovalResponseContent>(StringComparer.Ordinal);
        }

        public Task<IReadOnlyList<ToolApprovalResponseContent>> Task => _taskCompletionSource.Task;

        public bool TryAddResponse(ToolApprovalResponseContent response)
        {
            if (_taskCompletionSource.Task.IsCompleted ||
                !_requestsById.TryGetValue(response.RequestId, out var request) ||
                !IsValidResponseForRequest(request, response))
            {
                return false;
            }

            if (!_responses.TryAdd(response.RequestId, response))
            {
                return false;
            }

            if (_responses.Count == _requests.Count)
            {
                var orderedResponses = _requests
                    .Select(request => _responses[request.RequestId])
                    .ToArray();
                _taskCompletionSource.TrySetResult(orderedResponses);
            }

            return true;
        }

        public void TrySetCanceled() => _taskCompletionSource.TrySetCanceled();

        private static bool IsValidResponseForRequest(
            ToolApprovalRequestContent request,
            ToolApprovalResponseContent response)
        {
            if (response.ToolCall is null)
            {
                return true;
            }

            if (request.ToolCall is FunctionCallContent originalCall &&
                response.ToolCall is FunctionCallContent editedCall)
            {
                return string.Equals(editedCall.CallId, originalCall.CallId, StringComparison.Ordinal) &&
                       string.Equals(editedCall.Name, originalCall.Name, StringComparison.OrdinalIgnoreCase);
            }

            return request.ToolCall?.GetType() == response.ToolCall.GetType();
        }
    }
}
