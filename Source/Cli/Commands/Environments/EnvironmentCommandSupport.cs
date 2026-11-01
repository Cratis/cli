// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments;
using Cratis.Environments.Secrets;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Shared plumbing for the environment and config commands: locating the project, building the passphrase source and mapping
/// failures to exit codes without ever including a secret value.
/// </summary>
public static class EnvironmentCommandSupport
{
    /// <summary>
    /// Finds the environments of the project containing the current directory.
    /// </summary>
    /// <returns>The project environments.</returns>
    public static ProjectEnvironments Project() => ProjectEnvironments.Discover(Directory.GetCurrentDirectory());

    /// <summary>
    /// Creates the passphrase source: <c language="csharp">CRATIS_PASSPHRASE</c> or <c language="csharp">CRATIS_PASSPHRASE_FILE</c>, then a hidden prompt.
    /// </summary>
    /// <param name="creating">True when the passphrase is for a new environment, where the current-passphrase variables also supply the new one.</param>
    /// <returns>The passphrase source.</returns>
    public static IPassphraseSource Passphrase(bool creating) => PassphraseSources.FromEnvironment(new ConsolePassphraseSource(), creating);

    /// <summary>
    /// Runs a command body and maps environment failures to exit codes. Messages never contain secret values.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="body">The command body.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> Run(string format, Func<Task<int>> body)
    {
        try
        {
            return await body();
        }
        catch (InvalidPassphraseException ex)
        {
            OutputFormatter.WriteError(format, ex.Message, null, ExitCodes.AuthenticationErrorCode);
            return ExitCodes.AuthenticationError;
        }
        catch (EnvironmentNotFoundException ex)
        {
            OutputFormatter.WriteError(format, ex.Message, null, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }
        catch (SecretNotAvailableException ex)
        {
            OutputFormatter.WriteError(format, ex.Message, null, ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }
        catch (Exception ex) when (ex is EnvironmentException or InvalidOperationException)
        {
            OutputFormatter.WriteError(format, ex.Message, null, ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }
    }

    /// <summary>
    /// Opens the environment in effect for a command.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <param name="project">The project environments.</param>
    /// <returns>The opened environment.</returns>
    public static ProjectEnvironment Open(EnvironmentSettings settings, ProjectEnvironments project) =>
        ProjectEnvironment.Open(project, settings.ResolveEnvironment(project).Name, Passphrase(creating: false));
}
