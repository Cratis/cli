// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunStackPlanner;

public class when_planning_authentication_only : given.an_authenticated_application
{
    RunStackPlan _plan;
    PlannedContainer _authProxy;
    IReadOnlyList<string> _stage;

    void Because()
    {
        _plan = Plan();
        _authProxy = _plan.Containers.Single();
        _stage = StageArguments(_plan);
    }

    [Fact] void should_start_only_authproxy_beside_the_stage() => _plan.Containers.Select(_ => _.Role).ToArray().ShouldEqual(["authproxy"]);
    [Fact] void should_name_the_network_after_the_run() => _plan.Network.ShouldEqual("cratis-run-ab12cd34");
    [Fact] void should_run_the_image_of_the_tag() => _authProxy.Image.ShouldEqual("cratis/authproxy:latest");
    [Fact] void should_publish_authproxy_on_loopback() => _authProxy.Arguments.ShouldContain("127.0.0.1:8080:8080");
    [Fact] void should_make_authproxy_the_entry_point() => _plan.EntryUrl.ShouldEqual("http://localhost:8080");
    [Fact] void should_join_authproxy_to_the_network() => ValueAfter(_authProxy.Arguments, "--network", "cratis-run-").ShouldEqual(_plan.Network);
    [Fact] void should_run_authproxy_as_the_host_user() => ValueAfter(_authProxy.Arguments, "--user", "1000").ShouldEqual("1000:1000");
    [Fact] void should_tell_authproxy_it_is_a_development_run() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Application__Development=true");
    [Fact] void should_name_the_environment() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Application__EnvironmentName=development");
    [Fact] void should_hand_over_the_first_provider() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Application__Providers__0__Kind=GitHub");
    [Fact] void should_hand_over_the_aliased_provider_by_its_identity() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Application__Providers__1__Identity=Partner");
    [Fact] void should_route_to_the_stage() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Services__app__Backend__BaseUrl=http://stage:9090/");
    [Fact] void should_not_declare_onboarding() => _authProxy.Arguments.ShouldNotContain("Cratis__AuthProxy__Application__Onboarding=true");
    [Fact] void should_mount_the_secrets_read_only() => _authProxy.Arguments.ShouldContain($"{Path.Combine(_runDirectory, "authproxy", "secrets")}:/run/cratis/secrets:ro");
    [Fact] void should_point_authproxy_at_the_mounted_secrets() => _authProxy.Arguments.ShouldContain("Cratis__AuthProxy__Application__SecretsDirectory=/run/cratis/secrets");
    [Fact] void should_write_each_value_as_a_file_named_by_its_key() => _plan.Files.Single(_ => _.RelativePath == "authproxy/secrets/authentication.providers.GitHub.clientSecret").Content.ShouldEqual(ClientSecret);
    [Fact] void should_never_put_a_secret_in_an_argument() => CarriesSecrets(_plan, ClientSecret).ShouldBeFalse();
    [Fact] void should_publish_the_stage_on_loopback_only() => _stage.ShouldContain("127.0.0.1:9090:9090");
    [Fact] void should_name_the_stage_on_the_network() => ValueAfter(_stage, "--network-alias", "stage").ShouldEqual("stage");
    [Fact] void should_pin_the_event_store_name() => _stage.ShouldContain("STAGE_EVENT_STORE=Application");
    [Fact] void should_give_the_stage_its_environment() => _stage.ShouldContain("CRATIS_ENVIRONMENT=development");
    [Fact] void should_give_the_stage_the_secrets_directory() => _stage.ShouldContain("CRATIS_SECRETS_DIRECTORY=/run/cratis/secrets");
    [Fact] void should_give_the_stage_the_chronicle_connection_string_as_a_file() => _stage.ShouldContain("CRATIS_CHRONICLE_CONNECTION_STRING_FILE=/run/cratis/chronicle-connection-string");
    [Fact] void should_not_collect_emails_without_onboarding() => _plan.MailFolder.ShouldBeNull();
    [Fact] void should_list_authproxy_as_the_only_service() => _plan.Services.Select(_ => _.Url).ToArray().ShouldEqual(["http://localhost:8080"]);
}
