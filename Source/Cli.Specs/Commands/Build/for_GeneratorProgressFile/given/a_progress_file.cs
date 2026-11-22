// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Cli.Commands.Build.for_GeneratorProgressFile.given;

public class a_progress_file : Specification
{
    protected string _root;

    private protected static Channel<GeneratorProgressFile.Change> Changes() => Channel.CreateUnbounded<GeneratorProgressFile.Change>();

    private protected static async Task Signal(Channel<GeneratorProgressFile.Change> changes, GeneratorProgressFile.ChangeKind kind = GeneratorProgressFile.ChangeKind.Changed)
    {
        var processed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await changes.Writer.WriteAsync(new(kind, processed));
        await processed.Task;
    }

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"cratis-progress-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    void Destroy() => Directory.Delete(_root, true);
}
