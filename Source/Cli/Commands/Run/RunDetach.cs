// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Decides whether a run can be detached, and reports why not.
/// </summary>
/// <remarks>
/// The secrets, seeds and network of authentication, onboarding, extensions and sample data live in files and
/// containers that the command removes when it ends, so leaving them behind would either break the sandbox or
/// leak what they hold. Those runs stay in the foreground.
/// </remarks>
public static class RunDetach
{
    /// <summary>
    /// Refuses a detached run of a model that needs something removed when the command ends.
    /// </summary>
    /// <param name="settings">The run settings.</param>
    /// <param name="application">The compiled model, or null when it did not compile.</param>
    /// <param name="format">The output format.</param>
    /// <returns>True when the run can go on; false when it was refused and the reason written.</returns>
    public static bool Allowed(RunSettings settings, ApplicationSyntax? application, string format)
    {
        return !settings.Detach || !Needs(settings, application) || Refuse(format);
    }

    /// <summary>
    /// Refuses a detached run of a model that declares extensions, before they are admitted.
    /// </summary>
    /// <param name="settings">The run settings.</param>
    /// <param name="modelFolder">The folder of the model.</param>
    /// <param name="format">The output format.</param>
    /// <returns>True when the run can go on; false when it was refused and the reason written.</returns>
    public static bool AllowedBeforeAdmission(RunSettings settings, string modelFolder, string format)
    {
        if (!settings.Detach) return true;

        try
        {
            return ExtensionWorkspace.Load(modelFolder).References.Count == 0 || Refuse(format);
        }
        catch (InvalidDataException)
        {
            return true;
        }
    }

    static bool Refuse(string format)
    {
        OutputFormatter.WriteError(
            format,
            "--detach is not supported for models that declare authentication or onboarding, use extensions, or use --sample-data yet",
            "Run in the foreground with -o json from a background process, or remove the feature for the probe",
            ExitCodes.ValidationErrorCode);
        return false;
    }

    static bool Needs(RunSettings settings, ApplicationSyntax? application) =>
        settings.SampleData.IsSet ||
        application?.Authentication is not null ||
        application?.Onboarding is not null ||
        RunExtensions.Names(application);
}
