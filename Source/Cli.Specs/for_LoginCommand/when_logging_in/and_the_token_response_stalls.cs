// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_token_response_stalls : given.a_login_command
{
    int _result;
    StalledStream _stream = null!;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _stream = new StalledStream();
        _endpoint.ResponseContent = new StreamContent(_stream);
        _endpoint.Timeout = TimeSpan.FromMilliseconds(50);
        _previousError = Console.Error;
        _error = new StringWriter();
        Console.SetError(_error);
    }

    async Task Because() => _result = await Execute();

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            Console.SetError(_previousError);
        }
        finally
        {
            base.CleanUp();
        }
    }

    [Fact] void should_reach_the_response_body() => _stream.ReadStarted.ShouldBeTrue();
    [Fact] void should_fail_with_a_connection_error() => _result.ShouldEqual(ExitCodes.ConnectionError);
    [Fact] void should_report_the_timeout() => _error.ToString().ShouldContain("timed out");
    [Fact] void should_not_store_the_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
    [Fact] void should_leave_the_previous_credentials_untouched() => CliConfiguration.Load().Contexts["production"].ClientId.ShouldEqual("old-client");

    sealed class StalledStream : MemoryStream
    {
        internal bool ReadStarted { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadStarted = true;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
