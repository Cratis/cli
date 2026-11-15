// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.Configuration;
using Cratis.Templating.Orchestration;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;
using Spectre.Console;

namespace Cratis.Cli.for_NewCommand;

[Collection(CliSpecsCollection.Name)]
public class when_rendering_a_creation : Specification
{
    static readonly string _root = Path.Combine(Path.GetTempPath(), "Demo");

    StringWriter _console;
    TextWriter _previousOutput;
    IAnsiConsole _previousConsole;

    static PostActionResult Action(string description, PostActionOutcome outcome, string message = "", string instructions = "") =>
        new(new PostActionConfig { ActionId = "id", Description = description }, outcome, message, instructions);

    void Establish()
    {
        _previousOutput = Console.Out;
        _previousConsole = AnsiConsole.Console;
        _console = new StringWriter();
        Console.SetOut(_console);
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_console), Ansi = AnsiSupport.No });
        AnsiConsole.Console.Profile.Width = 1000;
    }

    void Because() => NewCreationOutput.Render(new NewSettings(), new NewCreation(
        new DiscoveredTemplate(new TemplateConfig { Name = "Sample", ShortName = "sample" }, _root),
        new InstantiationResult("Demo", _root, [new RenderedFile(Path.Combine(_root, "Demo.txt"), false)], [], new Dictionary<string, string>(), []),
        [
            Action("Restore", PostActionOutcome.Succeeded, "restored", "run it again"),
            Action("Optional", PostActionOutcome.Skipped, "condition was false"),
            Action("Manual", PostActionOutcome.NotPerformed, "no toolchain"),
            Action("Shown", PostActionOutcome.Displayed, "displayed"),
            Action("Broken", PostActionOutcome.Failed, "it broke"),
            Action("Mystery", PostActionOutcome.Unknown)
        ],
        new CreatedProjectAiUpdateResult("failed", [], "no configuration")));

    [Fact]
    void should_render_the_summary_each_action_and_the_ai_update() => _console.ToString().ReplaceLineEndings("\n").ShouldEqual(
        "\n" +
        $"  ✓ Created Demo at {_root}\n" +
        "\n" +
        "  ✓ Restore\n" +
        "      run it again\n" +
        "  · Optional\n" +
        "  · Manual\n" +
        "      no toolchain\n" +
        "  ✓ Shown\n" +
        "      displayed\n" +
        "  ✗ Broken\n" +
        "      it broke\n" +
        "  ✗ Mystery\n" +
        "  · Cratis AI: no configuration\n" +
        "\n");

    void Destroy()
    {
        AnsiConsole.Console = _previousConsole;
        Console.SetOut(_previousOutput);
        _console.Dispose();
    }
}
