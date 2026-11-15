// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_rendering_the_secrets : given.a_plan
{
    string _file;

    void Establish() => Create();

    void Because() => _file = Plan.RenderSecretsFile();

    [Fact] void should_hold_each_secret_as_a_name_and_value_line() => _file.ShouldContain($"systems__Payments__secretKey={SecretValue}\n");
    [Fact] void should_hold_registry_credentials_by_host() => _file.ShouldContain($"Stage__Extensions__Packages__Registries__ghcr.io__Password={RegistryPassword}\n");
    [Fact] void should_hold_the_registry_user_name() => _file.ShouldContain("Stage__Extensions__Packages__Registries__ghcr.io__Username=reader\n");
    [Fact] void should_not_hold_public_settings() => _file.ShouldNotContain("statementDescriptor");
}
