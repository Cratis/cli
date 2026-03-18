// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GlobalSettings.when_debug_flag_is_set;

[Collection(CliSpecsCollection.Name)]
public class and_no_explicit_value_given : Specification
{
    GlobalSettings _settings;

    void Establish() => _settings = new GlobalSettings();

    [Fact] void should_default_to_false() => _settings.Debug.ShouldBeFalse();
}
