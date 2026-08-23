// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class and_targeted_stream_and_event_schema_report_loss : given.a_marten_application_built_from_source
{
    static readonly string DiagnosticSource = string.Join(
        '\n',
        [
            "namespace Marten.Events",
            "{",
            "    public interface IEventStoreOptions",
            "    {",
            "        void MapEventType<TEvent>(string eventTypeName);",
            "    }",
            "}",
            "namespace Wolverine",
            "{",
            "    public class WolverineOptions;",
            "}",
            "namespace Wolverine.Persistence.EventSourcing",
            "{",
            "    public class WriteModelAttribute(string routeOrParameterName) : System.Attribute;",
            "}",
            "namespace JasperFx.Events",
            "{",
            "    public interface IEventStream<T>",
            "    {",
            "        T? Aggregate { get; }",
            "        void AppendOne(object @event);",
            "    }",
            "}",
            "namespace Banking",
            "{",
            "    public record AliasOnly;",
            "    public record RecordOpaqueAppend(System.Guid AccountId);",
            "    public static class EventSchemaConfiguration",
            "    {",
            "        public static void Configure(Marten.Events.IEventStoreOptions events) =>",
            "            events.MapEventType<AliasOnly>(\"alias-only\");",
            "    }",
            "    public static class RecordOpaqueAppendHandler",
            "    {",
            "        public static void Handle(",
            "            RecordOpaqueAppend command,",
            "            [Wolverine.Persistence.EventSourcing.WriteModel(nameof(RecordOpaqueAppend.AccountId))]",
            "            JasperFx.Events.IEventStream<Account> stream,",
            "            object opaque)",
            "        {",
            "            _ = command;",
            "            stream.AppendOne(opaque);",
            "        }",
            "    }",
            "}"
        ]);

    GeneratedScreenplay _generated = null!;
    ScreenplayGenerationProvenance _provenance = null!;

    void Establish()
    {
        var diagnosticTree = CSharpSyntaxTree.ParseText(DiagnosticSource, path: "/workspace/Banking/Diagnostics.cs");
        var compilation = Loaded.Compilations.Single().AddSyntaxTrees(diagnosticTree);
        Loaded = Loaded with
        {
            Compilations = [compilation],
            AuthoredSyntaxTrees = [Loaded.AuthoredSyntaxTrees.Single().Append(diagnosticTree).ToHashSet()],
            ProjectProvenance =
            [
                new ScreenplayProjectProvenance(
                    ProjectName,
                    "net10.0",
                    [
                        new ResolvedScreenplayPackage("Marten", "9.23.0"),
                        new ResolvedScreenplayPackage("WolverineFx", "6.29.1"),
                        new ResolvedScreenplayPackage("WolverineFx.Marten", "6.29.1")
                    ],
                    [new ScreenplayAssemblyIdentity("Marten", "9.23.0.0")],
                    ["marten.event-projection"])
            ]
        };
    }

    void Because()
    {
        _generated = CritterStackScreenplayGeneration.GenerateFrom(
            Loaded,
            "/workspace/Banking/Banking.csproj",
            ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.CritterStack });
        _provenance = ScreenplayCompatibility.Evaluate(new CritterStackSourceProvider(), Loaded).Complete(_generated.Diagnostics);
    }

    [Fact] void should_flow_the_targeted_stream_diagnostic_through_the_cli() => _generated.Diagnostics.Single(_ => _.Code == "WOLVERINE0013").Location.ShouldEqual("Diagnostics.cs");
    [Fact] void should_flow_the_marten_schema_diagnostic_through_the_cli() => _generated.Diagnostics.Single(_ => _.Code == "MARTEN0011").Location.ShouldEqual("Diagnostics.cs");
    [Fact] void should_report_lowering_loss() => _provenance.Compatibility!.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.LossReported);
    [Fact] void should_retain_the_canonical_support_tier() => _provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Canonical);
}
