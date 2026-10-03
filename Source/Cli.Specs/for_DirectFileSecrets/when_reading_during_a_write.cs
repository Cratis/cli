// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectFileSecrets;

public class when_reading_during_a_write : Specification
{
    string _home = null!;
    string? _before;
    string? _after;

    async Task Because()
    {
        _home = Path.Combine(Path.GetTempPath(), $"direct-file-spec-{Guid.NewGuid():N}");
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new DirectFileSecrets(_home);
        await store.Write("key", "{\"refresh\":\"original\"}", CancellationToken.None);
        var updating = new DirectFileSecrets(_home, async () =>
        {
            ready.SetResult();
            await release.Task;
        });
        var write = updating.Write("key", "{\"refresh\":\"replacement\"}", CancellationToken.None);
        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _before = await store.Read("key", CancellationToken.None);
        }
        finally
        {
            release.SetResult();
        }

        await write;
        _after = await store.Read("key", CancellationToken.None);
    }

    void Destroy() => Directory.Delete(_home, recursive: true);

    [Fact] void should_read_complete_previous_json_before_replacement() => _before.ShouldEqual("{\"refresh\":\"original\"}");
    [Fact] void should_read_complete_new_json_after_replacement() => _after.ShouldEqual("{\"refresh\":\"replacement\"}");
}
