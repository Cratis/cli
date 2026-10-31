// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Connections;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Cratis.Cli.for_ReplayExistingConsumerCommand;

[Collection(CliSpecsCollection.Name)]
public class when_running_the_cli_before_confirmation : given.a_temp_config_directory
{
    static readonly RawScopeArgument[] _scopeArguments =
    [
        new() { Field = "observer", Value = "observer" },
        new() { Field = "store", Option = "-e", Value = "producer" },
        new() { Field = "namespace", Option = "-n", Value = "namespace" },
        new() { Field = "sequence", Option = "--sequence", Value = "outbox", WrongValue = "Outbox" },
        new() { Field = "subscription", Option = "--subscription-id", Value = "[subscription]" },
        new() { Field = "consumer", Option = "--consumer-identity", Value = "[consumer]" }
    ];

    static readonly string[] _detectionVariables = ["CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT", "CURSOR_TRACE_DIR", "TERM_PROGRAM", "WINDSURF_SESSION_ID", "PI_CODING_AGENT", "PI_SESSION_ID", AiAgentEnvironment.NonInteractiveEnvironmentVariable, "NO_COLOR"];

    static readonly ImmutableArray<RawScenario> _formatScenarios =
    [
        new() { Name = "explicit-json-nonquiet", OutputArguments = ["--output", "json"], ConfirmationArguments = ["--yes"], ExpectedFormat = OutputFormats.Json },
        new() { Name = "compact-normal", OutputArguments = ["--output", "json-compact"], ConfirmationArguments = ["--yes"], ExpectedFormat = OutputFormats.JsonCompact },
        new() { Name = "compact-quiet", OutputArguments = ["--output", "json-compact"], ConfirmationArguments = ["--yes", "--quiet"], ExpectedFormat = OutputFormats.JsonQuiet },
        new() { Name = "automatic-ai-compact", OutputArguments = [], ConfirmationArguments = ["--yes"], ExpectedFormat = OutputFormats.JsonCompact, EnableAiEnvironment = true },
        new() { Name = "automatic-genuinely-redirected-json", OutputArguments = [], ConfirmationArguments = ["--yes"], ExpectedFormat = OutputFormats.Json, RequiresRedirectedOutput = true }
    ];

    Dictionary<string, string?> _previousEnvironment;
    PairedOutputCapture _capture;
    ProbeCommand _command;

    public static TheoryData<RawScenario> FormatScenarios => [.. _formatScenarios];

    void Establish()
    {
        _previousEnvironment = _detectionVariables.ToDictionary(_ => _, Environment.GetEnvironmentVariable);
        foreach (var variable in _detectionVariables) Environment.SetEnvironmentVariable(variable, null);
        Environment.SetEnvironmentVariable(AiAgentEnvironment.NonInteractiveEnvironmentVariable, "1");
        _capture = new PairedOutputCapture();
        _command = new ProbeCommand(_capture);
    }

    [Theory]
    [InlineData("missing-observer")]
    [InlineData("missing-store")]
    [InlineData("missing-namespace")]
    [InlineData("missing-sequence")]
    [InlineData("missing-subscription")]
    [InlineData("missing-consumer")]
    [InlineData("blank-store")]
    [InlineData("blank-observer")]
    [InlineData("blank-namespace")]
    [InlineData("blank-subscription")]
    [InlineData("blank-consumer")]
    [InlineData("blank-sequence")]
    [InlineData("wrong-sequence")]
    [InlineData("malformed-option")]
    [InlineData("noninteractive-refusal")]
    public async Task should_never_create_a_client_before_confirmation(string scenario)
    {
        var result = await Run(scenario);

        result.ShouldNotEqual(ExitCodes.Success);
        ShouldHaveAttempts(0);
        if (scenario == "malformed-option")
        {
            result.ShouldEqual(-1);
            _capture.Output.ToString().ShouldContain("Error:");
        }
    }

    [Fact]
    public async Task should_reach_the_factory_only_after_explicit_yes_and_display_the_warning_even_in_quiet_mode()
    {
        var result = await Run("confirmed");

        result.ShouldEqual(ExitCodes.ServerError);
        ShouldHaveAttempts(1);
        _command.WarningVerifiedBeforeConnection.ShouldBeTrue();
    }

    [Theory(DisableDiscoveryEnumeration = true)]
    [MemberData(nameof(FormatScenarios))]
    public async Task should_keep_machine_stdout_empty_until_the_connection_factory(RawScenario scenario)
    {
        if (scenario.EnableAiEnvironment) Environment.SetEnvironmentVariable("PI_CODING_AGENT", "1");
        if (scenario.RequiresRedirectedOutput) Console.IsOutputRedirected.ShouldBeTrue();

        var result = await Run(scenario);

        result.ShouldEqual(ExitCodes.ServerError);
        ShouldHaveAttempts(1);
        _command.ResolvedFormat.ShouldEqual(scenario.ExpectedFormat);
        _command.WarningVerifiedBeforeConnection.ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_create_a_client_after_a_preconfirmation_error()
    {
        _command.FailBeforeConfirmation = true;
        var result = await Run("confirmed");

        result.ShouldNotEqual(ExitCodes.Success);
        ShouldHaveAttempts(0);
        _capture.Error.ToString().ShouldContain("UNVERIFIED");
    }

    [Fact]
    public async Task should_not_create_a_client_after_an_interactive_decline()
    {
        string[] agentVariables = ["CLAUDECODE", "CLAUDE_CODE_ENTRYPOINT", "CURSOR_TRACE_DIR", "TERM_PROGRAM", "WINDSURF_SESSION_ID", "PI_CODING_AGENT", "PI_SESSION_ID", AiAgentEnvironment.NonInteractiveEnvironmentVariable];
        var previous = agentVariables.ToDictionary(_ => _, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var variable in agentVariables) Environment.SetEnvironmentVariable(variable, null);
            var output = Substitute.For<IAnsiConsoleOutput>();
            output.IsTerminal.Returns(true);
            output.Width.Returns(1000);
            output.Height.Returns(40);
            var console = Substitute.For<IAnsiConsole>();
            console.Profile.Returns(new Profile(output, new Capabilities { Interactive = true }, System.Text.Encoding.UTF8));
            console.Input.ReadKeyAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
            var result = await Run(Arguments("noninteractive-refusal"), console);

            result.ShouldEqual(ExitCodes.Success);
            ShouldHaveAttempts(0);
        }
        finally
        {
            foreach (var variable in previous) Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
    }

    /// <inheritdoc/>
    protected override void CleanUp()
    {
        try
        {
            _capture.Dispose();
        }
        finally
        {
            try
            {
                foreach (var variable in _previousEnvironment) Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
            finally
            {
                base.CleanUp();
            }
        }
    }

    Task<int> Run(string scenario) => Run(Arguments(scenario));

    Task<int> Run(RawScenario scenario) => Run(
    [
        "chronicle", "observers", "replay-existing-consumer",
        .. _scopeArguments.SelectMany(_ => _.ForScenario(scenario.Name)),
        .. ServerArguments(scenario.Name),
        .. scenario.OutputArguments,
        .. scenario.ConfirmationArguments
    ]);

    async Task<int> Run(string[] args, IAnsiConsole? console = null)
    {
        var previousOutput = Console.Out;
        var previousError = Console.Error;
        var previousConsole = AnsiConsole.Console;
        try
        {
            Console.SetOut(_capture.Output);
            Console.SetError(_capture.Error);
            AnsiConsole.Console = console ?? AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(_capture.Output), Ansi = AnsiSupport.No, Interactive = InteractionSupport.No });
            AnsiConsole.Console.Profile.Width = 1000;
            var app = CliApp.Create(new ProbeRegistrar(_command), AnsiConsole.Console, args);
            return await app.RunAsync(args, TestContext.Current.CancellationToken);
        }
        finally
        {
            try
            {
                AnsiConsole.Console = previousConsole;
            }
            finally
            {
                try
                {
                    Console.SetOut(previousOutput);
                }
                finally
                {
                    Console.SetError(previousError);
                }
            }
        }
    }

    void ShouldHaveAttempts(int connections)
    {
        _command.ConnectionAttempts.ShouldEqual(connections);
        _command.DispatchAttempts.ShouldEqual(0);
    }

    static string[] Arguments(string scenario) =>
    [
        "chronicle", "observers", "replay-existing-consumer",
        .. _scopeArguments.SelectMany(_ => _.ForScenario(scenario)),
        .. ServerArguments(scenario),
        "--output", "json",
        .. ConfirmationArguments(scenario)
    ];

    static string[] ServerArguments(string scenario) => scenario == "malformed-option" ? [] : ["--server", "chronicle://127.0.0.1:1"];

    static string[] ConfirmationArguments(string scenario) => scenario switch
    {
        "noninteractive-refusal" => [],
        "malformed-option" => ["--yes", "--server"],
        "confirmed" => ["--yes", "--quiet"],
        _ => ["--yes"]
    };

    public sealed record RawScenario
    {
        public required string Name { get; init; }
        public required ImmutableArray<string> OutputArguments { get; init; }
        public required ImmutableArray<string> ConfirmationArguments { get; init; }
        public required string ExpectedFormat { get; init; }
        public bool EnableAiEnvironment { get; init; }
        public bool RequiresRedirectedOutput { get; init; }

        public override string ToString() => Name;
    }

    sealed class PairedOutputCapture : IDisposable
    {
        public StringWriter Output { get; } = new();
        public StringWriter Error { get; } = new();

        public void Dispose()
        {
            try
            {
                Output.Dispose();
            }
            finally
            {
                Error.Dispose();
            }
        }
    }

    sealed record RawScopeArgument
    {
        public required string Field { get; init; }
        public string? Option { get; init; }
        public required string Value { get; init; }
        public string? WrongValue { get; init; }

        public string[] ForScenario(string scenario)
        {
            if (scenario == $"missing-{Field}") return [];
            var value = scenario == $"blank-{Field}" ? " " : Value;
            if (scenario == $"wrong-{Field}" && WrongValue is not null) value = WrongValue;
            return Option is null ? [value] : [Option, value];
        }
    }

    sealed class ProbeCommand(PairedOutputCapture capture) : ReplayExistingConsumerCommand
    {
        public int ConnectionAttempts { get; private set; }
        public int DispatchAttempts { get; private set; }
        public bool FailBeforeConfirmation { get; set; }
        public bool WarningVerifiedBeforeConnection { get; private set; }
        public string? ResolvedFormat { get; private set; }

        protected override int? ExecuteOffline(ReplayExistingConsumerSettings settings)
        {
            var result = base.ExecuteOffline(settings);
            ResolvedFormat = settings.ResolveOutputFormat();
            if (FailBeforeConfirmation) throw new InvalidOperationException("Preconfirmation failure");
            return result;
        }

        protected override Task<CliChronicleConnection> ConnectAsync(ChronicleConnectionString connectionString, CancellationToken cancellationToken)
        {
            ConnectionAttempts++;
            capture.Output.ToString().ShouldEqual(string.Empty);
            var warning = capture.Error.ToString();
            warning.ShouldContain("UNVERIFIED operator attestation: producer 'producer', namespace 'namespace', sequence 'outbox', observer 'observer', subscription label '[subscription]', consumer label '[consumer]'.");
            warning.ShouldContain("UNVERIFIED labels do not prove or restrict delivery. Replay writes/resets observer state and may duplicate effects.");
            WarningVerifiedBeforeConnection = true;
            throw new InvalidOperationException("Connection factory reached after confirmation");
        }

        protected override Task<int> ExecuteCommandAsync(Cratis.Chronicle.Contracts.IServices services, ReplayExistingConsumerSettings settings, string format)
        {
            DispatchAttempts++;
            return base.ExecuteCommandAsync(services, settings, format);
        }
    }

    sealed class ProbeRegistrar(ProbeCommand command) : ITypeRegistrar
    {
        readonly ServiceCollection _services = new();

        public ITypeResolver Build()
        {
            _services.AddSingleton<ReplayExistingConsumerCommand>(command);
            return new ProbeResolver(_services.BuildServiceProvider());
        }

        public void Register(Type service, Type implementation) => _services.AddTransient(service, implementation);
        public void RegisterInstance(Type service, object implementation) => _services.AddSingleton(service, implementation);
        public void RegisterLazy(Type service, Func<object> factory) => _services.AddSingleton(service, _ => factory());
    }

    sealed class ProbeResolver(ServiceProvider provider) : ITypeResolver, IDisposable
    {
        public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);
        public void Dispose() => provider.Dispose();
    }
}
