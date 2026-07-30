// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Keeps the most recent lines the Stage container wrote. The output is hidden while the container starts,
/// so this is what is left to show the user when it fails before becoming ready.
/// </summary>
/// <param name="capacity">The number of lines to keep.</param>
public class StageOutput(int capacity = 200)
{
    readonly Queue<string> _lines = new();
    readonly Lock _lock = new();

    /// <summary>
    /// Appends a line, discarding the oldest one when the capacity is reached.
    /// </summary>
    /// <param name="line">The line the container wrote.</param>
    public void Append(string line)
    {
        lock (_lock)
        {
            _lines.Enqueue(line);
            while (_lines.Count > capacity)
            {
                _lines.Dequeue();
            }
        }
    }

    /// <summary>
    /// Gets the last lines the container wrote, oldest first.
    /// </summary>
    /// <param name="count">The maximum number of lines to return.</param>
    /// <returns>The lines, oldest first.</returns>
    public IReadOnlyList<string> Tail(int count)
    {
        lock (_lock)
        {
            return [.. _lines.Skip(Math.Max(0, _lines.Count - count))];
        }
    }
}
