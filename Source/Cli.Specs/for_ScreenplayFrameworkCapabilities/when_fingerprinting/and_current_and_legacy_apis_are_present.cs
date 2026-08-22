// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ScreenplayFrameworkCapabilities.when_fingerprinting;

public class and_current_and_legacy_apis_are_present : Specification
{
    IReadOnlyList<string> _result;

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "Application",
            [CSharpSyntaxTree.ParseText(
                string.Join('\n',
                [
                    "namespace Marten.Events.Aggregation",
                    "{",
                    "    public class SingleStreamProjection<T, TId>;",
                    "}",
                    "namespace JasperFx.Events.Projections",
                    "{",
                    "    public enum ProjectionLifecycle { Inline, Async, Live }",
                    "}",
                    "namespace Marten.Subscriptions",
                    "{",
                    "    public interface ISubscription;",
                    "}",
                    "namespace Wolverine.Attributes",
                    "{",
                    "    public class WolverineHandlerAttribute : System.Attribute;",
                    "}",
                    "namespace Wolverine.Persistence.EventSourcing",
                    "{",
                    "    public class DeciderFunctionAttribute : System.Attribute;",
                    "    public class EventsToAppend;",
                    "}",
                    "namespace Wolverine.Marten",
                    "{",
                    "    public class AggregateHandlerAttribute : System.Attribute;",
                    "}"
                ]))]);

        _result = ScreenplayFrameworkCapabilities.From(compilation);
    }

    [Fact] void should_recognize_the_current_projection_shape() => _result.ShouldContain("marten.single-stream-projection.two-identities");
    [Fact] void should_recognize_the_current_lifecycle_namespace() => _result.ShouldContain("marten.projection-lifecycle.jasperfx");
    [Fact] void should_recognize_subscriptions() => _result.ShouldContain("marten.subscription");
    [Fact] void should_recognize_current_handler_metadata() => _result.ShouldContain("wolverine.handler-attribute");
    [Fact] void should_recognize_store_agnostic_event_capture() => _result.ShouldContain("wolverine.events-to-append");
    [Fact] void should_recognize_legacy_aggregate_metadata_separately() => _result.ShouldContain("wolverine.legacy-marten-aggregate");
}
