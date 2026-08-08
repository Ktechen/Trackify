#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Trackify — BlueZ setup for a Debian/Ubuntu Raspberry Pi.
#
# Installs and enables everything Trackify needs to talk to LEGO Powered Up
# hubs over the Pi's onboard Bluetooth: the BlueZ daemon (bluetoothd), powers
# the radio on, and grants the current user access. Safe to re-run (idempotent).
#
#   sudo ./setup-bluez.sh
#
# Ubuntu Server images ship WITHOUT bluez; Raspberry Pi OS usually includes it.
# Either way this brings the Pi to a known-good state.
# ---------------------------------------------------------------------------
set -euo pipefail

c_ok='\033[1;32m'; c_warn='\033[1;33m'; c_err='\033[1;31m'; c_hdr='\033[1;36m'; c_off='\033[0m'
step() { echo -e "\n${c_hdr}== $* ==${c_off}"; }
ok()   { echo -e "  ${c_ok}✓ $*${c_off}"; }
warn() { echo -e "  ${c_warn}» $*${c_off}"; }
die()  { echo -e "  ${c_err}✗ $*${c_off}"; exit 1; }

[[ "$(id -u)" -eq 0 ]] || die "Please run with sudo: sudo ./setup-bluez.sh"
command -v apt-get >/dev/null 2>&1 || die "This script targets Debian/Ubuntu (apt). Install 'bluez' with your distro's package manager instead."

# The human user to grant access to (the one who invoked sudo, not root).
TARGET_USER="${SUDO_USER:-${USER:-pi}}"

step "Install BlueZ + helpers"
if dpkg -s bluez >/dev/null 2>&1; then
    ok "bluez already installed."
else
    export DEBIAN_FRONTEND=noninteractive
    apt-get update -qq
    # bluez = bluetoothd + bluetoothctl (the stack Trackify drives over D-Bus).
    # rfkill = un/block the radio from the CLI.
    apt-get install -y bluez rfkill
    ok "bluez + rfkill installed."
fi

step "Enable and start bluetoothd"
systemctl enable --now bluetooth
if systemctl is-active --quiet bluetooth; then
    ok "bluetoothd is active."
else
    systemctl status bluetooth --no-pager -l | head -n 15 || true
    die "bluetoothd failed to start (see status above)."
fi

step "Unblock and power on the radio"
if command -v rfkill >/dev/null 2>&1; then
    rfkill unblock bluetooth || true
    ok "rfkill: Bluetooth unblocked."
fi
# Give bluetoothd a moment to expose the adapter on D-Bus, then power it on.
sleep 1
if bluetoothctl power on >/dev/null 2>&1; then
    ok "Adapter powered on."
else
    warn "Could not power the adapter yet — it may need a few seconds after boot."
fi

step "Grant '$TARGET_USER' access to org.bluez"
if id -nG "$TARGET_USER" 2>/dev/null | grep -qw bluetooth; then
    ok "'$TARGET_USER' is already in the 'bluetooth' group."
else
    usermod -aG bluetooth "$TARGET_USER"
    ok "Added '$TARGET_USER' to the 'bluetooth' group."
    NEED_RELOGIN=1
fi

step "Adapter state"
bluetoothctl show 2>/dev/null | grep -E 'Name|Powered|Discoverable|Address' | sed 's/^/  /' || warn "bluetoothctl show returned nothing."

echo
ok "BlueZ setup complete."
if [[ "${NEED_RELOGIN:-0}" = "1" ]]; then
    echo -e "${c_warn}IMPORTANT:${c_off} log out & back in (or reboot) so '$TARGET_USER' picks up the 'bluetooth' group,"
    echo "           then run:  trackify discover"
else
    echo "Next:  power on a hub (light blinking), then run:  trackify discover"
fi
