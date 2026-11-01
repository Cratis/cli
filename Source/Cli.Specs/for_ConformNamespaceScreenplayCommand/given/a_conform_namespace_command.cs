// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Observations;

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.given;

public class a_conform_namespace_command : Cli.given.a_temp_config_directory
{
    protected const string Source = "domain Library\nmodule Library\n  feature Registration\n    slice StateChange Register\n      command Register\n        id Uuid identifier\n        name String\n        produces Registered\n          for id\n          name = name\n      event Registered\n        name String\n";
    protected static readonly ObservedProducer _register = new(ObservedProducerKind.Command, "Register");
    protected string _folder = null!;
    protected string _model = null!;
    protected INamespaceObservations _observations = null!;
    protected List<ObservedEventMetadata> _events = [];
    protected List<ObservedEventType> _registered = [new("Registered", 1)];
    protected List<string> _namespaces = ["Default"];
    protected ConformNamespaceScreenplayCommand _command = null!;
    protected ConformNamespaceScreenplaySettings _settings = null!;
    protected int _exitCode;
    protected string _output = string.Empty;
    protected string _error = string.Empty;

    protected JsonElement Json => JsonSerializer.Deserialize<JsonElement>(_output);

    void Establish()
    {
        _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        _model = Path.Combine(_folder, "Library.play");
        File.WriteAllText(_model, Source);
        _observations = Substitute.For<INamespaceObservations>();
        _observations.EventStores(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>(["Library"]));
        _observations.Namespaces("Library", Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IReadOnlyList<string>>(_namespaces));
        _observations.EventSequences("Library", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>(["event-log"]));
        _observations.Tail("Library", "Default", "event-log", Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(_events.Count == 0 ? null : (ulong?)(_events.Count - 1)));
        _observations.RegisteredEventTypes("Library", Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IReadOnlyList<ObservedEventType>>(_registered));
        _observations.Consumers("Library", "Default", Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ObservedConsumer>>([]));
        _observations.Metadata("Library", "Default", "event-log", Arg.Any<IReadOnlyList<ulong>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var numbers = call.ArgAt<IReadOnlyList<ulong>>(3);
            return Task.FromResult<IReadOnlyList<ObservedEventMetadata>>([.. _events.Where(metadata => numbers.Contains(metadata.SequenceNumber))]);
        });
        _command = new(_ => _observations, TimeProvider.System);
        _settings = new() { ModelRoot = _model, EventStore = "Library", Namespace = "Default", Output = OutputFormats.JsonCompact };
    }

    protected void Appended(int count, ObservedProducer? producer = null, string type = "Registered")
    {
        for (var index = 0; index < count; index++)
        {
            _events.Add(new((ulong)_events.Count, type, producer));
        }
    }

    protected Task Run() => Run(CancellationToken.None);

    protected async Task Run(CancellationToken cancellationToken)
    {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            _exitCode = _command.Prepare(_settings) ?? await _command.Execute(_observations, _settings, _settings.ResolveOutputFormat(), cancellationToken);
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
            _output = output.ToString();
            _error = error.ToString();
        }
    }

    void Destroy() => Directory.Delete(_folder, true);
}
