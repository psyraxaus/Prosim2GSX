import { jsx as _jsx } from "react/jsx-runtime";
import { useCallback, useLayoutEffect, useRef, useState } from "react";
import styles from "./FitToViewport.module.css";
// Uniformly scales its content down so it always fits the available
// space with no scrolling — the EFB "fit to screen" behaviour. Never
// scales above 1, so desktop layouts are untouched. Used for the
// Takeoff/Landing perf tabs, whose fixed EFB layout otherwise overflows
// an iPad viewport and forces a scroll.
export function FitToViewport({ children }) {
    const outerRef = useRef(null);
    const contentRef = useRef(null);
    const [scale, setScale] = useState(1);
    const recompute = useCallback(() => {
        const outer = outerRef.current;
        const content = contentRef.current;
        if (!outer || !content)
            return;
        const availW = outer.clientWidth;
        const availH = outer.clientHeight;
        // offsetWidth/Height are the pre-transform layout size, so measuring
        // them is unaffected by the scale we apply — no feedback loop with
        // the ResizeObserver below.
        const natW = content.offsetWidth;
        const natH = content.offsetHeight;
        if (!availW || !availH || !natW || !natH)
            return;
        const next = Math.min(1, availW / natW, availH / natH);
        setScale((prev) => (Math.abs(prev - next) > 0.002 ? next : prev));
    }, []);
    useLayoutEffect(() => {
        recompute();
        const outer = outerRef.current;
        const content = contentRef.current;
        if (!outer || !content)
            return;
        // Observe the slot (viewport / orientation changes) and the content
        // (a banner appearing changes its natural height).
        const ro = new ResizeObserver(recompute);
        ro.observe(outer);
        ro.observe(content);
        window.addEventListener("orientationchange", recompute);
        return () => {
            ro.disconnect();
            window.removeEventListener("orientationchange", recompute);
        };
    }, [recompute]);
    return (_jsx("div", { ref: outerRef, className: styles.outer, children: _jsx("div", { ref: contentRef, className: styles.content, style: { transform: `scale(${scale})` }, children: children }) }));
}
