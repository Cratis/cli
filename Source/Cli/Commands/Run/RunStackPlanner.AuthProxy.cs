// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

public static partial class RunStackPlanner
{
    /// <summary>
    /// The tenant AuthProxy resolves every request to, which Ante is told is the scope of its lobby.
    /// </summary>
    public const string LobbyScope = "Default";

    const string Contract = "Cratis__AuthProxy__Application__";

    static PlannedContainer PlanAuthProxy(RunStackInput input, string id, string network, OnboardingSyntax? onboarding, List<string> directories, List<PlannedFile> files)
    {
        var name = $"cratis-authproxy-{id}";
        var image = $"{AuthProxyImage}:{input.Settings.AuthProxyTag}";
        var port = input.Settings.AuthPort.ToString(CultureInfo.InvariantCulture);

        // Every value the environment holds for a provider is handed over as a file named by its key, which is how the contract reads secrets.
        files.AddRange(input.Environment.Values
            .Where(pair => pair.Key.StartsWith("authentication.providers.", StringComparison.Ordinal))
            .Select(pair => new PlannedFile($"authproxy/secrets/{pair.Key}", pair.Value)));
        if (onboarding is not null)
        {
            files.AddRange(OnboardingValues(input).Select(pair => new PlannedFile($"authproxy/secrets/{pair.Key}", pair.Value)));
        }

        List<KeyValuePair<string, string>> environment =
        [
            new($"{Contract}EnvironmentName", input.Environment.Name),
            new($"{Contract}Development", "true"),
            new($"{Contract}SecretsDirectory", SecretsPath),
            new("Cratis__AuthProxy__TenantResolutions__0__Strategy", "Default"),
            new("Cratis__AuthProxy__TenantResolutions__0__Options__TenantId", LobbyScope),
            new("Cratis__AuthProxy__DataProtectionKeysPath", "/tmp/authproxy-keys"),
            new("Cratis__AuthProxy__Services__app__Backend__BaseUrl", $"http://{StageAlias}:{StageContainer.ApiPort}/"),
            new("Cratis__AuthProxy__Services__app__Frontend__BaseUrl", $"http://{StageAlias}:{StageContainer.ApiPort}/")
        ];

        var providers = input.Application.Authentication!.Providers.ToArray();
        for (var index = 0; index < providers.Length; index++)
        {
            environment.Add(new($"{Contract}Providers__{index}__Kind", providers[index].Name));
            environment.Add(new($"{Contract}Providers__{index}__Identity", providers[index].Identity));
        }

        if (onboarding is not null)
        {
            // Two services share the one AuthProxy, told apart by the port the browser used: the application, and Ante's lobby. Both are on localhost, so the one sign-in
            // cookie (which belongs to a host, not a port) is sent to both, and both addresses are loopback, which is the only form of plain HTTP the contract accepts.
            environment.Add(new($"{Contract}Onboarding", "true"));
            environment.Add(new("Cratis__AuthProxy__Invite__MatchingTenantInvitationDestination", "Lobby"));
            environment.Add(new("Cratis__AuthProxy__Services__app__Hosts__0", $"localhost:{port}"));
            environment.Add(new("Cratis__AuthProxy__Services__lobby__Hosts__0", $"localhost:{input.Settings.LobbyPort.ToString(CultureInfo.InvariantCulture)}"));
            environment.Add(new("Cratis__AuthProxy__Services__lobby__Backend__BaseUrl", $"http://localhost:{AntePort}/"));
            environment.Add(new("Cratis__AuthProxy__Services__lobby__Frontend__BaseUrl", $"http://localhost:{AntePort}/"));
        }

        var arguments = new List<string> { "run", "--rm", "--name", name, "--network", network, "--network-alias", "authproxy" };
        if (input.User is { } user)
        {
            arguments.AddRange(["--user", user.Argument]);
        }

        arguments.AddRange(["-p", $"127.0.0.1:{port}:{AuthProxyPort}"]);
        if (onboarding is not null)
        {
            arguments.AddRange(["-p", $"127.0.0.1:{input.Settings.LobbyPort.ToString(CultureInfo.InvariantCulture)}:{AuthProxyPort}"]);
        }

        arguments.AddRange(environment.SelectMany(pair => new[] { "-e", $"{pair.Key}={pair.Value}" }));
        arguments.AddRange(new ContainerMount(Path.Combine(input.RunDirectory, "authproxy", "secrets"), SecretsPath).BuildArguments());
        arguments.Add(image);
        return new("authproxy", name, image, arguments);
    }

    static IEnumerable<KeyValuePair<string, string>> OnboardingValues(RunStackInput input)
    {
        var ante = $"http://localhost:{AntePort}";
        yield return new("onboarding.stageUrl", $"{ante}/_invite/stage");
        yield return new("onboarding.exchangeUrl", $"{ante}/_invite/exchange");
        yield return new("onboarding.tokenMetadataUrl", $"{ante}/.well-known/ante/invitation-token");
        yield return new("onboarding.issuer", AnteIssuer);
        yield return new("onboarding.audience", AnteAudience);
        yield return new("onboarding.lobbyFrontendBaseUrl", $"{LobbyAddress(input.Settings.LobbyPort)}/");
        yield return new("onboarding.attestation.issuer", AttestationIssuer);
        yield return new("onboarding.attestation.audience", AttestationAudience);
        yield return new("onboarding.attestation.keyId", input.Attestation.KeyId);
        yield return new("onboarding.attestation.privateKeyPem", input.Attestation.PrivatePem);
    }
}
