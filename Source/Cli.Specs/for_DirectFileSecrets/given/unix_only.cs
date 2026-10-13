// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectFileSecrets.given;

public static class unix_only
{
    public sealed class FactAttribute : Xunit.FactAttribute
    {
        public FactAttribute()
        {
            if (OperatingSystem.IsWindows())
            {
                Skip = "Requires Unix permissions and atomic plaintext storage.";
            }
        }
    }
}
