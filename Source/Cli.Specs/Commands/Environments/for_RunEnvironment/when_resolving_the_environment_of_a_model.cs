// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments.Secrets;
using Cratis.Screenplay;

namespace Cratis.Cli.Commands.Environments.for_RunEnvironment;

[Collection(CliSpecsCollection.Name)]
public class when_resolving_the_environment_of_a_model : given.an_environment_project
{
    const string Model = "domain Invoicing\n\nauthentication\n  provider GitHub\n  provider OpenId name Partner\n";

    RunEnvironment _absent;
    RunEnvironment _incomplete;
    RunEnvironment _complete;
    RunEnvironment _withMailServer;
    RunEnvironment _withoutProvider;

    async Task Because()
    {
        var application = new ScreenplayCompiler().Compile(Model).Value;
        var passphrase = PassphraseSources.Fixed("spec passphrase");

        _absent = await RunEnvironment.Resolve(application, _root, null, passphrase, CancellationToken.None);

        await Init("development");
        await SetPlain("authentication.providers.GitHub.clientId", "github-client");
        _incomplete = await RunEnvironment.Resolve(application, _root, null, passphrase, CancellationToken.None);

        await SetSecret("authentication.providers.GitHub.clientSecret", "github-secret");
        await SetPlain("authentication.providers.Partner.clientId", "partner-client");
        await SetSecret("authentication.providers.Partner.clientSecret", "partner-secret");
        await SetPlain("authentication.providers.Partner.authority", "https://partner.example.com/");
        await SetPlain("authentication.providers.Partner.scopes", "openid email");
        await SetPlain("authentication.providers.Other.clientId", "not-in-the-model");
        _complete = await RunEnvironment.Resolve(application, _root, null, passphrase, CancellationToken.None);

        await SetPlain("onboarding.email.server.host", "smtp.example.com");
        _withMailServer = await RunEnvironment.Resolve(new ScreenplayCompiler().Compile(Model + OnboardingModel).Value, _root, null, passphrase, CancellationToken.None);

        await RemoveHost();
        _withoutProvider = await RunEnvironment.Resolve(new ScreenplayCompiler().Compile("domain Invoicing\n\nauthentication\n  provider GitHub\n").Value, _root, null, passphrase, CancellationToken.None);
    }

    static string OnboardingModel => "\npersona Admin\n  description \"x\"\n  goal \"y\"\n\nonboarding\n  signup open creates tenant\n  invite tenant as Admin\n";

    Task RemoveHost() => Run(new ConfigRemoveCommand(), new ConfigKeySettings { Key = "onboarding.email.server.host" });

    [Fact] void should_report_an_environment_that_does_not_exist() => _absent.Exists.ShouldBeFalse();
    [Fact] void should_name_the_default_environment() => _absent.Name.ShouldEqual("development");
    [Fact] void should_list_every_key_the_model_needs_when_the_environment_does_not_exist() => _absent.Missing.Count.ShouldEqual(5);
    [Fact] void should_list_only_the_keys_that_are_missing() => _incomplete.Missing.Order().ToArray().ShouldEqual(["authentication.providers.GitHub.clientSecret", "authentication.providers.Partner.authority", "authentication.providers.Partner.clientId", "authentication.providers.Partner.clientSecret"]);
    [Fact] void should_resolve_nothing_while_keys_are_missing() => _incomplete.Values.ShouldBeEmpty();
    [Fact] void should_lack_nothing_when_every_key_is_set() => _complete.Missing.ShouldBeEmpty();
    [Fact] void should_resolve_a_secret_to_its_value() => _complete.Values["authentication.providers.GitHub.clientSecret"].ShouldEqual("github-secret");
    [Fact] void should_resolve_a_plain_value() => _complete.Values["authentication.providers.Partner.authority"].ShouldEqual("https://partner.example.com/");
    [Fact] void should_hand_on_other_settings_of_a_declared_provider() => _complete.Values["authentication.providers.Partner.scopes"].ShouldEqual("openid email");
    [Fact] void should_leave_out_a_provider_the_model_does_not_declare() => _complete.Values.ContainsKey("authentication.providers.Other.clientId").ShouldBeFalse();
    [Fact] void should_not_need_the_mail_settings_without_a_mail_server() => _complete.HasMailServer.ShouldBeFalse();
    [Fact] void should_need_every_mail_setting_once_the_environment_names_a_mail_server() => _withMailServer.Missing.Order().ToArray().ShouldEqual(["onboarding.email.sender", "onboarding.email.server.password", "onboarding.email.server.port", "onboarding.email.server.username"]);
    [Fact] void should_not_need_a_provider_the_model_no_longer_declares() => _withoutProvider.Missing.ShouldBeEmpty();
}
