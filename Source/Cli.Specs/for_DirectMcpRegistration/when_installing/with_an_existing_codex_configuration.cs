// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_an_existing_codex_configuration : given.a_home_and_a_project
{
    byte[] _original;

    void Establish()
    {
        _original = Encoding.UTF8.GetBytes("# café\r\nmodel = \"example\"\r\n");
        Directory.CreateDirectory(HomeFile(".codex"));
        File.WriteAllBytes(HomeFile(".codex/config.toml"), _original);
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["codex"]);

    [Fact] void should_preserve_the_exact_original_bytes() => File.ReadAllBytes(Directory.GetFiles(HomeFile(".codex"), "config.toml.*.bak").Single()).ShouldEqual(_original);
}
