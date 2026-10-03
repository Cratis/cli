// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectFileSecrets;

public class when_a_write_is_interrupted : Specification
{
    string _home = null!;
    string? _remaining;
    Exception? _error;
    string[] _temporary = null!;

    async Task Because()
    {
        _home = Path.Combine(Path.GetTempPath(), $"direct-file-spec-{Guid.NewGuid():N}");
        var store = new DirectFileSecrets(_home);
        await store.Write("key", "{\"refresh\":\"original\"}", CancellationToken.None);
        var interrupted = new DirectFileSecrets(_home, () => throw new IOException("Interrupted before replace"));
        _error = await Catch.Exception(() => interrupted.Write("key", "{\"refresh\":\"replacement\"}", CancellationToken.None));
        _remaining = await store.Read("key", CancellationToken.None);
        _temporary = Directory.GetFiles(Path.Combine(_home, ".cratis", "direct-secrets"), "*.tmp");
    }

    void Destroy() => Directory.Delete(_home, recursive: true);

    [Fact] void should_fail_the_write() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_preserve_the_previous_credential() => _remaining.ShouldEqual("{\"refresh\":\"original\"}");
    [Fact] void should_remove_the_incomplete_file() => _temporary.ShouldBeEmpty();
}
