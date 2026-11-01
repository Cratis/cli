// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed class AiTestProcessOutput(Process process, CancellationToken cancellationToken) : IDisposable
{
    readonly CancellationTokenSource _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    readonly AiTestStreamCapture _output = new(process.StandardOutput);
    readonly AiTestStreamCapture _error = new(process.StandardError);

    internal string Output => _output.Text + _error.Text;
    internal string StandardError => _error.Text;

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    internal Task Drain() => Task.WhenAll(_output.Drain(_cancellation.Token), _error.Drain(_cancellation.Token));
}

internal sealed class AiTestStreamCapture(StreamReader reader)
{
    internal const int Limit = 64 * 1024;
    readonly StringBuilder _text = new();
    readonly object _lock = new();

    internal string Text
    {
        get
        {
            lock (_lock) return _text.ToString();
        }
    }

    internal async Task Drain(CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        try
        {
            while (true)
            {
                var count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (count == 0) return;
                lock (_lock) _text.Append(buffer, 0, Math.Min(count, Limit - _text.Length));
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or IOException)
        {
            // Cancellation/disposal closes pending pipe reads; captured bytes remain available.
        }
    }
}
