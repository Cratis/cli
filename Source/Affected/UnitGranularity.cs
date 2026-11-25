// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The size of the thing a coverage map attributes coverage to.
/// </summary>
public enum UnitGranularity
{
    /// <summary>
    /// A test class, named by its fully qualified name.
    /// </summary>
    Class,

    /// <summary>
    /// An individual test, named by its class followed by a dot and the method name.
    /// </summary>
    Test
}
