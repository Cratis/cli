// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_XunitV3Filter;

public class when_creating_filters : Specification
{
    IReadOnlyList<string> _class;
    IReadOnlyList<string> _test;
    IReadOnlyList<string> _namespace;
    IReadOnlyList<IReadOnlyList<string>> _batches;

    void Because()
    {
        _class = XunitV3Filter.QueriesForUnit("My.Ns.when_doing", UnitGranularity.Class);
        _test = XunitV3Filter.QueriesForUnit("My.Ns.when_doing.should_work", UnitGranularity.Test);
        _namespace = XunitV3Filter.QueriesForNamespace("My.Ns");
        var entries = Enumerable.Range(0, 100).Select(i => new SelectionEntry($"My.Ns.when_{i}", false)).ToList();
        _batches = XunitV3Filter.CreateBatches(entries, UnitGranularity.Class, 500);
    }

    [Fact] void should_match_a_class_exactly_by_namespace_and_name() => _class.ShouldContainOnly("/*/My.Ns/when_doing/*");
    [Fact] void should_match_a_test_exactly() => _test.ShouldContainOnly("/*/My.Ns/when_doing/should_work");
    [Fact] void should_match_a_namespace_and_everything_nested_in_it() => _namespace.ShouldContainOnly("/*/My.Ns/*/*", "/*/My.Ns.*/*/*");
    [Fact] void should_split_long_selections_into_batches() => _batches.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_query_exactly_once() => _batches.Sum(batch => batch.Count / 2).ShouldEqual(100);
    [Fact] void should_keep_each_batch_within_the_limit() => _batches.ShouldEachConformTo(batch => batch.Sum(argument => argument.Length) <= 500);
    [Fact] void should_pass_each_query_as_a_filter_argument() => _batches[0].Where((_, index) => index % 2 == 0).ShouldEachConformTo(argument => argument == "-filter");
}
