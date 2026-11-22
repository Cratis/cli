// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_mapping_source_lines.given;

public class an_inline_model : Specification
{
    protected const string Body = "return new Total(context.Event.Id, context.Event.Amount);";
    protected static readonly string Source = string.Join('\n',
        "module Orders",
        "  feature Ordering",
        "    slice StateChange PlaceOrder",
        "      command PlaceOrder",
        "        id Uuid identifier",
        "        amount Decimal",
        "        produces OrderPlaced",
        "          for id",
        "          id = id",
        "          amount = amount",
        "      event OrderPlaced",
        "        id Uuid",
        "        amount Decimal",
        "    slice StateView Totals",
        "      readmodel Total",
        "        id Uuid",
        "        amount Decimal",
        "      query ById => Total?",
        "        by id Uuid",
        "      reducer Fold => Total",
        "        on OrderPlaced",
        "          ```csharp",
        $"          {Body}",
        "          ```");

    protected string _folder = null!;
    protected string _root = null!;
    protected string _file = null!;
    private protected ScreenplayPlanning _planning = null!;
    private protected ScreenplayRenderPlan _result = null!;

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-source-lines-{Guid.NewGuid():N}")).FullName;
        _root = Directory.CreateDirectory(Path.Combine(_folder, "model")).FullName;
        _file = Path.Combine(_root, "Orders.play");
        File.WriteAllText(_file, Source);
        _planning = new ScreenplayPlanning();
    }

    private protected ScreenplayDocumentRenderRequest DocumentRequest(string? destination, string? modelRoot)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("Orders.play"), "Orders.play", "Orders.play", Source);
        return new(SemanticDocumentSet.Create([document], catalog), "Orders", "cratis")
        {
            Destination = destination,
            ModelRoot = modelRoot
        };
    }

    protected string[] RenderedCode() => [.. _result.Artifacts.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))];

    void Destroy() => Directory.Delete(_folder, true);
}
