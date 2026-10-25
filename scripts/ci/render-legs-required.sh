#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail

# Only a nonempty list made entirely of known-safe paths can skip the legs.
required=false
seen=false
while IFS= read -r path || [[ -n "$path" ]]; do
    seen=true
    case "$path" in
        *.props|*.targets) required=true ;;
        Documentation/*|*.md) ;;
        Source/Cli.Specs/*|Source/Templating.Specs/*|Source/Templating.Conformance/*) ;;
        Integration/Chronicle/*) ;;
        # No declared types or namespaces in these folders feed the render/run paths,
        # Screenplay, Init, New, GetStarted, Registration, Templating or CLI root files.
        Source/Cli/Commands/Arc/*|Source/Cli/Commands/Completions/*|Source/Cli/Commands/Context/*|Source/Cli/Commands/Direct/*|Source/Cli/Commands/Llm/*|Source/Cli/Commands/Prologue/*|Source/Cli/Commands/ReleaseNotes/*|Source/Cli/Commands/View/*) ;;
        *) required=true ;;
    esac
done

if [[ "$seen" == false ]]; then
    required=true
fi
printf '%s\n' "$required"
