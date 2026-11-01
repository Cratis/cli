// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

/// <summary>
/// Renders the canonical screen-composition corpus once, changes its <c language="csharp">ui profile</c> to name a package
/// the bundled target does not render, and renders again with the real planning and publication.
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class and_the_ui_profile_cannot_be_resolved : Specification
{
    string _root = null!;
    string _source = null!;
    string _destination = null!;
    string _previousDirectory = null!;
    int _firstResult;
    IReadOnlyDictionary<string, string> _before = null!;
    int _result;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-profile-render-{Guid.NewGuid():N}")).FullName;
        _source = Path.Combine(_root, "source");
        _destination = Path.Combine(_root, "out");
        foreach (var document in ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder").Documents)
        {
            var path = Path.Combine(_source, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }

        _previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_root);
        _firstResult = await Render();
        _before = Snapshot();

        var application = Path.Combine(_source, "application.play");
        var text = await File.ReadAllTextAsync(application);
        await File.WriteAllTextAsync(application, text.Replace("    Cratis.Components\n", "    Cratis.NotAPackage\n", StringComparison.Ordinal));
    }

    async Task Because() => _result = await Render();

    [Fact] void should_publish_the_authored_profile() => _firstResult.ShouldEqual(ExitCodes.Success);
    [Fact] void should_have_managed_artifacts_to_protect() => _before.ShouldNotBeEmpty();
    [Fact] void should_refuse_the_changed_profile() => _result.ShouldNotEqual(ExitCodes.Success);
    [Fact] void should_write_no_new_artifact() => Snapshot().Keys.Order(StringComparer.Ordinal).ShouldEqual(_before.Keys.Order(StringComparer.Ordinal));
    [Fact] void should_leave_every_managed_artifact_untouched() => Snapshot().ShouldEqual(_before);

    void Destroy()
    {
        Directory.SetCurrentDirectory(_previousDirectory);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    Task<int> Render() =>
        ((ICommand<RenderSettings>)new RenderCommand()).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "render", null),
            new RenderSettings
            {
                Path = _source,
                Destination = _destination,
                Name = ScreenCompositionCorpus.V1.ApplicationName,
                Output = OutputFormats.JsonCompact
            },
            CancellationToken.None);

    IReadOnlyDictionary<string, string> Snapshot() =>
        Directory.EnumerateFiles(_destination, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(_destination, path),
                path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
                StringComparer.Ordinal);
}
