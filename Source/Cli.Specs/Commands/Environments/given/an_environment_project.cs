// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Environments;
using Cratis.Environments;

namespace Cratis.Cli.Commands.Environments.given;

public class an_environment_project : Specification
{
    static readonly string[] _variables = ["CRATIS_PASSPHRASE", "CRATIS_PASSPHRASE_FILE", "CRATIS_NEW_PASSPHRASE", "CRATIS_NEW_PASSPHRASE_FILE", ProjectEnvironments.EnvironmentVariable];

    protected string _root = null!;
    protected StringWriter _output = null!;
    protected StringWriter _error = null!;
    protected int _result;
    string _previousDirectory = null!;
    TextWriter _previousOutput = null!;
    TextWriter _previousError = null!;
    TextReader _previousInput = null!;
    Dictionary<string, string?> _previousVariables = null!;

    protected string AllOutput => $"{_output}{_error}";

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "cratis-cli-env-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_root);
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _previousInput = Console.In;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
        _previousVariables = _variables.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        foreach (var name in _variables)
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        Environment.SetEnvironmentVariable("CRATIS_PASSPHRASE", "spec passphrase");
    }

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
        Console.SetIn(_previousInput);
        Directory.SetCurrentDirectory(_previousDirectory);
        foreach (var (name, value) in _previousVariables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        Directory.Delete(_root, recursive: true);
    }

    protected static CommandContext Context(string name) => new([], Substitute.For<IRemainingArguments>(), name, null);

    protected async Task<int> Run<TCommand, TSettings>(TCommand command, TSettings settings)
        where TCommand : AsyncCommand<TSettings>
        where TSettings : GlobalSettings
    {
        settings.Output = OutputFormats.JsonCompact;
        _output.GetStringBuilder().Clear();
        _error.GetStringBuilder().Clear();
        return _result = await ((ICommand<TSettings>)command).ExecuteAsync(Context("spec"), settings, CancellationToken.None);
    }

    protected Task<int> Init(string? name = null, string? provider = null) =>
        Run(new EnvInitCommand(), new EnvInitSettings { Name = name, SecretsProvider = provider });

    protected Task<int> SetPlain(string key, string value, string? environment = null) =>
        Run(new ConfigSetCommand(), new ConfigSetSettings { Key = key, Value = value, EnvironmentShort = environment });

    protected Task<int> SetSecret(string key, string value, string? environment = null)
    {
        Console.SetIn(new StringReader(value + Environment.NewLine));
        return Run(new ConfigSetCommand(), new ConfigSetSettings { Key = key, Secret = true, EnvironmentShort = environment });
    }

    protected string EnvironmentFile(string name) => File.ReadAllText(Path.Combine(_root, ".cratis", "environments", name + ".yaml"));
}
