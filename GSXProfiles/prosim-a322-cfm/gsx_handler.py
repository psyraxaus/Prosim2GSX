# Prosim2GSX GSX handler script.
#
# Loaded by GSX Pro v3.9.4+ (Couatl/Stackless Python). Polls Prosim2GSX's
# loopback HTTP API for the user-confirmed arrival gate from the OFP panel
# and feeds it to selectGate() at the GSX lifecycle hooks where parking
# auto-assignment makes sense (in-flight gate selection + airport entry).
#
# Constraints: no 'import' beyond what GSX provides, no file I/O, no
# threading. Only the GSX-provided API: fetchJson, selectGate, getGate,
# runAsync, wait, executeCalculatorCode, etc.
#
# Endpoint: GET http://127.0.0.1:5001/api/gsxmenu/pending-gate
# Returns the gate name as a JSON string ("C3") or JSON null when none is
# pending. Loopback-only, no auth — exempted from the bearer middleware
# because Stackless Python here has no token.
#
# If you've changed Prosim2GSX's WebServerPort from the default 5001, edit
# PROSIM2GSX_PORT below to match.

PROSIM2GSX_PORT = 5001
PROSIM2GSX_URL = "http://127.0.0.1:" + str(PROSIM2GSX_PORT) + "/api/gsxmenu/pending-gate"


class Prosim2GSXHandler:
    def _fetch_pending_gate(self):
        try:
            return fetchJson(PROSIM2GSX_URL)
        except Exception as ex:
            print("[Prosim2GSX] pending-gate fetch failed: " + str(ex))
            return None

    def _apply_gate(self, gate):
        if not gate:
            return
        result = selectGate(gate)
        if result is False:
            print("[Prosim2GSX] Gate assignment blocked for '" + str(gate) + "' "
                  "- aircraft parked with active services or user revoke in effect")
        elif isinstance(result, list):
            print("[Prosim2GSX] Ambiguous gate '" + str(gate) + "' matched "
                  + str(len(result)) + " parkings - selecting first match")
            selectGate(result[0])
        else:
            print("[Prosim2GSX] Gate '" + str(gate) + "' assigned via " + str(result))

    def onSelectGateInFlight(self):
        gate = self._fetch_pending_gate()
        if gate:
            self._apply_gate(gate)

    def onEnterAirport(self):
        # Skip if a gate is already assigned (from onSelectGateInFlight or
        # the user picking via menu). getGate() returns truthy when set.
        if getGate():
            return
        gate = self._fetch_pending_gate()
        if gate:
            self._apply_gate(gate)


handler = Prosim2GSXHandler()
