// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

internal interface IAiMcpDocument
{
    JsonNode? Get(string collection, string id);
    bool Contains(string collection, string id);
    void Set(string collection, string id, JsonNode? value);
    void Apply(AiFileOperations operations);
}
