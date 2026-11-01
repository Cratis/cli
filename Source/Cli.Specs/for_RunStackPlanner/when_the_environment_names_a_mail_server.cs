// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunStackPlanner;

public class when_the_environment_names_a_mail_server : given.an_authenticated_application
{
    RunStackPlan _plan;
    IReadOnlyList<string> _stage;

    void Establish()
    {
        _model = WithOnboarding;
        _values["onboarding.email.sender"] = "welcome@example.com";
        _values["onboarding.email.server.host"] = "smtp.example.com";
        _values["onboarding.email.server.port"] = "587";
        _values["onboarding.email.server.username"] = "mailer";
        _values["onboarding.email.server.password"] = MailPassword;
    }

    void Because()
    {
        _plan = Plan();
        _stage = StageArguments(_plan);
    }

    [Fact] void should_not_collect_the_emails_in_a_folder() => _plan.MailFolder.ShouldBeNull();
    [Fact] void should_not_give_the_stage_a_mail_folder() => _stage.Any(_ => _.StartsWith("CRATIS_MAIL_PICKUP_FOLDER", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_hand_the_password_to_the_stage_as_a_file() => _plan.Files.Single(_ => _.RelativePath == "stage/secrets/onboarding.email.server.password").Content.ShouldEqual(MailPassword);
    [Fact] void should_hand_the_host_to_the_stage_as_a_file() => _plan.Files.Single(_ => _.RelativePath == "stage/secrets/onboarding.email.server.host").Content.ShouldEqual("smtp.example.com");
    [Fact] void should_never_put_the_password_in_an_argument() => CarriesSecrets(_plan, MailPassword).ShouldBeFalse();
    [Fact] void should_not_give_authproxy_the_mail_settings() => _plan.Files.Any(_ => _.RelativePath.StartsWith("authproxy/", StringComparison.Ordinal) && _.RelativePath.Contains("onboarding.email", StringComparison.Ordinal)).ShouldBeFalse();
}
