import { useEffect, useRef } from "react";
import { getStoredToken, signalAuthFailure } from "../auth/auth";
// Long-lived WebSocket connection with first-frame auth + exponential
// reconnect (1s / 2s / 4s / 8s / 30s, capped at 30s, never gives up).
// Server expects { "auth": "<token>" } as the first text frame within
// 5s, otherwise closes with code 1008 (policy violation) — which we
// treat as an auth failure (clear token, prompt re-auth).
//
// Token rotation: the server also closes 1008 when the user regenerates
// the auth token while connections are open. Same handling — bounce to
// the auth gate, the user enters the new token (or the React UI's own
// regenerate-token call already has the new token in localStorage).
const RECONNECT_CADENCE = [1000, 2000, 4000, 8000, 30000];
export function useWebSocket(dispatch) {
    const wsRef = useRef(null);
    const reconnectAttempt = useRef(0);
    const reconnectTimer = useRef(null);
    const explicitClose = useRef(false);
    useEffect(() => {
        explicitClose.current = false;
        function connect() {
            const token = getStoredToken();
            if (!token)
                return;
            dispatch({
                type: "connection",
                status: reconnectAttempt.current === 0 ? "connecting" : "reconnecting",
            });
            const proto = window.location.protocol === "https:" ? "wss:" : "ws:";
            const ws = new WebSocket(`${proto}//${window.location.host}/ws`);
            wsRef.current = ws;
            ws.onopen = () => {
                ws.send(JSON.stringify({ auth: token }));
                // No server ack: if auth fails, server closes 1008. If the open
                // sticks beyond a couple of round-trips we treat the connection as
                // healthy and reset the reconnect counter.
                reconnectAttempt.current = 0;
                dispatch({ type: "connection", status: "open" });
            };
            ws.onmessage = (evt) => {
                try {
                    const env = JSON.parse(evt.data);
                    if ("logAdded" in env) {
                        dispatch({ type: "logAdded", msg: env.logAdded });
                        return;
                    }
                    if (env.channel && env.patch) {
                        dispatch({ type: "patch", channel: env.channel, patch: env.patch });
                    }
                }
                catch {
                    /* malformed envelope — ignore */
                }
            };
            ws.onclose = (evt) => {
                wsRef.current = null;
                if (explicitClose.current)
                    return;
                // 1008 = policy violation (auth failed, token rotated, etc.).
                if (evt.code === 1008) {
                    signalAuthFailure();
                    return;
                }
                // Otherwise reconnect with backoff.
                const delay = RECONNECT_CADENCE[Math.min(reconnectAttempt.current, RECONNECT_CADENCE.length - 1)];
                reconnectAttempt.current += 1;
                dispatch({ type: "connection", status: "reconnecting" });
                reconnectTimer.current = window.setTimeout(connect, delay);
            };
            ws.onerror = () => {
                // The close handler will fire next; reconnection logic lives there.
            };
        }
        connect();
        return () => {
            explicitClose.current = true;
            if (reconnectTimer.current !== null) {
                clearTimeout(reconnectTimer.current);
                reconnectTimer.current = null;
            }
            if (wsRef.current) {
                try {
                    wsRef.current.close();
                }
                catch {
                    /* ignore */
                }
                wsRef.current = null;
            }
            dispatch({ type: "connection", status: "closed" });
        };
    }, [dispatch]);
}
