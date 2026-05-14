import type { RunwayDto } from "../../types";
import styles from "./PerfShared.module.css";

// EFB-style runway + intersection picker. The intersection selector
// is only rendered when `withIntersections` is true (landing tab
// hides it — the EFB doesn't offer intersection landings) AND the
// selected runway has at least one intersection.
//
// "Full length" is encoded as `intersectionName === ""` to match the
// state-store convention.

interface Props {
  runways: RunwayDto[];
  runwayId: string;
  intersectionName?: string;
  withIntersections?: boolean;
  onRunwayChange: (runwayId: string) => void;
  onIntersectionChange?: (name: string) => void;
  disabled?: boolean;
}

export function RunwayDropdown({
  runways,
  runwayId,
  intersectionName = "",
  withIntersections = false,
  onRunwayChange,
  onIntersectionChange,
  disabled,
}: Props) {
  const current = runways.find((r) => r.runwayId === runwayId);
  const hasIntersections = withIntersections && (current?.intersections.length ?? 0) > 0;

  return (
    <div className={styles.runwayDropdown}>
      <select
        value={runwayId}
        disabled={disabled || runways.length === 0}
        onChange={(e) => onRunwayChange(e.target.value)}
        aria-label="Runway"
      >
        {runways.length === 0 && <option value="">—</option>}
        {runways.map((r) => (
          <option key={r.runwayId} value={r.runwayId}>
            RWY {r.runwayId}
            {r.lengthFt > 0 ? ` · ${r.lengthFt} ft` : ""}
          </option>
        ))}
      </select>

      {hasIntersections && (
        <select
          value={intersectionName}
          disabled={disabled}
          onChange={(e) => onIntersectionChange?.(e.target.value)}
          aria-label="Intersection"
        >
          <option value="">Full length</option>
          {current?.intersections.map((i) => (
            <option key={i.name} value={i.name}>
              {i.name} · {i.toraFt} ft
            </option>
          ))}
        </select>
      )}
    </div>
  );
}
