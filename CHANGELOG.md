# Changelog

Trackify's changelog is **generated from the commits**, not maintained by hand — so there is no file
here to keep in sync and no commit-message convention to obey. Write a readable commit subject and
it will read well in the release notes.

**Per-release notes live on the [Releases page](https://github.com/Ktechen/Trackify/releases).**
Each release lists every non-merge commit since the previous `v*` tag, plus a compare link.

## Previewing the notes

```bash
.github/scripts/release-notes.sh                # what the next release would say (HEAD vs. last v* tag)
.github/scripts/release-notes.sh v1.2.0         # what v1.2.0 said
.github/scripts/release-notes.sh v1.2.0 v1.1.0  # an explicit range
```

## Cutting a release

Versions are **Semantic Versions** and tags are `vMAJOR.MINOR.PATCH`. Two ways to cut one:

**The easy way — let the workflow do it.** Actions → *Release* → **Run workflow**, then pick:

| Pick | When | 1.4.2 becomes |
|---|---|---|
| `patch` | bug fixes only, nothing new | 1.4.3 |
| `minor` | new features, nothing broken for existing users | 1.5.0 |
| `major` | something existing users must change to keep working | 2.0.0 |

It computes the next version, bumps `TrackifyVersion`, commits, tags, builds and opens the release —
no tagging by hand.

**The manual way — tag it yourself.**

1. Bump `TrackifyVersion` in [`Directory.Build.props`](Directory.Build.props) — the single source of
   truth for the version. Everything else (Android `versionName`/`versionCode`, the MSIX package
   version, the `.deb` version, the release title) derives from it.
2. Merge that to `master`.
3. Tag the merge commit `v<TrackifyVersion>` and push the tag.

Either way [`release.yml`](.github/workflows/release.yml) builds the Android `.apk` + `.aab`, the
Windows `.msix` and the Raspberry Pi `.deb`, and opens a **draft** release carrying them plus these
notes. Review it and press Publish. On the manual-tag path the tag and `TrackifyVersion` must agree —
the workflow fails the release if they do not, rather than shipping artifacts labelled with the wrong
version.
