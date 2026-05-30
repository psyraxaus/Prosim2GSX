import { useChannel, useConnection } from "../state/AppStateContext";
import { ConnectionIndicator } from "./ConnectionIndicator";
import { SplitFlap } from "./SplitFlap";
import styles from "./Header.module.css";

export function Header() {
  const fs = useChannel("flightStatus");
  const connection = useConnection();

  const flightNumber = fs?.flightNumber ?? "--------";
  const utcTime = fs?.utcTime ?? "--:--Z";
  const utcDate = fs?.utcDate ?? "------";

  return (
    <header className={styles.header}>
      <div className={styles.brand}>
        <span className={styles.brandName}>PROSIM2GSX</span>
        <span className={styles.brandTag}>WEB</span>
      </div>

      <div className={styles.flapCenter}>
        <SplitFlap text={flightNumber} count={8} staggerDelayMs={80} />
        <span className={styles.flapLabel}>FLT NO</span>
      </div>

      <div className={styles.flapRight}>
        <div className={styles.flapBlock}>
          <SplitFlap text={utcTime} count={6} staggerDelayMs={80} />
          <span className={styles.flapLabel}>UTC</span>
        </div>
        <div className={styles.flapBlock}>
          <SplitFlap text={utcDate} count={6} staggerDelayMs={80} />
          <span className={styles.flapLabel}>DATE</span>
        </div>
        <ConnectionIndicator status={connection} />
      </div>
    </header>
  );
}
