// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.given;

/// <summary>
/// A package folder that passes every check that can run, for a specification to break in one way.
/// </summary>
public abstract class a_valid_package : Specification
{
    /// <summary>The bytes of a minimal PNG: its signature and some content.</summary>
    protected static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    protected static readonly string Contract = string.Join('\n', [
        "extension Demo",
        "  display name \"Demo\"",
        "  description \"A demo extension\"",
        "  icon \"assets/icon.png\"",
        "  image \"assets/shot.png\"",
        "    caption \"The demo in use\"",
        "  requires stage \"^5\"",
        "  requires platform web",
        "",
        "  ability Ping idempotent",
        "    text String",
        "",
        "  component Card",
        "    kind react",
        "    platform web",
        "    placeholder \"placeholders/card.html\"",
        "    property title String",
        "",
        "  backend runtime dotnet 9 assembly \"backend/Demo.dll\"",
        "  frontend \"frontend/index.js\""]);

    protected string Folder { get; private set; } = null!;

    protected ExtensionVerifier Verifier { get; } = new(
        new UnavailableRuntimeCheck(ExtensionVerifyCodes.FrontendCheck, ExtensionVerifyCodes.FrontendNotRun, "not run"),
        new UnavailableRuntimeCheck(ExtensionVerifyCodes.BackendCheck, ExtensionVerifyCodes.BackendNotRun, "not run"));

    protected ExtensionVerifyReport Report { get; set; } = null!;

    void Establish()
    {
        Folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-extension-verify", Guid.NewGuid().ToString("N"))).FullName;
        Write("contract.play", Contract);
        Write("assets/icon.png", Png);
        Write("assets/shot.png", Png);
        Write("placeholders/card.html", "<div style=\"padding:8px\"><img src=\"data:image/png;base64,AAAA\"/>Card</div>");
        Write("frontend/index.js", "export {};");
        Write("backend/Demo.dll", [1, 2, 3]);
    }

    protected void Write(string path, string content) => Write(path, System.Text.Encoding.UTF8.GetBytes(content));

    protected void Write(string path, byte[] content)
    {
        var full = Path.Combine(Folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
    }

    protected void ChangeContract(string from, string to) => Write("contract.play", Contract.Replace(from, to, StringComparison.Ordinal));

    protected Task<ExtensionVerifyReport> Run(ExtensionVerifyOptions? options = null) => Verifier.Verify("demo", Folder, options ?? new(CompilerVersion: "6.22.0"), CancellationToken.None);

    protected VerifyCheckResult Check(string name) => Report.Checks.Single(check => check.Check == name);

    protected string[] Codes(string check) => [.. Check(check).Diagnostics.Select(diagnostic => diagnostic.Code)];
}
