// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.given;

/// <summary>
/// Base context that puts a document in a temporary folder and substitutes the rendering.
/// </summary>
public class a_render_command : Specification
{
    protected string _folder;
    protected string _document;
    protected string _previousDirectory;
    protected IScreenplayRendering _rendering;
    protected RenderCommand _command;
    protected RenderSettings _settings;

    void Establish()
    {
        var created = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(created);

        // Read the folder back so that specs compare against the same fully resolved path the command sees —
        // the temp folder is reached through a symbolic link on macOS.
        _folder = Directory.GetCurrentDirectory();
        _document = Path.Combine(_folder, "MyApp.play");
        File.WriteAllText(_document, "domain Library\n");

        _rendering = Substitute.For<IScreenplayRendering>();
        _rendering.Render(Arg.Any<string>(), Arg.Any<string>()).Returns(new RenderedScreenplay(1, [], []));

        _command = new RenderCommand(_rendering);
        _settings = new RenderSettings { Output = OutputFormats.JsonCompact };
    }

    /// <summary>
    /// Executes the command with the established settings.
    /// </summary>
    /// <returns>The exit code.</returns>
    protected Task<int> Execute() =>
        ((ICommand<RenderSettings>)_command).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "render", null),
            _settings,
            CancellationToken.None);

    void Destroy()
    {
        Directory.SetCurrentDirectory(_previousDirectory);

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
