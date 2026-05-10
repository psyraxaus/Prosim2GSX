import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import {
  FmsSyncResultDto,
  PassengerManifestDto,
  PassengerSimulationResultDto,
  WeightBalanceDto,
} from "../types";
import { A320_OUTLINE_PATH } from "./a320Silhouette";
import styles from "./WeightBalancePanel.module.css";

// Aircraft Status section colours — matching the WPF model's brushes so
// the two surfaces read identically. Bulk on a non-fitted airframe falls
// back to a neutral grey; the rect is hidden, but the brush stays
// defined for the status text colour.
const DOOR_CLOSED_COLOR = "#4CAF50"; // green
const DOOR_OPEN_COLOR   = "#F5A623"; // amber
const DOOR_NA_COLOR     = "#555555"; // grey

// Seat overlay colours. Filled seats use the same green as a closed door
// for tonal consistency; empty seats use a darker neutral so the cabin
// reads as "empty by default" rather than "broken / N/A".
const SEAT_OCCUPIED_COLOR = "#4CAF50";
const SEAT_EMPTY_COLOR    = "#2A2A2A";

function doorColor(open: boolean, fitted = true): string {
  if (!fitted) return DOOR_NA_COLOR;
  return open ? DOOR_OPEN_COLOR : DOOR_CLOSED_COLOR;
}

function doorStatus(open: boolean, fitted = true): string {
  if (!fitted) return "N/A";
  return open ? "OPEN" : "CLOSED";
}

// Parse the comma-separated seatOccupation string ("true,false,...") into
// a 132-bool array. Mirrors WeightBalanceService.CountTrueChars but keeps
// per-seat granularity for the cabin overlay. Tolerates the "1"/"0" shape
// some legacy producers used; case-insensitive.
function parseSeatOccupation(s: string | undefined | null): boolean[] {
  if (!s) return [];
  return s.split(",").map(t => {
    const v = t.trim().toLowerCase();
    return v === "true" || v === "1";
  });
}

// ── Seat layout (source SVG coords, identical to door coords) ────────────
// Cabin tube runs source x=216..552 (aft → forward), y=355..395 (port →
// starboard) with the aisle at y=375. The -180° rotation around (375,
// 375) on the parent <g> flips this so the displayed cabin reads
// nose-LEFT, with port doors on the upper edge and starboard doors on
// the lower edge — same orientation the cargo doors already use.
//
// Zone breakdown is fixed by the A320 standard layout written into
// ProsimConstants.PaxZoneLimits {24, 30, 36, 42}. The seatOccupation
// string is indexed forward → aft, so seat index 0 is in zone 1 (most
// forward) and index 131 is in zone 4 (most aft). Each zone holds 6
// seats per row (3 port + aisle + 3 starboard).
const SEAT_ROWS_PER_ZONE = [4, 5, 6, 7]; // 24/6, 30/6, 36/6, 42/6 = 22 rows
const SEAT_ROW_PITCH     = 15;            // source-x distance between rows
const SEAT_RECT_W        = 11;            // source-x rect length
const SEAT_RECT_H        = 4;             // source-y rect height
const SEAT_X_FWD         = 552;           // source x of the forward-most row's leading edge
// Per-column source-y centres. ProSim's seat-string convention indexes
// each row STARBOARD-first: column 0 sits on the starboard side of the
// fuselage and column 5 on the port side. Mapping the columns to this
// y-array (after the parent <g> rotation flips the source frame) puts
// seat-string index 0 on the starboard half, so the cabin-fill
// animation runs starboard→port matching ProSim's own EFB. Spacing
// leaves a visible aisle gap at y=375.
const SEAT_Y_BY_COL = [392, 386, 380, 370, 364, 358];

// Pre-computed per-seat rect coords. Index = global seat number 0..131.
// Computed once at module load so the render loop stays cheap.
const SEAT_RECTS: { x: number; y: number; zone: number }[] = (() => {
  const out: { x: number; y: number; zone: number }[] = [];
  let rowOffset = 0;
  for (let zone = 0; zone < SEAT_ROWS_PER_ZONE.length; zone++) {
    const rowsInZone = SEAT_ROWS_PER_ZONE[zone];
    for (let r = 0; r < rowsInZone; r++) {
      const globalRow = rowOffset + r;
      const x = SEAT_X_FWD - globalRow * SEAT_ROW_PITCH;
      for (let col = 0; col < 6; col++) {
        out.push({ x, y: SEAT_Y_BY_COL[col] - SEAT_RECT_H / 2, zone: zone + 1 });
      }
    }
    rowOffset += rowsInZone;
  }
  return out;
})();

// ── Entry / overwing door positions (source SVG coords) ──────────────────
// Same coordinate space as the cargo doors. Source HIGH y is the
// starboard edge (R doors), source LOW y is the port edge (L doors).
// L1/R1 sit forward of zone 1 cabin start; L2/R2 + L3/R3 are at the
// overwing exit stations within the wing band; L4/R4 sit aft of zone 4
// where the tail begins to taper, so they're rotated slightly to follow
// the fuselage edge. Rect size (18×10) is smaller than the cargo doors
// (41×14) so the silhouette reads correctly at a glance — pax doors are
// visibly narrower than cargo doors in real life. Coords + rotations
// match ViewWeightBalance.xaml exactly so the WPF + web silhouettes
// stay aligned.
const DOOR_ENTRY_W = 13;
const DOOR_ENTRY_H = 7;
type EntryDoor = {
  id: "L1" | "R1" | "L2" | "R2" | "L3" | "R3" | "L4" | "R4";
  x: number;
  y: number;
  rotate?: number;
};
const ENTRY_DOORS: readonly EntryDoor[] = [
  { id: "L1", x: 563, y: 348 },
  { id: "L2", x: 415, y: 348 },
  { id: "L3", x: 400, y: 348 },
  { id: "L4", x: 215, y: 352, rotate: -8.374 },
  { id: "R1", x: 563, y: 395 },
  { id: "R2", x: 415, y: 395 },
  { id: "R3", x: 400, y: 395 },
  { id: "R4", x: 215, y: 392, rotate:  8.902 },
];

// Read-only Weight & Balance panel. Initial REST load on mount; live
// updates arrive through the WebSocket "weightBalance" channel and are
// merged into AppState by the default reducer branch.
//
// Layout system: every annotation, tick, label, and dot is positioned
// relative to the PNG image element's measured bounding box
// (offsetLeft / offsetTop / offsetWidth / offsetHeight). The PNG sizes
// itself naturally via CSS (width 80%, height auto, centred), and a
// ResizeObserver re-reads the bounding box whenever the chart container
// changes size. Position formulas are simple ratios — e.g. MTOW sits at
// (left + 0.47 × width, top + 0.125 × height) — making the layout
// resolution-independent and identical at any container size.

// ── Axis ranges (A320 family, KG mode) ─────────────────────────────────
const TOP_NUM_MIN     = 20;
const TOP_NUM_MAX     = 39;
const LEFT_NUM_MIN_KG = 35;
const LEFT_NUM_MAX_KG = 78;
const LEFT_KG_STEP    = 5;

// MAC% range used for clamping the live CG dots and the gauge-bar fills.
const AXIS_MAC_MIN = 21;
const AXIS_MAC_MAX = 38;

// ── Generated label / tick arrays ──────────────────────────────────────
// X-axis %MAC labels: skip the endpoint values (20, 39) and label every
// integer between. Yields [21, 22, …, 38].
const TOP_NUMBERS = Array(TOP_NUM_MAX - TOP_NUM_MIN - 1)
  .fill(0)
  .map((_, S) => TOP_NUM_MIN + S + 1);

// Y-axis weight labels at 5T intervals. Yields [35, 40, 45, …, 75].
const LEFT_NUMBERS_KG = Array(Math.round((LEFT_NUM_MAX_KG - LEFT_NUM_MIN_KG) / LEFT_KG_STEP))
  .fill(0)
  .map((_, S) => LEFT_NUM_MIN_KG + S * LEFT_KG_STEP);

// Y-axis tick marks at 1T intervals. Long when divisible by 5T (matches a
// numeric label), short otherwise. Yields [35, 36, …, 77].
const LEFT_LINES_KG = Array(Math.round(LEFT_NUM_MAX_KG - LEFT_NUM_MIN_KG))
  .fill(0)
  .map((_, S) => LEFT_NUM_MIN_KG + S);

// ── Annotation positions as ratios of PNG width / height ───────────────
const ANNOT_RATIOS = {
  mtow:  { x: 0.47, y: 0.125 },     // MTOW = 73,500KG
  mlw:   { x: 0.43, y: 0.29  },     // MLW  = 64,500KG
  mzfw:  { x: 0.40, y: 0.37  },     // MZFW = 61,000KG
  opLim: { x: 0.40, y: 0.56  },     // Operational Limits (centre, horizontal)
  tolR:  { x: 0.69, y: 0.56  },     // Take-Off Limits — right side, rotated
  zfwR:  { x: 0.70, y: 0.66  },     // Zfw limit — right side, rotated
  zfwL:  { x: 0.16, y: 0.64  },     // Zfw limit — left side, rotated
  tolL:  { x: 0.12, y: 0.80  },     // Take-Off Limits — bottom-left, rotated
};

// Dot half-size in px. Fixed (the source chart uses a separate small
// element measured at runtime; ours is a constant for simplicity).
const DOT_RADIUS = 11;

export function WeightBalancePanel() {
  const { get, post } = useApi();
  const { state, dispatch } = useAppState();

  // Sync-to-FMS UI state. "idle" | "pending" | "success" | "error".
  // Success/error transitions are auto-cleared on a setTimeout matching
  // the WPF DispatcherTimer durations (3 s green / 5 s red).
  const [syncStatus, setSyncStatus] = useState<"idle" | "pending" | "success" | "error">("idle");
  const [syncMessage, setSyncMessage] = useState<string>("");
  const syncTimerRef = useRef<number | null>(null);

  // Passenger simulation UI state. Mirrors the FMS-sync flash pattern —
  // pending while the POST is in flight, success/error on resolution,
  // cleared by a setTimeout. Manifest persists across the success flash
  // so the user can see the generated names after the toast clears.
  const [simOpen, setSimOpen] = useState(false);
  const [simCount, setSimCount] = useState<string>("");
  const [simStatus, setSimStatus] = useState<"idle" | "pending" | "success" | "error">("idle");
  const [simMessage, setSimMessage] = useState<string>("");
  const [manifest, setManifest] = useState<PassengerManifestDto | null>(null);
  const [manifestOpen, setManifestOpen] = useState(false);
  const simTimerRef = useRef<number | null>(null);

  // PNG image bounding box. All chart annotations are positioned relative
  // to these four values. Re-measured by ResizeObserver on container size
  // changes; also re-measured on the image's `load` event so the first
  // useful measurement happens as soon as the bitmap dimensions are known.
  const imgRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const [pngBox, setPngBox] = useState({ left: 0, top: 0, width: 0, height: 0 });

  const measure = () => {
    const el = imgRef.current;
    if (!el || el.offsetHeight === 0) return;
    setPngBox({
      left:   el.offsetLeft,
      top:    el.offsetTop,
      width:  el.offsetWidth,
      height: el.offsetHeight,
    });
  };

  useLayoutEffect(() => {
    measure();
    const ro = new ResizeObserver(() => measure());
    if (containerRef.current) ro.observe(containerRef.current);
    return () => ro.disconnect();
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const dto = await get<WeightBalanceDto>("/weightbalance");
        if (!cancelled) {
          dispatch({
            type: "set",
            channel: "weightBalance",
            state: dto as unknown as Record<string, unknown>,
          });
        }
      } catch {
        /* useApi already handled 401; WS will fill in once connected */
      }
    })();
    return () => { cancelled = true; };
  }, [get, dispatch]);

  // Clear any pending flash timer on unmount so a navigate-away mid-flash
  // doesn't fire setState on an unmounted component.
  useEffect(() => () => {
    if (syncTimerRef.current !== null) {
      window.clearTimeout(syncTimerRef.current);
      syncTimerRef.current = null;
    }
    if (simTimerRef.current !== null) {
      window.clearTimeout(simTimerRef.current);
      simTimerRef.current = null;
    }
  }, []);

  // Fetch the cached simulation manifest on mount so a refresh of the
  // page after a SIMULATE click brings back the same names. Server holds
  // it in-memory only — non-zero totalPassengers means a manifest exists.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const m = await get<PassengerManifestDto>("/passengers/manifest");
        if (!cancelled && m && m.totalPassengers > 0) setManifest(m);
      } catch {
        /* useApi already handled 401; not having a manifest is fine */
      }
    })();
    return () => { cancelled = true; };
  }, [get]);

  const wb = state.weightBalance as unknown as WeightBalanceDto | null;
  if (!wb) {
    return <div className={styles.loading}>Loading weight &amp; balance…</div>;
  }

  const triggerFlash = (status: "success" | "error", message: string, durationMs: number) => {
    setSyncStatus(status);
    setSyncMessage(message);
    if (syncTimerRef.current !== null) window.clearTimeout(syncTimerRef.current);
    syncTimerRef.current = window.setTimeout(() => {
      setSyncStatus("idle");
      setSyncMessage("");
      syncTimerRef.current = null;
    }, durationMs);
  };

  const triggerSimFlash = (status: "success" | "error", message: string, durationMs: number) => {
    setSimStatus(status);
    setSimMessage(message);
    if (simTimerRef.current !== null) window.clearTimeout(simTimerRef.current);
    simTimerRef.current = window.setTimeout(() => {
      setSimStatus("idle");
      setSimMessage("");
      simTimerRef.current = null;
    }, durationMs);
  };

  const handleSimulate = async () => {
    if (simStatus === "pending") return;
    let parsedCount: number | undefined;
    const trimmed = simCount.trim();
    if (trimmed !== "") {
      const n = parseInt(trimmed, 10);
      if (Number.isNaN(n) || n < 0) {
        triggerSimFlash("error", "Invalid count", 4000);
        return;
      }
      parsedCount = n;
    }
    setSimStatus("pending");
    setSimMessage("");
    try {
      const result = await post<PassengerSimulationResultDto>("/passengers/simulate", { count: parsedCount });
      if (result?.success && result.manifest) {
        setManifest(result.manifest);
        setManifestOpen(true);
        triggerSimFlash("success", `Generated ${result.manifest.totalPassengers} pax`, 3000);
      } else {
        triggerSimFlash("error", result?.errorMessage || "Generation failed", 5000);
      }
    } catch (e) {
      triggerSimFlash("error", (e as Error)?.message || "Generation failed", 5000);
    }
  };

  const handleClearPax = async () => {
    if (simStatus === "pending") return;
    setSimStatus("pending");
    setSimMessage("");
    try {
      const result = await post<PassengerSimulationResultDto>("/passengers/clear");
      if (result?.success) {
        setManifest(null);
        triggerSimFlash("success", "Cabin cleared", 2000);
      } else {
        triggerSimFlash("error", result?.errorMessage || "Clear failed", 5000);
      }
    } catch (e) {
      triggerSimFlash("error", (e as Error)?.message || "Clear failed", 5000);
    }
  };

  const handleSync = async () => {
    if (syncStatus === "pending" || wb.maczfwResolvedError) return;
    setSyncStatus("pending");
    setSyncMessage("");
    try {
      const result = await post<FmsSyncResultDto>("/fms/sync");
      if (result?.success) {
        triggerFlash("success", "FMS UPDATED", 3000);
      } else {
        triggerFlash("error", result?.errorMessage || "FMS sync failed", 5000);
      }
    } catch (e) {
      triggerFlash("error", (e as Error)?.message || "FMS sync failed", 5000);
    }
  };

  // ── Coordinate helpers ─────────────────────────────────────────────────
  // X position for a MAC% value, anchored at the PNG's left edge.
  const xForMac = (m: number) =>
    pngBox.left + (pngBox.width * (m - TOP_NUM_MIN)) / (TOP_NUM_MAX - TOP_NUM_MIN);
  // Y position for a weight (in tonnes), anchored at the PNG's top edge.
  // The −2 offset matches the source chart so labels visually align with
  // the centre of their tick marks.
  const yForT = (t: number) =>
    pngBox.top + (pngBox.height * (LEFT_NUM_MAX_KG - t)) / (LEFT_NUM_MAX_KG - LEFT_NUM_MIN_KG) - 2;

  const annotPos = (key: keyof typeof ANNOT_RATIOS) => ({
    left: pngBox.left + ANNOT_RATIOS[key].x * pngBox.width,
    top:  pngBox.top  + ANNOT_RATIOS[key].y * pngBox.height,
  });

  // Map live DTO values to PNG-relative pixel positions for the dots.
  const macToX = (mac: number) => {
    if (!Number.isFinite(mac) || mac <= 0) return xForMac(AXIS_MAC_MIN);
    const clamped = Math.max(AXIS_MAC_MIN, Math.min(AXIS_MAC_MAX, mac));
    return xForMac(clamped);
  };
  const weightToY = (kg: number) => {
    const t = (kg ?? 0) / 1000;
    if (!Number.isFinite(t) || t <= 0) return yForT(LEFT_NUM_MIN_KG);
    const clamped = Math.max(LEFT_NUM_MIN_KG, Math.min(75, t));
    return yForT(clamped);
  };
  const macFraction = (mac: number) => {
    if (!Number.isFinite(mac) || mac <= 0) return 0;
    const clamped = Math.max(AXIS_MAC_MIN, Math.min(AXIS_MAC_MAX, mac));
    return (clamped - AXIS_MAC_MIN) / (AXIS_MAC_MAX - AXIS_MAC_MIN);
  };

  // Single live marker at (MACGW, GW) — matches the ProSim EFB style.
  // The ZFW gauge bar below the chart still uses zfwBarPct, so the
  // ZFW MAC value retains a visual representation; only the redundant
  // chart dot was dropped.
  const gwDotLeft = macToX(wb.macgwPercent) - DOT_RADIUS;
  const gwDotTop  = weightToY(wb.gwKg) - DOT_RADIUS;

  const zfwBarPct = macFraction(wb.maczfwPercent) * 100;
  const gwBarPct  = macFraction(wb.macgwPercent) * 100;

  // Hide the dot when there's no data (avoids parking it at a corner).
  const showGwDot = wb.gwKg > 0 && wb.macgwPercent > 0;

  const totalCapacity = wb.zone1Capacity + wb.zone2Capacity + wb.zone3Capacity + wb.zone4Capacity;
  const cargoLoadedTotal = wb.cargoFwdLoadedKg + wb.cargoAftLoadedKg;

  // Suppress overlay rendering until the PNG box is measured; otherwise
  // labels would briefly stack at (0, 0) on first paint.
  const ready = pngBox.height > 0;

  return (
    <div className={styles.panel}>
      {/* LEFT COLUMN — chart + bottom info area */}
      <section className={styles.chartCol}>
        <h2 className={styles.colHeading}>CG Envelope</h2>
        <div className={styles.chartCard}>
          <div ref={containerRef} className={styles.chart}>
            <img
              ref={imgRef}
              className={styles.chartImg}
              src="/assets/img/wandb.png"
              alt=""
              onLoad={measure}
            />

            {ready && (
              <>
                {/* Y-axis labels (left side, weight in tonnes). */}
                {LEFT_NUMBERS_KG.map((t) => (
                  <div
                    key={`ylbl-${t}`}
                    className={`${styles.axisLabel} ${styles.axisLabelY}`}
                    style={{ left: pngBox.left, top: yForT(t) }}
                  >{t}</div>
                ))}

                {/* Y-axis tick marks. Long every 5T (next to numeric labels),
                    short every 1T between. */}
                {LEFT_LINES_KG.map((t) => {
                  const isLong = t % 5 === 0;
                  return (
                    <div
                      key={`tick-${t}`}
                      className={isLong ? styles.tickLong : styles.tickShort}
                      style={{ left: pngBox.left, top: yForT(t) }}
                    />
                  );
                })}

                {/* X-axis "%MAC" gutter label + integer MAC labels. */}
                <div
                  className={`${styles.axisLabel} ${styles.axisLabelX}`}
                  style={{ left: pngBox.left - 10, top: pngBox.top - 10 }}
                >%MAC</div>
                {TOP_NUMBERS.map((m) => (
                  <div
                    key={`xlbl-${m}`}
                    className={`${styles.axisLabel} ${styles.axisLabelX}`}
                    style={{ left: xForMac(m), top: pngBox.top - 10 }}
                  >{m}</div>
                ))}

                {/* Limit-line annotations. */}
                <div className={styles.limitLabel} style={annotPos("mtow")}>MTOW = 73,500KG</div>
                <div className={styles.limitLabel} style={annotPos("mlw")}>MLW = 64,500KG</div>
                <div className={styles.limitLabel} style={annotPos("mzfw")}>MZFW = 61,000KG</div>

                {/* Envelope-region annotations (rotation per-id via .annot1..5). */}
                <div className={`${styles.envLabel} ${styles.annot1}`} style={annotPos("opLim")}>Operational Limits</div>
                <div className={`${styles.envLabel} ${styles.annot2}`} style={annotPos("tolR")}>Take-Off Limits</div>
                <div className={`${styles.envLabel} ${styles.annot3}`} style={annotPos("zfwR")}>Zfw limit</div>
                <div className={`${styles.envLabel} ${styles.annot4}`} style={annotPos("zfwL")}>Zfw limit</div>
                <div className={`${styles.envLabel} ${styles.annot5}`} style={annotPos("tolL")}>Take-Off Limits</div>

                {/* Live GW dot — red+white quadrant crosshair. Single
                    marker matches ProSim EFB exactly: positioned at
                    (MACGW, GW), styled with the red quadrant-fill +
                    white crosshair. The previous ZFW dot was dropped to
                    keep the chart uncluttered (ProSim shows only one
                    marker; live ZFW reads in the summary table below). */}
                {showGwDot && (
                  <svg
                    className={styles.dot}
                    style={{ left: gwDotLeft, top: gwDotTop }}
                    width={DOT_RADIUS * 2} height={DOT_RADIUS * 2} viewBox="0 0 22 22"
                  >
                    <circle cx="11" cy="11" r="10" fill="#FFFFFF" stroke="#000" strokeOpacity="0.5" />
                    <path d="M 11 1 A 10 10 0 0 1 21 11 L 11 11 Z" fill="#D81E1E" />
                    <path d="M 11 21 A 10 10 0 0 1 1 11 L 11 11 Z" fill="#D81E1E" />
                    <line x1="11" y1="-2" x2="11" y2="24" stroke="#FFFFFF" strokeWidth="1.2" />
                    <line x1="-2" y1="11" x2="24" y2="11" stroke="#FFFFFF" strokeWidth="1.2" />
                  </svg>
                )}
              </>
            )}
          </div>

          {/* MAC% gauge bars + bordered summary table — full chart width. */}
          <div className={styles.gaugeBar}>
            <div className={styles.gaugeFillZfw} style={{ width: `${zfwBarPct}%` }} />
          </div>
          <div className={styles.gaugeBar}>
            <div className={styles.gaugeFillGw} style={{ width: `${gwBarPct}%` }} />
          </div>

          {/* Two-row summary: LIVE reads the aircraft datarefs each tick;
              LOADSHEET mirrors the dispatcher-signed values (final →
              prelim). When no loadsheet has been received yet the row is
              greyed and shows dashes. The "GW/MACGW" columns of the
              LOADSHEET row are the loadsheet's TOW/MACTOW (ProSim's
              loadsheet doesn't carry a separate gross-weight at current
              fuel — TOW is the equivalent take-off snapshot). */}
          <table className={styles.summary}>
            <thead>
              <tr>
                <th></th>
                <th>ZFW (KG)</th>
                <th>MACZFW (%)</th>
                <th>GW (KG)</th>
                <th>MACGW (%)</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <th scope="row" className={styles.rowLabel}>LIVE</th>
                <td>{wb.zfwKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>{wb.maczfwPercent.toFixed(1)}</td>
                <td>{wb.gwKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>{wb.macgwPercent.toFixed(1)}</td>
              </tr>
              <tr className={wb.loadsheetSource === "none" ? styles.rowMuted : ""}>
                <th scope="row" className={styles.rowLabel}>
                  L/S
                  {wb.loadsheetSource === "final" && <span className={styles.rowLabelTag}> FIN</span>}
                  {wb.loadsheetSource === "prelim" && <span className={styles.rowLabelTag}> PRE</span>}
                </th>
                <td>{wb.loadsheetSource === "none"
                      ? "—"
                      : wb.loadsheetZfwKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>{wb.loadsheetSource === "none" ? "—" : wb.loadsheetMaczfwPercent.toFixed(1)}</td>
                <td>{wb.loadsheetSource === "none"
                      ? "—"
                      : wb.loadsheetTowKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>{wb.loadsheetSource === "none" ? "—" : wb.loadsheetMactowPercent.toFixed(1)}</td>
              </tr>
            </tbody>
          </table>

          <p className={styles.note}>
            LIVE reads aircraft datarefs each tick. L/S mirrors the latest
            loadsheet (final preferred, prelim fallback).
          </p>

          {/* MACZFW row + SYNC TO FMS button. Resolved MACZFW is sourced
              from FmsSyncService (final → prelim → computed) and pushed
              into wb.maczfwResolvedPercent each tick by WeightBalanceService.
              The button is disabled while syncing OR when MACZFW is out of
              envelope; the flash background follows syncStatus and clears
              on a 3s/5s timer matching the WPF parity. */}
          <div className={styles.mactowRow}>
            <div className={styles.mactowLine}>
              <span className={styles.mactowLabel}>MACZFW (%):</span>
              <span className={wb.maczfwResolvedError ? styles.mactowValueError : styles.mactowValueOk}>
                {wb.maczfwResolvedPercent.toFixed(1)}
              </span>
              {wb.maczfwResolvedError && (
                <span className={styles.mactowWarn} aria-label="out of range">⚠</span>
              )}
              {/* Resolution-source chip: FINAL LS / PRELIM LS / COMPUTED. */}
              <span
                className={[
                  styles.sourceChip,
                  wb.maczfwResolvedSource === "final" ? styles.sourceChipFinal : "",
                  wb.maczfwResolvedSource === "prelim" ? styles.sourceChipPrelim : "",
                  wb.maczfwResolvedSource === "computed" ? styles.sourceChipComputed : "",
                ].filter(Boolean).join(" ")}
                title={
                  wb.maczfwResolvedSource === "final"
                    ? "MACZFW from final loadsheet (authoritative)"
                    : wb.maczfwResolvedSource === "prelim"
                    ? "MACZFW from preliminary loadsheet (will upgrade when final arrives)"
                    : "No loadsheet received yet — value is live aircraft.zfwcg mirror"
                }
              >
                {wb.maczfwResolvedSource === "final" ? "FINAL LS" : wb.maczfwResolvedSource === "prelim" ? "PRELIM LS" : "COMPUTED"}
              </span>
            </div>
            {wb.maczfwResolvedError && (
              <div className={styles.mactowRange}>
                VALID RANGE: {wb.minMacTow.toFixed(1)} – {wb.maxMacTow.toFixed(1)}
              </div>
            )}
            {wb.fmsSyncStale && (
              <div className={styles.fmsStale}>
                FMS OUT OF DATE
                {wb.fmsLastSyncedAt && (
                  <> — last sync {new Date(wb.fmsLastSyncedAt).toLocaleTimeString()}{" "}
                    {wb.fmsLastSyncedSource && `(${wb.fmsLastSyncedSource.toUpperCase()})`}
                  </>
                )}
              </div>
            )}
          </div>

          <div className={styles.fmsSyncRow}>
            <button
              type="button"
              className={[
                styles.fmsSyncButton,
                syncStatus === "success" ? styles.fmsSyncSuccess : "",
                syncStatus === "error" ? styles.fmsSyncError : "",
              ].filter(Boolean).join(" ")}
              disabled={syncStatus === "pending" || wb.maczfwResolvedError}
              title={wb.maczfwResolvedError ? "MACZFW out of range" : ""}
              onClick={handleSync}
            >
              {syncStatus === "pending"
                ? "SYNCING…"
                : (() => {
                    const verb = wb.fmsSyncStale ? "RESYNC TO FMS" : "SYNC TO FMS";
                    const suffix =
                      wb.maczfwResolvedSource === "final" ? " (FINAL)" :
                      wb.maczfwResolvedSource === "prelim" ? " (PRELIM)" :
                      " (COMPUTED)";
                    return verb + suffix;
                  })()}
            </button>
            {syncMessage && (
              <span
                className={
                  syncStatus === "success"
                    ? styles.fmsSyncMessageOk
                    : styles.fmsSyncMessageError
                }
              >
                {syncMessage}
              </span>
            )}
          </div>
        </div>
      </section>

      {/* RIGHT COLUMN — passengers / cargo / fuel cards */}
      <section className={styles.dataCol}>
        <h2 className={styles.colHeading}>Passengers</h2>
        <div className={styles.dataCard}>
          <div className={styles.capacity}>
            CAPACITY: {totalCapacity} ECONOMY
          </div>
          <div className={styles.dataRow}>
            <span className={styles.label}>Pax</span>
            <span className={styles.headerCell}>PLANNED</span>
            <span className={styles.headerCell}>BOARDED</span>
          </div>
          <div className={styles.dataRow}>
            <span className={styles.label}>&nbsp;</span>
            <span className={styles.value}>{wb.passengersPlanned}</span>
            <span className={styles.value}>{wb.passengersBoarded}</span>
          </div>

          {/* Passenger simulation. SIMULATE expands an inline form with a
              count input + GENERATE/CLEAR. Submission writes
              seatOccupation.string directly via the SDK; the seating map
              on the Aircraft Status silhouette below picks up the change
              on the next StateUpdateWorker tick because WeightBalanceService
              re-reads the dataref unconditionally. */}
          <div className={styles.simulateRow}>
            {!simOpen ? (
              <button type="button"
                      className={styles.simulateButton}
                      onClick={() => setSimOpen(true)}>
                SIMULATE
              </button>
            ) : (
              <>
                <label className={styles.simulateLabel}>
                  COUNT
                  <input
                    type="number"
                    min={0}
                    max={totalCapacity}
                    value={simCount}
                    placeholder={String(totalCapacity)}
                    onChange={e => setSimCount(e.target.value)}
                    className={styles.simulateInput}
                  />
                </label>
                <button
                  type="button"
                  className={[
                    styles.simulateButton,
                    styles.simulateGenerate,
                    simStatus === "success" ? styles.simulateSuccess : "",
                    simStatus === "error" ? styles.simulateError : "",
                  ].filter(Boolean).join(" ")}
                  disabled={simStatus === "pending"}
                  onClick={handleSimulate}>
                  {simStatus === "pending" ? "…" : "GENERATE"}
                </button>
                <button type="button"
                        className={styles.simulateButton}
                        disabled={simStatus === "pending"}
                        onClick={handleClearPax}>
                  CLEAR
                </button>
                <button type="button"
                        className={styles.simulateClose}
                        onClick={() => setSimOpen(false)}
                        title="Hide controls">×</button>
              </>
            )}
            {simMessage && (
              <span className={
                simStatus === "success" ? styles.simulateMessageOk :
                simStatus === "error"   ? styles.simulateMessageError : ""
              }>{simMessage}</span>
            )}
          </div>
          {simOpen && (
            <p className={styles.simulateNote}>
              SIMULATE works for headless cabins; GSX boarding will overwrite.
            </p>
          )}
        </div>

        <h2 className={styles.colHeading}>Cargo</h2>
        <div className={styles.dataCard}>
          <div className={styles.capacity}>
            CAPACITY:&nbsp;
            {wb.cargoFwdCapacityKg.toLocaleString()} KG FWD,&nbsp;
            {wb.cargoAftCapacityKg.toLocaleString()} KG AFT,&nbsp;
            {wb.cargoBulkCapacityKg.toLocaleString()} KG BULK
          </div>
          <div className={styles.dataRow}>
            <span className={styles.label}>Cargo</span>
            <span className={styles.headerCell}>PLANNED (KG)</span>
            <span className={styles.headerCell}>LOADED (KG)</span>
          </div>
          <div className={styles.dataRow}>
            <span className={styles.label}>&nbsp;</span>
            <span className={styles.value}>{wb.cargoPlannedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</span>
            <span className={styles.value}>{cargoLoadedTotal.toLocaleString(undefined, { maximumFractionDigits: 0 })}</span>
          </div>
          {/* FWD/AFT split row — label sits in the Cargo column, the
              split values sit aligned under PLANNED so the per-hold
              numbers read as a refinement of the planned total above
              them rather than as a separate trailing footer. */}
          <div className={styles.dataRow}>
            <span className={styles.label}>FWD / AFT</span>
            <span className={styles.subValue}>
              {wb.cargoFwdLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}
              {" / "}
              {wb.cargoAftLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}
            </span>
            <span className={styles.value}>&nbsp;</span>
          </div>
        </div>

        {/* AIRCRAFT STATUS — top-down silhouette combining: 132-seat
            cabin overlay (fed by wb.seatOccupation), 8 pax/overwing
            doors L1..R4, 3 cargo doors (FWD/AFT/BULK), and a
            departure-readiness banner. All elements share one SVG
            in source coords + a -180° rotation around (375, 375) on
            the parent <g>, so the WPF tab and React panel stay
            visually identical (same A320_OUTLINE_PATH on both sides).
            The readiness banner mirrors !Aircraft.HasOpenDoors —
            same predicate the GSX state machine uses to gate
            CloseAllDoors() on final, so it can read "DOORS OPEN"
            even when only a pax door is open. */}
        <h2 className={styles.colHeading}>Aircraft Status</h2>
        <div className={styles.dataCard}>
          {/* viewBox crops to the central horizontal band (source y=270..480
              = fuselage + engines + inner wing) so the silhouette renders
              wide and short, filling the card's wider axis. The -180°
              rotation puts source-RIGHT (nose) on display-LEFT, giving a
              horizontal aircraft with fuselage along the card width.
              Door positions and seat positions stay the same in source
              coords; they rotate with the path. */}
          <svg viewBox="0 270 750 210" className={styles.silhouetteSvg}>
            <g transform="rotate(-180 375 375)">
              <path d={A320_OUTLINE_PATH} fill="#3F3F3F" stroke="#7A7A7A" strokeWidth={2} />

              {/* Seat overlay — 132 small rects, source coords inside the
                  fuselage tube. Indexing matches the seatOccupation
                  string: index 0 is forward-most port window in zone 1,
                  index 131 is aft-most starboard window in zone 4.
                  When seatOccupation is shorter or missing (null SDK)
                  every seat reads as empty, which matches reality. */}
              {(() => {
                const occupied = parseSeatOccupation(wb.seatOccupation);
                return SEAT_RECTS.map((s, i) => (
                  <rect key={`seat-${i}`} x={s.x} y={s.y}
                        width={SEAT_RECT_W} height={SEAT_RECT_H}
                        fill={occupied[i] ? SEAT_OCCUPIED_COLOR : SEAT_EMPTY_COLOR}
                        stroke="#1A1A1A" strokeWidth={0.4} />
                ));
              })()}

              {/* Cargo doors — three on the starboard fuselage edge
                  (source HIGH y) matching the real A320: fwd, aft, and
                  bulk all sit on the right side of the airframe. Long
                  axis runs along source X (fore-aft) and becomes
                  vertical in display. Coords match ViewWeightBalance.xaml
                  exactly so WPF + web stay aligned. */}
              <rect x={498} y={403} width={41} height={14}
                    fill={doorColor(wb.fwdCargoDoorOpen)}
                    stroke="#FFFFFF" strokeWidth={1.2} />
              <rect x={293} y={403} width={41} height={14}
                    fill={doorColor(wb.aftCargoDoorOpen)}
                    stroke="#FFFFFF" strokeWidth={1.2} />
              {/* BULK is always rendered so the silhouette reads
                  consistently across airframes; doorColor returns grey
                  when CargoBulkCapacityKg = 0, mirroring the WPF
                  BulkDoorBrush. Status text below still reads "N/A"
                  in that case. */}
              <rect x={255} y={403} width={20} height={11}
                    transform="rotate(-0.265 265 408.5)"
                    fill={doorColor(wb.bulkCargoDoorOpen, wb.cargoBulkCapacityKg > 0)}
                    stroke="#FFFFFF" strokeWidth={1.2} />

              {/* Entry / overwing doors L1..R4 — port doors on the
                  upper fuselage edge (source LOW y), starboard doors on
                  the lower edge (source HIGH y). Smaller than cargo
                  rects so the silhouette reads at a glance: pax doors
                  are visibly narrower than cargo doors in real life.
                  L4/R4 carry a small rotation matching the tail taper
                  in ViewWeightBalance.xaml so the rect aligns with the
                  fuselage edge. The rotation pivots around the rect's
                  centre to mirror WPF's RenderTransformOrigin="0.5,0.5". */}
              {ENTRY_DOORS.map(d => {
                const open =
                  d.id === "L1" ? wb.door1LOpen :
                  d.id === "R1" ? wb.door1ROpen :
                  d.id === "L2" ? wb.door2LOpen :
                  d.id === "R2" ? wb.door2ROpen :
                  d.id === "L3" ? wb.door3LOpen :
                  d.id === "R3" ? wb.door3ROpen :
                  d.id === "L4" ? wb.door4LOpen :
                                  wb.door4ROpen;
                const cx = d.x + DOOR_ENTRY_W / 2;
                const cy = d.y + DOOR_ENTRY_H / 2;
                const transform = d.rotate !== undefined
                  ? `rotate(${d.rotate} ${cx} ${cy})`
                  : undefined;
                return (
                  <rect key={`entry-${d.id}`}
                        x={d.x} y={d.y}
                        width={DOOR_ENTRY_W} height={DOOR_ENTRY_H}
                        transform={transform}
                        fill={doorColor(open)}
                        stroke="#FFFFFF" strokeWidth={1} />
                );
              })}
            </g>
          </svg>

          {/* Per-door status grid — three equal cells, value text takes
              the matching door colour so a quick scan picks the right
              door without reading the label. */}
          <div className={styles.doorStatusGrid}>
            <div className={styles.doorStatusCell}>
              <span className={styles.doorStatusLabel}>FWD</span>
              <span className={styles.doorStatusValue}
                    style={{ color: doorColor(wb.fwdCargoDoorOpen) }}>
                {doorStatus(wb.fwdCargoDoorOpen)}
              </span>
            </div>
            <div className={styles.doorStatusCell}>
              <span className={styles.doorStatusLabel}>AFT</span>
              <span className={styles.doorStatusValue}
                    style={{ color: doorColor(wb.aftCargoDoorOpen) }}>
                {doorStatus(wb.aftCargoDoorOpen)}
              </span>
            </div>
            <div className={styles.doorStatusCell}>
              <span className={styles.doorStatusLabel}>BULK</span>
              <span className={styles.doorStatusValue}
                    style={{ color: doorColor(wb.bulkCargoDoorOpen, wb.cargoBulkCapacityKg > 0) }}>
                {doorStatus(wb.bulkCargoDoorOpen, wb.cargoBulkCapacityKg > 0)}
              </span>
            </div>
          </div>

          <div className={[
            styles.readinessBanner,
            wb.allDoorsClosed ? styles.readinessOk : styles.readinessOpen,
          ].join(" ")}>
            {wb.allDoorsClosed ? "ALL DOORS CLOSED" : "DOORS OPEN"}
          </div>

          {/* MANIFEST — the names + seat assignments backing the current
              SIMULATE output. Collapsed by default after the success
              flash clears so the panel stays compact. Empty manifest
              hides the section entirely. */}
          {manifest && manifest.totalPassengers > 0 && (
            <div className={styles.manifestSection}>
              <button type="button"
                      className={styles.manifestToggle}
                      onClick={() => setManifestOpen(o => !o)}
                      aria-expanded={manifestOpen}>
                <span>{manifestOpen ? "▾" : "▸"} MANIFEST ({manifest.totalPassengers})</span>
                {!manifest.seatOccupationWritten && (
                  <span className={styles.manifestWarn} title="seatOccupation write failed">⚠</span>
                )}
              </button>
              {manifestOpen && (
                <div className={styles.manifestTableWrap}>
                  <table className={styles.manifestTable}>
                    <thead>
                      <tr>
                        <th>SEAT</th>
                        <th>NAME</th>
                        <th>ZONE</th>
                      </tr>
                    </thead>
                    <tbody>
                      {manifest.passengers.map(p => (
                        <tr key={p.seatNumber}>
                          <td>{p.seatNumber}</td>
                          <td>{p.firstName} {p.lastName}</td>
                          <td>Z{p.zone}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </div>

        <h2 className={styles.colHeading}>Fuel</h2>
        <div className={styles.dataCard}>
          <div className={styles.capacity}>
            CAPACITY USABLE {wb.fuelCapacityKg.toLocaleString(undefined, { maximumFractionDigits: 0 })} KG — SG: 0.80
          </div>
          <div className={styles.dataRow}>
            <span className={styles.label}>Fuel</span>
            <span className={styles.headerCell}>PLANNED (KG)</span>
            <span className={styles.headerCell}>IN TANKS (KG)</span>
          </div>
          <div className={styles.dataRow}>
            <span className={styles.label}>&nbsp;</span>
            <span className={styles.value}>{wb.fuelPlannedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</span>
            <span className={styles.value}>{wb.fuelInTanksKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</span>
          </div>
        </div>
      </section>
    </div>
  );
}
