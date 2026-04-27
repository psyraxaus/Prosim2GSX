import { jsx as _jsx } from "react/jsx-runtime";
import { createContext, useContext, useReducer, } from "react";
// Single source of truth for live state. WS deltas dispatch "patch"
// actions; REST initial loads dispatch "set" actions. Each top-level
// channel (flightStatus / audio / gsxSettings / appSettings) is opaque
// here — the panels that consume it know the DTO shapes. Phase 7B will
// strengthen the typing once the full DTO types are in.
// MessageLog mirror cap — matches the server's FlightStatusState ring
// buffer (set in Phase 1 to 500). Trimmed when WS log adds push past it.
const MESSAGE_LOG_CAP = 500;
const initialState = {
    flightStatus: null,
    audio: null,
    gsxSettings: null,
    appSettings: null,
    ofp: null,
    connection: "closed",
};
function reducer(state, action) {
    switch (action.type) {
        case "set":
            return { ...state, [action.channel]: action.state };
        case "patch": {
            // The WS "gsx" channel patches into flightStatus.gsx (live runtime
            // is part of the FlightStatusDto's nested GsxLiveDto).
            if (action.channel === "gsx") {
                if (!state.flightStatus)
                    return state;
                const currentGsx = state.flightStatus.gsx ?? {};
                return {
                    ...state,
                    flightStatus: {
                        ...state.flightStatus,
                        gsx: { ...currentGsx, ...action.patch },
                    },
                };
            }
            const target = action.channel;
            const current = state[target];
            if (!current)
                return state;
            return { ...state, [target]: { ...current, ...action.patch } };
        }
        case "logAdded": {
            if (!state.flightStatus)
                return state;
            const log = state.flightStatus.messageLog ?? [];
            const next = [...log, action.msg];
            if (next.length > MESSAGE_LOG_CAP) {
                next.splice(0, next.length - MESSAGE_LOG_CAP);
            }
            return {
                ...state,
                flightStatus: { ...state.flightStatus, messageLog: next },
            };
        }
        case "connection":
            return { ...state, connection: action.status };
        default:
            return state;
    }
}
const AppStateContext = createContext(null);
export function AppStateProvider({ children }) {
    const [state, dispatch] = useReducer(reducer, initialState);
    return (_jsx(AppStateContext.Provider, { value: { state, dispatch }, children: children }));
}
export function useAppState() {
    const ctx = useContext(AppStateContext);
    if (!ctx) {
        throw new Error("useAppState must be used inside <AppStateProvider>.");
    }
    return ctx;
}
