// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetConfig.when_discovering_feeds;

public class and_the_config_carries_base64_credentials : given_a_configuration_directory
{
    void Because()
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("secret"));
        WriteConfig("nuget.config", $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <add key=\"private\" value=\"https://private.example/v3/index.json\" />\n  </packageSources>\n  <packageSourceCredentials>\n    <private>\n      <add key=\"Username\" value=\"user\" />\n      <add key=\"Password\" value=\"{encoded}\" />\n    </private>\n  </packageSourceCredentials>\n</configuration>");
        Feeds = NuGetConfig.ParseFeeds(Path.Combine(ConfigRoot, "nuget.config"));
    }

    [Fact] void should_decode_the_base64_password() => Feeds[0].Password.ShouldEqual("secret");
}
