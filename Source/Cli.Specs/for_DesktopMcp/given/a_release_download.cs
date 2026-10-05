// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Cli.for_DesktopMcp.given;

public class a_release_download : a_personal_marketplace
{
    protected const string Name = "screenplay-4.55.0-osx-arm64.mcpb";
    protected const string Url = "https://github.com/Cratis/Screenplay/releases/download/v4.55.0/" + Name;
    protected const string Bytes = "verified artifact bytes";
    protected List<string> _requests;
    protected string _download;
    protected bool _corruptResponse;
    private protected DesktopMcpArtifacts _downloads;
    HttpClient _http;

    void Establish()
    {
        _requests = [];
        var handler = new release_responses(request =>
        {
            var url = request.RequestUri!.AbsoluteUri;
            _requests.Add(url);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Bytes)));
            var content = _corruptResponse ? "corrupted download" : Bytes;
            if (url.EndsWith(".sha256", StringComparison.Ordinal)) content = $"{hash}  {Name}\n";
            return new(HttpStatusCode.OK) { Content = new StringContent(content) };
        });
        _http = new(handler);
        _downloads = new(_http, _platform);
    }

    void Destroy() => _http.Dispose();
}
