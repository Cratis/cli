// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_a_concept_declared_at_the_root_is_used_in_a_nested_slice : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("application.play", "domain Invoicing\n\nconcept InvoiceId : Uuid\n");
        WriteDocument(Path.Combine("Invoicing", "Invoicing.play"), "module Invoicing\n");
        WriteDocument(Path.Combine("Invoicing", "Invoices", "Invoices.play"), "module Invoicing\n  feature Invoices\n");
        WriteDocument(
            Path.Combine("Invoicing", "Invoices", "Register", "Register.play"),
            "module Invoicing\n" +
            "  feature Invoices\n" +
            "    slice StateChange Register\n" +
            "      command Register\n" +
            "        invoiceId InvoiceId\n" +
            "        produces InvoiceRegistered\n" +
            "      event InvoiceRegistered\n" +
            "        invoiceId InvoiceId\n");
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_compile_every_document_beneath_it() => _result.FileCount.ShouldEqual(4);
    [Fact] void should_compile_the_documents_as_one_application() => _result.Applications.Count.ShouldEqual(1);
    [Fact] void should_resolve_the_cross_file_reference() => _result.Diagnostics.ShouldBeEmpty();
}
