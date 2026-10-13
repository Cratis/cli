// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_WindowsDirectSecrets.given;

internal sealed class a_fake_credential_api : IWindowsCredentialApi
{
    readonly Dictionary<string, string> _credentials = [];

    public int Writes { get; private set; }
    public int LargestBlob { get; private set; }

    public string? Read(string target) => _credentials.GetValueOrDefault(target);

    public void Write(string target, byte[] bytes)
    {
        if (bytes.Length > 2560)
        {
            throw new InvalidOperationException("Fake Credential Manager refused an oversized blob.");
        }

        LargestBlob = Math.Max(LargestBlob, bytes.Length);
        Writes++;
        _credentials[target] = Encoding.UTF8.GetString(bytes);
    }

    public void Delete(string target) => _credentials.Remove(target);
}
