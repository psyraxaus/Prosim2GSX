// Token storage + URL-hash bootstrap. Tokens live in localStorage so they
// survive reloads. The WPF QR code encodes the token as a URL hash fragment
// (#token=...) — hash fragments are NEVER sent to the server, never appear
// in access logs, and we strip them from the address bar via replaceState
// immediately after reading so the browser-history entry is clean.

const TOKEN_KEY = "prosim2gsx.auth.token";

export function getStoredToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function storeToken(token: string): void {
  try {
    localStorage.setItem(TOKEN_KEY, token);
  } catch {
    /* localStorage disabled — fall back to in-memory (lost on reload) */
  }
}

export function clearStoredToken(): void {
  try {
    localStorage.removeItem(TOKEN_KEY);
  } catch {
    /* ignore */
  }
}

function readTokenFromHash(): string | null {
  const hash = window.location.hash;
  if (!hash) return null;
  // Tolerate #token=... or #foo=bar&token=...
  const match = hash.match(/[#&]token=([^&]+)/);
  return match ? decodeURIComponent(match[1]) : null;
}

function clearHashFromUrl(): void {
  if (window.location.hash) {
    history.replaceState(null, "", window.location.pathname + window.location.search);
  }
}

// Called once from main.tsx before React mounts. If the URL carried a
// token in the hash fragment (e.g. user just scanned the QR code on
// their phone), persist it and scrub the URL.
export function bootstrapAuth(): void {
  const fromHash = readTokenFromHash();
  if (fromHash) {
    storeToken(fromHash);
    clearHashFromUrl();
  }
}

// Custom event raised whenever a request returns 401. AppShell listens
// and reloads so the auth gate re-prompts. Decoupled this way so any
// hook that talks to the server can signal auth failure without taking
// a dependency on the React state context.
export const AUTH_FAIL_EVENT = "prosim:authfail";

export function signalAuthFailure(): void {
  clearStoredToken();
  window.dispatchEvent(new CustomEvent(AUTH_FAIL_EVENT));
}
