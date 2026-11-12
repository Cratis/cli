// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_FilterableTableView.when_the_data_is_unavailable;

[Collection(CliSpecsCollection.Name)]
public class and_nothing_matches_an_available_list : given.a_populated_table_view
{
    void Establish() => _view.Rows = [];

    void Because() => _view.SetFilter("zzz");

    [Fact] void should_show_no_matches() => _view.EmptyStateForSpecs.ShouldEqual("No matches for 'zzz'");
}
