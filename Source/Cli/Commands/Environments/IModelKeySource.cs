// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Supplies the configuration keys a model needs. This is the seam through which <c language="csharp">cratis config --missing</c> learns
/// the keys of a Screenplay model; the binding convention itself lives in Screenplay (Cratis/Screenplay#1115).
/// </summary>
public interface IModelKeySource
{
    /// <summary>
    /// Determines whether this source understands the path.
    /// </summary>
    /// <param name="path">A file or folder path.</param>
    /// <returns>True when <see cref="ReadKeys"/> can read it.</returns>
    bool CanRead(string path);

    /// <summary>
    /// Reads the keys the model at the path needs.
    /// </summary>
    /// <param name="path">A file or folder path.</param>
    /// <returns>The distinct keys.</returns>
    IReadOnlyList<string> ReadKeys(string path);
}
