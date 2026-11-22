// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildOrder;

public class when_parsing_a_removal_order : Specification
{
    BuildOrder _order;
    Exception _contractTwo;
    Exception _mixed;
    Exception _noChanges;
    Exception _withSlices;
    Exception _invalidShape;
    Exception _unknownVersion;
    Exception _unknownKind;

    void Because()
    {
        _order = given.an_order.ParseRemoval();
        _contractTwo = Catch.Exception(() => BuildOrder.Parse(given.an_order.RemovalJson("2")));
        _unknownVersion = Catch.Exception(() => BuildOrder.Parse(given.an_order.RemovalJson("4")));
        _mixed = Catch.Exception(() => BuildOrder.Parse(given.an_order.RemovalJson(change: change => change["kind"] = "added")));
        _unknownKind = Catch.Exception(() => BuildOrder.Parse(given.an_order.RemovalJson(change: change => change["kind"] = "renamed")));
        _invalidShape = Catch.Exception(() => BuildOrder.Parse(given.an_order.RemovalJson(change: change => change["declarations"] = "CancelInvoice")));
        _noChanges = Catch.Exception(() => BuildOrder.Parse(Without("changes")));
        _withSlices = Catch.Exception(() => BuildOrder.Parse(WithSlices()));
    }

    static string Without(string property)
    {
        var order = JsonNode.Parse(given.an_order.RemovalJson())!.AsObject();
        order.Remove(property);
        return order.ToJsonString();
    }

    static string WithSlices()
    {
        var order = JsonNode.Parse(given.an_order.RemovalJson())!.AsObject();
        order["scope"]!["slices"] = JsonNode.Parse(JsonNode.Parse(given.an_order.Json())!["scope"]!["slices"]!.ToJsonString());
        return order.ToJsonString();
    }

    [Fact] void should_accept_it_under_contract_three() => _order.IsRemoval.ShouldBeTrue();
    [Fact] void should_target_no_slices() => _order.Slices.ShouldBeEmpty();
    [Fact] void should_carry_the_removed_change() => _order.RemovedChanges.Single().SliceId.ShouldEqual(given.an_order.GoneSliceId);
    [Fact] void should_name_the_slice_by_its_former_name() => _order.DisplayNames.ShouldContainOnly("CancelInvoice");
    [Fact] void should_refuse_it_under_contract_two() => _contractTwo.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_an_unknown_version() => _unknownVersion.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_mixed_changes() => _mixed.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_an_unknown_kind() => _unknownKind.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_an_invalid_change_shape() => _invalidShape.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_it_without_changes() => _noChanges.ShouldBeOfExactType<InvalidBuildOrder>();
    [Fact] void should_refuse_it_when_it_also_targets_slices() => _withSlices.ShouldBeOfExactType<InvalidBuildOrder>();
}
