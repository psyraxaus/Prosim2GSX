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
    // Resend the most-recent slot. Once the final has been received, the
    // operationally meaningful action is to resend FINAL (it's the latest
    // authoritative loadsheet); otherwise we resend PRELIM. Mirrors the
    // WPF tab's OnResend logic so both surfaces produce identical
    // observable behaviour against the slot-aware controller endpoint.
    const onResend = async () => {
        setBusy("resend");
        try {
            const slot = ls.final?.status === "received" ? "final" : "prelim";
            await post(`/loadsheet/resend?slot=${slot}`);
        }
        catch {
            /* SDK errors are logged server-side; no user-visible toast yet */
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
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.header, children: [_jsx("h2", { className: styles.title, children: "LOADSHEET" }), _jsxs("div", { className: styles.actions, children: [_jsx("button", { type: "button", className: styles.btn, onClick: onResend, disabled: busy !== null, children: busy === "resend" ? "RESENDING…" : "RESEND" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnDanger}`, onClick: onReset, disabled: busy !== null, children: busy === "reset" ? "RESETTING…" : "RESET" })] })] }), _jsx(StdControl, {}), _jsxs("div", { className: styles.cards, children: [_jsx(LoadsheetCard, { label: "PRELIM", dto: ls.prelim }), _jsx(LoadsheetCard, { label: "FINAL", dto: ls.final, finalErrorBorder: true })] })] }));
}
// STD control — shows the current effective STD and lets the user set
// (or clear) a manual override when no OFP is loaded. OFP-derived STD
// is read-only here; the INIT tab is where you change it.
function StdControl() {
    const { get, post } = useApi();
    const [std, setStd] = useState({ std: null, source: "none" });
    const [draft, setDraft] = useState(""); // HH:MM in UTC
    const [busy, setBusy] = useState(null);
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const r = await get("/loadsheet/std");
                if (!cancelled) {
                    setStd(r);
                    if (r.source === "manual" && r.std) {
                        setDraft(formatHHMMUtc(r.std));
                    }
                }
            }
            catch {
                /* useApi handled 401 */
            }
        })();
        return () => { cancelled = true; };
    }, [get]);
    const onSet = async () => {
        if (!/^\d{2}:\d{2}$/.test(draft))
            return;
        const iso = hhmmToIsoUtcToday(draft);
        if (!iso)
            return;
        setBusy("set");
        try {
            const r = await post("/loadsheet/set-std", { std: iso });
            setStd(r);
        }
        catch {
            /* ignore */
        }
        finally {
            setBusy(null);
        }
    };
    const onClear = async () => {
        setBusy("clear");
        try {
            const r = await post("/loadsheet/set-std", { std: null });
            setStd(r);
            setDraft("");
        }
        catch {
            /* ignore */
        }
        finally {
            setBusy(null);
        }
    };
    return (_jsxs("div", { className: styles.stdRow, children: [_jsx("span", { className: styles.stdLabel, children: "STD (UTC)" }), _jsx("span", { className: styles.stdValue, children: std.std ? formatHHMMUtc(std.std) + "Z" : "—" }), _jsx("span", { className: styles.stdSource, children: std.source === "ofp" ? "from OFP" : std.source === "manual" ? "manual" : "not set" }), std.source !== "ofp" && (_jsxs("span", { className: styles.stdInputWrap, children: [_jsx("input", { type: "time", className: styles.stdInput, value: draft, onChange: e => setDraft(e.target.value), disabled: busy !== null }), _jsx("button", { type: "button", className: styles.btn, onClick: onSet, disabled: busy !== null || !/^\d{2}:\d{2}$/.test(draft), children: busy === "set" ? "SETTING…" : "SET" }), std.source === "manual" && (_jsx("button", { type: "button", className: `${styles.btn} ${styles.btnDanger}`, onClick: onClear, disabled: busy !== null, children: busy === "clear" ? "CLEARING…" : "CLEAR" }))] }))] }));
}
// ISO-8601 → "HH:MM" in UTC.
function formatHHMMUtc(iso) {
    const d = new Date(iso);
    if (isNaN(d.getTime()))
        return "—";
    const hh = String(d.getUTCHours()).padStart(2, "0");
    const mm = String(d.getUTCMinutes()).padStart(2, "0");
    return `${hh}:${mm}`;
}
// "HH:MM" (interpreted as UTC) + today's UTC date → ISO-8601 string.
// The timing service compares time-of-day only, so the date doesn't
// affect the trigger logic — we just need a valid DateTime to round-
// trip through System.Text.Json.
function hhmmToIsoUtcToday(hhmm) {
    const m = /^(\d{2}):(\d{2})$/.exec(hhmm);
    if (!m)
        return null;
    const h = parseInt(m[1], 10);
    const min = parseInt(m[2], 10);
    if (h > 23 || min > 59)
        return null;
    const now = new Date();
    const utc = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), h, min, 0, 0));
    return utc.toISOString();
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
