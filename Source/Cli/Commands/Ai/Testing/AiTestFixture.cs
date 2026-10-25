// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Cli.Commands.Ai.Testing;

internal enum AiTestPhase
{
    Result,
    Install
}

internal sealed record AiTestFixture
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string[] Profiles { get; init; } = [];
    public string[] Languages { get; init; } = [];
    public string[] Harnesses { get; init; } = [];
    public string[] ExpectedInstructionPaths { get; init; } = [];
    public AiTestCheck[] Checks { get; init; } = [];
    public AiTestBounds Bounds { get; init; } = new();

    internal static JsonSerializerOptions Json { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter<AiTestPhase>(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) }
    };

    internal static IReadOnlyList<AiTestFixtureSource> Load(string root, AiConfiguration configuration, string[] names)
    {
        var fixtures = Directory.GetDirectories(root).Where(directory => File.Exists(Path.Combine(directory, "fixture.json"))).Order(StringComparer.Ordinal).Select(Read).ToArray();
        var missing = names.Except(fixtures.Select(source => source.Fixture.Name)).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException($"Unknown fixtures: {string.Join(", ", missing)}");
        return [.. fixtures.Where(source => source.Fixture.Matches(configuration, names))];
    }

    internal bool Matches(AiConfiguration configuration, string[] names) =>
        (names.Length == 0 || names.Contains(Name, StringComparer.Ordinal)) &&
        Matches(Profiles, configuration.Profiles) && Matches(Languages, configuration.Languages);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Prompt)) throw new InvalidOperationException("Fixture requires name and prompt.");
        Bounds.Validate();
        if (!Checks.Any(check => check.Phase == AiTestPhase.Result && !check.Optional)) throw new InvalidOperationException("Fixture requires a non-optional result-phase check.");
        foreach (var path in ExpectedInstructionPaths) AiTestPaths.Validate(path);
        foreach (var check in Checks) check.Validate();
    }

    static bool Matches(string[] restriction, IReadOnlyList<string> selection) =>
        restriction.Length == 0 || restriction.Intersect(selection, StringComparer.Ordinal).Any();

    static AiTestFixtureSource Read(string directory)
    {
        try
        {
            var fixture = JsonSerializer.Deserialize<AiTestFixture>(File.ReadAllText(Path.Combine(directory, "fixture.json")), Json)
                ?? throw new InvalidOperationException("Empty fixture.");
            return new(directory, fixture);
        }
        catch (JsonException exception)
        {
            return new(directory, new() { Name = Path.GetFileName(directory) }, exception.Message);
        }
    }
}

internal sealed record AiTestFixtureSource(string Directory, AiTestFixture Fixture, string? Error = null);

internal sealed record AiTestBounds
{
    public double TimeoutSeconds { get; init; } = 120;
    public decimal? MaxBudgetUsd { get; init; }
    public int? MaxTurns { get; init; }

    internal void Validate()
    {
        if (!double.IsFinite(TimeoutSeconds)) throw new InvalidOperationException("Fixture timeout must be finite.");
        if (TimeoutSeconds <= 0 || TimeoutSeconds > uint.MaxValue / 1000d) throw new InvalidOperationException("Fixture timeout is outside the supported timer range.");
        if (MaxBudgetUsd is <= 0 || MaxTurns is <= 0) throw new InvalidOperationException("Fixture budget and turn bounds must be positive.");
    }

    internal AiTestBounds Lower(double? timeout, decimal? budget) => this with
    {
        TimeoutSeconds = Math.Min(TimeoutSeconds, timeout ?? TimeoutSeconds),
        MaxBudgetUsd = budget is null ? MaxBudgetUsd : Math.Min(MaxBudgetUsd ?? budget.Value, budget.Value)
    };
}

internal sealed record AiTestCheck
{
    public string Type { get; init; } = string.Empty;
    public string? Path { get; init; }
    public string? Glob { get; init; }
    public string? Pattern { get; init; }
    public string[] Run { get; init; } = [];
    public int ExitCode { get; init; }
    public bool Optional { get; init; }
    public AiTestPhase Phase { get; init; }

    internal void Validate()
    {
        if (Type == "command")
        {
            ValidateCommand();
            return;
        }
        ValidateFile();
    }

    void ValidateCommand()
    {
        if (Phase == AiTestPhase.Install) throw new InvalidOperationException("Command checks are not allowed in the install phase.");
        if (Run.Length == 0) throw new InvalidOperationException("Command check requires run: [executable, arguments...].");
    }

    void ValidateFile()
    {
        string[] types = ["file_exists", "file_absent", "file_matches", "file_not_matches"];
        if (!types.Contains(Type, StringComparer.Ordinal)) throw new InvalidOperationException($"Unknown check type: {Type}");
        if ((Path is null) == (Glob is null)) throw new InvalidOperationException("File check requires exactly one path or glob.");
        AiTestPaths.Validate(Path ?? Glob!);
        if (Type.EndsWith("_matches", StringComparison.Ordinal) && Pattern is null) throw new InvalidOperationException("Regex check requires pattern.");
    }
}
