// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.for_DirectMcpRegistration;

public class when_rewriting_an_existing_document : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"a longer existing document\"}\r\n";
    const string Shorter = "{}\n";
    string _path = string.Empty;

    void Establish()
    {
        _path = Path.Combine(AiPhysicalRoot.Resolve(_project), ".mcp.json");
        Write(_path, Original);
    }

    void Because() => AiConfigurationFile.Write(_path, Shorter, Encoding.UTF8.GetBytes(Original), beforeReplace: null, beforeOpen: null, write: null, writing: () => { });

    [Fact] void should_overwrite_at_offset_zero() => File.ReadAllText(_path).ShouldEqual(Shorter);
    [Fact] void should_truncate_the_old_document_tail() => new FileInfo(_path).Length.ShouldEqual(Encoding.UTF8.GetByteCount(Shorter));
    [Fact] void should_leave_one_parseable_document() => ReadJson(_path).Count.ShouldEqual(0);
}
