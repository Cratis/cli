// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_a_request_completes_during_its_cancellation_callback : Specification
{
    CancellationTokenSource _source;
    DirectMcpRequest _request;
    CancellationTokenRegistration _registration;
    Exception? _duringCallback;
    Exception? _afterCancellation;
    bool _clientWasTheCause;

    void Establish()
    {
        _source = new();
        _request = new(_source);
        _registration = _request.Token.Register(() =>
        {
            _clientWasTheCause = _request.CancelledByClient;
            _request.Dispose();
            _duringCallback = Catch.Exception(() => { _ = _source.Token; });
        });
    }

    async Task Because()
    {
        await _request.Cancel().WaitAsync(TimeSpan.FromSeconds(10));
        _afterCancellation = Catch.Exception(() => { _ = _source.Token; });
    }

    [Fact] void should_record_the_client_as_the_cause_before_running_callbacks() => _clientWasTheCause.ShouldBeTrue();
    [Fact] void should_keep_the_source_alive_until_the_callback_finishes() => _duringCallback.ShouldBeNull();
    [Fact] void should_dispose_the_source_after_cancellation_finishes() => _afterCancellation.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_cancel_the_request() => _request.Token.IsCancellationRequested.ShouldBeTrue();

    void Destroy()
    {
        _registration.Dispose();
        _request.Dispose();
    }
}
