// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.given;

/// <summary>
/// Base context that puts a solution in a temporary folder, substitutes the generation, and captures what the
/// command writes to standard output.
/// </summary>
public class a_generate_screenplay_command : Specification
{
    protected const string GeneratedSource = "domain Library\n\nmodule Library\n";

    protected string _folder;
    protected string _solution;
    protected string _previousDirectory;
    protected IScreenplayGeneration _generation;
    protected MemoryStream _standardOutput;
    protected GenerateScreenplayCommand _command;
    protected GenerateScreenplaySettings _settings;

    void Establish()
    {
        var created = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(created);

        // Read the folder back so that specs compare against the same fully resolved path the command sees —
        // the temp folder is reached through a symbolic link on macOS.
        _folder = Directory.GetCurrentDirectory();
        _solution = Path.Combine(_folder, "MyApp.slnx");
        File.WriteAllText(_solution, "<Solution />");

        _generation = Substitute.For<IScreenplayGeneration>();
        _generation
            .Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedScreenplay(GeneratedSource, [])));

        _standardOutput = new MemoryStream();
        _command = new GenerateScreenplayCommand(_generation, () => _standardOutput);
        _settings = new GenerateScreenplaySettings { Output = OutputFormats.JsonCompact };
    }

    /// <summary>
    /// Executes the command with the established settings.
    /// </summary>
    /// <returns>The exit code.</returns>
    protected Task<int> Execute() =>
        ((ICommand<GenerateScreenplaySettings>)_command).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "generate", null),
            _settings,
            CancellationToken.None);

    void Destroy()
    {
        Directory.SetCurrentDirectory(_previousDirectory);
        _standardOutput.Dispose();

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
