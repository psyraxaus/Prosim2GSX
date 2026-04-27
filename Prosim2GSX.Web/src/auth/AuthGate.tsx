import { FormEvent, useState } from "react";
import { storeToken } from "./auth";
import styles from "./AuthGate.module.css";

interface Props {
  onAuth: () => void;
}

// First-paint screen when no token is in localStorage. The user pastes
// the GUID shown in the WPF App Settings tab (or the URL is loaded with
// a #token=... hash, in which case the bootstrap in main.tsx fills in
// localStorage before React mounts and this gate is skipped).
export function AuthGate({ onAuth }: Props) {
  const [value, setValue] = useState("");

  function submit(e: FormEvent) {
    e.preventDefault();
    const trimmed = value.trim();
    if (!trimmed) return;
    storeToken(trimmed);
    onAuth();
  }

  return (
    <div className={styles.gate}>
      <form className={styles.card} onSubmit={submit}>
        <h1 className={styles.title}>Prosim2GSX</h1>
        <p className={styles.subtitle}>
          Paste the auth token from the desktop app's
          <br />
          <strong>App Settings → Web Interface</strong> panel
        </p>
        <input
          autoFocus
          className={styles.input}
          type="password"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder="Auth token"
          spellCheck={false}
          autoCapitalize="off"
          autoComplete="off"
        />
        <button type="submit" className={styles.button} disabled={!value.trim()}>
          Connect
        </button>
        <p className={styles.hint}>
          Or scan the QR code on the desktop app to skip this step.
        </p>
      </form>
    </div>
  );
}
