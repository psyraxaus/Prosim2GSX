import styles from "./PerfShared.module.css";

// Single-row METAR display. Shows the raw METAR text plus a fetched-at
// timestamp; renders an italic "no METAR available" line when the
// gateway returned 204.

interface Props {
  icao: string;
  metarText: string;
  fetchedAt: string | null;
}

export function MetarStrip({ icao, metarText, fetchedAt }: Props) {
  const trimmed = (metarText ?? "").trim();
  const stamp = fetchedAt ? formatStamp(fetchedAt) : "";
  return (
    <div className={styles.metarStrip}>
      <div className={styles.metarHeader}>
        <span>METAR {icao || "----"}</span>
        {stamp && <span>· {stamp}</span>}
      </div>
      {trimmed ? (
        <div className={styles.metarText}>{trimmed}</div>
      ) : (
        <div className={styles.metarMissing}>No METAR available</div>
      )}
    </div>
  );
}

function formatStamp(iso: string): string {
  try {
    const d = new Date(iso);
    const h = String(d.getUTCHours()).padStart(2, "0");
    const m = String(d.getUTCMinutes()).padStart(2, "0");
    return `${h}:${m}Z`;
  } catch {
    return "";
  }
}
