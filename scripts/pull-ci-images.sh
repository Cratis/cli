#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Pulls the container images the build's tests start, through a public registry mirror first.
#
# GitHub-hosted runners share a small pool of IP addresses, so anonymous Docker Hub pulls hit the
# per-IP rate limit ("toomanyrequests") on busy days and fail the build for a reason that has nothing to
# do with the change under test. mirror.gcr.io serves cached copies of public Docker Hub images without
# credentials. Each image is pulled from the mirror and tagged with its Docker Hub name, so the tests
# (which reference the Docker Hub name) find it locally. A tag the mirror has not cached yet falls back to
# Docker Hub; only if both fail does the step fail.
#
# Usage: scripts/pull-ci-images.sh <image:tag> [<image:tag> ...]   (Docker Hub names, e.g. cratis/chronicle:19.83.0-development)
set -uo pipefail

MIRROR="${CI_IMAGE_MIRROR:-mirror.gcr.io}"

if [[ $# -eq 0 ]]; then
    echo "usage: $0 <image:tag> [<image:tag> ...]" >&2
    exit 2
fi

failed=0
for image in "$@"; do
    if docker pull --quiet "$MIRROR/$image" >/dev/null && docker tag "$MIRROR/$image" "$image"; then
        echo "Pulled $image through $MIRROR"
    elif docker pull --quiet "$image" >/dev/null; then
        echo "Pulled $image from Docker Hub ($MIRROR does not have it)"
    else
        echo "::error::Could not pull $image from $MIRROR or Docker Hub" >&2
        failed=1
    fi
done

exit "$failed"
