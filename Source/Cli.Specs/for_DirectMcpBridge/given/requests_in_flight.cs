// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_DirectMcpBridge.given;

/// <summary>A request and a notification that Direct never answers until they are cancelled.</summary>
public class requests_in_flight : a_bridge
{
    protected const string CallTool = "{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"tools/call\",\"params\":{\"name\":\"slow\"}}";
    readonly TaskCompletionSource _inFlight = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _started;

    void Establish()
    {
        _direct.AnswerWhen("tools/call", Hang);
        _direct.AnswerWhen("notifications/initialized", Hang);
    }

    /// <summary>Waits until Direct holds both messages.</summary>
    /// <returns>A task completing when both are in flight.</returns>
    protected Task InFlight() => _inFlight.Task.WaitAsync(TimeSpan.FromSeconds(10));

    async Task<HttpResponseMessage> Hang(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _started) == 2)
        {
            _inFlight.TrySetResult();
        }

        await Task.Delay(Timeout.Infinite, cancellationToken);
        return Status(HttpStatusCode.OK);
    }

    /// <summary>Sends lines, then ends the input the way the specification decides.</summary>
    /// <param name="end">Produces the final read: null to end the input, or an exception.</param>
    /// <param name="lines">The lines to send first.</param>
    protected sealed class Input(Func<CancellationToken, Task<string?>> end, params string[] lines) : TextReader
    {
        int _next;

        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
            _next < lines.Length ? ValueTask.FromResult<string?>(lines[_next++]) : new(end(cancellationToken));
    }
}
