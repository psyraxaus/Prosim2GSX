import { useCallback } from "react";
import { getStoredToken, signalAuthFailure } from "../auth/auth";

// Typed REST client. Attaches the bearer token from localStorage on every
// request, and on 401 wipes the token + raises the AUTH_FAIL_EVENT so the
// AppShell can reload back to the auth gate. Same instance can be used
// across components — no per-component caching, the network layer below
// (browser fetch + ASP.NET MVC) handles its own concerns.

class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function call<T>(method: "GET" | "POST", path: string, body?: unknown): Promise<T> {
  const token = getStoredToken();
  const headers: Record<string, string> = {};
  if (token) headers["Authorization"] = `Bearer ${token}`;
  if (body !== undefined) headers["Content-Type"] = "application/json";

  const res = await fetch(`/api${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401) {
    signalAuthFailure();
    throw new ApiError(401, "Unauthorised");
  }
  if (!res.ok) {
    let detail = "";
    try {
      detail = await res.text();
    } catch {
      /* ignore */
    }
    throw new ApiError(res.status, detail || `HTTP ${res.status}`);
  }

  // 204 No Content
  if (res.status === 204) return undefined as T;

  // Some endpoints (e.g. /themes) return a plain JSON array — no special handling.
  return (await res.json()) as T;
}

export function useApi() {
  const get = useCallback(<T,>(path: string) => call<T>("GET", path), []);
  const post = useCallback(<T,>(path: string, body?: unknown) => call<T>("POST", path, body), []);
  return { get, post };
}

export { ApiError };
