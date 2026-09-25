// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_document_has_event_generations : given.a_render_command
{
    int _result;
    string _output = null!;

    void Establish()
    {
        File.WriteAllText(_document, string.Join('\n',
            "concept ProjectId : Uuid",
            "concept ProjectName : String",
            "module Projects",
            "  feature Registration",
            "    slice StateChange RegisterProject",
            "      event ProjectRegistered generation 1",
            "        projectId ProjectId",
            "        name ProjectName",
            "      event ProjectRegistered generation 2",
            "        name ProjectName"));
        _planning = new ScreenplayPlanning();
        _command = new RenderCommand(_planning, _publication);
    }

    async Task Because()
    {
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            _result = await Execute();
        }
        finally
        {
            Console.SetOut(previousOutput);
            Console.SetError(previousError);
        }
        _output = error.ToString();
    }

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_refused_event_and_diagnostic()
    {
        _output.ShouldContain("STAGE-ESM-016");
        _output.ShouldContain("CLI-RENDER-003");
        _output.ShouldContain("ProjectRegistered");
    }
    [Fact] void should_not_publish_anything() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
