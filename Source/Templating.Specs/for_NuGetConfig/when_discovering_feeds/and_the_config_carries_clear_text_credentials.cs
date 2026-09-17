// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetConfig.when_discovering_feeds;

public class and_the_config_carries_clear_text_credentials : given_a_configuration_directory
{
    void Because()
    {
        Environment.SetEnvironmentVariable("SPEC_FEED_USER", "resolved-user");
        WriteConfig("nuget.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <add key=\"private\" value=\"https://private.example/v3/index.json\" />\n  </packageSources>\n  <packageSourceCredentials>\n    <private>\n      <add key=\"Username\" value=\"%SPEC_FEED_USER%\" />\n      <add key=\"ClearTextPassword\" value=\"clear-secret\" />\n    </private>\n  </packageSourceCredentials>\n</configuration>");
        Feeds = NuGetConfig.ParseFeeds(Path.Combine(ConfigRoot, "nuget.config"));
    }

    void Destroy() => Environment.SetEnvironmentVariable("SPEC_FEED_USER", null);

    [Fact] void should_expand_environment_variables_in_the_username() => Feeds[0].Username.ShouldEqual("resolved-user");

    [Fact] void should_read_the_clear_text_password() => Feeds[0].Password.ShouldEqual("clear-secret");
}
