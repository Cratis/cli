// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>Opens a builder connection supplied by its launcher, without consulting saved credentials.</summary>
internal static class BuilderBootstrap
{
    internal const string HandoffVariable = "CRATIS_BUILDER_HANDOFF";
    internal const string StdinVersion = "stdin-v1";

    internal static async Task<StudioConnection?> Open(Func<string, string?> variable, Func<CancellationToken, Task<string?>> read, CancellationToken cancellationToken)
    {
        var marker = variable(HandoffVariable);
        var token = marker switch
        {
            null => variable(StudioConnection.BuilderTokenVariable),
            StdinVersion => await read(cancellationToken),
            _ => throw new StudioNotConnected("Unsupported builder credential handoff.")
        };
        var url = variable(StudioConnection.UrlVariable);
        var organization = variable(StudioConnection.OrganizationVariable);
        var id = variable(StudioConnection.BuilderIdVariable);
        if (new[] { url, organization, id, token }.Any(string.IsNullOrWhiteSpace))
        {
            if (marker is not null) throw new StudioNotConnected("Incomplete builder credential handoff.");
            return null;
        }

        var builder = BuilderTransport.Connect(url!, organization!);
        builder.Authenticate(id!, token!);
        return new(new() { Url = url!, Organization = organization!, BuilderId = id }, builder, null, [token!], true, token);
    }
}
