// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetConfig.when_discovering_feeds;

public class and_the_config_is_malformed_xml : given_a_configuration_directory
{
    Exception? _error;

    void Because()
    {
        var path = WriteConfig("nuget.config", "<configuration><not-closed>");
        _error = Catch.Exception(() => NuGetConfig.ParseFeeds(path));
    }

    [Fact] void should_tolerate_the_malformed_file_by_returning_no_feeds() => _error.ShouldBeNull();
}
