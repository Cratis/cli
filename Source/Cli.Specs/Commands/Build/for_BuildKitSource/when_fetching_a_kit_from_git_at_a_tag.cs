// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildKitSource;

public class when_fetching_a_kit_from_git_at_a_tag : Build.given.a_kit_folder
{
    string _repository;
    string _tagged;
    BuildKitFetched _fetched;

    async Task Establish()
    {
        _repository = Path.Combine(Path.GetDirectoryName(_kit)!, "kits");
        BuildFiles.CopyFolder(_kit, Path.Combine(_repository, "shell"));
        await Git("init", "--quiet");
        await Git("-c", "user.name=Spec", "-c", "user.email=spec@example.com", "-c", "commit.gpgsign=false", "add", ".");
        await Git("-c", "user.name=Spec", "-c", "user.email=spec@example.com", "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", "Kit 1.0.0");
        await Git("tag", "v1.0.0");
        _tagged = (await BuildProcess.Run("git", ["rev-parse", "HEAD"], _repository)).Output.Trim();
        await File.WriteAllTextAsync(Path.Combine(_repository, "shell", "build-kit.yaml"), Manifest.Replace("version: 1.0.0", "version: 2.0.0", StringComparison.Ordinal));
        await Git("-c", "user.name=Spec", "-c", "user.email=spec@example.com", "-c", "commit.gpgsign=false", "commit", "--quiet", "-am", "Kit 2.0.0");
    }

    async Task Because()
    {
        var scratch = Path.Combine(Path.GetDirectoryName(_kit)!, "scratch");
        Directory.CreateDirectory(scratch);
        _fetched = await new BuildKitSource(new HttpClient(), null, scratch).Fetch($"file://{_repository}#v1.0.0", "shell", CancellationToken.None);
    }

    [Fact] void should_load_the_kit_at_the_tag() => _fetched.Kit.Version.ShouldEqual("1.0.0");
    [Fact] void should_load_the_kit_from_the_named_folder() => _fetched.Kit.Name.ShouldEqual("shell-demo");
    [Fact] void should_record_the_commit_the_tag_resolved_to() => _fetched.Commit.ShouldEqual(_tagged);
    [Fact] void should_record_the_url() => _fetched.Source.ShouldEqual($"file://{_repository}#v1.0.0");
    [Fact] void should_not_record_a_checksum() => _fetched.Sha256.ShouldBeNull();

    async Task Git(params string[] arguments)
    {
        var result = await BuildProcess.Run("git", arguments, _repository);
        if (result.ExitCode != 0) throw new InvalidOperationException(result.Error);
    }
}
