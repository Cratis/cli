// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Cli.given;

namespace Cratis.Cli.for_ChronicleTokenCache.given;

public class a_token_cache : a_temp_config_directory
{
    protected const string Context = "production";

    protected static ChronicleConnectionString Connection(string server = "localhost:35000", bool disableTls = false, string client = "client") =>
        new($"chronicle://{client}:fixture-secret@{server}/?disableTls={disableTls}&skipTlsValidation=true");

    protected static string CachePath(ChronicleConnectionString connection, string context = Context) =>
        ChronicleTokenCache.GetCachePath(connection, context)!;

    protected static async Task WithProvider(ChronicleConnectionString connection, Func<ITokenProvider, Task> action)
    {
        var provider = ChronicleTokenCache.Create(connection, Context);
        using var lifetime = (IDisposable)provider;
        await action(provider);
    }

    protected static void Seed(string path, string content = "{}")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
