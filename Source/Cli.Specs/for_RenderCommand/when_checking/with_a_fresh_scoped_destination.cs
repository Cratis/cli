// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_checking;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

[Collection(CliSpecsCollection.Name)]
public class with_a_fresh_scoped_destination : given.a_check_command
{
    ScreenplayRenderPlan _scoped = null!;

    async Task Establish()
    {
        await File.WriteAllTextAsync(_document, """
            module Orders
              feature Placing
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId Uuid identifier
                    produces event OrderPlaced
                      for orderId
            """);
        _settings.Scope = ["Orders.Placing.PlaceOrder"];
        _scoped = await new ScreenplayPlanning().Plan(new(_folder, "MyApp", "cratis") { Scope = _settings.Scope }, CancellationToken.None);
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(_scoped);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_count_the_selected_artifacts_and_the_fresh_scaffold(bool check)
    {
        _settings.Check = check;

        var output = await Capture();

        _scoped.Success.ShouldBeTrue();
        output.ExitCode.ShouldEqual(check ? ExitCodes.ChangesPending : ExitCodes.Success);
        using var json = JsonDocument.Parse(output.Stdout);
        var expected = _scoped.Artifacts!.Artifacts.Length + CratisRendering.PlanScaffold(_scoped.ScaffoldOptions!).Artifacts.Length;
        json.RootElement.GetProperty("artifacts").GetInt32().ShouldEqual(expected);
        json.RootElement.GetProperty("written").GetInt32().ShouldEqual(expected);
        Directory.Exists(_destination).ShouldEqual(!check);
    }
}
