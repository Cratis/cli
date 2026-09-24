// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Runs the <c language="csharp">docker</c> executable and reads back what it wrote, for callers that only
/// want an answer rather than a running process.
/// </summary>
public static class DockerCli
{
    /// <summary>
    /// Runs Docker with the given arguments and returns its standard output.
    /// </summary>
    /// <param name="arguments">The arguments to invoke <c language="csharp">docker</c> with.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Standard output, or an empty string when Docker could not be run at all - not installed, not
    /// on the PATH, or the call was cancelled. Every caller here treats that the same as "nothing to report"
    /// rather than an error, because a machine with no Docker on it is not a broken one.</returns>
    public static async Task<string> Run(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return string.Empty;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

            // Standard error is drained rather than shown - by the time a caller here asks, a complaint from
            // Docker is not worth putting in front of anyone; "nothing to report" is the answer either way.
            await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return output;
        }
        catch
        {
            return string.Empty;
        }
    }
}
