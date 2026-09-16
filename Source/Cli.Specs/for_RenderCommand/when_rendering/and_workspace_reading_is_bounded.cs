// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_workspace_reading_is_bounded : given.a_workspace_render_command
{
    [Fact]
    public async Task should_admit_actual_canonical_bytes_even_when_delivered_in_short_reads()
    {
        var workspace = CreateWorkspace("folder");
        var bytes = ScreenplayWorkspaceSerializer.Serialize(workspace);
        await using var stream = new ShortReadingStream(bytes);

        var result = await RenderWorkspaceInput.Read(stream, CancellationToken.None);

        ScreenplayWorkspaceSerializer.Serialize(result).SequenceEqual(bytes).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(65536)]
    public async Task should_check_actual_bytes_including_one_probe_byte_instead_of_reported_length(int excess)
    {
        await using var stream = new GrowingStream(RenderWorkspaceInput.MaximumBytes + excess);

        var failure = await Assert.ThrowsAsync<InvalidScreenplayWorkspace>(() => RenderWorkspaceInput.Read(stream, CancellationToken.None));

        stream.ReadBytes.ShouldEqual(RenderWorkspaceInput.MaximumBytes + (excess == 0 ? 0 : 1));
        failure.Message.Contains("exceeds", StringComparison.Ordinal).ShouldEqual(excess != 0);

        // At exactly the cap the malformed bytes reach the shared deserializer, not a size rejection.
    }

    [Fact]
    public async Task should_observe_cancellation_during_a_read_even_if_the_stream_returns_bytes()
    {
        using var cancellation = new CancellationTokenSource();
        await using var stream = new ShortReadingStream(ScreenplayWorkspaceSerializer.Serialize(CreateWorkspace()), cancellation.Cancel);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RenderWorkspaceInput.Read(stream, cancellation.Token));

        stream.Position.ShouldEqual(7L);
        await AssertNoPublication();
    }

    sealed class ShortReadingStream(byte[] bytes, Action? afterRead = null) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = base.ReadAsync(buffer[..Math.Min(7, buffer.Length)], cancellationToken);
            afterRead?.Invoke();
            return result;
        }
    }

    sealed class GrowingStream(int available) : MemoryStream
    {
        public int ReadBytes { get; private set; }

        public override long Length => 1;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(available - ReadBytes, buffer.Length);
            buffer.Span[..count].Clear();
            ReadBytes += count;
            return ValueTask.FromResult(count);
        }
    }
}
