import { useEffect, useState } from "react";
import { AUTH_FAIL_EVENT, getStoredToken } from "./auth/auth";
import { AuthGate } from "./auth/AuthGate";
import { AppStateProvider, useAppState } from "./state/AppStateContext";
import { useWebSocket } from "./ws/useWebSocket";
import { Header } from "./components/Header";
import { TabBar, TabKey } from "./components/TabBar";
import { FlightStatusPanel } from "./panels/FlightStatusPanel";
import { AudioSettingsPanel } from "./panels/AudioSettingsPanel";
import { AppSettingsPanel } from "./panels/AppSettingsPanel";
import { GsxSettingsPanel } from "./panels/GsxSettingsPanel";
import styles from "./App.module.css";

export function App() {
  // The auth gate is shown until a token is in localStorage. Re-checked
  // when the AUTH_FAIL_EVENT fires (any 401 from REST or 1008 close from
  // WS routes through there) so the user is bumped back here cleanly.
  const [authed, setAuthed] = useState(() => !!getStoredToken());

  useEffect(() => {
    function onFail() {
      setAuthed(false);
    }
    window.addEventListener(AUTH_FAIL_EVENT, onFail);
    return () => window.removeEventListener(AUTH_FAIL_EVENT, onFail);
  }, []);

  if (!authed) {
    return <AuthGate onAuth={() => setAuthed(true)} />;
  }

  return (
    <AppStateProvider>
      <AppShell />
    </AppStateProvider>
  );
}

function AppShell() {
  const { dispatch } = useAppState();
  useWebSocket(dispatch);

  const [tab, setTab] = useState<TabKey>("flightStatus");

  return (
    <div className={styles.app}>
      <Header />
      <TabBar active={tab} onSelect={setTab} />
      <main className={styles.main}>
        {tab === "flightStatus" && <FlightStatusPanel />}
        {tab === "audioSettings" && <AudioSettingsPanel />}
        {tab === "appSettings" && <AppSettingsPanel />}
        {tab === "gsxSettings" && <GsxSettingsPanel />}
      </main>
    </div>
  );
}
