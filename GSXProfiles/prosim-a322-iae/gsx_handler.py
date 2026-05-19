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
# Two responsibilities:
#  1. Auto-assign the user-confirmed arrival gate from the OFP panel:
#       onSelectGateInFlight - fires when the user picks an airport from
#           the in-flight "nearby airports" list; best moment to pre-assign
#           the gate and skip the gate menu entirely.
#       onAircraftEngaged    - fires when GSX engages the parked aircraft.
#           Replaces the pre-v4 onEnterAirport, which in v4.0.0 is an
#           AIRPORT-tier-only callback and would never fire from this
#           per-aircraft script.
#  2. Event bridge: push lifecycle/service hook events to Prosim2GSX so
#     the app gets precise timing instead of only fragile LVAR polling.
#     Each hook runs the built-in (_super_) logic first — guarded so we
#     never suppress real stock behaviour — then fire-and-forget reports
#     the event. The sandbox has no HTTP POST, so events are GET-encoded.
#  3. VDGS flight display: render the canonical Prosim2GSX flight identity
#     on the gate's VDGS via addVdgsMessage() (replace-by-id, re-pushed on
#     engage/boarding/departure, cleared on disengage).
#
# Constraints: no 'import' beyond what GSX provides, no file I/O, no
# threading. GSX-provided globals only: fetchJson, selectGate, getGate,
# showMessage, hasStockBehavior, getattr, addVdgsMessage,
# removeVdgsMessage, runAsync, wait, executeCalculatorCode.
#
# Endpoints (loopback, no auth — the Couatl Python runtime has no token,
# and /api/gsxmenu/* is exempt from Prosim2GSX's bearer middleware):
#   GET /api/gsxmenu/pending-gate     -> "C3" | null
#   GET /api/gsxmenu/events?e=&r=&ts= -> null  (event push)
#   GET /api/gsxmenu/flight-info      -> {callsign,...} | null
#
# If you've changed Prosim2GSX's WebServerPort from the default 5001, edit
# PROSIM2GSX_PORT below to match (Prosim2GSX rewrites this automatically
# on config change via GsxHandlerSync).

PROSIM2GSX_PORT = 5001
PROSIM2GSX_BASE = "http://127.0.0.1:" + str(PROSIM2GSX_PORT) + "/api/gsxmenu"
PROSIM2GSX_URL = PROSIM2GSX_BASE + "/pending-gate"

# Loopback fetch budget (seconds). The endpoints are local and instant;
# cap it low so a missing or hung Prosim2GSX degrades gracefully instead
# of blocking the GSX tasklet that called the handler hook.
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


def _emit(event, reason=None):
    # Fire-and-forget. Event names and GSX reason tokens are fixed
    # url-safe identifiers, so no escaping is needed. Swallow everything:
    # a reporting failure must never disrupt a ground operation.
    try:
        url = PROSIM2GSX_BASE + "/events?e=" + event
        if reason:
            url = url + "&r=" + str(reason)
        fetchJson(url, timeout=_FETCH_TIMEOUT)
    except Exception as ex:
        print("[Prosim2GSX] event emit failed (" + str(event) + "): " + str(ex))


# ── VDGS flight display ────────────────────────────────────────────────
#
# Render the canonical Prosim2GSX flight identity on the gate's VDGS via
# addVdgsMessage(). No companion JSON file and no background tasklet:
# replace-by-id makes repeated calls idempotent, so we just re-push on the
# natural milestone hooks (engage / boarding / departure) and clear on
# disengage. Lines respect the per-display char limits (narrow 6, wide 9,
# x 10) from the GSX VGDS spec.

_VDGS_MSG_ID = "prosim2gsx_flight"


def _clip(s, n):
    return (s or "")[:n]


def _build_vdgs_message(info):
    cs = (info.get("callsign") or "").strip()
    fn = (info.get("flightNumber") or "").strip()
    o = (info.get("origin") or "").strip()
    d = (info.get("destination") or "").strip()
    ident = cs or fn or "PROSIM2GSX"
    route = (o + "-" + d) if (o and d) else (d or o or "----")
    return {
        "id": _VDGS_MSG_ID,
        "display": {
            "narrow": {"pages": [{
                "lines": ["FLT", _clip(ident, 6), "DEST", _clip(d, 6) or "----"],
                "duration": 5000}]},
            "wide": {"pages": [{
                "lines": ["FLIGHT", _clip(ident, 9), "ROUTE", _clip(route, 9)],
                "duration": 5000}]},
            "x": {"pages": [{
                "lines": ["FLIGHT", _clip(ident, 10), "ROUTE", _clip(route, 10)],
                "duration": 5000}]},
        },
    }


def _push_flight_info():
    try:
        info = fetchJson(PROSIM2GSX_BASE + "/flight-info", timeout=_FETCH_TIMEOUT)
    except Exception as ex:
        print("[Prosim2GSX] flight-info fetch failed: " + str(ex))
        return
    if not info:
        # No OFP loaded (or nothing meaningful yet) — clear any stale page.
        try:
            removeVdgsMessage(_VDGS_MSG_ID)
        except Exception:
            pass
        return
    try:
        addVdgsMessage(_build_vdgs_message(info))
    except Exception as ex:
        print("[Prosim2GSX] addVdgsMessage failed: " + str(ex))


def _clear_flight_info():
    try:
        removeVdgsMessage(_VDGS_MSG_ID)
    except Exception:
        pass


def _run_super(self, name, *args):
    # Run the built-in implementation only when the stock handler actually
    # has one (many hooks are just `pass`). Preserves real behaviour such
    # as PMDG/Fenix door automation that lives in onBoardingRequested etc.
    if hasStockBehavior(name):
        getattr(self, "_super_" + name)(*args)


# ── Gate assignment ────────────────────────────────────────────────────

def onSelectGateInFlight(self):
    gate = _fetch_pending_gate()
    if gate:
        _apply_gate(gate)


def onAircraftEngaged(self):
    # Built-in engagement logic first, then apply our pending gate. At
    # engagement the aircraft is always at a parking, so the pre-v4
    # "skip if getGate()" guard is meaningless; GSX's own user-revoke
    # protection covers the don't-override case (selectGate returns False).
    _run_super(self, 'onAircraftEngaged')
    gate = _fetch_pending_gate()
    if gate:
        _apply_gate(gate)
    _push_flight_info()
    _emit('aircraftEngaged')


def onAircraftDisengaged(self):
    _run_super(self, 'onAircraftDisengaged')
    _clear_flight_info()
    _emit('aircraftDisengaged')


# ── Event bridge ───────────────────────────────────────────────────────

def onGateReset(self, reason):
    _run_super(self, 'onGateReset', reason)
    _emit('gateReset', reason)


def onBoardingRequested(self):
    _run_super(self, 'onBoardingRequested')
    _push_flight_info()
    _emit('boardingRequested')


def onDeboardingRequested(self):
    _run_super(self, 'onDeboardingRequested')
    _emit('deboardingRequested')


def onRefuelingRequested(self):
    _run_super(self, 'onRefuelingRequested')
    _emit('refuelingRequested')


def onCateringRequested(self):
    _run_super(self, 'onCateringRequested')
    _emit('cateringRequested')


def onDepartureRequested(self):
    _run_super(self, 'onDepartureRequested')
    _push_flight_info()
    _emit('departureRequested')


def onJetwayConnected(self):
    _run_super(self, 'onJetwayConnected')
    _emit('jetwayConnected')


def onJetwayDisconnected(self):
    _run_super(self, 'onJetwayDisconnected')
    _emit('jetwayDisconnected')


def onBypassPinConnected(self):
    _run_super(self, 'onBypassPinConnected')
    _emit('bypassPinConnected')


def onBypassPinDisconnected(self):
    _run_super(self, 'onBypassPinDisconnected')
    _emit('bypassPinDisconnected')


def onDeicingAction(self):
    _run_super(self, 'onDeicingAction')
    _emit('deicingAction')
