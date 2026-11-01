// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_preparing;

[Collection(CliSpecsCollection.Name)]
public class and_no_event_store_is_selected : given.a_conform_namespace_command
{
    int? _result;

    void Establish() => _settings.EventStore = null;

    void Because() => _result = _command.Prepare(_settings);

    [Fact] void should_refuse_before_connecting() => _result.ShouldEqual(ExitCodes.ValidationError);
}
