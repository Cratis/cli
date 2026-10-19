// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_preparing;

[Collection(CliSpecsCollection.Name)]
public class and_the_model_has_errors : given.a_conform_namespace_command
{
    int? _result;

    void Establish() => File.WriteAllText(_model, "module Library\n  feature Registration\n    slice StateChange Register\n      command Register\n        produces Missing\n");

    void Because() => _result = _command.Prepare(_settings);

    [Fact] void should_refuse_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);
}
