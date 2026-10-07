// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Credential Manager API boundary, substitutable without loading Windows libraries in specs.</summary>
internal interface IWindowsCredentialApi
{
    string? Read(string target);
    void Write(string target, byte[] bytes);
    void Delete(string target);
}

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
    internal static IDirectSecretStore Select(bool insecureFileStore, string home, string? platform = null)
    {
        if (insecureFileStore && (platform == "windows" || (platform is null && OperatingSystem.IsWindows())))
        {
            throw new DirectAuthError("Plaintext fallback requires Unix 0600 permissions and is unavailable on Windows.");
        }

        return insecureFileStore ? new DirectFileSecrets(home) : SelectPlatform(platform);
    }

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
/// <param name="beforeReplace">Optional seam for observing the flushed file before replacement.</param>
internal sealed class DirectFileSecrets(string home, Func<Task>? beforeReplace = null) : IDirectSecretStore
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
            throw new DirectAuthError("Direct secret file permissions must be 0600.", invalidCredential: true);
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
        var temporary = Path.Combine(DirectoryPath(), $".{Guid.NewGuid():N}.tmp");
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite };
            await using (var file = new FileStream(temporary, options))
            {
                await using var writer = new StreamWriter(file, leaveOpen: true);
                await writer.WriteAsync(value.AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
#pragma warning disable CA1849 // FileStream.Flush(true) is the only API that requests a disk flush.
                file.Flush(flushToDisk: true);
#pragma warning restore CA1849
            }

            if (beforeReplace is not null)
            {
                await beforeReplace();
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
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
/// <param name="tool">The secret-tool executable; substitutable in specs.</param>
/// <param name="operationTimeout">The bounded operation deadline; substitutable in specs.</param>
internal sealed class LinuxDirectSecrets(string tool = "secret-tool", TimeSpan? operationTimeout = null) : IDirectSecretStore
{
    public async Task<string?> Read(string key, CancellationToken cancellationToken)
    {
        var (exit, output, failed) = await Execute(["lookup", .. Attributes(key)], null, cancellationToken);

        // secret-tool exits 1 silently when nothing matches, and 1 with a diagnostic when it could not look.
        if (exit == 1 && !failed)
        {
            return null;
        }

        Check(exit);
        return output.TrimEnd('\r', '\n');
    }

    public async Task Write(string key, string value, CancellationToken cancellationToken)
    {
        var (exit, _, _) = await Execute(["store", "--label=Cratis Direct", .. Attributes(key)], value, cancellationToken);
        Check(exit);
    }

    public async Task Delete(string key, CancellationToken cancellationToken)
    {
        var (exit, _, failed) = await Execute(["clear", .. Attributes(key)], null, cancellationToken);
        if (exit == 1 && !failed)
        {
            return;
        }

        Check(exit);
    }

    /// <summary>Attribute/value pairs; the full set identifies one item, so store replaces it and lookup finds it.</summary>
    /// <param name="key">The credential key.</param>
    /// <returns>The secret-tool attribute arguments.</returns>
    static string[] Attributes(string key) => ["application", "cratis-cli", "service", "cratis-direct", "key", key];

    static void Check(int exit)
    {
        if (exit != 0)
        {
            throw new DirectAuthError("Linux credential manager failed. Install and unlock libsecret/secret-tool, or explicitly use --insecure-file-store.");
        }
    }

    async Task<(int Exit, string Output, bool Failed)> Execute(string[] arguments, string? input, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(tool) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
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
            throw new DirectAuthError("secret-tool was not found. Install libsecret-tools (for example 'sudo apt install libsecret-tools'), or explicitly use --insecure-file-store.");
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(operationTimeout ?? TimeSpan.FromSeconds(15));
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            try
            {
                if (input is not null)
                {
                    await process.StandardInput.WriteAsync(input.AsMemory(), deadline.Token);
                }

                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The tool exited before reading its input; its exit code reports the failure.
            }

            await process.WaitForExitAsync(deadline.Token);

            // Diagnostics are only inspected for presence, never shown: they could echo stored content.
            return (process.ExitCode, await output.WaitAsync(deadline.Token), (await error.WaitAsync(deadline.Token)).Length != 0);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DirectAuthError("Linux credential manager timed out. A locked keyring may be waiting for an unlock prompt; unlock the Secret Service keyring and retry, or explicitly use --insecure-file-store.");
        }
        finally
        {
            // Disposing Process does not stop it. No owned writer may outlive the credential lock.
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(output, error);
        }
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

/// <summary>Windows Credential Manager generic credentials, retaining only the refresh token and metadata.</summary>
/// <param name="credentialApi">Native credential API, or a substitute for specs.</param>
internal sealed class WindowsDirectSecrets(IWindowsCredentialApi? credentialApi = null) : IDirectSecretStore
{
    const int MaxBlobSize = 2560;
    readonly IWindowsCredentialApi _api = credentialApi ?? new NativeWindowsCredentialApi();
    readonly Dictionary<string, (string Persisted, string Complete)> _accessTokens = [];
    readonly object _cacheLock = new();

    public Task<string?> Read(string key, CancellationToken cancellationToken)
    {
        var persisted = _api.Read("Cratis.Direct." + key);
        lock (_cacheLock)
        {
            return Task.FromResult(persisted is not null && _accessTokens.TryGetValue(key, out var cached) && cached.Persisted == persisted ? cached.Complete : persisted);
        }
    }

    public Task Write(string key, string value, CancellationToken cancellationToken)
    {
        DirectTokens? tokens;
        try
        {
            tokens = JsonSerializer.Deserialize<DirectTokens>(value);
        }
        catch (JsonException)
        {
            throw new DirectAuthError("Stored Direct credentials are invalid.");
        }

        if (tokens is null)
        {
            throw new DirectAuthError("Stored Direct credentials are invalid.");
        }

        var persisted = JsonSerializer.Serialize(tokens with { AccessToken = string.Empty });
        var bytes = Encoding.UTF8.GetBytes(persisted);
        if (bytes.Length > MaxBlobSize)
        {
            throw new DirectAuthError("Direct refresh token and metadata exceed the Windows Credential Manager limit (2560 bytes).");
        }

        _api.Write("Cratis.Direct." + key, bytes);
        lock (_cacheLock)
        {
            _accessTokens[key] = (persisted, value);
        }

        return Task.CompletedTask;
    }

    public Task Delete(string key, CancellationToken cancellationToken)
    {
        _api.Delete("Cratis.Direct." + key);
        lock (_cacheLock)
        {
            _accessTokens.Remove(key);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Native Windows credential API adapter.</summary>
internal sealed class NativeWindowsCredentialApi : IWindowsCredentialApi
{
    const int NotFound = 1168;

    public string? Read(string target)
    {
        if (!CredRead(target, 1, 0, out var pointer))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == NotFound)
            {
                return null;
            }

            throw new DirectAuthError($"Windows Credential Manager read failed (error {error}).");
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(pointer);
            if (credential.CredentialBlobSize == 0 || credential.CredentialBlob == IntPtr.Zero)
            {
                // An empty credential is not one this CLI wrote; reading it as JSON reports it as invalid.
                return string.Empty;
            }

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            CredFree(pointer);
        }
    }

    public void Write(string target, byte[] bytes)
    {
        var blob = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);
            var credential = new Credential { Type = 1, TargetName = target, CredentialBlobSize = (uint)bytes.Length, CredentialBlob = blob, Persist = 2, UserName = "cratis-cli" };
            if (!CredWrite(ref credential, 0))
            {
                throw new DirectAuthError($"Windows Credential Manager write failed (error {Marshal.GetLastWin32Error()}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blob);
        }
    }

    public void Delete(string target)
    {
        if (!CredDelete(target, 1, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != NotFound)
            {
                throw new DirectAuthError($"Windows Credential Manager delete failed (error {error}).");
            }
        }
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
