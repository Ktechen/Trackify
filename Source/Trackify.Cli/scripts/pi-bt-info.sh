#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Trackify — Raspberry Pi Bluetooth (BlueZ) diagnostics (read-only)
# Gathers everything needed to debug LEGO Powered Up hub discovery/connection.
#   ./pi-bt-info.sh                 # basic (user-level)
#   sudo ./pi-bt-info.sh            # includes hciconfig/btmgmt/dmesg
#   SCAN_SECONDS=15 ./pi-bt-info.sh # longer LE scan
#   sudo ./pi-bt-info.sh | tee bt-report.txt   # capture for sharing
#
# Only writes: 'bluetoothctl power on' + a timed scan. Nothing is paired/removed.
# ---------------------------------------------------------------------------
set -uo pipefail

SCAN_SECONDS="${SCAN_SECONDS:-10}"
LEGO_COMPANY_ID="0397"   # LEGO manufacturer id in BLE advertising data

c_hdr='\033[1;36m'; c_ok='\033[1;32m'; c_warn='\033[1;33m'; c_err='\033[1;31m'; c_off='\033[0m'
section() { echo; echo -e "${c_hdr}══════ $* ══════${c_off}"; }
have()    { command -v "$1" >/dev/null 2>&1; }
run()     { echo -e "\$ $*"; "$@" 2>&1 | sed 's/^/    /'; echo; }
note()    { echo -e "  ${c_warn}» $*${c_off}"; }
ok()      { echo -e "  ${c_ok}✓ $*${c_off}"; }
bad()     { echo -e "  ${c_err}✗ $*${c_off}"; }

SUDO=""
if [ "$(id -u)" -ne 0 ]; then
  if have sudo && sudo -n true 2>/dev/null; then SUDO="sudo"; else
    note "Not root and no passwordless sudo — hciconfig/btmgmt/dmesg sections may be skipped."
  fi
fi

section "SYSTEM"
run uname -a
[ -f /proc/device-tree/model ] && { echo "  Model: $(tr -d '\0' </proc/device-tree/model)"; echo; }
have lsb_release && run lsb_release -a

section "BLUEZ VERSION"
have bluetoothctl && run bluetoothctl --version
have bluetoothd  && run bluetoothd --version
have hcitool     || note "hcitool not installed (some bluez utils missing)."

section "RF-KILL (is the radio blocked?)"
if have rfkill; then
  run rfkill list
  if rfkill list bluetooth 2>/dev/null | grep -qi 'Soft blocked: yes'; then
    bad "Bluetooth is SOFT-blocked → run: sudo rfkill unblock bluetooth"
  elif rfkill list bluetooth 2>/dev/null | grep -qi 'Hard blocked: yes'; then
    bad "Bluetooth is HARD-blocked (physical/firmware switch)."
  else
    ok "No rfkill block on Bluetooth."
  fi
else
  note "rfkill not installed."
fi

section "KERNEL / DRIVER"
run lsmod | grep -iE 'bluetooth|btbcm|hci_uart|btintel|btusb' || note "no BT modules listed"
if [ -n "$SUDO" ] || [ "$(id -u)" -eq 0 ]; then
  echo "  dmesg (Bluetooth lines):"
  $SUDO dmesg 2>/dev/null | grep -iE 'bluetooth|hci|bcm43|firmware' | tail -n 30 | sed 's/^/    /'
else
  note "Skipping dmesg (needs root)."
fi

section "bluetoothd SERVICE"
if have systemctl; then
  run systemctl is-active bluetooth
  run systemctl status bluetooth --no-pager -l | head -n 15
  systemctl is-active bluetooth >/dev/null 2>&1 && ok "bluetoothd is running." || bad "bluetoothd is NOT active → sudo systemctl enable --now bluetooth"
else
  note "systemctl not available."
fi

section "D-BUS (BlueZ talks over the system bus)"
[ -S /var/run/dbus/system_bus_socket ] && ok "System D-Bus socket present." || bad "No /var/run/dbus/system_bus_socket (Docker: mount /var/run/dbus)."
have busctl && run busctl --system list | grep -i bluez || true

section "PERMISSIONS (current user)"
echo "  user: $(id -un)   groups: $(id -Gn)"
if id -Gn | grep -qw bluetooth; then ok "In 'bluetooth' group."; else
  bad "NOT in 'bluetooth' group → sudo usermod -aG bluetooth $(id -un)  (then re-login)"; fi

section "HCI ADAPTERS"
if have hciconfig; then
  run ${SUDO:+$SUDO }hciconfig -a
else
  note "hciconfig not installed (package: bluez)."
fi
have btmgmt && { echo "  btmgmt info:"; ${SUDO:+$SUDO }btmgmt info 2>&1 | sed 's/^/    /'; echo; }

section "ADAPTER STATE (bluetoothctl show)"
if have bluetoothctl; then
  out="$(bluetoothctl show 2>&1)"; echo "$out" | sed 's/^/    /'; echo
  echo "$out" | grep -qi 'Powered: yes'    && ok "Adapter Powered = yes"    || bad "Adapter Powered = no → bluetoothctl power on"
  echo "$out" | grep -qi 'Discovering: yes' && note "Adapter already Discovering (a scan is running)."
else
  bad "bluetoothctl not installed → run setup-bluez.sh"
fi

section "KNOWN / PAIRED DEVICES"
if have bluetoothctl; then
  run bluetoothctl devices
  run bluetoothctl paired-devices
fi

section "LIVE LE SCAN (${SCAN_SECONDS}s) — looking for LEGO hubs"
if have bluetoothctl; then
  note "Power the LEGO hub ON now (its light should blink)."
  bluetoothctl power on >/dev/null 2>&1
  ( echo "menu scan"; echo "transport le"; echo "back"; echo "scan on"; sleep "$SCAN_SECONDS"; echo "scan off"; echo "quit" ) \
      | timeout $((SCAN_SECONDS + 5)) bluetoothctl >/tmp/bt_scan.log 2>&1
  echo "  --- devices seen after scan ---"
  bluetoothctl devices 2>/dev/null | sed 's/^/    /'
  echo
  echo "  --- scan log (advertisements) ---"
  grep -iE 'Device|Name|ManufacturerData|RSSI|0397' /tmp/bt_scan.log 2>/dev/null | tail -n 40 | sed 's/^/    /'
  echo
  if grep -qi "$LEGO_COMPANY_ID" /tmp/bt_scan.log 2>/dev/null; then
    ok "Saw LEGO manufacturer data (0x$LEGO_COMPANY_ID) — a hub is advertising."
  elif bluetoothctl devices 2>/dev/null | grep -qiE 'HUB|LEGO|Move|Technic|Duplo'; then
    ok "A likely LEGO hub is in the device list."
  else
    bad "No LEGO hub seen. Check: hub powered on, in range, radio powered, LE transport."
  fi
else
  note "bluetoothctl missing — cannot scan (run setup-bluez.sh)."
fi

section "SUMMARY HINTS"
cat <<'EOF'
  If discovery/connection still fails, the usual culprits (in order):
    1. BlueZ missing:  sudo ./setup-bluez.sh   (installs + enables everything)
    2. Radio off:      sudo rfkill unblock bluetooth ; bluetoothctl power on
    3. Service down:   sudo systemctl enable --now bluetooth
    4. Permissions:    sudo usermod -aG bluetooth $USER   (then log out/in)
    5. Docker only:    -v /var/run/dbus:/var/run/dbus  --network host
    6. Hub not in LE:  Trackify forces an LE-transport scan; confirm the hub
                       blinks (advertising) before connecting.
EOF
echo
echo -e "${c_ok}Done. Copy this whole output when reporting an issue.${c_off}"
