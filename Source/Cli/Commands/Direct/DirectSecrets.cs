// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Stores credentials separately from Chronicle contexts and CLI configuration.</summary>
internal interface IDirectSecretStore
{
    Task<string?> Read(string key, CancellationToken cancellationToken);
    Task Write(string key, string value, CancellationToken cancellationToken);
    Task Delete(string key, CancellationToken cancellationToken);
}

/// <summary>Selects an OS credential manager, unless plaintext storage was explicitly enabled.</summary>
internal static class DirectSecretStores
{
    internal static IDirectSecretStore Select(bool insecureFileStore, string home, string? platform = null) =>
        insecureFileStore ? new DirectFileSecrets(home) : SelectPlatform(platform);

    static IDirectSecretStore SelectPlatform(string? platform)
    {
        if (platform == "macos" || (platform is null && OperatingSystem.IsMacOS()))
        {
            return new MacDirectSecrets();
        }

        if (platform == "windows" || (platform is null && OperatingSystem.IsWindows()))
        {
            return new WindowsDirectSecrets();
        }

        if (platform == "linux" || (platform is null && OperatingSystem.IsLinux()))
        {
            return new LinuxDirectSecrets();
        }

        throw new DirectAuthError("No supported OS credential manager. Use --insecure-file-store only if you accept plaintext storage.");
    }
}

/// <summary>Explicitly opted-in, permission-restricted plaintext credentials.</summary>
/// <param name="home">User home directory.</param>
internal sealed class DirectFileSecrets(string home) : IDirectSecretStore
{
    public async Task<string?> Read(string key, CancellationToken cancellationToken)
    {
        RequireUnix();
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows() && File.GetUnixFileMode(path) != (UnixFileMode.UserRead | UnixFileMode.UserWrite))
        {
            throw new DirectAuthError("Direct secret file permissions must be 0600.");
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task Write(string key, string value, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new DirectAuthError("Plaintext fallback requires Unix 0600 permissions and is unavailable on Windows.");
        }

        Directory.CreateDirectory(DirectoryPath());
        File.SetUnixFileMode(DirectoryPath(), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = PathFor(key);
        if (File.Exists(path))
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite };
        await using var file = new FileStream(path, options);
        await using var writer = new StreamWriter(file);
        await writer.WriteAsync(value.AsMemory(), cancellationToken);
    }

    public Task Delete(string key, CancellationToken cancellationToken)
    {
        RequireUnix();
        File.Delete(PathFor(key));
        return Task.CompletedTask;
    }

    static void RequireUnix()
    {
        if (OperatingSystem.IsWindows())
        {
            throw new DirectAuthError("Plaintext fallback requires Unix 0600 permissions and is unavailable on Windows.");
        }
    }

    string DirectoryPath() => Path.Combine(home, ".cratis", "direct-secrets");
    string PathFor(string key) => Path.Combine(DirectoryPath(), key + ".json");
}

/// <summary>Linux libsecret through secret-tool. Credentials are passed only through standard input.</summary>
internal sealed class LinuxDirectSecrets : IDirectSecretStore
{
    public async Task<string?> Read(string key, CancellationToken cancellationToken)
    {
        var (exit, output) = await Execute(["lookup", "cratis-direct", "key", key], null, cancellationToken);
        if (exit == 1)
        {
            return null;
        }

        Check(exit);
        return output.TrimEnd('\r', '\n');
    }

    public async Task Write(string key, string value, CancellationToken cancellationToken)
    {
        var (exit, _) = await Execute(["store", "--label=Cratis Direct", "cratis-direct", "key", key], value, cancellationToken);
        Check(exit);
    }

    public async Task Delete(string key, CancellationToken cancellationToken)
    {
        var (exit, _) = await Execute(["clear", "cratis-direct", "key", key], null, cancellationToken);
        Check(exit);
    }

    static void Check(int exit)
    {
        if (exit != 0)
        {
            throw new DirectAuthError("Linux credential manager failed. Install and unlock libsecret/secret-tool, or explicitly use --insecure-file-store.");
        }
    }

    static async Task<(int Exit, string Output)> Execute(string[] arguments, string? input, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("secret-tool") { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            throw new DirectAuthError("secret-tool is unavailable. Install libsecret, or explicitly use --insecure-file-store.");
        }

        if (input is not null)
        {
            await process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken);
        }

        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken); // Drain but never expose secret-bearing diagnostics.
        await process.WaitForExitAsync(cancellationToken);
        await error;
        return (process.ExitCode, await output);
    }
}

/// <summary>macOS Keychain native API; secrets never enter process arguments.</summary>
internal sealed class MacDirectSecrets : IDirectSecretStore
{
    const string Service = "Cratis.Direct";
    const int NotFound = -25300;

    public Task<string?> Read(string key, CancellationToken cancellationToken)
    {
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)key.Length, key, out var length, out var data, out var item);
        if (status == NotFound)
        {
            return Task.FromResult<string?>(null);
        }

        Check(status);
        try
        {
            var bytes = new byte[length];
            Marshal.Copy(data, bytes, 0, checked((int)length));
            return Task.FromResult<string?>(Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            Check(SecKeychainItemFreeContent(IntPtr.Zero, data));
            CFRelease(item);
        }
    }

    public Task Write(string key, string value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)key.Length, key, out _, out var data, out var item);
        if (status == NotFound)
        {
            Check(SecKeychainAddGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)key.Length, key, (uint)bytes.Length, bytes, out var added));
            CFRelease(added);
        }
        else
        {
            Check(status);
            try
            {
                Check(SecKeychainItemModifyAttributesAndData(item, IntPtr.Zero, (uint)bytes.Length, bytes));
            }
            finally
            {
                Check(SecKeychainItemFreeContent(IntPtr.Zero, data));
                CFRelease(item);
            }
        }

        return Task.CompletedTask;
    }

    public Task Delete(string key, CancellationToken cancellationToken)
    {
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)key.Length, key, out _, out var data, out var item);
        if (status != NotFound)
        {
            Check(status);
            try
            {
                Check(SecKeychainItemDelete(item));
            }
            finally
            {
                Check(SecKeychainItemFreeContent(IntPtr.Zero, data));
                CFRelease(item);
            }
        }

        return Task.CompletedTask;
    }

    static void Check(int status)
    {
        if (status != 0)
        {
            throw new DirectAuthError($"macOS Keychain operation failed ({status}).");
        }
    }

#pragma warning disable SYSLIB1054, CA2101 // Security.framework uses variable-sized buffers; strings explicitly marshal as UTF-8.
    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    static extern int SecKeychainFindGenericPassword(IntPtr keychain, uint serviceLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string service, uint accountLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string account, out uint passwordLength, out IntPtr passwordData, out IntPtr itemRef);
    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    static extern int SecKeychainAddGenericPassword(IntPtr keychain, uint serviceLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string service, uint accountLength, [MarshalAs(UnmanagedType.LPUTF8Str)] string account, uint passwordLength, byte[] passwordData, out IntPtr itemRef);
    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    static extern int SecKeychainItemModifyAttributesAndData(IntPtr itemRef, IntPtr attributes, uint length, byte[] data);
    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    static extern int SecKeychainItemDelete(IntPtr itemRef);
    [DllImport("/System/Library/Frameworks/Security.framework/Security")]
    static extern int SecKeychainItemFreeContent(IntPtr attributes, IntPtr data);
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    static extern void CFRelease(IntPtr cf);
#pragma warning restore SYSLIB1054, CA2101
}

/// <summary>Windows Credential Manager generic credentials, using the native API.</summary>
internal sealed class WindowsDirectSecrets : IDirectSecretStore
{
    public Task<string?> Read(string key, CancellationToken cancellationToken)
    {
        if (!CredRead("Cratis.Direct." + key, 1, 0, out var pointer))
        {
            if (Marshal.GetLastWin32Error() == 1168)
            {
                return Task.FromResult<string?>(null);
            }

            throw new DirectAuthError("Windows Credential Manager read failed.");
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(pointer);
            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            CredFree(pointer);
        }
    }

    public Task Write(string key, string value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var blob = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);
            var credential = new Credential { Type = 1, TargetName = "Cratis.Direct." + key, CredentialBlobSize = (uint)bytes.Length, CredentialBlob = blob, Persist = 2, UserName = "cratis-cli" };
            if (!CredWrite(ref credential, 0))
            {
                throw new DirectAuthError("Windows Credential Manager write failed.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blob);
        }

        return Task.CompletedTask;
    }

    public Task Delete(string key, CancellationToken cancellationToken)
    {
        if (!CredDelete("Cratis.Direct." + key, 1, 0) && Marshal.GetLastWin32Error() != 1168)
        {
            throw new DirectAuthError("Windows Credential Manager delete failed.");
        }

        return Task.CompletedTask;
    }

#pragma warning disable SYSLIB1054 // CREDENTIAL has platform-specific pointer layout.
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CredWrite(ref Credential credential, uint flags);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CredDelete(string target, uint type, uint flags);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr credential);
#pragma warning restore SYSLIB1054

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct Credential
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string UserName;
    }
}
