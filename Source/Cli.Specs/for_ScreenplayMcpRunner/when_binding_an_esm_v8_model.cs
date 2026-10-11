// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayMcpRunner;

public class when_binding_an_esm_v8_model : given.an_embedded_connection
{
    JsonElement _details;

    void Establish() => File.WriteAllText(Path.Combine(_root, "Routes.play"), for_ScreenplayPlanning.when_planning_an_esm_v8_model.RoutedSource);

    void Because()
    {
        Exchange("{}", """
            {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"declaration-details","arguments":{"address":"Banking.Deposits.Deposit.Deposit","kind":"Command","view":"summary"}}}
            """);
        _details = _responses.Single(response => response.GetProperty("id").GetInt32() == 3)
            .GetProperty("result").GetProperty("structuredContent").GetProperty("details");
    }

    [Fact] void should_admit_the_routed_command_for_binding() => _details.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_no_execution_readiness_refusal() => _details.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
}
