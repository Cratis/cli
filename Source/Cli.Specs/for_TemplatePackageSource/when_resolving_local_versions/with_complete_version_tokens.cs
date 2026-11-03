// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.Specs.for_TemplatePackageSource.when_resolving_local_versions;

public class with_complete_version_tokens : Specification
{
    string _root = null!;

    void Establish() => _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-source-specs", Guid.NewGuid().ToString("N"))).FullName;

    [Theory]
    [InlineData("1.0.0", "2.0.0-beta", "1.0.0")]
    [InlineData("1.0.0+build.7", "2.0.0-beta", "1.0.0+build.7")]
    [InlineData("1.0.0-beta", "2.0.0-beta", "2.0.0-beta")]
    [InlineData("01.002.0003", "2.0.0-beta", "1.2.3")]
    async Task should_prefer_the_stable_full_token(string stable, string prerelease, string expected)
    {
        await File.WriteAllTextAsync(Path.Combine(_root, $"Mixed.Package.{stable}.nupkg"), string.Empty, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_root, $"Mixed.Package.{prerelease}.nupkg"), string.Empty, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_root, "nuget.config"), $"<configuration><packageSources><clear/><add key=\"local\" value=\"{_root}\"/></packageSources></configuration>", TestContext.Current.CancellationToken);
        var source = new TemplatePackageSource(Path.Combine(_root, "store"), _root);
        (await source.GetLatestVersion("Mixed.Package")).ShouldEqual(expected);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
