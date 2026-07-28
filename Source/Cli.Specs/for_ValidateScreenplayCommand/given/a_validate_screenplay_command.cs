// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.given;

/// <summary>
/// Base context that puts a document in a temporary folder and substitutes the validation.
/// </summary>
public class a_validate_screenplay_command : Specification
{
    protected string _folder;
    protected string _document;
    protected string _previousDirectory;
    protected IScreenplayValidation _validation;
    protected ValidateScreenplayCommand _command;
    protected ValidateScreenplaySettings _settings;

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

        _validation = Substitute.For<IScreenplayValidation>();
        _validation.Validate(Arg.Any<string>()).Returns(new ValidatedScreenplay(1, []));

        _command = new ValidateScreenplayCommand(_validation);
        _settings = new ValidateScreenplaySettings { Output = OutputFormats.JsonCompact };
    }

    /// <summary>
    /// Executes the command with the established settings.
    /// </summary>
    /// <returns>The exit code.</returns>
    protected Task<int> Execute() =>
        ((ICommand<ValidateScreenplaySettings>)_command).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null),
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
