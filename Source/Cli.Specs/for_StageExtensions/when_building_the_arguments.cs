// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_building_the_arguments : given.a_plan
{
    IReadOnlyList<string> _arguments;
    string _line;

    void Establish() => Create();

    void Because()
    {
        _arguments = Plan.BuildArguments(new("/run/secrets.env", "/run/extensions.json"), "/eventmodel");
        _line = string.Join(' ', _arguments);
    }

    [Fact] void should_turn_the_extension_protocol_on() => _line.ShouldContain("-e Stage__Extensions__HostPort=50100");
    [Fact] void should_pass_public_settings_as_environment() => _line.ShouldContain("-e systems__Payments__statementDescriptor=ACME");
    [Fact] void should_pass_the_npm_registry_as_environment() => _line.ShouldContain("-e Stage__Extensions__Packages__NpmRegistry=https://npm.example.com");
    [Fact] void should_never_put_a_secret_on_the_command_line() => _line.ShouldNotContain(SecretValue);
    [Fact] void should_never_put_a_registry_credential_on_the_command_line() => _line.ShouldNotContain(RegistryPassword);
    [Fact] void should_hand_secrets_over_in_a_file() => _line.ShouldContain("--env-file /run/secrets.env");
    [Fact] void should_keep_packages_in_a_cache_that_survives_the_run() => _line.ShouldContain("-v /home/user/.cache/packages:/stage-extension-cache");
    [Fact] void should_tell_stage_where_the_cache_is() => _line.ShouldContain("-e Stage__Extensions__Packages__CacheDirectory=/stage-extension-cache");
    [Fact] void should_give_stage_the_allow_list_where_it_looks_for_the_project_file() => _line.ShouldContain("-v /run/extensions.json:/.cratis/extensions.json:ro");
    [Fact] void should_not_replace_a_project_file_the_model_folder_does_not_have() => _line.ShouldNotContain("/eventmodel/.cratis/extensions.json");
    [Fact] void should_not_leak_unrelated_keys() => _line.ShouldNotContain("unrelated");
}
