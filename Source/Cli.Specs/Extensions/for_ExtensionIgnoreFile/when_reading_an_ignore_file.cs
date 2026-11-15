// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionIgnoreFile;

public class when_reading_an_ignore_file : Specification
{
    ExtensionIgnoreFile _ignore = null!;

    void Because()
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-extension-ignore", Guid.NewGuid().ToString("N"))).FullName;
        File.WriteAllText(Path.Combine(folder, ".extensionignore"), "# comment\nsrc/\nDockerfile\n*.sln\n**/Specs/\n");
        _ignore = ExtensionIgnoreFile.Read(folder);
    }

    [Fact] void should_ignore_a_folder() => _ignore.Ignores("src/Program.cs").ShouldBeTrue();
    [Fact] void should_ignore_a_named_file() => _ignore.Ignores("Dockerfile").ShouldBeTrue();
    [Fact] void should_ignore_a_wildcard_at_the_root() => _ignore.Ignores("Demo.sln").ShouldBeTrue();
    [Fact] void should_not_let_a_wildcard_cross_a_folder() => _ignore.Ignores("assets/Demo.sln").ShouldBeFalse();
    [Fact] void should_ignore_a_folder_at_any_depth() => _ignore.Ignores("backend/Specs/a.cs").ShouldBeTrue();
    [Fact] void should_not_ignore_what_is_not_named() => _ignore.Ignores("assets/icon.png").ShouldBeFalse();
    [Fact] void should_always_ignore_what_every_package_ignores() => (_ignore.Ignores("README.md"), _ignore.Ignores(".git/config"), _ignore.Ignores("node_modules/x/y.js")).ShouldEqual((true, true, true));
}
