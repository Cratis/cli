// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ConformScreenplayCommand.given;

public class a_conform_command : Specification
{
    protected const string Source = "domain Library\nmodule Library\n  feature Registration\n    slice StateChange Register\n      command Register\n        id Uuid identifier\n        name String\n        produces Registered\n          for id\n          name = name\n      event Registered\n        name String\n";
    protected string _folder = null!;
    protected string _model = null!;
    protected string _project = null!;
    protected IScreenplayGeneration _generation = null!;
    protected ConformScreenplayCommand _command = null!;
    protected ConformScreenplaySettings _settings = null!;
    protected int _exitCode;
    protected JsonElement _output;
    protected string _error = string.Empty;
    protected ScreenplayGenerationOptions _options = null!;
    string _previousDirectory = null!;

    void Establish()
    {
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName);
        _folder = Directory.GetCurrentDirectory();
        _model = Path.Combine(_folder, "Library.play");
        _project = Path.Combine(_folder, "Library.csproj");
        File.WriteAllText(_model, Source);
        File.WriteAllText(_project, "<Project />");
        _generation = Substitute.For<IScreenplayGeneration>();
        Generated(Source);
        _command = new ConformScreenplayCommand(_generation, new ScreenplayConformance());
        _settings = new ConformScreenplaySettings { ModelRoot = _model, Output = OutputFormats.JsonCompact };
    }

    protected void Generated(string source, params ScreenplayDiagnostic[] diagnostics) => _generation.Generate(
        Arg.Any<string>(), Arg.Do<ScreenplayGenerationOptions>(options => _options = options), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(new GeneratedScreenplay(source, diagnostics) { Projects = ["Library"] }));

    protected async Task Execute()
    {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            _exitCode = await ((ICommand<ConformScreenplaySettings>)_command).ExecuteAsync(new CommandContext([], Substitute.For<IRemainingArguments>(), "conform", null), _settings, CancellationToken.None);
            _error = error.ToString();
            if (output.ToString().Length > 0)
            {
                _output = JsonSerializer.Deserialize<JsonElement>(output.ToString());
            }
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    protected JsonElement[] Findings(string property) => [.. _output.GetProperty(property).EnumerateArray()];

    void Destroy()
    {
        Directory.SetCurrentDirectory(_previousDirectory);
        Directory.Delete(_folder, true);
    }
}
