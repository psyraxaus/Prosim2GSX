import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useRef, useState } from "react";
import styles from "./SplitFlap.module.css";
// Mirrors Prosim2GSX/UI/Controls/SplitFlapCharacterControl.xaml.cs:
// same drum, same min-flip count, same deceleration zone, same per-tick
// squeeze-and-drop visual.
const DRUM = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-/";
const MIN_FLIPS = 5;
const FAST_TICK_MS = 30;
const SLOW_TICK_MS = 90;
const DECEL_ZONE = 3;
function SplitFlapChar({ target }) {
    const [display, setDisplay] = useState("-");
    const [tick, setTick] = useState(0);
    const drumRef = useRef(0);
    const timerRef = useRef(null);
    useEffect(() => {
        const ch = (target ?? " ").toUpperCase().charAt(0) || " ";
        const targetIdx = DRUM.indexOf(ch);
        if (timerRef.current !== null) {
            window.clearTimeout(timerRef.current);
            timerRef.current = null;
        }
        // Unknown character — snap directly with no animation.
        if (targetIdx < 0) {
            setDisplay(ch);
            return;
        }
        const curIdx = drumRef.current;
        if (DRUM.charAt(curIdx) === ch) {
            setDisplay(ch);
            return;
        }
        const forward = (targetIdx - curIdx + DRUM.length) % DRUM.length;
        let remaining = forward < MIN_FLIPS ? forward + DRUM.length : forward;
        let idx = curIdx;
        const step = () => {
            idx = (idx + 1) % DRUM.length;
            drumRef.current = idx;
            setDisplay(DRUM.charAt(idx));
            setTick((t) => t + 1);
            remaining--;
            if (remaining <= 0) {
                timerRef.current = null;
                return;
            }
            let nextMs = FAST_TICK_MS;
            if (remaining <= DECEL_ZONE) {
                const t = 1 - remaining / DECEL_ZONE;
                nextMs = FAST_TICK_MS + t * (SLOW_TICK_MS - FAST_TICK_MS);
            }
            timerRef.current = window.setTimeout(step, nextMs);
        };
        timerRef.current = window.setTimeout(step, FAST_TICK_MS);
        return () => {
            if (timerRef.current !== null) {
                window.clearTimeout(timerRef.current);
                timerRef.current = null;
            }
        };
    }, [target]);
    return (_jsxs("span", { className: styles.cell, children: [_jsx("span", { className: styles.seam }), _jsx("span", { className: styles.char, children: display }, tick)] }));
}
export function SplitFlap({ text, count, staggerDelayMs = 80, }) {
    const padded = (text ?? "").padEnd(count, " ").slice(0, count);
    const [staged, setStaged] = useState(() => Array(count).fill("-"));
    useEffect(() => {
        const timers = [];
        for (let i = 0; i < count; i++) {
            const targetChar = i < padded.length ? padded.charAt(i) : " ";
            const delay = i * staggerDelayMs;
            if (delay === 0) {
                setStaged((prev) => {
                    if (prev[i] === targetChar)
                        return prev;
                    const copy = prev.slice();
                    copy[i] = targetChar;
                    return copy;
                });
            }
            else {
                const handle = window.setTimeout(() => {
                    setStaged((prev) => {
                        if (prev[i] === targetChar)
                            return prev;
                        const copy = prev.slice();
                        copy[i] = targetChar;
                        return copy;
                    });
                }, delay);
                timers.push(handle);
            }
        }
        return () => {
            timers.forEach((t) => window.clearTimeout(t));
        };
        // padded is derived from text — listing text + count + stagger is enough.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [text, count, staggerDelayMs]);
    // Re-build cell array if count changes.
    useEffect(() => {
        setStaged((prev) => {
            if (prev.length === count)
                return prev;
            const next = Array(count).fill(" ");
            for (let i = 0; i < Math.min(prev.length, count); i++)
                next[i] = prev[i];
            return next;
        });
    }, [count]);
    return (_jsx("span", { className: styles.text, children: staged.map((ch, i) => (_jsx(SplitFlapChar, { target: ch }, i))) }));
}
