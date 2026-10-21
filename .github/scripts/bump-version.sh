#!/usr/bin/env bash
#
# Compute and apply the next Semantic Version for Trackify.
#
# TrackifyVersion in Directory.Build.props is the single source of truth for the version, so bumping
# a release means rewriting exactly that one element. This script does the SemVer arithmetic
# (MAJOR.MINOR.PATCH) and nothing else - the caller commits and tags.
#
# Usage:
#   .github/scripts/bump-version.sh patch            # 1.2.3 -> 1.2.4, writes the file
#   .github/scripts/bump-version.sh minor --dry-run  # print 1.3.0, change nothing
#   .github/scripts/bump-version.sh --current        # print the version as it stands
#
# Prints the resulting version to stdout and nothing else, so a caller can capture it directly.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
props="$repo_root/Directory.Build.props"

bump=""
dry_run=0

while [[ $# -gt 0 ]]; do
    case "$1" in
    patch | minor | major)
        bump="$1"
        shift
        ;;
    --dry-run)
        dry_run=1
        shift
        ;;
    --current)
        bump="current"
        shift
        ;;
    *)
        echo "bump-version.sh: unknown argument '$1' (expected patch|minor|major|--current)" >&2
        exit 1
        ;;
    esac
done

if [[ -z "$bump" ]]; then
    echo "bump-version.sh: say which part to bump: patch, minor, major (or --current)." >&2
    exit 1
fi

if [[ ! -f "$props" ]]; then
    echo "bump-version.sh: $props not found." >&2
    exit 1
fi

# One authoritative line, matched on the element rather than on a line number.
current="$(sed -n 's|.*<TrackifyVersion[^>]*>\([0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*\)</TrackifyVersion>.*|\1|p' "$props" | head -1)"

if [[ -z "$current" ]]; then
    echo "bump-version.sh: could not read a X.Y.Z TrackifyVersion from $props." >&2
    exit 1
fi

if [[ "$bump" == "current" ]]; then
    echo "$current"
    exit 0
fi

major="${current%%.*}"
rest="${current#*.}"
minor="${rest%%.*}"
patch="${rest#*.}"

# SemVer: a bump resets everything to its right, so 1.2.3 --minor is 1.3.0, never 1.3.3.
case "$bump" in
major)
    major=$((major + 1))
    minor=0
    patch=0
    ;;
minor)
    minor=$((minor + 1))
    patch=0
    ;;
patch)
    patch=$((patch + 1))
    ;;
esac

next="${major}.${minor}.${patch}"

if [[ "$dry_run" -eq 0 ]]; then
    # Rewrite in place, keeping the Condition attribute and the surrounding formatting intact.
    sed -i "s|\(<TrackifyVersion[^>]*>\)[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*\(</TrackifyVersion>\)|\1${next}\2|" "$props"

    # Paranoia: a silently-failed sed would otherwise tag a version the build does not carry.
    written="$(sed -n 's|.*<TrackifyVersion[^>]*>\([0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*\)</TrackifyVersion>.*|\1|p' "$props" | head -1)"
    if [[ "$written" != "$next" ]]; then
        echo "bump-version.sh: tried to write $next but $props now reads '$written'." >&2
        exit 1
    fi
fi

echo "$next"
