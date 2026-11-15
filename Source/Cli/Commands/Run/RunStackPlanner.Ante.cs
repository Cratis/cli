// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Environments;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

public static partial class RunStackPlanner
{
    const string AnteStore = "Ante";
    const string AnteNamespace = "Default";
    const string LegalPath = "/run/cratis/legal";
    const string DataPath = "/run/cratis/data";
    const string Onboarding = "Ante__Onboarding__";

    static string AnteIssuer => $"urn:cratis:ante:{AnteStore}:{AnteNamespace}";

    static string AnteAudience => $"{AnteIssuer}:lobby";

    static string AttestationIssuer => "urn:cratis:authproxy:local";

    static string AttestationAudience => AnteIssuer;

    static PlannedContainer PlanAnte(RunStackInput input, string id, OnboardingSyntax onboarding, List<string> directories, List<PlannedFile> files)
    {
        var authProxy = $"cratis-authproxy-{id}";
        var name = $"cratis-ante-{id}";
        var image = $"{AnteImage}:{input.Settings.AnteTag}";
        directories.AddRange(["ante", "ante/secrets", "ante/data"]);

        // The public half of the attestation key is not a secret, but it is multi-line, so it travels as a file named after the setting it fills.
        files.Add(new("ante/secrets/Ante__Invitations__Exchange__Attestation__PublicKeys__0__PublicKeyPem", input.Attestation.PublicPem));

        var environment = AnteEnvironment(input, onboarding);
        var mounts = new List<ContainerMount>
        {
            new(Path.Combine(input.RunDirectory, "ante", "secrets"), SecretsPath),
            new(Path.Combine(input.RunDirectory, "ante", "data"), DataPath, ReadOnly: false)
        };
        mounts.AddRange(LegalMounts(input, onboarding, environment));

        // Ante shares AuthProxy's network stack: AuthProxy reaches it on loopback, which is the only address it accepts over plain HTTP, and nothing else reaches it at all.
        var arguments = new List<string> { "run", "--rm", "--name", name, "--network", $"container:{authProxy}" };
        if (input.User is { } user)
        {
            arguments.AddRange(["--user", user.Argument]);
        }

        arguments.AddRange(environment.SelectMany(pair => new[] { "-e", $"{pair.Key}={pair.Value}" }));
        arguments.AddRange(mounts.SelectMany(mount => mount.BuildArguments()));
        arguments.Add(image);
        return new("ante", name, image, arguments);
    }

    static List<KeyValuePair<string, string>> AnteEnvironment(RunStackInput input, OnboardingSyntax onboarding)
    {
        var invitations = onboarding.Invitations.ToArray();
        var personas = invitations.SelectMany(_ => _.Grant).Distinct(StringComparer.Ordinal).ToArray();
        var types = invitations.Select(_ => _.Kind == OnboardingInvitationKind.Member ? "JoinTenant" : "CreateTenant").Distinct(StringComparer.Ordinal).ToArray();
        var signup = onboarding.Signup;
        var publicUrl = $"http://localhost:{AntePort}";

        List<KeyValuePair<string, string>> environment =
        [
            new("ASPNETCORE_URLS", $"http://127.0.0.1:{AntePort}"),
            new("ASPNETCORE_ENVIRONMENT", "Development"),

            // AuthProxy strips client identity headers, and Ante is loopback-only in their shared network stack.
            new("Cratis__Arc__TrustForwardedIdentityHeaders", "true"),
            new($"{Onboarding}Enabled", "true"),
            new($"{Onboarding}Development__Enabled", "true"),
            new($"{Onboarding}Development__DataPath", DataPath),
            new($"{Onboarding}Secrets__Path", SecretsPath),
            new($"{Onboarding}HostStore", AppStore),
            new($"{Onboarding}EventStore", AnteStore),
            new($"{Onboarding}Namespace", AnteNamespace),
            new($"{Onboarding}PublicBaseUrl", publicUrl),
            new($"{Onboarding}ApplicationUrl", $"http://localhost:{input.Settings.AuthPort}/"),
            new($"{Onboarding}SignUp", signup?.Outcome == OnboardingSignupOutcome.JoinsTenant ? "JoinTenant" : "CreateTenant"),
            new($"{Onboarding}RegistrationEnabled", (signup is not null).ToString().ToLowerInvariant()),
            new("Cratis__Chronicle__ConnectionString", $"chronicle://{StageAlias}:{StageContainer.WorkbenchPort}/?auth=none"),
            new("Cratis__MongoDB__Server", $"mongodb://{MongoAlias}:27017"),
            new("Cratis__MongoDB__Database", "Ante"),
            new("Ante__Invitations__Exchange__Mode", "Attested"),
            new("Ante__Invitations__Exchange__Attestation__Issuer", AttestationIssuer),
            new("Ante__Invitations__Exchange__Attestation__Audience", AttestationAudience),
            new("Ante__Invitations__Exchange__Attestation__LobbyScope", LobbyScope),
            new("Ante__Invitations__Exchange__Attestation__PublicKeys__0__KeyId", input.Attestation.KeyId)
        ];
        environment.AddRange(personas.Select((persona, index) => new KeyValuePair<string, string>($"{Onboarding}Personas__{index}", persona)));
        environment.AddRange(types.Select((type, index) => new KeyValuePair<string, string>($"{Onboarding}InvitationTypes__{index}", type)));
        environment.AddRange(AttestedProviders(input));
        return environment;
    }

    static IEnumerable<KeyValuePair<string, string>> AttestedProviders(RunStackInput input)
    {
        var providers = input.Application.Authentication!.Providers.ToArray();
        for (var index = 0; index < providers.Length; index++)
        {
            var provider = providers[index];
            var prefix = $"Ante__Invitations__Exchange__Attestation__Providers__{index}__";
            yield return new($"{prefix}Key", provider.Identity.ToLowerInvariant());
            yield return new($"{prefix}Issuer", ProviderIssuer(input.Environment, provider));
            yield return new($"{prefix}AcceptableAssurances__0", provider.Name == "GitHub" ? "oauth" : "oidc");
        }
    }

    static string ProviderIssuer(RunEnvironment environment, AuthenticationProviderSyntax provider)
    {
        string? Value(string setting) => environment.Values.GetValueOrDefault(EnvironmentRequirements.ProviderKey(provider, setting));
        return provider.Name switch
        {
            "EntraId" => Value("authority") ?? $"https://login.microsoftonline.com/{Value("tenantId")}/v2.0",
            "OpenId" => Value("authority") ?? string.Empty,
            "Google" => Value("authority") ?? "https://accounts.google.com",
            "Apple" => Value("authority") ?? "https://appleid.apple.com",
            _ => Value("issuer") ?? "https://github.com"
        };
    }

    static IEnumerable<ContainerMount> LegalMounts(RunStackInput input, OnboardingSyntax onboarding, List<KeyValuePair<string, string>> environment)
    {
        var documents = onboarding.LegalTerms?.Documents.Select(_ => _.Path).ToArray() ?? [];
        if (documents.Length == 0)
        {
            yield break;
        }

        var privacy = documents.FirstOrDefault(path => path.Contains("privacy", StringComparison.OrdinalIgnoreCase));
        var terms = documents.FirstOrDefault(path => path != privacy);
        if (terms is not null)
        {
            environment.Add(new($"{Onboarding}Legal__TermsAndConditions", $"{LegalPath}/terms.md"));
            yield return new(ResolveLegal(input.ModelRoot, terms), $"{LegalPath}/terms.md");
        }

        if (privacy is not null)
        {
            environment.Add(new($"{Onboarding}Legal__PrivacyPolicy", $"{LegalPath}/privacy.md"));
            yield return new(ResolveLegal(input.ModelRoot, privacy), $"{LegalPath}/privacy.md");
        }
    }

    static string ResolveLegal(string root, string path)
    {
        var resolved = Path.GetFullPath(Path.Combine(root, path));
        return File.Exists(resolved) && resolved.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal)
            ? resolved
            : throw new InvalidOperationException($"The legal terms file '{path}' does not exist under '{root}'");
    }
}
