// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1251, SA1502 // Marker type is intentionally empty

namespace Cratis.Cli.Registration;

/// <summary>
/// Environment configuration values and secrets.
/// </summary>
[CliBranch("config", "Read and write the configuration values and secrets of the selected environment. Secrets are encrypted or kept in a vault and never printed unless --show-secret is passed.")]
public static class ConfigBranch;
