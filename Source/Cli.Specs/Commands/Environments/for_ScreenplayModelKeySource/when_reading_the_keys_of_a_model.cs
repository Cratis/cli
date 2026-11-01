// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments.for_ScreenplayModelKeySource;

[Collection(CliSpecsCollection.Name)]
public class when_reading_the_keys_of_a_model : given.an_environment_project
{
    string _model;
    IReadOnlyList<string> _keys;
    Exception _broken;
    bool _canReadPlayFile;
    bool _canReadKeyList;
    int _missingResult;
    string _missingOutput;

    async Task Because()
    {
        _model = Path.Combine(_root, "model");
        Directory.CreateDirectory(_model);
        var file = Path.Combine(_model, "invoicing.play");
        await File.WriteAllTextAsync(file, "domain Invoicing\n\nauthentication\n  provider EntraId\n  provider OpenId name Partner\n");
        var source = new ScreenplayModelKeySource();
        _keys = source.ReadKeys(_model);
        _canReadPlayFile = source.CanRead(file);

        await File.WriteAllTextAsync(Path.Combine(_root, "keys.txt"), "a\nb\n");
        _canReadKeyList = source.CanRead(Path.Combine(_root, "keys.txt"));

        await File.WriteAllTextAsync(Path.Combine(_model, "broken.play"), "this is not screenplay");
        _broken = Catch.Exception(() => source.ReadKeys(_model));
        File.Delete(Path.Combine(_model, "broken.play"));

        await Init("development");
        await SetPlain("authentication.providers.EntraId.clientId", "client");
        _missingResult = await Run(new ConfigListCommand(), new ConfigListSettings { Missing = "model" });
        _missingOutput = _output.ToString();
    }

    [Fact] void should_read_a_folder_of_play_files() => _canReadPlayFile.ShouldBeTrue();
    [Fact] void should_leave_key_lists_to_the_key_list_source() => _canReadKeyList.ShouldBeFalse();
    [Fact] void should_need_the_client_id_of_each_provider() => _keys.ShouldContain("authentication.providers.EntraId.clientId");
    [Fact] void should_need_the_client_secret_of_each_provider() => _keys.ShouldContain("authentication.providers.EntraId.clientSecret");
    [Fact] void should_need_the_tenant_of_an_entra_id_provider() => _keys.ShouldContain("authentication.providers.EntraId.tenantId");
    [Fact] void should_need_the_authority_of_an_openid_provider_by_its_alias() => _keys.ShouldContain("authentication.providers.Partner.authority");
    [Fact] void should_need_nothing_for_onboarding_when_the_model_declares_none() => _keys.Any(_ => _.StartsWith("onboarding.", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_refuse_a_model_that_does_not_compile() => _broken.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_config_missing_when_keys_are_missing() => _missingResult.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_list_the_missing_key_of_the_model() => _missingOutput.ShouldContain("authentication.providers.EntraId.tenantId");
    [Fact] void should_not_list_a_key_that_is_set() => _missingOutput.ShouldNotContain("authentication.providers.EntraId.clientId");
}
