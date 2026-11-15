// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_FilterableTableView.when_the_data_is_unavailable;

[Collection(CliSpecsCollection.Name)]
public class and_a_filter_is_set : given.a_populated_table_view
{
    void Establish()
    {
        _view.Rows = [];
        _view.Unavailable = "Requires an administrator.";
    }

    void Because() => _view.SetFilter("zzz");

    [Fact] void should_show_the_unavailable_message_rather_than_no_matches() => _view.EmptyStateForSpecs.ShouldEqual("Requires an administrator.");
}
