// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v8_model : given.a_screenplay_planning
{
    /// <summary>
    /// Stage v4.62.0's when_planning_event_source_routes fixture, with a routed expectation.
    /// </summary>
#pragma warning disable MA0136 // The Screenplay compiler accepts either line ending in model source.
    internal const string RoutedSource = """
        concept AccountId : Uuid
        concept Month : Int
        eventsource Account
          identifier AccountId
          stream Transactions
            streamId Month
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                month Month
                amount Decimal
                stream Account.Transactions
                  streamId = month
                produces event Deposited
                  for accountId
                  amount Decimal = amount
              specification Depositing
                when Deposit
                  accountId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  month = 10
                  amount = 5
                then Deposited
                  stream Account.Transactions
                    streamId = 10
                  amount = 5
        """;
#pragma warning restore MA0136

    ScreenplayRenderPlan _result = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        File.WriteAllText(_file, RoutedSource);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("routes"), "routes", "Routes.play", RoutedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        _model = compilation.Value!.Model;
    }

    async Task Because() => _result = await Plan();

    [Fact] void should_bind_an_esm_v8_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_plan_the_routed_model() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_emit_the_event_source_definition() => _result.Artifacts!.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("EventSources/AccountEventSource.cs");
    [Fact] void should_route_the_command_to_the_declared_source_and_stream() => CommandCode.ShouldContain("EventSourceAttribute<global::Projects.EventSources.AccountEventSource>(\"Transactions\")");
    [Fact] void should_resolve_the_stream_id_from_the_command() => CommandCode.ShouldContain("EventStreamId = global::Projects.GeneratedEventSources.StreamIds.Integer(Month.Value)");
    [Fact] void should_emit_the_routed_specification() => _result.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("when_depositing.cs", StringComparison.Ordinal)).ShouldBeTrue();

    string CommandCode => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Deposit.cs", StringComparison.Ordinal)).Bytes.AsSpan());
}
