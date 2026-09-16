// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetConfig.when_discovering_feeds;

public class given_a_configuration_directory : Specification
{
    protected string ConfigRoot = null!;
    protected IReadOnlyList<NuGetFeed> Feeds = null!;

    void Establish()
    {
        ConfigRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-nugetconfig", Guid.NewGuid().ToString("N"))).FullName;
    }

    protected string WriteConfig(string path, string content)
    {
        var full = Path.Combine(ConfigRoot, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }
}
