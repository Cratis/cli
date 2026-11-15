// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// The choices an extension package can be scaffolded with. These are also the values of the template's choice symbols.
/// </summary>
public static class ExtensionScaffoldVocabulary
{
    /// <summary>The value that means "do not include this part".</summary>
    public const string None = "none";

    /// <summary>The backend languages of a full extension, in the order the wizard offers them.</summary>
    public static readonly ImmutableArray<string> Backends = ["csharp", "kotlin", "go", "typescript"];

    /// <summary>The frontend kinds of a full extension, in the order the wizard offers them. <c language="text">screenplay</c> is a Screenplay-authored component.</summary>
    public static readonly ImmutableArray<string> Frontends = ["react", "mount", "screenplay"];

    /// <summary>The component kinds of a frontend-only package, in the order the wizard offers them.</summary>
    public static readonly ImmutableArray<string> Components = ["react", "mount"];
}
