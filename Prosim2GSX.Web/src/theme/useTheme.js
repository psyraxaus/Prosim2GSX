import { useEffect } from "react";
import { useApi } from "../api/useApi";
import { applyTheme } from "./applyTheme";
// Fetches the named theme from `GET /api/appsettings/theme/<name>` and
// applies its colour tokens. Re-runs whenever the name changes (initial
// load, WS-driven cross-client sync, or in-app save).
//
// `themeName` may be undefined while the AppSettings snapshot is still
// loading on app boot — the hook no-ops in that case so the React UI
// briefly renders with the default tokens from src/styles/theme.css.
export function useTheme(themeName) {
    const { get } = useApi();
    useEffect(() => {
        if (!themeName)
            return;
        let cancelled = false;
        (async () => {
            try {
                const theme = await get(`/appsettings/theme/${encodeURIComponent(themeName)}`);
                if (!cancelled && theme && theme.colors)
                    applyTheme(theme.colors);
            }
            catch {
                // Theme fetch is best-effort. If it 404s or 500s the existing CSS
                // variables stay in place; the UI is still usable.
            }
        })();
        return () => {
            cancelled = true;
        };
    }, [themeName, get]);
}
