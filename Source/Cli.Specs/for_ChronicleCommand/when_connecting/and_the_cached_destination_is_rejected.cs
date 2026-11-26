// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Cli.for_ChronicleTokenCache.given;
using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_connecting;

[Collection(CliSpecsCollection.Name)]
public class and_the_cached_destination_is_rejected : a_token_cache
{
    string _selected;
    string _sibling;
    int _attempts;
    int _result;
    bool _removedBeforeRetry;

    void Establish()
    {
        new CliConfiguration
        {
            ActiveContext = Context,
            Contexts = new Dictionary<string, CliContext> { [Context] = new() }
        }.Save();
        _selected = CachePath(Connection());
        _sibling = CachePath(Connection(disableTls: true));
        Seed(_selected);
        Seed(_sibling);
    }

    async Task Because()
    {
        var command = new RejectedCommand(() =>
        {
            _attempts++;
            if (_attempts == 2)
            {
                _removedBeforeRetry = !File.Exists(_selected);
            }
        });
        _result = await command.ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "test", null),
            new ChronicleSettings { Server = "chronicle://client:fixture-secret@localhost:35000", Output = OutputFormats.JsonCompact },
            CancellationToken.None);
    }

    [Fact] void should_invalidate_the_selected_destination_before_retrying() => _removedBeforeRetry.ShouldBeTrue();
    [Fact] void should_preserve_the_other_destination() => File.Exists(_sibling).ShouldBeTrue();
    [Fact] void should_retry_only_once() => _attempts.ShouldEqual(2);
    [Fact] void should_report_authentication_failure_if_the_retry_is_rejected() => _result.ShouldEqual(ExitCodes.AuthenticationError);

    public sealed class RejectedCommand(Action attempted) : ChronicleCommand<ChronicleSettings>
    {
        /// <inheritdoc/>
        protected override Task<int> ExecuteConnectedAsync(ChronicleConnectionString connectionString, ChronicleSettings settings, string format, CancellationToken cancellationToken)
        {
            attempted();
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Bad gRPC response. HTTP status code: 401"));
        }

        /// <inheritdoc/>
        protected override Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format) => Task.FromResult(ExitCodes.Success);
    }
}
