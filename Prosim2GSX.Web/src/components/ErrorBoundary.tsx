import { Component, ErrorInfo, ReactNode } from "react";
import styles from "./ErrorBoundary.module.css";

interface Props {
  children: ReactNode;
  /** Shown in the fallback so the user knows which area failed. */
  label?: string;
}

interface State {
  error: Error | null;
}

// Catches render / lifecycle exceptions in the subtree so one bad panel
// (e.g. a partial WS snapshot that violates a DTO-shape assumption, or an
// unguarded nested-field access) shows a contained fallback instead of
// blanking the entire UI — important for a headless sim-PC web client that
// must stay usable. The parent keys this boundary by the active tab, so
// switching tabs remounts it and clears a stuck error without a page reload.
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // No client-side logging backend; surface to the browser console.
    console.error("Panel render error", this.props.label ?? "", error, info.componentStack);
  }

  private handleRetry = (): void => this.setState({ error: null });

  render(): ReactNode {
    if (this.state.error) {
      return (
        <div className={styles.fallback} role="alert">
          <h2 className={styles.title}>Something went wrong</h2>
          <p className={styles.detail}>
            {this.props.label ? `The "${this.props.label}" view ` : "This view "}
            hit an error and couldn&rsquo;t render. Try switching tabs, or retry.
          </p>
          <pre className={styles.message}>{this.state.error.message}</pre>
          <button type="button" className={styles.retry} onClick={this.handleRetry}>
            Retry
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
