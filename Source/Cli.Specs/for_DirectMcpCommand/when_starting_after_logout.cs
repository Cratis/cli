// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand;

[Collection(CliSpecsCollection.Name)]
public class when_starting_after_logout : given.an_inactive_login
{
    Exception? _error;
    readonly StringWriter _output = new();
    readonly StringWriter _log = new();

    async Task Because() => _error = await Catch.Exception(() => new DirectMcpRunner().Run(new(null, null), new StringReader(string.Empty), _output, _log, CancellationToken.None));

    [Fact] void should_require_an_active_login() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_explain_how_to_log_in() => _error!.Message.ShouldEqual("Not logged in to Direct. Run 'cratis direct login'.");
    [Fact] void should_not_start_the_bridge() => (_output.ToString() + _log).ShouldBeEmpty();
    [Fact] void should_keep_the_inactive_no_tenant_login() => _direct.Credentials.Single().Tenant.ShouldBeNull();
    [Fact] void should_have_no_active_selection() => _direct.HasActiveSelection.ShouldBeFalse();

    void Destroy()
    {
        _output.Dispose();
        _log.Dispose();
    }
}
