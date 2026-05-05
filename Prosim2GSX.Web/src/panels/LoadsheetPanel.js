import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { getStoredToken, signalAuthFailure } from "../auth/auth";
import styles from "./LoadsheetPanel.module.css";
// Read-only loadsheet panel with two cards (PRELIM / FINAL). Initial REST
// load on mount fetches both slots independently; live updates arrive as
// a single combined patch on the WebSocket "loadsheet" channel and merge
// straight into AppState via the default reducer branch.
//
// Action buttons: RESEND POSTs to /api/loadsheet/resend (placeholder log
// server-side, no EFB call wired). RESET DELETEs /api/loadsheet after a
// confirm() prompt, server writes the slots back to "none"/"pending" and
// broadcasts the reset.
export function LoadsheetPanel() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    const [busy, setBusy] = useState(null);
    // Initial fetch: hit both endpoints in parallel and seed the snapshot.
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const [prelim, final] = await Promise.all([
                    get("/loadsheet/prelim"),
                    get("/loadsheet/final"),
                ]);
                if (!cancelled) {
                    dispatch({
                        type: "set",
                        channel: "loadsheet",
                        state: { prelim, final },
                    });
                }
            }
            catch {
                /* useApi already handled 401; WS will fill in once connected */
            }
        })();
        return () => { cancelled = true; };
    }, [get, dispatch]);
    const ls = state.loadsheet;
    if (!ls) {
        return _jsx("div", { className: styles.loading, children: "Loading loadsheet\u2026" });
    }
    const onResend = async () => {
        setBusy("resend");
        try {
            await post("/loadsheet/resend");
        }
        catch {
            /* silently ignored — placeholder endpoint, never fails meaningfully */
        }
        finally {
            setBusy(null);
        }
    };
    const onReset = async () => {
        if (!window.confirm("Reset both PRELIM and FINAL loadsheet slots?"))
            return;
        setBusy("reset");
        // useApi has no DELETE helper; do it directly so we still get the
        // bearer-token attachment + 401 routing.
        try {
            const token = getStoredToken();
            const res = await fetch("/api/loadsheet", {
                method: "DELETE",
                headers: token ? { Authorization: `Bearer ${token}` } : {},
            });
            if (res.status === 401)
                signalAuthFailure();
            // Reset broadcasts a WS patch — no need to optimistically update
            // local state.
        }
        catch {
            /* network errors fall through; WS reconnect will resync */
        }
        finally {
            setBusy(null);
        }
    };
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.header, children: [_jsx("h2", { className: styles.title, children: "LOADSHEET" }), _jsxs("div", { className: styles.actions, children: [_jsx("button", { type: "button", className: styles.btn, onClick: onResend, disabled: busy !== null, children: busy === "resend" ? "RESENDING…" : "RESEND" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnDanger}`, onClick: onReset, disabled: busy !== null, children: busy === "reset" ? "RESETTING…" : "RESET" })] })] }), _jsxs("div", { className: styles.cards, children: [_jsx(LoadsheetCard, { label: "PRELIM", dto: ls.prelim }), _jsx(LoadsheetCard, { label: "FINAL", dto: ls.final, finalErrorBorder: true })] })] }));
}
function LoadsheetCard({ label, dto, finalErrorBorder }) {
    const tone = dto.status === "received" ? "ok" :
        dto.status === "error" ? "bad" : "warn";
    const showData = dto.status === "received";
    const errBorder = finalErrorBorder && dto.macTowError && showData;
    return (_jsxs("div", { className: `${styles.card} ${errBorder ? styles.cardError : ""}`, children: [_jsxs("div", { className: styles.cardHead, children: [_jsx("div", { className: styles.cardLabel, children: label }), _jsx("span", { className: `${styles.badge} ${styles[`badge_${tone}`]}`, children: dto.status.toUpperCase() })] }), !showData && (_jsx("div", { className: styles.empty, children: dto.status === "error"
                    ? "Failed to parse loadsheet JSON. See log."
                    : "Awaiting loadsheet from Dispatch." })), showData && (_jsxs(_Fragment, { children: [_jsxs("div", { className: styles.kvGrid, children: [_jsx(KV, { label: "MacTow", value: `${dto.macTow.toFixed(1)} %`, bad: dto.macTowError }), _jsx(KV, { label: "TOW", value: `${(dto.towKg / 1000).toFixed(1)} t` }), _jsx(KV, { label: "Ident", value: dto.loadsheetIdent || "—" }), _jsx(KV, { label: "Received", value: formatReceivedAt(dto.receivedAt) })] }), _jsx(MacTowRange, { value: dto.macTow, min: dto.minMacTow, max: dto.maxMacTow, error: dto.macTowError })] }))] }));
}
function KV({ label, value, bad }) {
    return (_jsxs(_Fragment, { children: [_jsx("div", { className: styles.kvLabel, children: label }), _jsx("div", { className: `${styles.kvValue} ${bad ? styles.kvValueBad : ""}`, children: value })] }));
}
// Horizontal range bar — min and max bracket the bar; the marker dot sits at
// (value − min) / (max − min). When value is out of range we still render
// the dot at the clamped edge but flag it red so the operator can see WHICH
// way it's out (high-side vs low-side).
function MacTowRange({ value, min, max, error, }) {
    const span = Math.max(0.0001, max - min);
    const pct = Math.max(0, Math.min(1, (value - min) / span)) * 100;
    return (_jsxs("div", { className: styles.range, children: [_jsxs("div", { className: styles.rangeLabels, children: [_jsx("span", { children: min.toFixed(1) }), _jsx("span", { className: styles.rangeMid, children: "MAC%" }), _jsx("span", { children: max.toFixed(1) })] }), _jsx("div", { className: styles.rangeBar, children: _jsx("div", { className: `${styles.rangeMarker} ${error ? styles.rangeMarkerBad : ""}`, style: { left: `${pct}%` } }) })] }));
}
function formatReceivedAt(iso) {
    if (!iso)
        return "—";
    const d = new Date(iso);
    if (isNaN(d.getTime()))
        return "—";
    // Local time, HH:MM:SS — same convention the FlightStatus log lines use.
    const hh = String(d.getHours()).padStart(2, "0");
    const mm = String(d.getMinutes()).padStart(2, "0");
    const ss = String(d.getSeconds()).padStart(2, "0");
    return `${hh}:${mm}:${ss}`;
}
