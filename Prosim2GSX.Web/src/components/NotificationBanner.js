import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useMemo, useRef } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import styles from "./NotificationBanner.module.css";
// Single banner shown above the tab bar. Picks the most-recent non-
// dismissed entry from state.notifications.items and renders it with
// a severity-coloured strip. Info auto-dismisses 10s after first
// becoming visible client-side; warning + error stay until the user
// clicks the close button (matching the Phase 6 spec).
//
// The store is owned by AppService.NotificationsState — REST GET on
// mount seeds the initial snapshot, WS "notifications" channel pushes
// updates. Dismiss POSTs to /api/notifications/{id}/dismiss; the
// server flips Dismissed and broadcasts the new snapshot, so we don't
// optimistically update local state.
export function NotificationBanner() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    // Initial fetch — same pattern the Loadsheet panel uses. WS will
    // overwrite as soon as it connects, but seeding via REST avoids a
    // visible "no banner" flash for clients that connect after a
    // notification has already been emitted.
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const snap = await get("/notifications");
                if (!cancelled) {
                    dispatch({
                        type: "set",
                        channel: "notifications",
                        state: snap,
                    });
                }
            }
            catch {
                /* useApi handles 401; WS will refill once connected */
            }
        })();
        return () => { cancelled = true; };
    }, [get, dispatch]);
    // Most-recent non-dismissed entry, or null when nothing to show.
    const current = useMemo(() => {
        const snap = state.notifications;
        if (!snap?.items?.length)
            return null;
        for (let i = snap.items.length - 1; i >= 0; i--) {
            if (!snap.items[i].dismissed)
                return snap.items[i];
        }
        return null;
    }, [state.notifications]);
    // Track which ids have already had an auto-dismiss timer scheduled so
    // a re-render doesn't pile up duplicate timeouts (the snapshot fires
    // on every store mutation — including the dismiss broadcast itself).
    const dismissTimers = useRef(new Map());
    useEffect(() => {
        if (!current)
            return;
        if (current.severity !== "info")
            return;
        if (dismissTimers.current.has(current.id))
            return;
        const handle = window.setTimeout(() => {
            // Fire-and-forget — the WS broadcast will mark it dismissed,
            // which makes the banner pick the next-most-recent entry.
            post(`/notifications/${current.id}/dismiss`).catch(() => { });
        }, 10_000);
        dismissTimers.current.set(current.id, handle);
        return () => {
            const h = dismissTimers.current.get(current.id);
            if (h != null) {
                clearTimeout(h);
                dismissTimers.current.delete(current.id);
            }
        };
    }, [current, post]);
    if (!current)
        return null;
    const onDismiss = () => {
        post(`/notifications/${current.id}/dismiss`).catch(() => { });
    };
    return (_jsxs("div", { className: `${styles.banner} ${styles[`sev_${current.severity}`]}`, children: [_jsx("span", { className: styles.severity, children: current.severity.toUpperCase() }), _jsx("span", { className: styles.message, children: current.message }), _jsx("button", { type: "button", className: styles.dismiss, onClick: onDismiss, "aria-label": "Dismiss", children: "\u00D7" })] }));
}
