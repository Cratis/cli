// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_RunStackPlanner.given;

/// <summary>
/// A model that signs people in with GitHub and a partner's OpenID provider, and onboards them, planned against an environment holding every value it needs.
/// </summary>
public class an_authenticated_application : Specification
{
    public const string ClientSecret = "spec-client-secret-9f3a";
    public const string MailPassword = "spec-mail-password-77c1";

    protected string _modelRoot;
    protected string _runDirectory;
    protected string _mailFolder;
    protected AttestationKey _attestation;
    protected Dictionary<string, string> _values;
    protected HostUser _user;
    protected RunSettings _settings;
    protected string _model;

    protected string AuthenticationOnly => "domain Invoicing\n\nauthentication\n  provider GitHub\n  provider OpenId name Partner\n";

    protected string WithOnboarding => AuthenticationOnly +
        "\npersona Admin\n  description \"Runs the application\"\n  goal \"Run the application\"\n" +
        "\npersona Accountant\n  description \"Does the books\"\n  goal \"Do the books\"\n" +
        "\nonboarding\n  signup open creates tenant\n  invite tenant as Admin\n  invite member\n    by Admin\n    grant Accountant\n" +
        "  legal terms\n    file Legal/terms-of-service.md\n    file Legal/privacy-policy.md\n";

    void Establish()
    {
        _modelRoot = Directory.CreateTempSubdirectory("cratis-spec-model-").FullName;
        Directory.CreateDirectory(Path.Combine(_modelRoot, "Legal"));
        File.WriteAllText(Path.Combine(_modelRoot, "Legal", "terms-of-service.md"), "# Terms");
        File.WriteAllText(Path.Combine(_modelRoot, "Legal", "privacy-policy.md"), "# Privacy");
        _runDirectory = Path.Combine(_modelRoot, "run");
        _mailFolder = Path.Combine(_modelRoot, "mail");
        _attestation = new("spec-key", "-----BEGIN PRIVATE KEY-----\nprivate-spec\n-----END PRIVATE KEY-----", "-----BEGIN PUBLIC KEY-----\npublic-spec\n-----END PUBLIC KEY-----");
        _user = new("1000", "1000");
        _settings = new() { Tag = "1.2.0" };
        _values = new()
        {
            ["authentication.providers.GitHub.clientId"] = "github-client",
            ["authentication.providers.GitHub.clientSecret"] = ClientSecret,
            ["authentication.providers.Partner.clientId"] = "partner-client",
            ["authentication.providers.Partner.clientSecret"] = ClientSecret,
            ["authentication.providers.Partner.authority"] = "https://partner.example.com/"
        };
        _model = AuthenticationOnly;
    }

    void Destroy() => Directory.Delete(_modelRoot, true);

    protected RunStackPlan Plan()
    {
        var application = ApplicationOf(_model);
        var environment = new RunEnvironment("development", true, _values, [], _modelRoot);
        return RunStackPlanner.Plan(new(application, environment, _settings, _modelRoot, _runDirectory, "cratis-stage-ab12cd34", _mailFolder, _attestation, _user));
    }

    protected static ApplicationSyntax ApplicationOf(string model)
    {
        var parsed = new ScreenplayCompiler().Compile(model);
        parsed.Success.ShouldBeTrue();
        return parsed.Value!;
    }

    protected static bool CarriesSecrets(RunStackPlan plan, params string[] secrets) =>
        plan.Containers.SelectMany(_ => _.Arguments)
            .Concat(StageArguments(plan))
            .Any(argument => secrets.Any(secret => argument.Contains(secret, StringComparison.Ordinal)));

    protected static IReadOnlyList<string> StageArguments(RunStackPlan plan) =>
        StageContainer.BuildRunArguments("/model", "1.2.0", 9090, 35000, "cratis-stage-ab12cd34", runtime: plan.Stage);

    protected static string ValueAfter(IReadOnlyList<string> arguments, string option, string prefix) =>
        arguments.Zip(arguments.Skip(1)).Where(pair => pair.First == option && pair.Second.StartsWith(prefix, StringComparison.Ordinal)).Select(pair => pair.Second).First();
}
