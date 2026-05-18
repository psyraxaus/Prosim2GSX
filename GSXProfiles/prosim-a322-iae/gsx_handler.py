# Prosim2GSX GSX handler script.
#
# Loaded by GSX Pro v4.0.0+ as a per-aircraft (tier 3) handler script,
# installed to %APPDATA%\Virtuali\Airplanes\<profile>\gsx_handler.py.
#
# v4.0.0 applies overrides from TOP-LEVEL functions/variables only: a
# function whose name does not start with "_" is bound onto the handler
# instance with `self` injected, like a normal method. The pre-v4
# `class ... / handler = Instance()` pattern is NOT applied by v4 (the
# loader would only see the class + instance as top-level names, never
# the methods), so it is deliberately not used here.
#
# Purpose: auto-assign the user-confirmed arrival gate from the OFP panel.
#   onSelectGateInFlight - aircraft handler hook; fires when the user
#       picks an airport from the in-flight "nearby airports" list. Best
#       moment to pre-assign the gate and skip the gate menu entirely.
#   onAircraftEngaged    - aircraft handler hook; fires when GSX engages
#       the parked aircraft. Replaces the pre-v4 onEnterAirport, which in
#       v4.0.0 is an AIRPORT-tier-only callback and would never fire from
#       this per-aircraft script.
#
# Constraints: no 'import' beyond what GSX provides, no file I/O, no
# threading. GSX-provided globals only: fetchJson, selectGate, getGate,
# showMessage, hasStockBehavior, runAsync, wait, executeCalculatorCode.
#
# Endpoint: GET http://127.0.0.1:5001/api/gsxmenu/pending-gate
# Returns the gate name as a JSON string ("C3") or JSON null when none is
# pending. Loopback-only, no auth - exempted from the bearer middleware
# because the Couatl Python runtime here has no token.
#
# If you've changed Prosim2GSX's WebServerPort from the default 5001, edit
# PROSIM2GSX_PORT below to match (Prosim2GSX rewrites this automatically
# on config change via GsxHandlerSync).

PROSIM2GSX_PORT = 5001
PROSIM2GSX_URL = "http://127.0.0.1:" + str(PROSIM2GSX_PORT) + "/api/gsxmenu/pending-gate"

# Loopback fetch budget (seconds). The endpoint is local and instant; cap
# it low so a missing or hung Prosim2GSX degrades gracefully instead of
# blocking the GSX tasklet that called the handler hook.
_FETCH_TIMEOUT = 2


def _fetch_pending_gate():
    try:
        return fetchJson(PROSIM2GSX_URL, timeout=_FETCH_TIMEOUT)
    except Exception as ex:
        print("[Prosim2GSX] pending-gate fetch failed: " + str(ex))
        return None


def _apply_gate(gate):
    if not gate:
        return
    result = selectGate(gate)
    if result is False:
        print("[Prosim2GSX] Gate assignment blocked for '" + str(gate)
              + "' - parked with active services, or user revoke in effect")
        showMessage("Prosim2GSX: gate " + str(gate) + " could not be assigned")
    elif isinstance(result, list):
        print("[Prosim2GSX] Ambiguous gate '" + str(gate) + "' matched "
              + str(len(result)) + " parkings - selecting first match")
        selectGate(result[0])
        showMessage("Prosim2GSX: arrival gate " + str(gate) + " assigned")
    else:
        print("[Prosim2GSX] Gate '" + str(gate) + "' assigned via " + str(result))
        showMessage("Prosim2GSX: arrival gate " + str(gate) + " assigned")


def onSelectGateInFlight(self):
    gate = _fetch_pending_gate()
    if gate:
        _apply_gate(gate)


def onAircraftEngaged(self):
    # Let the built-in engagement logic run first if the stock handler
    # has any, then apply our pending gate. At engagement the aircraft is
    # always at a parking, so the pre-v4 "skip if getGate()" guard is
    # meaningless here; GSX's own user-revoke protection covers the
    # don't-override case (selectGate returns False, reported by _apply_gate).
    if hasStockBehavior('onAircraftEngaged'):
        self._super_onAircraftEngaged()
    gate = _fetch_pending_gate()
    if gate:
        _apply_gate(gate)
