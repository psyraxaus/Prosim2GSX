import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { WeightBalanceDto } from "../types";
import styles from "./WeightBalancePanel.module.css";

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
  const { get } = useApi();
  const { state, dispatch } = useAppState();

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

  const wb = state.weightBalance as unknown as WeightBalanceDto | null;
  if (!wb) {
    return <div className={styles.loading}>Loading weight &amp; balance…</div>;
  }

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

  const zfwDotLeft = macToX(wb.maczfwPercent) - DOT_RADIUS;
  const zfwDotTop  = weightToY(wb.zfwKg) - DOT_RADIUS;
  const gwDotLeft  = macToX(wb.macgwPercent) - DOT_RADIUS;
  const gwDotTop   = weightToY(wb.gwKg) - DOT_RADIUS;

  const zfwBarPct = macFraction(wb.maczfwPercent) * 100;
  const gwBarPct  = macFraction(wb.macgwPercent) * 100;

  // Hide dots when there's no data (avoids parking them at a corner).
  const showZfwDot = wb.zfwKg > 0 && wb.maczfwPercent > 0;
  const showGwDot  = wb.gwKg > 0 && wb.macgwPercent > 0;

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

                {/* Live ZFW dot — pink quadrant-fill crosshair. */}
                {showZfwDot && (
                  <svg
                    className={styles.dot}
                    style={{ left: zfwDotLeft, top: zfwDotTop }}
                    width={DOT_RADIUS * 2} height={DOT_RADIUS * 2} viewBox="0 0 22 22"
                  >
                    <circle cx="11" cy="11" r="10" fill="#FFFFFF" stroke="#000" strokeOpacity="0.5" />
                    <path d="M 11 1 A 10 10 0 0 1 21 11 L 11 11 Z" fill="#E91E63" />
                    <path d="M 11 21 A 10 10 0 0 1 1 11 L 11 11 Z" fill="#E91E63" />
                    <line x1="11" y1="-2" x2="11" y2="24" stroke="#FFFFFF" strokeWidth="1.2" />
                    <line x1="-2" y1="11" x2="24" y2="11" stroke="#FFFFFF" strokeWidth="1.2" />
                  </svg>
                )}

                {/* Live GW dot — light-blue with white crosshair. */}
                {showGwDot && (
                  <svg
                    className={styles.dot}
                    style={{ left: gwDotLeft, top: gwDotTop }}
                    width={DOT_RADIUS * 2} height={DOT_RADIUS * 2} viewBox="0 0 22 22"
                  >
                    <circle cx="11" cy="11" r="10" fill="#80B7CFD9" stroke="#FFFFFF" />
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

          <table className={styles.summary}>
            <thead>
              <tr>
                <th>ZFW (KG)</th>
                <th>MACZFW (%)</th>
                <th>GW (KG)</th>
                <th>MACGW (%)</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>{wb.zfwKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>{wb.maczfwPercent.toFixed(1)}</td>
                <td>{wb.gwKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>{wb.macgwPercent.toFixed(1)}</td>
              </tr>
            </tbody>
          </table>

          <p className={styles.note}>NOTE: THE ABOVE FIGURES ARE LIVE.</p>
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
          <div className={styles.dataRow}>
            <span className={styles.label}>FWD / AFT</span>
            <span className={styles.value}>&nbsp;</span>
            <span className={styles.subValue}>
              {wb.cargoFwdLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}
              {" / "}
              {wb.cargoAftLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })}
            </span>
          </div>
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
