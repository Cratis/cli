// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.for_ExtensionContractDiff;

public class when_comparing_contracts : Specification
{
    ExtensionContract _previous;
    ExtensionContract _shifted;
    ExtensionContract _retyped;
    ExtensionContract _extended;
    ExtensionContract _reduced;

    static ExtensionContract Parse(string text) => ExtensionContractParser.Parse(text).Value!;

    void Establish()
    {
        _previous = Parse(ExtensionContracts.Stripe2);
        _shifted = Parse("\n\n" + ExtensionContracts.Stripe2.Replace("setting secretKey secret\n", "setting secretKey secret\n\n\n"));
        _retyped = Parse(ExtensionContracts.Stripe2.Replace("orderId String", "orderId Guid"));
        _extended = Parse(ExtensionContracts.Stripe3WithRefund);
        _reduced = Parse(ExtensionContracts.Stripe3WithoutTrigger);
    }

    [Fact] void should_not_count_moved_lines_as_a_change() => ExtensionContractDiff.Removed(_previous, _shifted).ShouldBeEmpty();
    [Fact] void should_not_count_moved_lines_as_an_addition() => ExtensionContractDiff.Added(_previous, _shifted).ShouldBeEmpty();
    [Fact] void should_report_a_changed_input_type() => ExtensionContractDiff.Removed(_previous, _retyped).ShouldContain("ability 'CreatePaymentIntent' has different inputs or returns");
    [Fact] void should_report_a_new_ability() => ExtensionContractDiff.Added(_previous, _extended).ShouldContain("ability 'RefundPayment' is new");
    [Fact] void should_report_nothing_removed_from_a_superset() => ExtensionContractDiff.Removed(_previous, _extended).ShouldBeEmpty();
    [Fact] void should_report_a_trigger_that_is_no_longer_offered() => ExtensionContractDiff.Removed(_previous, _reduced).ShouldContain("trigger 'PaymentSucceeded' is not offered");
    [Fact] void should_report_a_trigger_that_is_new() => ExtensionContractDiff.Added(_previous, _reduced).ShouldContain("trigger 'Settled' is new");
}
