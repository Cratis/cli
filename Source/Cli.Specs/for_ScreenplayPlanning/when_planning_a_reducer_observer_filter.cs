// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_reducer_observer_filter : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

#pragma warning disable MA0136 // The Screenplay compiler accepts either line ending in model source.
    void Establish() => File.WriteAllText(_file, """
        eventsource OrdersSource
          id "stored-orders"
          identifier Uuid
          stream Entries
            id "stored-entries"
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                id Uuid identifier
                amount Decimal
                produces OrderPlaced
                  for id
                  id = id
                  amount = amount
              event OrderPlaced
                id Uuid
                amount Decimal
            slice StateView Totals
              readmodel Total
                id Uuid
                amount Decimal
              query ById => Total?
                by id Uuid
              reducer Fold => Total
                from OrdersSource.Entries
                on OrderPlaced
                  ```csharp
                  return new Total(context.Event.Id, context.Event.Amount);
                  ```
        """);
#pragma warning restore MA0136

    async Task Because() => _result = await Plan();

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();
    [Fact] void should_render_the_source_and_stream_filter_through_the_stream_constant() => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Fold.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("[FromEventSource<OrdersSourceEventSource>(OrdersSourceEventSource.Entries)]");
    [Fact] void should_render_the_source_definition() => _result.Artifacts!.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("EventSources/OrdersSourceEventSource.cs");
    [Fact] void should_keep_the_stored_stream_name_in_the_constant() => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/OrdersSourceEventSource.cs").Bytes.AsSpan()).ShouldContain("\"stored-entries\"");
}
