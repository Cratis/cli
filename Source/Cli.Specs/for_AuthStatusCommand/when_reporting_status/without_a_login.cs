// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class without_a_login : given.an_auth_status
{
    void Establish() => _configuration.Contexts["default"] = new();
    async Task Because() => await Execute();

    [Fact] void should_report_no_bound_server() => _status.RootElement.GetProperty("tokenServer").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_no_login_match() => _status.RootElement.GetProperty("loginMatchesServer").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_resolve_the_default_server() => _status.RootElement.GetProperty("server").GetString().ShouldEqual("chronicle://localhost:35000");
}
