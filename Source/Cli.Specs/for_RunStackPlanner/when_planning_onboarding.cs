// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunStackPlanner;

public class when_planning_onboarding : given.an_authenticated_application
{
    RunStackPlan _plan;
    PlannedContainer _authProxy;
    PlannedContainer _ante;
    IReadOnlyList<string> _stage;

    void Establish() => _model = WithOnboarding;

    void Because()
    {
        _plan = Plan();
        _authProxy = _plan.Containers.Single(_ => _.Role == "authproxy");
        _ante = _plan.Containers.Single(_ => _.Role == "ante");
        _stage = StageArguments(_plan);
    }

    [Fact] void should_start_the_database_first_then_authproxy_then_ante() => _plan.Containers.Select(_ => _.Role).ToArray().ShouldEqual(["mongo", "authproxy", "ante"]);
    [Fact] void should_share_authproxys_network_stack_with_ante() => ValueAfter(_ante.Arguments, "--network", "container:").ShouldEqual($"container:{_authProxy.Name}");
    [Fact] void should_run_ante_as_the_host_user() => ValueAfter(_ante.Arguments, "--user", "1000").ShouldEqual("1000:1000");
    [Fact] void should_not_publish_ante() => _ante.Arguments.ShouldNotContain("-p");
    [Fact] void should_run_ante_from_the_tag() => _ante.Image.ShouldEqual("ghcr.io/cratis/ante:1.6.0");
    [Fact] void should_turn_the_onboarding_contract_on_in_development_mode() => _ante.Arguments.ShouldContain("Ante__Onboarding__Development__Enabled=true");
    [Fact] void should_use_the_stages_event_store_as_the_host_store() => _ante.Arguments.ShouldContain("Ante__Onboarding__HostStore=Application");
    [Fact] void should_let_anyone_sign_up() => _ante.Arguments.ShouldContain("Ante__Onboarding__RegistrationEnabled=true");
    [Fact] void should_create_a_tenant_on_signup() => _ante.Arguments.ShouldContain("Ante__Onboarding__SignUp=CreateTenant");
    [Fact] void should_allow_both_invitation_types() => _ante.Arguments.Where(_ => _.StartsWith("Ante__Onboarding__InvitationTypes__", StringComparison.Ordinal)).Order().ToArray().ShouldEqual(["Ante__Onboarding__InvitationTypes__0=CreateTenant", "Ante__Onboarding__InvitationTypes__1=JoinTenant"]);
    [Fact] void should_allow_only_the_personas_an_invitation_grants() => _ante.Arguments.Where(_ => _.StartsWith("Ante__Onboarding__Personas__", StringComparison.Ordinal)).Order().ToArray().ShouldEqual(["Ante__Onboarding__Personas__0=Admin", "Ante__Onboarding__Personas__1=Accountant"]);
    [Fact] void should_connect_to_the_stages_chronicle() => _ante.Arguments.ShouldContain("Cratis__Chronicle__ConnectionString=chronicle://stage:35000/?auth=none");
    [Fact] void should_connect_to_the_database() => _ante.Arguments.ShouldContain("Cratis__MongoDB__Server=mongodb://mongo:27017");
    [Fact] void should_mount_the_terms_read_only() => _ante.Arguments.ShouldContain($"{Path.Combine(_modelRoot, "Legal", "terms-of-service.md")}:/run/cratis/legal/terms.md:ro");
    [Fact] void should_mount_the_privacy_policy_read_only() => _ante.Arguments.ShouldContain($"{Path.Combine(_modelRoot, "Legal", "privacy-policy.md")}:/run/cratis/legal/privacy.md:ro");
    [Fact] void should_point_ante_at_the_terms() => _ante.Arguments.ShouldContain("Ante__Onboarding__Legal__TermsAndConditions=/run/cratis/legal/terms.md");
    [Fact] void should_trust_authproxys_attestation_key_as_a_file() => _plan.Files.Single(_ => _.RelativePath == "ante/secrets/Ante__Invitations__Exchange__Attestation__PublicKeys__0__PublicKeyPem").Content.ShouldContain("public-spec");
    [Fact] void should_give_authproxy_the_private_key_as_a_file() => _plan.Files.Single(_ => _.RelativePath == "authproxy/secrets/onboarding.attestation.privateKeyPem").Content.ShouldContain("private-spec");
    [Fact] void should_not_give_ante_the_private_key() => _plan.Files.Any(_ => _.RelativePath.StartsWith("ante/", StringComparison.Ordinal) && _.Content.Contains("private-spec", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_pin_the_assurance_of_each_provider() => _ante.Arguments.ShouldContain("Ante__Invitations__Exchange__Attestation__Providers__0__AcceptableAssurances__0=oauth");
    [Fact] void should_pin_the_issuer_of_the_openid_provider() => _ante.Arguments.ShouldContain("Ante__Invitations__Exchange__Attestation__Providers__1__Issuer=https://partner.example.com/");
    [Fact] void should_tell_authproxy_about_onboarding() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Application__Onboarding=true");
    [Fact] void should_reach_ante_from_authproxy_on_loopback() => _plan.Files.Single(_ => _.RelativePath == "authproxy/secrets/onboarding.stageUrl").Content.ShouldEqual("http://localhost:5002/_invite/stage");
    [Fact] void should_send_people_to_the_lobby() => _plan.Files.Single(_ => _.RelativePath == "authproxy/secrets/onboarding.lobbyFrontendBaseUrl").Content.ShouldEqual("http://localhost:8081/");
    [Fact] void should_route_the_lobby_port_to_ante() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Services__lobby__Hosts__0=localhost:8081");
    [Fact] void should_route_the_application_port_to_the_stage() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Services__app__Hosts__0=localhost:8080");
    [Fact] void should_publish_the_lobby_port_on_loopback() => _authProxy.Arguments.ShouldContain("127.0.0.1:8081:8080");
    [Fact] void should_reach_ante_from_the_lobby_service() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Services__lobby__Backend__BaseUrl=http://localhost:5002/");
    [Fact] void should_list_the_lobby_as_a_service() => _plan.Services.Select(_ => _.Url).ToArray().ShouldEqual(["http://localhost:8080", "http://localhost:8081"]);
    [Fact] void should_write_onboarding_emails_to_a_folder() => _plan.MailFolder.ShouldEqual(_mailFolder);
    [Fact] void should_give_the_stage_the_mail_folder() => _stage.ShouldContain("CRATIS_MAIL_PICKUP_FOLDER=/run/cratis-mail");
    [Fact] void should_mount_the_mail_folder_writable() => _stage.ShouldContain($"{_mailFolder}:/run/cratis-mail");
    [Fact] void should_never_put_a_secret_in_an_argument() => CarriesSecrets(_plan, ClientSecret, "private-spec", "BEGIN PRIVATE KEY").ShouldBeFalse();
}
