// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_release_with_wrong_typed_fields : given.a_release_file
{
    readonly List<bool> _valids = [];

    void Because()
    {
        foreach (var content in new[]
        {
            Valid.Replace("\"protocolVersion\": 1", "\"protocolVersion\": \"1\""),
            Valid.Replace("\"protocolVersion\": 1", "\"protocolVersion\": true"),
            Valid.Replace("\"generation\": 1", "\"generation\": \"1\""),
            Valid.Replace("\"id\": \"BookAdded\"", "\"id\": 5"),
            Valid.Replace("\"schema\": \"{}\"", "\"schema\": {}"),
            Valid.Replace("\"eventTypes\": [", "\"eventTypes\": 3, \"x\": ["),
            Valid.Replace("\"eventTypes\": [ { \"type\": { \"id\": \"BookAdded\", \"generation\": 1 }, \"schema\": \"{}\" } ]", "\"eventTypes\": [ 7 ]"),
            Valid.Replace("\"type\": { \"id\": \"BookAdded\", \"generation\": 1 }", "\"type\": \"BookAdded\""),
            Valid.Replace("\"client\": {", "\"client\": [], \"y\": {"),
            Valid.Replace("\"release\": \"v3.2.0\"", "\"release\": 3"),
            Valid.Replace("\"descriptorSet\": \"AQID\"", "\"descriptorSet\": 1")
        })
        {
            Read(content);
            _valids.Add(_valid);
        }
    }

    [Fact] void should_reject_every_one_as_malformed_input() => _valids.TrueForAll(_ => !_).ShouldBeTrue();
}
