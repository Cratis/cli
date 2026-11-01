// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Writes the files of a run to a directory only the current user can open, and removes the directory when the run ends.
/// </summary>
public static class RunStackFiles
{
    const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    const UnixFileMode OwnerOnlyFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>
    /// Creates the run directory.
    /// </summary>
    /// <returns>The directory.</returns>
    public static DirectoryInfo CreateRunDirectory()
    {
        var directory = Directory.CreateTempSubdirectory("cratis-run-");
        Restrict(directory.FullName, DirectoryMode);
        return directory;
    }

    /// <summary>
    /// Writes the planned directories and files.
    /// </summary>
    /// <param name="runDirectory">The run directory.</param>
    /// <param name="plan">The plan.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Write(string runDirectory, RunStackPlan plan, CancellationToken cancellationToken)
    {
        foreach (var directory in plan.Directories)
        {
            var path = Path.Combine(runDirectory, directory);
            Directory.CreateDirectory(path);
            Restrict(path, DirectoryMode);
        }

        foreach (var file in plan.Files)
        {
            var path = Path.Combine(runDirectory, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // The file is created with its restricted mode, so its content is never readable by anyone else, not even briefly.
            var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = OwnerOnlyFile;
            }

            await using var stream = new FileStream(path, options);
            await stream.WriteAsync(new UTF8Encoding(false).GetBytes(file.Content), cancellationToken);
        }
    }

    /// <summary>
    /// Creates the folder onboarding emails are written to, and keeps it out of version control.
    /// </summary>
    /// <param name="projectRoot">The project root.</param>
    /// <returns>The folder.</returns>
    public static string CreateMailFolder(string projectRoot)
    {
        var folder = Path.Combine(projectRoot, ".cratis", "local", "mail");
        Directory.CreateDirectory(folder);
        Cratis.Environments.ProjectEnvironments.ForProject(projectRoot).EnsureLocalIgnored();
        return folder;
    }

    /// <summary>
    /// Removes the run directory and everything the run wrote to it.
    /// </summary>
    /// <param name="runDirectory">The run directory.</param>
    public static void Delete(string runDirectory)
    {
        try
        {
            if (Directory.Exists(runDirectory))
            {
                Directory.Delete(runDirectory, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A container that is still shutting down may hold a file open; the directory is in the temporary folder and only its owner can read it.
        }
    }

    static void Restrict(string path, UnixFileMode mode)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, mode);
        }
    }
}
