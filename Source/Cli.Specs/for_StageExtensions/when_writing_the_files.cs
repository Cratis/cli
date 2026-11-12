// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_writing_the_files : given.a_plan
{
    string _directory;
    StageExtensionFiles _files;
    UnixFileMode _secretsMode;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory("cratis-extension-files-").FullName;
        Create();
        Plan = Plan with { PackageCache = Path.Combine(_directory, "packages") };
    }

    async Task Because()
    {
        _files = await Plan.Write(_directory, CancellationToken.None);
        if (!OperatingSystem.IsWindows()) _secretsMode = File.GetUnixFileMode(_files.SecretsFile);
    }

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_write_the_secrets() => File.ReadAllText(_files.SecretsFile).ShouldContain(SecretValue);
    [Fact] void should_write_the_allow_list() => File.ReadAllText(_files.AllowListFile).ShouldContain(Publisher);
    [Fact] void should_create_the_package_cache() => Directory.Exists(Plan.PackageCache).ShouldBeTrue();
    [Fact] void should_make_the_secrets_readable_by_the_user_only() => (OperatingSystem.IsWindows() || _secretsMode == (UnixFileMode.UserRead | UnixFileMode.UserWrite)).ShouldBeTrue();
}
