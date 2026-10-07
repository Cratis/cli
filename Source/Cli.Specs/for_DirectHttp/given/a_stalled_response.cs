// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectHttp.given;

public class a_stalled_response : Specification
{
    protected HttpClient Http = null!;
    protected readonly TaskCompletionSource BodyReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected bool BodyWasRead;
    protected bool BodyWasCanceled;
    private protected readonly DirectTarget Target = DirectTarget.Create("https://direct.example", "team");

    void Establish() => Http = new HttpClient(new Handler(this)) { Timeout = TimeSpan.FromMilliseconds(100) };

    private protected DirectTokenProvider Provider(IDirectSecretStore? store = null)
    {
        var refreshLock = Substitute.For<IDirectRefreshLock>();
        refreshLock.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<IAsyncDisposable>());
        return new DirectTokenProvider(store ?? Substitute.For<IDirectSecretStore>(), refreshLock, new DirectDiscovery(Http), Http, new Uri("https://identity.example/"));
    }

    void Destroy() => Http.Dispose();

    sealed class Handler(a_stalled_response context) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StalledStream(context)) });
    }

    sealed class StalledStream(a_stalled_response context) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            context.BodyWasRead = true;
            context.BodyReadStarted.TrySetResult();
            try
            {
                // A deliberately stalled transport: only a body-read deadline can release it.
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                context.BodyWasCanceled = true;
                throw;
            }

            return 0;
        }
    }
}
