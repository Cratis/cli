// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_render_admission;

public class with_an_unsupported_profile_package : given.a_completeness_cli_process
{
    int _renderExitCode;
    string[] _renderCodes = null!;

    void Establish()
    {
        var source = ScreenCompositionCorpus.V1.SourceForms.Single(form => form.Name == "single").Documents.Single();
        var text = Encoding.UTF8.GetString(source.Bytes.AsSpan());
        var changed = Regex.Replace(text, "^( +)Cratis\\.Components$", "$1Cratis.NotAPackage", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        changed.ShouldNotEqual(text);
        File.WriteAllText(_document, changed);
    }

    async Task Because()
    {
        await RunCommand("render", _document, "--check", "--name", "App", "--destination", Path.Combine(Path.GetDirectoryName(_document)!, "out"), "-o", _format);
        _renderExitCode = _exitCode;
        _renderCodes = RenderCodes();
        await Run("--executable", "--target", "cratis", "--name", "App");
    }

    [Fact] void should_refuse_render_check() => _renderExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_refuse_target_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_unsupported_package() => _renderCodes.ShouldContain("CLI-RENDER-007");
    [Fact] void should_report_the_same_render_admission_codes() => RenderCodes().SequenceEqual(_renderCodes).ShouldBeTrue();
    [Fact] void should_report_binding_as_passed()
    {
        using var document = JsonDocument.Parse(_output);
        document.RootElement.GetProperty("binding").GetProperty("executable").GetBoolean().ShouldBeTrue();
    }
    [Fact] void should_write_nothing() => Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(_document)!).Count().ShouldEqual(1);

    string[] RenderCodes()
    {
        using var document = JsonDocument.Parse(_error.Split('\n')[0]);
        return [.. document.RootElement.GetProperty("diagnostics").EnumerateArray()
            .Select(diagnostic => diagnostic.GetProperty("code").GetString()!)
            .Where(code => code.StartsWith("CLI-RENDER-", StringComparison.Ordinal))
            .Distinct()
            .Order(StringComparer.Ordinal)];
    }
}
