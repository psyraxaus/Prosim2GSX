import { useAppState } from "../state/AppStateContext";
import { ConnectionIndicator } from "./ConnectionIndicator";
import styles from "./Header.module.css";

export function Header() {
  const { state } = useAppState();
  return (
    <header className={styles.header}>
      <div className={styles.brand}>
        <span className={styles.brandName}>PROSIM2GSX</span>
        <span className={styles.brandTag}>WEB</span>
      </div>
      <ConnectionIndicator status={state.connection} />
    </header>
  );
}
