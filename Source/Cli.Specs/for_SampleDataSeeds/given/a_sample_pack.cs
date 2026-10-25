// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.SampleData;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.for_SampleDataSeeds.given;

public class a_sample_pack : Specification
{
    protected ApplicationSyntax _application;
    protected SceneApplication _scene;
    protected JsonObject _row;
    protected string? _seed;
    protected string? _error;
    private protected IReadOnlyList<SampleDataSeedWarning> _warnings;

    protected static SourceLocation Location => SourceLocation.Start;

    void Establish()
    {
        _row = new JsonObject { ["id"] = "product-1", ["name"] = "Mug", ["price"] = 49, ["status"] = "draft" };
        _application = Application(
            [new("Drafted", [Property("name")], Location)],
            [new("Products", [Property("id"), Property("name"), Property("price"), Property("status")], Location)],
            [From("Drafted", Set("id", new EventSourceIdExpressionSyntax(Location)), Set("name", new PathExpressionSyntax("name", Location)), Set("status", new LiteralExpressionSyntax("draft", Location)))]);
        _scene = new([new UiProfile("Web", "web", [], SampleData: new([], [], "demo"))], [], [], [], [], [])
        {
            SampleDataPacks = [new("demo", "Demo", "en", "user", [new("Products", [_row])])]
        };
    }

    protected static PropertySyntax Property(string name, bool collection = false, bool optional = false) =>
        new(name, new("String", collection, optional, Location), Location);

    protected static SetMappingSyntax Set(string property, ExpressionSyntax source) => new(property, source, Location);

    protected static FromSyntax From(string name, params MappingSyntax[] mappings) => new([new(name, null, Location)], null, null, mappings, Location);

    protected static ApplicationSyntax Application(IEnumerable<EventSyntax> events, IEnumerable<ReadModelSyntax> models, IEnumerable<ProjectionBlockSyntax> blocks) =>
        new([], [], [], [new("Shop", [], [new("Catalog", [], [new(SliceType.StateView, "List", events, [], [], [new("ProductProjection", "Products", null, AutoMapMode.Disabled, null, blocks, Location)], [], [], [], [], [], Location, ReadModels: models)], Location)], Location)], Location);
}
