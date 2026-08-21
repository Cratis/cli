// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.given;

public class a_confirmation_request : Specification
{
    protected GlobalSettings _settings;
    protected bool _isInteractiveEnvironment;
    protected bool? _confirmationAnswer;
    protected bool _confirmationWasRequested;
    protected bool _defaultConfirmation;
    protected ConfirmationOutcome _result;

    void Establish()
    {
        _settings = new GlobalSettings();
        _isInteractiveEnvironment = true;
        _confirmationAnswer = null;
    }

    protected bool Confirm(bool defaultValue)
    {
        _confirmationWasRequested = true;
        _defaultConfirmation = defaultValue;

        return _confirmationAnswer ?? defaultValue;
    }

    protected void Decide() =>
        _result = ConfirmationHelper.Confirm(_settings, _isInteractiveEnvironment, Confirm);
}
