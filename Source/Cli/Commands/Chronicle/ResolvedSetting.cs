// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// A resolved setting value together with the source it was resolved from.
/// </summary>
/// <param name="Value">The resolved value.</param>
/// <param name="Source">The <see cref="SettingSource"/> the value came from.</param>
public record ResolvedSetting(string Value, SettingSource Source);
