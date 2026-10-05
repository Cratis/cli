// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Isolates client failures so one failed host never prevents another selected host from completing.</summary>
internal static class DesktopMcpOperations
{
    internal static async Task<int> Run(IEnumerable<IDesktopMcpClient> clients, Func<IDesktopMcpClient, Task<string>> operation, TextWriter output)
    {
        var failed = false;
        foreach (var client in clients)
        {
            try
            {
                await output.WriteLineAsync($"{client.DisplayName}: {await operation(client)}");
            }
            catch (Exception exception)
            {
                failed = true;
                var guidance = exception is HttpRequestException or OperationCanceledException
                    ? " Check connectivity and the exact Cratis/Screenplay release assets. Wait for desktop publication to finish; use --version to pin an available release."
                    : string.Empty;
                await output.WriteLineAsync($"{client.DisplayName}: {exception.Message}{guidance}");
            }
        }
        return failed ? ExitCodes.ValidationError : ExitCodes.Success;
    }
}
