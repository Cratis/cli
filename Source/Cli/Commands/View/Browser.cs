// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Opens an address in the user's default browser.
/// </summary>
public static class Browser
{
    /// <summary>
    /// Opens an address in the default browser.
    /// </summary>
    /// <param name="address">The address to open.</param>
    /// <returns>True when the browser was asked to open the address, false when it could not be started.</returns>
    public static bool Open(Uri address)
    {
        try
        {
            var start = OperatingSystem.IsWindows()
                ? new ProcessStartInfo(address.ToString()) { UseShellExecute = true }
                : new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open", address.ToString()) { UseShellExecute = false };
            using var process = Process.Start(start);
            return process is not null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
