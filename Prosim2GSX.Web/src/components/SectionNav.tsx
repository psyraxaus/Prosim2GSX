import styles from "./SectionNav.module.css";

export interface SectionItem<K extends string> {
  key: K;
  label: string;
}

interface Props<K extends string> {
  items: SectionItem<K>[];
  active: K;
  onSelect: (key: K) => void;
}

// Vertical left-rail navigation on desktop, horizontal pill bar at top on
// narrow viewports. Mirrors the WPF GSX Settings tab's left sidebar
// (ViewAutomation.xaml) without requiring fixed pixel widths in CSS — the
// rail width adapts to the longest label, the pill bar scrolls horizontally
// when items overflow.
export function SectionNav<K extends string>({ items, active, onSelect }: Props<K>) {
  return (
    <nav className={styles.nav} role="tablist" aria-orientation="vertical">
      {items.map((item) => (
        <button
          key={item.key}
          role="tab"
          aria-selected={item.key === active}
          className={`${styles.item} ${item.key === active ? styles.active : ""}`}
          onClick={() => onSelect(item.key)}
        >
          {item.label}
        </button>
      ))}
    </nav>
  );
}
