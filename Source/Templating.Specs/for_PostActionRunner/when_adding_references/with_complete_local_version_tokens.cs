// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_references;

[Collection(TemplatingSpecsCollection.Name)]
public class with_complete_local_version_tokens : given_a_project_to_finish
{
    [Theory]
    [InlineData("1.0.0", "2.0.0-beta", "1.0.0")]
    [InlineData("1.0.0+build.7", "2.0.0-beta", "1.0.0+build.7")]
    [InlineData("1.0.0-beta", "2.0.0-beta", "2.0.0-beta")]
    [InlineData("01.002.0003", "2.0.0-beta", "1.2.3")]
    async Task should_resolve_a_stable_full_token(string stable, string prerelease, string expected)
    {
        await File.WriteAllTextAsync(Path.Combine(FeedRoot, $"Mixed.Package.{stable}.nupkg"), string.Empty, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(FeedRoot, $"Mixed.Package.{prerelease}.nupkg"), string.Empty, TestContext.Current.CancellationToken);
        var project = WriteProject("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var results = await new PostActionRunner().Run(
            new TemplateConfig { Name = "T", ShortName = "t", PostActions = [PackageReferenceAction("Mixed.Package")] },
            ResultFor("App.csproj"),
            new Dictionary<string, string>(),
            cancellationToken: TestContext.Current.CancellationToken);
        results[0].Outcome.ShouldEqual(PostActionOutcome.Succeeded);
        (await File.ReadAllTextAsync(project, TestContext.Current.CancellationToken)).ShouldContain($"Version=\"{expected}\"");
    }
}
