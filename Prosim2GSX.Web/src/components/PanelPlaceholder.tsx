import styles from "./PanelPlaceholder.module.css";

interface Props {
  title: string;
}

// Stand-in for each tab while Phase 7B builds the real panels. Mounted
// as a child of the active tab so the AppShell layout (header + tab bar
// + scrollable main area) is exercised end-to-end before the panel
// content lands.
export function PanelPlaceholder({ title }: Props) {
  return (
    <section className={styles.panel}>
      <h2 className={styles.heading}>{title}</h2>
      <p className={styles.body}>
        Phase 7B will populate this panel. The auth gate, WebSocket
        connection, and state context are live — open the browser
        console to watch deltas stream in.
      </p>
    </section>
  );
}
