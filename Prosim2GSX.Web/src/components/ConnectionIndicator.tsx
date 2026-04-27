import { ConnectionStatus } from "../types";
import styles from "./ConnectionIndicator.module.css";

interface Props {
  status: ConnectionStatus;
}

const LABEL: Record<ConnectionStatus, string> = {
  connecting: "Connecting…",
  open: "Connected",
  reconnecting: "Reconnecting…",
  closed: "Disconnected",
};

// Coloured dot + label. Green when WS is open, amber while reconnecting,
// red when closed/disconnected, neutral while the initial connect attempt
// is in flight.
export function ConnectionIndicator({ status }: Props) {
  return (
    <div className={styles.indicator} title={LABEL[status]}>
      <span className={`${styles.dot} ${styles[status]}`} aria-hidden="true" />
      <span className={styles.label}>{LABEL[status]}</span>
    </div>
  );
}
