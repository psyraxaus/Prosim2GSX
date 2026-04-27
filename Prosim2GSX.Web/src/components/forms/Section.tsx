import { ReactNode } from "react";
import styles from "./Section.module.css";

interface Props {
  title: string;
  hint?: string;
  children: ReactNode;
}

// Card-style grouping for related fields. Mirrors the WPF
// "CategoryHeader + SectionCardBorder" pattern.
export function Section({ title, hint, children }: Props) {
  return (
    <section className={styles.section}>
      <div className={styles.header}>
        <h3 className={styles.title}>{title}</h3>
        {hint && <span className={styles.hint}>{hint}</span>}
      </div>
      <div className={styles.body}>{children}</div>
    </section>
  );
}
