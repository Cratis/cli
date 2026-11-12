// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;

namespace Cratis.Cli.Commands.Run.for_StageExtensions.given;

/// <summary>
/// A plan for a model whose Payments system has a secret key, a public setting and a private registry.
/// </summary>
public class a_plan : Specification
{
    protected const string SecretValue = "sk_live_must_never_be_on_a_command_line";
    protected const string RegistryPassword = "registry-password-must-not-leak";
    protected const string PublisherKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEexample";
    protected const string Publisher = "https://github.com/cratis";

    protected StageExtensions Plan { get; set; }

    protected void Create(bool replacesModelProjectFile = false, string? secret = SecretValue, IReadOnlyList<string>? references = null)
    {
        var values = new RunExtensionValues(
            [new("systems__Payments__statementDescriptor", "ACME")],
            [new("systems__Payments__secretKey", secret ?? string.Empty)],
            []);
        var allowList = new ExtensionAllowList([new(Publisher, "sha256:abc", references)]);
        IReadOnlyDictionary<string, IReadOnlyList<string>> keys = new Dictionary<string, IReadOnlyList<string>> { [Publisher] = [PublisherKey] };
        var environment = new Dictionary<string, string>
        {
            ["extensions.registry.ghcr.io.username"] = "reader",
            ["extensions.registry.ghcr.io.token"] = RegistryPassword,
            ["extensions.npm.registry"] = "https://npm.example.com",
            ["authentication.providers.EntraId.clientSecret"] = "unrelated"
        };
        Plan = StageExtensions.Create(values, allowList, keys, environment, "/home/user/.cache/packages", replacesModelProjectFile);
    }
}
