// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.given;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class a_reducer_render_command : a_check_command
{
    protected readonly List<ArtifactRenderRequest> _requests = [];
    private protected readonly List<ScreenplayRenderPlan> _plans = [];
    protected string _configuration = null!;
    protected (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        File.WriteAllText(_document, """
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
                    on OrderPlaced
                      ```csharp
                      return new Total(context.Event.Amount, context.Event.Id);
                      ```
            """);
        _configuration = Path.Combine(_destination, ReducerConventionInput.RelativePath);
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            var request = call.Arg<ArtifactRenderRequest>();
            _requests.Add(request);
            return new CratisArtifactRenderPlanner().Plan(request);
        });
        var planning = new ScreenplayPlanning(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var plan = await planning.Plan(call.Arg<ScreenplayRenderRequest>(), call.Arg<CancellationToken>());
            _plans.Add(plan);
            return plan;
        });
        _settings.Check = false;
    }

    protected ArtifactRenderInput Convention => _requests[^1].Profile.Inputs.Single(input => input.Name == ReducerConventionInput.Name);

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());

    protected void WriteConfiguration(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configuration)!);
        File.WriteAllText(_configuration, json);
    }

    protected async Task PublishClassic()
    {
        var plan = await new ScreenplayPlanning().Plan(new(_folder, "MyApp", "cratis"), CancellationToken.None);
        plan.Success.ShouldBeTrue();
        await new ArtifactPublisher().Publish(new(plan.Artifacts!, _destination, false) { SemanticAddresses = plan.SemanticAddresses }, CancellationToken.None);
    }
}
