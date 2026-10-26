// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_file_bodies_in_imported_slices : given.a_screenplay_planning
{
    const string Body = "return new Total(context.Event.Id, context.Event.Amount);";
    ScreenplayRenderPlan _result = null!;
    string _model = null!;

    void Establish()
    {
        _model = Directory.CreateDirectory(Path.Combine(_folder, "imported")).FullName;
        var feature = Directory.CreateDirectory(Path.Combine(_model, "orders", "ordering")).FullName;
        File.WriteAllText(Path.Combine(_model, "application.play"), "domain Orders\n");
        File.WriteAllText(Path.Combine(_model, "orders", "orders.play"), "module Orders\n  import \"ordering/ordering.play\"\n");
        File.WriteAllText(Path.Combine(feature, "ordering.play"), "feature Ordering\n  import \"totals.play\"\n");
        File.WriteAllText(Path.Combine(feature, "Fold.cs"), Body);
        File.WriteAllText(Path.Combine(feature, "totals.play"), string.Join('\n',
            "slice StateChange PlaceOrder",
            "  command PlaceOrder",
            "    id Uuid identifier",
            "    amount Decimal",
            "    produces OrderPlaced",
            "      for id",
            "      id = id",
            "      amount = amount",
            "  event OrderPlaced",
            "    id Uuid",
            "    amount Decimal",
            "slice StateView Totals",
            "  readmodel Total",
            "    id Uuid",
            "    amount Decimal",
            "  query ById => Total optional",
            "    by id Uuid",
            "  reducer Fold => Total",
            "    on OrderPlaced",
            "      file orders/ordering/Fold.cs"));
    }

    async Task Because() => _result = await Plan(_model);

    [Fact] void should_plan_successfully() => Assert.True(_result.Success, string.Join(Environment.NewLine, _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_not_report_a_missing_body() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-020");
    [Fact] void should_render_the_resolved_body() => _result.Artifacts!.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).ShouldContain(content => content.Contains(Body, StringComparison.Ordinal));
}
