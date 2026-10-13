// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_root_imports_a_glob : given.a_folder_with_documents
{
    string _root;
    ValidatedScreenplay _result;

    void Establish()
    {
        _root = WriteDocument("root.play", string.Join('\n',
            "import \"parts/*.play\"",
            "module Billing",
            "  feature Invoicing",
            "    slice StateChange Charge",
            "      command ChargeOrder",
            "        orderId OrderId identifier",
            "        produces OrderPlaced",
            "          for orderId"));
        WriteDocument("parts/orders.play", string.Join('\n',
            "concept OrderId : Uuid",
            "module Orders",
            "  feature Placing",
            "    slice StateChange PlaceOrder",
            "      event OrderPlaced",
            "      command PlaceOrder",
            "        orderId OrderId identifier",
            "        produces OrderPlaced",
            "          for orderId"));
        WriteDocument("unrelated.play", InvalidSource);
    }

    void Because() => _result = _validation.Validate(_root);

    [Fact] void should_count_the_imported_document() => _result.FileCount.ShouldEqual(2);
    [Fact] void should_resolve_imported_declarations() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_produce_one_application() => _result.Applications.Count.ShouldEqual(1);
}
