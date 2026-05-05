import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { LoadsheetDto, LoadsheetSnapshotDto } from "../types";
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
  const [busy, setBusy] = useState<"resend" | "reset" | null>(null);

  // Initial fetch: hit both endpoints in parallel and seed the snapshot.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [prelim, final] = await Promise.all([
          get<LoadsheetDto>("/loadsheet/prelim"),
          get<LoadsheetDto>("/loadsheet/final"),
        ]);
        if (!cancelled) {
          dispatch({
            type: "set",
            channel: "loadsheet",
            state: { prelim, final } as unknown as Record<string, unknown>,
          });
        }
      } catch {
        /* useApi already handled 401; WS will fill in once connected */
      }
    })();
    return () => { cancelled = true; };
  }, [get, dispatch]);

  const ls = state.loadsheet as unknown as LoadsheetSnapshotDto | null;
  if (!ls) {
    return <div className={styles.loading}>Loading loadsheet…</div>;
  }

  const onResend = async () => {
    setBusy("resend");
    try {
      await post("/loadsheet/resend");
    } catch {
      /* silently ignored — placeholder endpoint, never fails meaningfully */
    } finally {
      setBusy(null);
    }
  };

  const onReset = async () => {
    if (!window.confirm("Reset both PRELIM and FINAL loadsheet slots?")) return;
    setBusy("reset");
    // useApi has no DELETE helper; do it directly so we still get the
    // bearer-token attachment + 401 routing.
    try {
      const token = getStoredToken();
      const res = await fetch("/api/loadsheet", {
        method: "DELETE",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.status === 401) signalAuthFailure();
      // Reset broadcasts a WS patch — no need to optimistically update
      // local state.
    } catch {
      /* network errors fall through; WS reconnect will resync */
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <h2 className={styles.title}>LOADSHEET</h2>
        <div className={styles.actions}>
          <button
            type="button"
            className={styles.btn}
            onClick={onResend}
            disabled={busy !== null}
          >
            {busy === "resend" ? "RESENDING…" : "RESEND"}
          </button>
          <button
            type="button"
            className={`${styles.btn} ${styles.btnDanger}`}
            onClick={onReset}
            disabled={busy !== null}
          >
            {busy === "reset" ? "RESETTING…" : "RESET"}
          </button>
        </div>
      </div>

      <div className={styles.cards}>
        <LoadsheetCard label="PRELIM" dto={ls.prelim} />
        <LoadsheetCard label="FINAL"  dto={ls.final}  finalErrorBorder />
      </div>
    </div>
  );
}

interface CardProps {
  label: string;
  dto: LoadsheetDto;
  // Final card highlights with a red border when MacTowError is set; the
  // prelim card just colours the value cell. Spec requires the final
  // border treatment specifically.
  finalErrorBorder?: boolean;
}

function LoadsheetCard({ label, dto, finalErrorBorder }: CardProps) {
  const tone =
    dto.status === "received" ? "ok" :
    dto.status === "error"    ? "bad" : "warn";

  const showData = dto.status === "received";
  const errBorder = finalErrorBorder && dto.macTowError && showData;

  return (
    <div className={`${styles.card} ${errBorder ? styles.cardError : ""}`}>
      <div className={styles.cardHead}>
        <div className={styles.cardLabel}>{label}</div>
        <span className={`${styles.badge} ${styles[`badge_${tone}`]}`}>
          {dto.status.toUpperCase()}
        </span>
      </div>

      {!showData && (
        <div className={styles.empty}>
          {dto.status === "error"
            ? "Failed to parse loadsheet JSON. See log."
            : "Awaiting loadsheet from Dispatch."}
        </div>
      )}

      {showData && (
        <>
          <div className={styles.kvGrid}>
            <KV label="MacTow" value={`${dto.macTow.toFixed(1)} %`} bad={dto.macTowError} />
            <KV label="TOW" value={`${(dto.towKg / 1000).toFixed(1)} t`} />
            <KV label="Ident" value={dto.loadsheetIdent || "—"} />
            <KV label="Received" value={formatReceivedAt(dto.receivedAt)} />
          </div>

          <MacTowRange
            value={dto.macTow}
            min={dto.minMacTow}
            max={dto.maxMacTow}
            error={dto.macTowError}
          />
        </>
      )}
    </div>
  );
}

function KV({ label, value, bad }: { label: string; value: string; bad?: boolean }) {
  return (
    <>
      <div className={styles.kvLabel}>{label}</div>
      <div className={`${styles.kvValue} ${bad ? styles.kvValueBad : ""}`}>{value}</div>
    </>
  );
}

// Horizontal range bar — min and max bracket the bar; the marker dot sits at
// (value − min) / (max − min). When value is out of range we still render
// the dot at the clamped edge but flag it red so the operator can see WHICH
// way it's out (high-side vs low-side).
function MacTowRange({
  value, min, max, error,
}: {
  value: number;
  min: number;
  max: number;
  error: boolean;
}) {
  const span = Math.max(0.0001, max - min);
  const pct = Math.max(0, Math.min(1, (value - min) / span)) * 100;
  return (
    <div className={styles.range}>
      <div className={styles.rangeLabels}>
        <span>{min.toFixed(1)}</span>
        <span className={styles.rangeMid}>MAC%</span>
        <span>{max.toFixed(1)}</span>
      </div>
      <div className={styles.rangeBar}>
        <div
          className={`${styles.rangeMarker} ${error ? styles.rangeMarkerBad : ""}`}
          style={{ left: `${pct}%` }}
        />
      </div>
    </div>
  );
}

function formatReceivedAt(iso: string | null): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (isNaN(d.getTime())) return "—";
  // Local time, HH:MM:SS — same convention the FlightStatus log lines use.
  const hh = String(d.getHours()).padStart(2, "0");
  const mm = String(d.getMinutes()).padStart(2, "0");
  const ss = String(d.getSeconds()).padStart(2, "0");
  return `${hh}:${mm}:${ss}`;
}
