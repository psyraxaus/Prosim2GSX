import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import {
  CloneProfileRequest,
  DeleteProfileRequest,
  PROFILE_MATCH_TYPE_OPTIONS,
  ProfileMatchType,
  ProfileSummaryDto,
  ProfilesListDto,
  RenameProfileRequest,
  SetActiveProfileRequest,
  UpdateProfileMetadataRequest,
} from "../types";
import styles from "./AircraftProfilesPanel.module.css";

// CRUD UI for Config.AircraftProfiles. Mirrors the WPF Profiles tab:
// "Current Aircraft" context section above + per-profile cards. Uses
// the Phase 8.0b CommandRegistry-backed REST endpoints; each command's
// response carries the full updated list, so the panel re-renders from
// that without a separate refetch round-trip.
//
// Editing is per-card: clicking Edit replaces the read-only fields with
// inputs + Save / Cancel. On Save, if the Name changed the panel
// dispatches profiles.rename first, then profiles.updateMetadata for any
// MatchType / MatchString diff. The default profile is editable only by
// MatchString — its Name is locked (Config fallback path references it
// by string) and MatchType must stay Default (server enforces both).
export function AircraftProfilesPanel() {
  const { get, post } = useApi();
  const [list, setList] = useState<ProfilesListDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  // Per-card edit draft. Single editing card at a time keeps state simple.
  const [editingName, setEditingName] = useState<string | null>(null);
  const [draftName, setDraftName] = useState("");
  const [draftMatchType, setDraftMatchType] = useState<ProfileMatchType>("Default");
  const [draftMatchString, setDraftMatchString] = useState("");

  async function reload() {
    setError(null);
    try {
      const dto = await get<ProfilesListDto>("/profiles");
      setList(dto);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }
  useEffect(() => { reload(); }, []);

  function flashInfo(message: string) {
    setInfo(message);
    setTimeout(() => setInfo(null), 1500);
  }

  function startEdit(p: ProfileSummaryDto) {
    setEditingName(p.name);
    setDraftName(p.name);
    setDraftMatchType(p.matchType);
    setDraftMatchString(p.matchString);
    setError(null);
    setInfo(null);
  }

  function cancelEdit() {
    setEditingName(null);
    setError(null);
  }

  async function saveEdit(p: ProfileSummaryDto) {
    setBusy(true);
    setError(null);
    setInfo(null);
    try {
      let working = p.name;
      const trimmedName = draftName.trim();

      // Rename first (if changed and non-empty) so the metadata update can
      // address the new name. Server enforces guards (default rename
      // forbidden, duplicates rejected) and surfaces them as 400s.
      if (trimmedName && trimmedName !== p.name) {
        const renamed = await post<ProfilesListDto>("/profiles/rename", {
          oldName: p.name, newName: trimmedName,
        } satisfies RenameProfileRequest);
        setList(renamed);
        working = trimmedName;
      }

      // Update metadata if anything else changed.
      if (draftMatchType !== p.matchType || (draftMatchString ?? "") !== (p.matchString ?? "")) {
        const updated = await post<ProfilesListDto>("/profiles/update-metadata", {
          name: working, matchType: draftMatchType, matchString: draftMatchString,
        } satisfies UpdateProfileMetadataRequest);
        setList(updated);
      }

      setEditingName(null);
      flashInfo("Saved.");
    } catch (e: unknown) {
      setError((e as Error).message ?? "Save failed");
    } finally {
      setBusy(false);
    }
  }

  async function setActive(p: ProfileSummaryDto) {
    if (p.isActive) return;
    setBusy(true); setError(null); setInfo(null);
    try {
      const dto = await post<ProfilesListDto>("/profiles/set-active", {
        name: p.name,
      } satisfies SetActiveProfileRequest);
      setList(dto);
      flashInfo(`Active profile: ${p.name}`);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Set active failed");
    } finally {
      setBusy(false);
    }
  }

  async function cloneProfile(p: ProfileSummaryDto) {
    const suggested = `Clone of ${p.name}`;
    const newName = window.prompt(`Clone "${p.name}" as:`, suggested);
    if (!newName?.trim()) return;
    setBusy(true); setError(null); setInfo(null);
    try {
      const dto = await post<ProfilesListDto>("/profiles/clone", {
        sourceName: p.name, newName: newName.trim(),
      } satisfies CloneProfileRequest);
      setList(dto);
      flashInfo(`Cloned to "${newName.trim()}".`);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Clone failed");
    } finally {
      setBusy(false);
    }
  }

  async function deleteProfile(p: ProfileSummaryDto) {
    if (!window.confirm(`Delete profile "${p.name}"? This cannot be undone.`)) return;
    setBusy(true); setError(null); setInfo(null);
    try {
      const dto = await post<ProfilesListDto>("/profiles/delete", {
        name: p.name,
      } satisfies DeleteProfileRequest);
      setList(dto);
      flashInfo(`Deleted "${p.name}".`);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Delete failed");
    } finally {
      setBusy(false);
    }
  }

  if (!list) {
    return (
      <div className={styles.loading}>
        {error ? `Error: ${error}` : "Loading aircraft profiles…"}
      </div>
    );
  }

  const hasAircraftInfo =
    !!list.currentAirline || !!list.currentTitle;

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        <span className={styles.activeName}>
          Active: <strong>{list.activeName || "—"}</strong>
        </span>
        <div className={styles.toolbarStatus}>
          {error && <span className={styles.error}>{error}</span>}
          {info && <span className={styles.info}>{info}</span>}
        </div>
      </div>

      <Section title="Current Aircraft" hint={hasAircraftInfo ? "" : "Sim not connected — profile matching will use defaults"}>
        <div className={styles.kv}>
          <span className={styles.kvLabel}>Airline</span>
          <span className={styles.kvValue}>{list.currentAirline || "—"}</span>
        </div>
        <div className={styles.kv}>
          <span className={styles.kvLabel}>Title / Livery</span>
          <span className={styles.kvValue}>{list.currentTitle || "—"}</span>
        </div>
        <div className={styles.kv}>
          <span className={styles.kvLabel}>Active Profile</span>
          <span className={styles.kvValue}><strong>{list.activeName || "—"}</strong></span>
        </div>
      </Section>

      <Section title="Profiles" hint="Profiles are matched in this order: Title → Airline → Default fallback">
        {list.profiles.length === 0 && (
          <div className={styles.empty}>No profiles configured.</div>
        )}

        <div className={styles.profileList}>
          {list.profiles.map((p) => {
            const isEditing = editingName === p.name;
            // Default profile: name is locked and MatchType must stay Default
            // (server-enforced). Match string is still editable.
            const nameLocked = p.isDefault;
            const typeLocked = p.isDefault;

            return (
              <div
                key={p.name}
                className={`${styles.card} ${p.isActive ? styles.cardActive : ""}`}
              >
                <div className={styles.cardHeader}>
                  {isEditing ? (
                    <input
                      type="text"
                      value={draftName}
                      onChange={(e) => setDraftName(e.target.value)}
                      disabled={nameLocked || busy}
                      className={styles.nameInput}
                      placeholder="Profile name"
                      spellCheck={false}
                    />
                  ) : (
                    <span className={styles.name}>{p.name}</span>
                  )}
                  <div className={styles.badges}>
                    {p.isActive && <span className={`${styles.badge} ${styles.badgeActive}`}>ACTIVE</span>}
                    {p.isDefault && <span className={`${styles.badge} ${styles.badgeDefault}`}>DEFAULT</span>}
                  </div>
                </div>

                <div className={styles.cardBody}>
                  <div className={styles.field}>
                    <label className={styles.fieldLabel}>Match Type</label>
                    {isEditing ? (
                      <select
                        value={draftMatchType}
                        onChange={(e) => setDraftMatchType(e.target.value as ProfileMatchType)}
                        disabled={typeLocked || busy}
                        className={styles.fieldInput}
                      >
                        {PROFILE_MATCH_TYPE_OPTIONS.map((o) => (
                          <option key={o.value} value={o.value}>{o.label}</option>
                        ))}
                      </select>
                    ) : (
                      <span className={styles.fieldValue}>{p.matchType}</span>
                    )}
                  </div>

                  <div className={styles.field}>
                    <label className={styles.fieldLabel}>Match String</label>
                    {isEditing ? (
                      <input
                        type="text"
                        value={draftMatchString}
                        onChange={(e) => setDraftMatchString(e.target.value)}
                        disabled={busy}
                        className={styles.fieldInput}
                        placeholder="ICAO airline / aircraft title"
                        spellCheck={false}
                      />
                    ) : (
                      <span className={styles.fieldValueMono}>{p.matchString || "—"}</span>
                    )}
                  </div>
                </div>

                <div className={styles.cardActions}>
                  {isEditing ? (
                    <>
                      <PrimaryButton onClick={() => saveEdit(p)} disabled={busy}>
                        {busy ? "Saving…" : "Save"}
                      </PrimaryButton>
                      <PrimaryButton onClick={cancelEdit} variant="secondary" disabled={busy}>
                        Cancel
                      </PrimaryButton>
                    </>
                  ) : (
                    <>
                      {!p.isActive && (
                        <PrimaryButton onClick={() => setActive(p)} disabled={busy}>
                          Set Active
                        </PrimaryButton>
                      )}
                      <PrimaryButton onClick={() => startEdit(p)} variant="secondary" disabled={busy}>
                        Edit
                      </PrimaryButton>
                      <PrimaryButton onClick={() => cloneProfile(p)} variant="secondary" disabled={busy}>
                        Clone
                      </PrimaryButton>
                      {!p.isDefault && !p.isActive && (
                        <PrimaryButton onClick={() => deleteProfile(p)} variant="danger" disabled={busy}>
                          Delete
                        </PrimaryButton>
                      )}
                    </>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </Section>
    </div>
  );
}
