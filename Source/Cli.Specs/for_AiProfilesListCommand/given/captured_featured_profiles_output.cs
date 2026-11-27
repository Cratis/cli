// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiProfilesListCommand.given;

public class captured_featured_profiles_output : captured_profiles_output
{
    void Establish() => _profiles =
    [
        new("cratis/application", "public", [], ["cratis/application/csharp"], "Build an application on Cratis.", true),
        new("cratis/application/csharp", "public", ["csharp"], [])
    ];
}
