// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Represents a model root with the extension references its systems make and the lock beside it.
/// </summary>
/// <param name="Root">The model root.</param>
/// <param name="Application">The parsed model.</param>
/// <param name="References">The <c language="csharp">extension</c> lines, with the system that makes each.</param>
/// <param name="Lock">The lock as it is on disk, empty when there is none.</param>
public record ExtensionWorkspace(string Root, ApplicationSyntax Application, IReadOnlyList<ExtensionReference> References, ExtensionLock Lock)
{
    /// <summary>
    /// Gets the path of the lock file.
    /// </summary>
    public string LockPath => Path.Combine(Root, ExtensionLock.FileName);

    /// <summary>
    /// Gets the folder of vendored packages the compiler reads, beside the lock.
    /// </summary>
    public string VendorFolder => Path.Combine(Root, ExtensionContractResolver.VendorFolder);

    /// <summary>
    /// Gets the project root holding <c language="csharp">.cratis</c>: the nearest folder at or above the model root that has one, else the model root.
    /// </summary>
    public string ProjectRoot
    {
        get
        {
            for (var directory = new DirectoryInfo(Root); directory is not null; directory = directory.Parent)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, ".cratis"))) return directory.FullName;
            }

            return Root;
        }
    }

    /// <summary>
    /// Loads a model root.
    /// </summary>
    /// <param name="root">The model root.</param>
    /// <returns>The workspace.</returns>
    /// <exception cref="InvalidDataException">Thrown when the model or the lock cannot be read.</exception>
    public static ExtensionWorkspace Load(string root)
    {
        root = Path.GetFullPath(root);
        var parsed = new PlayFileCompiler().CompileFolder(root);
        var application = parsed.Result.Value;
        if (application is null)
        {
            var errors = parsed.Result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}");
            throw new InvalidDataException($"No readable model beneath '{root}'. {string.Join(' ', errors)}".Trim());
        }

        var references = application.Systems
            .Where(system => system.Extension is not null)
            .Select(system => new ExtensionReference(system.Name, system.Extension!))
            .ToArray();
        var lockPath = Path.Combine(root, ExtensionLock.FileName);
        var current = ExtensionLock.Empty;
        if (File.Exists(lockPath))
        {
            var read = ExtensionLock.Parse(File.ReadAllText(lockPath), lockPath);
            if (read.Value is null || read.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                throw new InvalidDataException(string.Join(' ', read.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
            }

            current = read.Value;
        }

        return new(root, application, references, current);
    }

    /// <summary>
    /// Writes a lock, only when its entries differ from the lock that was loaded.
    /// </summary>
    /// <param name="extensionLock">The lock.</param>
    /// <returns><see langword="true"/> when the file changed.</returns>
    public bool Save(ExtensionLock extensionLock)
    {
        if (extensionLock.Entries.SequenceEqual(Lock.Entries)) return false;
        File.WriteAllText(LockPath, extensionLock.Format());
        return true;
    }
}

/// <summary>
/// Represents one <c language="csharp">extension</c> line of the model and the system that makes it.
/// </summary>
/// <param name="System">The system name.</param>
/// <param name="Syntax">The line.</param>
public record ExtensionReference(string System, SystemExtensionSyntax Syntax)
{
    /// <summary>
    /// Gets the reference as the model writes it.
    /// </summary>
    public string Reference => Syntax.Reference;

    /// <summary>
    /// Gets the range the line asks for, <c language="csharp">latest</c> when it names none.
    /// </summary>
    public string Range => ExtensionLock.RangeOf(Syntax);
}
