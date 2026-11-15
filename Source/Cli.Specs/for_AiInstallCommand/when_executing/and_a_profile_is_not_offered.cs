// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_executing;

[Collection(CliSpecsCollection.Name)]
public class and_a_profile_is_not_offered : Specification
{
    string _corpus = string.Empty;
    StringWriter _error = null!;
    TextWriter _previousError = null!;
    int _exitCode;

    void Establish()
    {
        _corpus = Path.Combine(Path.GetTempPath(), $"cratis-ai-unoffered-{Guid.NewGuid():N}");
        var managed = Path.Combine(_corpus, AiCorpusSynchronizer.ManagedRoot);
        Directory.CreateDirectory(managed);
        File.WriteAllText(Path.Combine(managed, "manifest.json"), """{ "harnesses": ["pi"], "profiles": ["cratis/documentation"], "languages": ["csharp"] }""");
        _error = new StringWriter();
        _previousError = Console.Error;
        Console.SetError(_error);
    }

    async Task Because() => _exitCode = await CliApp.Create().RunAsync(
        ["ai", "install", "--source", _corpus, "--harnesses", "pi", "--profiles", "cratis/unknown", "--languages", "csharp", "-o", "json"],
        TestContext.Current.CancellationToken);

    [Fact] void should_exit_with_the_invalid_usage_code() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_value_the_corpus_does_not_offer() => _error.ToString().ShouldContain("does not offer profile");
    [Fact] void should_report_a_validation_error() => _error.ToString().ShouldContain(ExitCodes.ValidationErrorCode);
    [Fact] void should_not_report_an_unexpected_error() => _error.ToString().ShouldNotContain(ExitCodes.UnexpectedErrorCode);

    void Destroy()
    {
        Console.SetError(_previousError);
        _error.Dispose();
        Directory.Delete(_corpus, recursive: true);
    }
}
