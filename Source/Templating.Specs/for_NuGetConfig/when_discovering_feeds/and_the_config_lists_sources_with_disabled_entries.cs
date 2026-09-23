// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetConfig.when_discovering_feeds;

public class and_the_config_lists_sources_with_disabled_entries : given_a_configuration_directory
{
    void Because()
    {
        WriteConfig("nuget.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <add key=\"disabled-feed\" value=\"https://disabled.example/v3/index.json\" />\n    <add key=\"primary\" value=\"https://primary.example/v3/index.json\" />\n  </packageSources>\n  <disabledPackageSources>\n    <add key=\"disabled-feed\" value=\"true\" />\n  </disabledPackageSources>\n</configuration>");
        Feeds = NuGetConfig.ParseFeeds(Path.Combine(ConfigRoot, "nuget.config"));
    }

    [Fact] void should_read_the_enabled_source() => Feeds.Select(feed => feed.Name).ShouldContainOnly(["primary"]);

    [Fact] void should_skip_the_disabled_source() => Feeds.ShouldNotContain(feed => feed.Name == "disabled-feed");
}
