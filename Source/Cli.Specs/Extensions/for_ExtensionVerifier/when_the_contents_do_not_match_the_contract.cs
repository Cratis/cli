// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_contents_do_not_match_the_contract : given.a_valid_package
{
    ExtensionVerifyReport _undeclared = null!;
    ExtensionVerifyReport _ignored = null!;
    ExtensionVerifyReport _noBackend = null!;
    ExtensionVerifyReport _noFrontendFile = null!;
    ExtensionVerifyReport _noFrontendDeclared = null!;

    async Task Because()
    {
        Write("stray/notes.txt", "x");
        Write("frontend/chunk.js", "export {};");
        Write("backend/Demo.deps.json", "{}");
        _undeclared = await Run();

        Write(".extensionignore", "# the build ships none of these\nstray/\n");
        _ignored = await Run();

        File.Delete(Path.Combine(Folder, "backend", "Demo.dll"));
        _noBackend = await Run();
        Write("backend/Demo.dll", [1]);

        File.Delete(Path.Combine(Folder, "frontend", "index.js"));
        _noFrontendFile = await Run();
        Write("frontend/index.js", "export {};");

        ChangeContract("  frontend \"frontend/index.js\"", string.Empty);
        _noFrontendDeclared = await Run();
    }

    static string[] Codes(ExtensionVerifyReport report, string check) => [.. report.Checks.Single(item => item.Check == check).Diagnostics.Select(diagnostic => diagnostic.Code)];

    [Fact] void should_flag_a_file_the_contract_does_not_declare() => _undeclared.Diagnostics.Single(diagnostic => diagnostic.Code == "EXTV022").Path.ShouldEqual("stray/notes.txt");
    [Fact] void should_accept_files_beside_the_frontend_entry_and_the_backend() => _undeclared.Diagnostics.Count(diagnostic => diagnostic.Code == "EXTV022").ShouldEqual(1);
    [Fact] void should_accept_what_the_ignore_file_names() => Codes(_ignored, "contents").ShouldBeEmpty();
    [Fact] void should_flag_a_backend_that_is_not_shipped() => Codes(_noBackend, "contents").ShouldContain(ExtensionVerifyCodes.BackendNotShipped);
    [Fact] void should_flag_a_frontend_that_is_not_shipped() => Codes(_noFrontendFile, "contents").ShouldContain("PLAY0998");
    [Fact] void should_flag_a_component_when_no_frontend_is_declared() => Codes(_noFrontendDeclared, "contents").ShouldContain(ExtensionVerifyCodes.ComponentWithoutFrontend);
}
