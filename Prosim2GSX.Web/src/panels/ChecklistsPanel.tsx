import { useEffect, useMemo, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import {
  ChecklistDto,
  ChecklistItemDto,
  ChecklistSectionDto,
  ResetSectionRequest,
  SelectChecklistRequest,
  SelectSectionRequest,
  ToggleItemRequest,
} from "../types";
import styles from "./ChecklistsPanel.module.css";

// ECAM-style Checklists tab. Mirrors the WPF ModelChecklist surface:
// dropdown for the checklist set (saved to AircraftProfile.ChecklistName),
// dropdown for the current section, ECAM-style item list with green
// checked / cyan pending / current-item border highlight, RESET / C/L
// MENU / MSG LIST / C/L COMPLETE footer buttons.
//
// All state mutations go through POST /api/checklists/* endpoints; the
// server fans out the resulting whole-snapshot via WS so multiple
// clients (e.g. WPF + browser) stay in lockstep.
export function ChecklistsPanel() {
  const { get, post } = useApi();
  const { state, dispatch } = useAppState();
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);
    try {
      const dto = await get<ChecklistDto>("/checklists");
      dispatch({ type: "set", channel: "checklists", state: dto as unknown as Record<string, unknown> });
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const dto = state.checklists as unknown as ChecklistDto | null;

  const currentSection = useMemo<ChecklistSectionDto | null>(() => {
    if (!dto?.sections) return null;
    if (dto.currentSectionIndex < 0 || dto.currentSectionIndex >= dto.sections.length) return null;
    return dto.sections[dto.currentSectionIndex];
  }, [dto]);

  async function selectChecklist(name: string) {
    if (!name || name === dto?.currentChecklistName) return;
    setError(null);
    try {
      const body: SelectChecklistRequest = { name };
      await post<{ snapshot: ChecklistDto }>("/checklists/select", body);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to select checklist");
    }
  }

  async function selectSection(idx: number) {
    if (idx === dto?.currentSectionIndex) return;
    setError(null);
    try {
      const body: SelectSectionRequest = { sectionIndex: idx };
      await post<{ snapshot: ChecklistDto }>("/checklists/select-section", body);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to change section");
    }
  }

  async function toggleItem(itemIndex: number) {
    if (dto == null) return;
    setError(null);
    try {
      const body: ToggleItemRequest = {
        sectionIndex: dto.currentSectionIndex,
        itemIndex,
      };
      await post<{ snapshot: ChecklistDto }>("/checklists/toggle", body);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to toggle item");
    }
  }

  async function resetSection() {
    if (dto == null) return;
    setError(null);
    try {
      const body: ResetSectionRequest = { sectionIndex: dto.currentSectionIndex };
      await post<{ snapshot: ChecklistDto }>("/checklists/reset-section", body);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to reset section");
    }
  }

  async function complete() {
    setError(null);
    try {
      await post<{ snapshot: ChecklistDto }>("/checklists/complete", {});
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to complete section");
    }
  }

  if (!dto) {
    return (
      <div className={styles.panel}>
        <div className={styles.error}>{error ? `Error: ${error}` : "Loading checklist…"}</div>
      </div>
    );
  }

  return (
    <div className={styles.panel}>
      <div className={styles.headerBar}>
        <div className={styles.row}>
          <span className={styles.label}>C/L</span>
          <select
            className={styles.select}
            value={dto.currentChecklistName}
            onChange={(e) => selectChecklist(e.target.value)}
          >
            {dto.availableChecklists.length === 0 && <option value="">(none available)</option>}
            {dto.availableChecklists.map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
        </div>
        <div className={styles.row}>
          <select
            className={styles.select}
            value={dto.currentSectionIndex}
            onChange={(e) => selectSection(parseInt(e.target.value, 10))}
          >
            {dto.sections.map((s, i) => (
              <option key={i} value={i}>
                {s.title}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className={styles.body}>
        {currentSection == null && <div className={styles.error}>No section loaded.</div>}
        {currentSection?.items?.map((it, i) => (
          <ChecklistRow
            key={i}
            item={it}
            isCurrent={i === dto.currentItemIndex}
            onClick={() => toggleItem(i)}
          />
        ))}
      </div>

      {error && <div className={styles.error}>{error}</div>}

      <div className={styles.footer}>
        <button type="button" className={styles.button} onClick={resetSection}>
          RESET
        </button>
        <button
          type="button"
          className={styles.button}
          onClick={() => {
            const sel = document.querySelector<HTMLSelectElement>(`.${styles.headerBar} select:nth-of-type(1)`);
            sel?.focus();
          }}
        >
          C/L MENU
        </button>
        <button type="button" className={styles.button} disabled>
          MSG LIST
        </button>
        <button type="button" className={styles.button} onClick={complete}>
          C/L COMPLETE
        </button>
      </div>
    </div>
  );
}

interface RowProps {
  item: ChecklistItemDto;
  isCurrent: boolean;
  onClick: () => void;
}

function ChecklistRow({ item, isCurrent, onClick }: RowProps) {
  if (item.isSeparator) {
    return <hr className={styles.separator} />;
  }
  if (item.isNote) {
    return <div className={styles.note}>{item.label}</div>;
  }
  if (item.isChecked) {
    return (
      <div className={`${styles.itemRow} ${styles.checked}`}>
        <span className={styles.itemCheck}>✓</span>
        <span>{item.label}</span>
        <span className={styles.itemDots}>....................................................................................................</span>
        <span>{item.value}</span>
      </div>
    );
  }
  if (isCurrent) {
    return (
      <div
        className={`${styles.itemRow} ${styles.pending} ${styles.current} ${item.isManual ? styles.clickable : ""}`}
        onClick={item.isManual ? onClick : undefined}
        role={item.isManual ? "button" : undefined}
      >
        <span className={styles.itemCheck}>□</span>
        <span>{item.label}</span>
        <span className={styles.itemDots}>....................................................................................................</span>
        <span>{item.value}</span>
      </div>
    );
  }
  return (
    <div className={`${styles.itemRow} ${styles.pending}`}>
      <span></span>
      <span>{item.label}</span>
      <span className={styles.itemDots}>....................................................................................................</span>
      <span>{item.value}</span>
    </div>
  );
}
