// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// The sources <c language="csharp">cratis config --missing</c> asks for the keys a model needs: a Screenplay model (a <c language="csharp">.play</c> file or a folder of them),
/// whose keys follow Screenplay's binding convention, then a key list file.
/// </summary>
public static class ModelKeySources
{
    static readonly List<IModelKeySource> _sources = [new ScreenplayModelKeySource(), new KeyListFile()];

    /// <summary>
    /// Registers a source ahead of the built-in ones.
    /// </summary>
    /// <param name="source">The source.</param>
    public static void Register(IModelKeySource source) => _sources.Insert(0, source);

    /// <summary>
    /// Reads the keys a model needs.
    /// </summary>
    /// <param name="path">A model path or key list file.</param>
    /// <returns>The keys.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no source understands the path.</exception>
    public static IReadOnlyList<string> Read(string path)
    {
        var source = _sources.Find(candidate => candidate.CanRead(path)) ??
            throw new InvalidOperationException(
                $"Cannot read model keys from '{path}'. Pass a Screenplay model (a .play file, or a folder of them) or a key list file (a JSON array of keys, or one key per line).");
        return source.ReadKeys(path);
    }
}
