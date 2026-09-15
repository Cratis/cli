// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Spectre.Console;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_workspace_input_is_rejected : given.a_workspace_render_command
{
    [Theory]
    [InlineData("malformed")]
    [InlineData("version")]
    [InlineData("revision")]
    [InlineData("source")]
    [InlineData("catalog")]
    [InlineData("oversized")]
    public async Task should_reject_transport_errors_before_recovery_or_planning(string failure)
    {
        var workspace = CreateWorkspace();
        WriteWorkspace(workspace);
        var json = await File.ReadAllTextAsync(_input);
        switch (failure)
        {
            case "malformed": await File.WriteAllTextAsync(_input, "{not json"); break;
            case "version": await File.WriteAllTextAsync(_input, json.Replace("\"schemaVersion\":1", "\"schemaVersion\":99", StringComparison.Ordinal)); break;
            case "revision": await File.WriteAllTextAsync(_input, json.Replace(workspace.ApplicationName, "TamperedApplication", StringComparison.Ordinal)); break;
            case "source": await File.WriteAllTextAsync(_input, json.Replace(Convert.ToBase64String(workspace.Documents.Single().Bytes.AsSpan()), Convert.ToBase64String("tampered source"u8), StringComparison.Ordinal)); break;
            case "catalog": await File.WriteAllTextAsync(_input, json.Replace("\"identityCatalog\":{", "\"identityCatalog\":{\"unknown\":true,", StringComparison.Ordinal)); break;
            case "oversized":
                await using (var stream = File.OpenWrite(_input))
                {
                    stream.SetLength(RenderWorkspaceInput.MaximumBytes + 1L);
                }

                break;
        }

        await using var errors = new StringWriter();
        await using var output = new StringWriter();
        var previousError = Console.Error;
        var previousOutput = Console.Out;
        int result;
        try
        {
            Console.SetError(errors);
            Console.SetOut(output);
            result = await Execute();
        }
        finally
        {
            Console.SetError(previousError);
            Console.SetOut(previousOutput);
        }

        result.ShouldEqual(ExitCodes.ValidationError);
        output.ToString().ShouldEqual(string.Empty);
        Assert.Contains("Workspace input", errors.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(_folder, errors.ToString(), StringComparison.Ordinal);
        _documentSets.ShouldBeEmpty();
        await AssertNoPublication();
    }

    [Theory]
    [InlineData(false, ExitCodes.NotFound)]
    [InlineData(true, ExitCodes.ValidationError)]
    public async Task should_require_an_existing_file(bool directory, int expected)
    {
        if (directory)
        {
            _settings.Workspace = _folder;
        }

        (await Execute()).ShouldEqual(expected);

        await AssertNoPublication();
    }

    [Theory]
    [InlineData("")]
    [InlineData("RenamedApplication")]
    [InlineData("projects")]
    public async Task should_reject_a_name_mismatch_without_recovering_an_existing_destination(string name)
    {
        WriteWorkspace(CreateWorkspace());
        _settings.Name = name;
        Directory.CreateDirectory(Path.Combine(_destination, ".cratis-render"));
        var sentinel = Path.Combine(_destination, ".cratis-render", "journal.json");
        await File.WriteAllTextAsync(sentinel, "prior recovery evidence");

        (await Execute()).ShouldEqual(ExitCodes.ValidationError);

        (await File.ReadAllTextAsync(sentinel)).ShouldEqual("prior recovery evidence");
        await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        _documentSets.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_conflicting_input_or_name_through_the_parser(bool nameMismatch)
    {
        WriteWorkspace(CreateWorkspace());
        string[] arguments = ["render", "--workspace", _input, "--destination", _destination, "-o", "json-compact"];
        arguments = nameMismatch ? [.. arguments, "--name", "RenamedApplication"] : [.. arguments, _folder];

        (await CliApp.Create().RunAsync(arguments)).ShouldEqual(ExitCodes.ValidationError);

        Directory.Exists(_destination).ShouldBeFalse();
    }

    [Fact]
    public async Task should_require_a_value_for_the_workspace_option_through_the_parser()
    {
        await using var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output), Ansi = AnsiSupport.No });
        console.Profile.Width = 200;
        var app = CliApp.Create();
        app.Configure(configuration => configuration.ConfigureConsole(console));

        var result = await app.RunAsync(["render", "--workspace"]);

        result.ShouldNotEqual(ExitCodes.Success);
        Assert.Contains("Option 'workspace' is defined but no value has been provided.", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_not_treat_structural_admission_of_unbindable_source_as_a_successful_render()
    {
        var workspace = CreateWorkspace(invalidSource: true);
        workspace.Compilation.Success.ShouldBeFalse();
        WriteWorkspace(workspace);

        (await Execute()).ShouldEqual(ExitCodes.ValidationError);

        _documentSets.Count.ShouldEqual(1);
        _requests.ShouldBeEmpty();
        await AssertNoPublication(recovered: true);
    }

    [Theory]
    [InlineData("target")]
    [InlineData("project")]
    [InlineData("namespace")]
    public async Task should_keep_target_and_rendering_option_validation_in_the_existing_planner(string invalid)
    {
        WriteWorkspace(CreateWorkspace());
        _settings.Target = invalid == "target" ? "not-bundled" : null;
        _settings.ProjectName = invalid == "project" ? "../escape" : null;
        _settings.RootNamespace = invalid == "namespace" ? "../escape" : null;

        (await Execute()).ShouldEqual(ExitCodes.ValidationError);

        await AssertNoPublication(recovered: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_abort_without_publication_when_canceled_before_reading_or_during_real_compilation(bool duringCompilation)
    {
        WriteWorkspace(CreateWorkspace());
        using var cancellation = new CancellationTokenSource();
        if (duringCompilation)
        {
            _afterCompilation = cancellation.Cancel;
        }
        else
        {
            await cancellation.CancelAsync();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(cancellation.Token));

        _requests.ShouldBeEmpty();
        await AssertNoPublication(recovered: duringCompilation);
    }
}
