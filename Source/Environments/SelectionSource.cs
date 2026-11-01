// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// Where the effective environment name came from, in order of precedence.
/// </summary>
public enum SelectionSource
{
    /// <summary>
    /// The <c language="csharp">--environment</c> / <c language="csharp">-e</c> option.
    /// </summary>
    Option,

    /// <summary>
    /// The <c language="csharp">CRATIS_ENVIRONMENT</c> environment variable.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// The per-user selection in <c language="csharp">.cratis/local/selected-environment</c>.
    /// </summary>
    SelectionFile,

    /// <summary>
    /// No selection: the <c language="csharp">development</c> default.
    /// </summary>
    Default
}
