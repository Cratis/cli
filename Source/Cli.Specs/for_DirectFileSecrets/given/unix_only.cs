// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Cli.for_DirectFileSecrets.given;

public static class unix_only
{
    public sealed class FactAttribute : Xunit.FactAttribute
    {
        public FactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            if (OperatingSystem.IsWindows())
            {
                Skip = "Requires Unix permissions and atomic plaintext storage.";
            }
        }
    }
}
