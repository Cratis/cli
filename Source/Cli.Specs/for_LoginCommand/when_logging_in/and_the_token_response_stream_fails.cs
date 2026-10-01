// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_LoginCommand.when_logging_in;

[Collection(CliSpecsCollection.Name)]
public class and_the_token_response_stream_fails : given.a_login_command
{
    int _result;
    FailingStream _stream = null!;
    StringWriter _error = null!;
    TextWriter _previousError = null!;

    void Establish()
    {
        _stream = new FailingStream();
        _endpoint.ResponseContent = new StreamContent(_stream);
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

    [Fact] void should_read_part_of_the_response_before_failing() => _stream.PartialBodyRead.ShouldBeTrue();
    [Fact] void should_fail_with_a_connection_error() => _result.ShouldEqual(ExitCodes.ConnectionError);
    [Fact] void should_report_the_connection_failure() => _error.ToString().ShouldContain(CliDefaults.CannotConnectMessage);
    [Fact] void should_not_report_invalid_configuration() => _error.ToString().ShouldNotContain("Invalid CLI configuration");
    [Fact] void should_not_store_the_token() => CliConfiguration.Load().Contexts["production"].AccessToken.ShouldBeNull();
    [Fact] void should_leave_the_previous_credentials_untouched() => CliConfiguration.Load().Contexts["production"].ClientId.ShouldEqual("old-client");

    sealed class FailingStream : MemoryStream
    {
        internal bool PartialBodyRead { get; private set; }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (PartialBodyRead)
            {
                throw new HttpIOException(HttpRequestError.ResponseEnded, "The response ended prematurely.");
            }

            var partialBody = Encoding.UTF8.GetBytes("{\"access_token\":\"partial-token");
            partialBody.CopyTo(buffer);
            PartialBodyRead = true;
            return ValueTask.FromResult(partialBody.Length);
        }
    }
}
