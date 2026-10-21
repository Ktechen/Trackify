#!/usr/bin/env bash
#
# Pack a published self-contained Trackify CLI into an installable .deb.
#
# Why a .deb and not a tarball, a single-file binary or only the Docker image: on a Raspberry Pi
# `apt install ./trackify_<version>_arm64.deb` is the one command that also solves the things the
# README otherwise asks the operator to do by hand - `Depends: bluez` installs and starts the
# Bluetooth stack, and the maintainer scripts create the service user, put it in the `bluetooth`
# group, make the store directory and register the systemd unit. A tarball would still leave all
# four steps manual, and Docker needs a container runtime on the Pi.
#
# Layout the package installs:
#   /usr/lib/trackify/            the self-contained publish output (no .NET needed on the Pi)
#   /usr/bin/trackify             symlink onto it, so `trackify` is just on PATH
#   /lib/systemd/system/          trackify.service (registered, not enabled - see postinst)
#   /var/lib/trackify/            created by postinst; holds trackify.db
#
# Usage:
#   .github/scripts/build-deb.sh --version 1.2.3 --publish-dir publish/linux-arm64
#   .github/scripts/build-deb.sh --version 1.2.3 --publish-dir <dir> --stage-only   # inspect tree
#
# --stage-only builds the package tree and the control file but skips `dpkg-deb --build`, so the
# layout can be checked on a machine without dpkg (a Windows dev box). Note that on a Windows
# filesystem `chmod` and `ln -s` are both no-ops, so in a --stage-only tree there /usr/bin/trackify
# shows up as a copy rather than a symlink and the mode bits read 644. Both are correct when this
# runs on Linux, which is where the package is actually built.

set -euo pipefail

version=""
publish_dir=""
arch="arm64"
output_dir="."
stage_only=0

while [[ $# -gt 0 ]]; do
    case "$1" in
    --version)
        version="${2:-}"
        shift 2
        ;;
    --publish-dir)
        publish_dir="${2:-}"
        shift 2
        ;;
    --arch)
        arch="${2:-}"
        shift 2
        ;;
    --output)
        output_dir="${2:-}"
        shift 2
        ;;
    --stage-only)
        stage_only=1
        shift
        ;;
    *)
        echo "build-deb.sh: unknown argument '$1'" >&2
        exit 1
        ;;
    esac
done

if [[ -z "$version" || -z "$publish_dir" ]]; then
    echo "build-deb.sh: --version and --publish-dir are required." >&2
    exit 1
fi

# Debian versions are ordered, and a malformed one silently sorts wrong, so refuse anything that is
# not the plain X.Y.Z this project releases.
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "build-deb.sh: --version must be X.Y.Z (got '$version')." >&2
    exit 1
fi

if [[ ! -x "$publish_dir/trackify" && ! -f "$publish_dir/trackify" ]]; then
    echo "build-deb.sh: no 'trackify' executable in '$publish_dir'." >&2
    echo "  Publish it first: dotnet publish Source/Trackify.Cli/Trackify.Cli.csproj \\" >&2
    echo "    -f net10.0 -c Release -r linux-${arch} --self-contained -o '$publish_dir'" >&2
    exit 1
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
debian_src="$repo_root/packaging/debian"

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

mkdir -p "$stage/DEBIAN" \
    "$stage/usr/lib/trackify" \
    "$stage/usr/bin" \
    "$stage/lib/systemd/system" \
    "$stage/usr/share/doc/trackify"

cp -r "$publish_dir/." "$stage/usr/lib/trackify/"
chmod 755 "$stage/usr/lib/trackify/trackify"

# Relative, so the link is correct no matter where dpkg unpacks it.
ln -sf ../lib/trackify/trackify "$stage/usr/bin/trackify"

cp "$debian_src/trackify.service" "$stage/lib/systemd/system/trackify.service"

# Debian policy wants the licence shipped with the package; copying the repo's own LICENSE keeps
# this accurate with nothing to maintain separately.
cp "$repo_root/LICENSE" "$stage/usr/share/doc/trackify/copyright"

for script in postinst prerm postrm; do
    cp "$debian_src/$script" "$stage/DEBIAN/$script"
    chmod 755 "$stage/DEBIAN/$script"
done

installed_size="$(du -sk "$stage" | cut -f1)"

# Depends:
#   bluez     - the whole point: Trackify drives hubs by talking to bluetoothd over D-Bus, and
#               Ubuntu Server images do not ship it. apt pulling it in is what makes the package
#               "ready to use" instead of "installed".
#   libc6, libgcc-s1, libstdc++6, zlib1g - a self-contained .NET app still links against these.
#               ICU is NOT needed: the CLI sets InvariantGlobalization=true.
cat >"$stage/DEBIAN/control" <<EOF
Package: trackify
Version: $version
Section: misc
Priority: optional
Architecture: $arch
Depends: bluez, libc6, libgcc-s1, libstdc++6, zlib1g
Maintainer: ktechen <noreply@github.com>
Homepage: https://github.com/Ktechen/Trackify
Installed-Size: $installed_size
Description: Control LEGO Powered Up train hubs over Bluetooth LE
 Trackify configures and drives LEGO Powered Up train hubs directly over
 Bluetooth LE, with no server in between. This package carries the
 command-line front-end for a Raspberry Pi or Linux server, which talks to
 the onboard radio through BlueZ.
 .
 It ships as a self-contained build, so no .NET runtime is required. A
 systemd unit for unattended operation is installed but left disabled; see
 /lib/systemd/system/trackify.service.
EOF

if [[ "$stage_only" -eq 1 ]]; then
    staged_copy="$output_dir/trackify-deb-stage"
    rm -rf "$staged_copy"
    mkdir -p "$staged_copy"
    cp -r "$stage/." "$staged_copy/"
    echo "Staged package tree (not built) in: $staged_copy"
    echo "--- DEBIAN/control ---"
    cat "$staged_copy/DEBIAN/control"
    exit 0
fi

# A publish output that lost its executable bit (cross-publishing from Windows does that) would
# install a package whose binary cannot run, and nothing downstream would notice until the Pi
# refused to start it. Fail here instead.
if [[ ! -x "$stage/usr/lib/trackify/trackify" ]]; then
    echo "build-deb.sh: '$stage/usr/lib/trackify/trackify' is not executable - refusing to build a" >&2
    echo "  package that cannot run. Publish on Linux, or chmod +x the published binary first." >&2
    exit 1
fi

mkdir -p "$output_dir"
deb="$output_dir/trackify_${version}_${arch}.deb"

# --root-owner-group: the files belong to root in the package, not to whichever CI user ran this.
dpkg-deb --build --root-owner-group "$stage" "$deb"

echo "Built $deb"
dpkg-deb --info "$deb"
dpkg-deb --contents "$deb" | head -20
