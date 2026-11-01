// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Cli.for_DesktopMcp.given;

public static class windows_only
{
    public sealed class FactAttribute : Xunit.FactAttribute
    {
        public FactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
            if (!OperatingSystem.IsWindows()) Skip = "Requires Windows file-sharing semantics.";
        }
    }
}
