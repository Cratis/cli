// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Environments.for_ConfigCommands;

[Collection(CliSpecsCollection.Name)]
public class when_listing_what_a_model_requires : given.an_environment_project
{
    const string SecretValue = "sk_live_must_never_be_printed";
    const string Model = "secret StripeKey\n  description \"The Stripe secret key\"\n\nsystem Payments\n  extension \"cratis/stripe\" version 2\n  setting secretKey from secret StripeKey\n\nmodule M\n  feature F\n    slice Automation S\n      event Settled\n        paymentIntentId String\n      reaction Settle\n        when Payments.PaymentSucceeded\n          paymentIntentId\n          produces Settled\n            for paymentIntentId\n            paymentIntentId = paymentIntentId\n";

    ExtensionProject _project;
    string _previousCache;
    int _missing;
    string _missingOutput;
    int _complete;
    string _completeOutput;
    int _stored;
    string _environmentFile;
    int _notAModel;

    async Task Because()
    {
        _project = new();
        _project.AllowPublisher();
        var package = ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2);
        _project.Registry.With(package);
        _project.Lock("2", package);
        await File.WriteAllTextAsync(Path.Combine(_project.Model, "payments.play"), Model);
        _previousCache = Environment.GetEnvironmentVariable(ExtensionCache.EnvironmentVariable);
        Environment.SetEnvironmentVariable(ExtensionCache.EnvironmentVariable, _project.Cache.Root);
        await _project.Service().Restore(_project.Workspace(), true, CancellationToken.None);

        await Init("development");
        _missing = await Run(new ConfigListCommand(), new ConfigListSettings { Requires = _project.Model });
        _missingOutput = _output.ToString();

        _output.GetStringBuilder().Clear();
        _stored = await SetSecret("secrets.StripeKey", SecretValue);
        _environmentFile = EnvironmentFile("development");
        _output.GetStringBuilder().Clear();
        _complete = await Run(new ConfigListCommand(), new ConfigListSettings { Requires = _project.Model });
        _completeOutput = _output.ToString();

        _notAModel = await Run(new ConfigListCommand(), new ConfigListSettings { Requires = _root });
    }

    void Destroy()
    {
        Environment.SetEnvironmentVariable(ExtensionCache.EnvironmentVariable, _previousCache);
        _project.Dispose();
    }

    [Fact] void should_fail_while_the_named_secret_is_missing() => _missing.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_named_secret_as_missing() => _missingOutput.ShouldContain("secrets.StripeKey");
    [Fact] void should_describe_what_it_is_for() => _missingOutput.ShouldContain("The Stripe secret key");
    [Fact] void should_store_the_secret() => _stored.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_write_the_secret_in_the_clear() => _environmentFile.ShouldNotContain(SecretValue);
    [Fact] void should_succeed_once_it_is_set() => _complete.ShouldEqual(ExitCodes.Success);
    [Fact] void should_never_print_the_secret_value() => (_missingOutput + _completeOutput).ShouldNotContain(SecretValue);
    [Fact] void should_refuse_a_folder_that_is_not_a_model() => _notAModel.ShouldEqual(ExitCodes.ValidationError);
}
