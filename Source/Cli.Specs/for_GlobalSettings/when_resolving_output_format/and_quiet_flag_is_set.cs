// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GlobalSettings.when_resolving_output_format;

[Collection(CliSpecsCollection.Name)]
public class and_quiet_flag_is_set : Specification
{
    GlobalSettings _settings;
    string _result;

    void Establish() => _settings = new GlobalSettings { Quiet = true, Output = OutputFormats.Table };

    void Because() => _result = _settings.ResolveOutputFormat();

    [Fact] void should_return_quiet_regardless_of_output_flag() => _result.ShouldEqual(OutputFormats.Quiet);
}
