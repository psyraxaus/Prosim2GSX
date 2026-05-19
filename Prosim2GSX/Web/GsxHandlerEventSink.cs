using CFIT.AppLogger;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Web
{
    // Consumes lifecycle/service events pushed by the in-sim GSX handler
    // script (gsx_handler.py) through GET /api/gsxmenu/events. The handler
    // sandbox is GET-only (no HTTP POST primitive), so events arrive as
    // query parameters on a GET that returns JSON null — the same shape as
    // the pending-gate endpoint.
    //
    // Phase 1 is intentionally observability-only: it proves the
    // handler→app event pipe is reliable (surfacing the latest event on
    // GsxState for the Monitor tab / web, plus structured logging) WITHOUT
    // yet letting events drive automation or mutate the OFP gate workflow.
    // Retiring the fragile LVAR-polling/menu-title heuristics in favour of
    // these events — and the gateReset→clear-pending behaviour change — is
    // a separate, decision-gated follow-up (Phase 1b).
    internal static class GsxHandlerEventSink
    {
        // Whitelist of events the handler is allowed to raise. Anything else
        // is logged at debug and dropped — a typo or a future handler
        // revision can't inject arbitrary state.
        private static readonly HashSet<string> KnownEvents = new(StringComparer.Ordinal)
        {
            "aircraftEngaged",
            "aircraftDisengaged",
            "gateReset",
            "boardingRequested",
            "deboardingRequested",
            "refuelingRequested",
            "cateringRequested",
            "departureRequested",
            "jetwayConnected",
            "jetwayDisconnected",
            "bypassPinConnected",
            "bypassPinDisconnected",
            "deicingAction",
        };

        // Never throws: a failure here must not propagate back into the GSX
        // tasklet that called the handler hook.
        public static void Process(AppService app, string evt, string reason)
        {
            try
            {
                if (app == null || string.IsNullOrWhiteSpace(evt))
                    return;

                evt = evt.Trim();
                if (!KnownEvents.Contains(evt))
                {
                    Logger.Debug($"GsxHandlerEventSink: ignoring unknown event '{evt}'");
                    return;
                }

                reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

                string label = reason == null
                    ? $"{evt} @ {DateTime.Now:HH:mm:ss}"
                    : $"{evt}:{reason} @ {DateTime.Now:HH:mm:ss}";

                if (app.Gsx != null)
                    app.Gsx.LastHandlerEvent = label;

                // gateReset carries the reason GSX is dropping/changing the
                // gate (user_revoked / user_changed / taxied_away /
                // airport_exit / reposition).
                if (evt == "gateReset")
                {
                    Logger.Information($"GSX handler: gate reset (reason={reason ?? "unknown"})");
                    HandleGateReset(app, reason);
                }
                else
                {
                    Logger.Debug($"GSX handler event: {evt}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("GsxHandlerEventSink: failed to process handler event");
                Logger.LogException(ex);
            }
        }

        // Stop the handler re-asserting the OFP arrival gate once the user
        // has made a *deliberate* gate decision in the GSX menu:
        //   user_revoked — chose "Revoke parking services"
        //   user_changed — manually selected a different gate
        // The situational reasons (taxied_away / airport_exit / reposition)
        // are intentionally NOT cleared: taxiing away from the gate after
        // pushback, a go-around, or a diversion would otherwise wipe a still-
        // valid pending assignment. Clearing PendingArrivalGate also stops
        // OfpAutoSendService from re-firing (it short-circuits on empty), and
        // the SetGate-LVAR readback (AssignedArrivalGate) continues to show
        // whatever the user actually picked.
        private static void HandleGateReset(AppService app, string reason)
        {
            if (reason != "user_revoked" && reason != "user_changed")
                return;

            var ofp = app.Ofp;
            if (ofp == null || string.IsNullOrWhiteSpace(ofp.PendingArrivalGate))
                return;

            string cleared = ofp.PendingArrivalGate;
            ofp.PendingArrivalGate = "";
            ofp.GsxAssignmentStatus = reason == "user_revoked"
                ? $"Cleared — parking services revoked in GSX (was {cleared})."
                : $"Cleared — gate changed manually in GSX (was {cleared}).";
            Logger.Information(
                $"GSX handler: cleared pending arrival gate '{cleared}' (reason={reason})");
        }
    }
}
