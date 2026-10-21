#!/usr/bin/env bash
#
# Print the changelog for a release as Markdown: every non-merge commit since the previous `v*` tag.
#
# Deliberately simple - no Conventional Commits parsing, no categorisation, no file to keep in sync.
# The commit subjects in this repository are already written as readable one-liners ("Give the
# Streckenplaner real track CRUD and a self-fitting layout"), so listing them verbatim answers
# "what is in this release?" without any extra discipline for the author.
#
# Usage:
#   .github/scripts/release-notes.sh                # notes for HEAD vs. the last v* tag (preview)
#   .github/scripts/release-notes.sh v1.2.0         # notes for the tag v1.2.0 (what CI runs)
#   .github/scripts/release-notes.sh v1.2.0 v1.1.0  # explicit previous tag
#
# Merge commits are skipped: this repository merges PRs, so the real subjects sit underneath them
# and "Merge pull request #13 from Ktechen/feat/..." carries no information for a reader.

set -euo pipefail

ref="${1:-HEAD}"
previous="${2:-}"

if ! git rev-parse --verify --quiet "$ref" >/dev/null; then
    echo "release-notes.sh: '$ref' is not a commit or tag in this repository." >&2
    exit 1
fi

# The newest release tag reachable from the commit *before* $ref. Starting at "$ref^" means this
# works whether $ref is the release tag itself (skips it, finds its predecessor) or just HEAD.
#
# The glob is deliberately SemVer-shaped rather than a bare `v*`: this repository already carries a
# tag named `Alpha`, and any future non-SemVer tag must not be mistaken for the previous release.
if [[ -z "$previous" ]]; then
    previous="$(git describe --tags --abbrev=0 \
        --match 'v[0-9]*.[0-9]*.[0-9]*' "${ref}^" 2>/dev/null || true)"
fi

# First release: no previous release tag, so the range is the whole history up to $ref.
if [[ -n "$previous" ]]; then
    range="${previous}..${ref}"
else
    range="$ref"
fi

echo "## What's in this release"
echo

commits="$(git log --no-merges --pretty='- %s (%h)' "$range")"
if [[ -n "$commits" ]]; then
    echo "$commits"
else
    echo "_No code changes since ${previous:-the start of the repository}._"
fi

# A compare link, when the remote is a GitHub URL we can recognise. Both the SSH
# (git@github.com:Owner/Repo.git) and HTTPS (https://github.com/Owner/Repo.git) forms normalise to
# the same browsable base; anything else (no remote, a mirror, a local path) just omits the link.
origin="$(git remote get-url origin 2>/dev/null || true)"
repo_url=""
case "$origin" in
    git@github.com:*) repo_url="https://github.com/${origin#git@github.com:}" ;;
    https://github.com/*) repo_url="$origin" ;;
esac
repo_url="${repo_url%.git}"

if [[ -n "$repo_url" && -n "$previous" ]]; then
    echo
    echo "**Full changelog:** ${repo_url}/compare/${previous}...${ref}"
fi
