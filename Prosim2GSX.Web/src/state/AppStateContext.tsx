import {
  Dispatch,
  ReactNode,
  createContext,
  useContext,
  useReducer,
} from "react";
import { ConnectionStatus, StateChannel, WsChannel } from "../types";

// Single source of truth for live state. WS deltas dispatch "patch"
// actions; REST initial loads dispatch "set" actions. Each top-level
// channel (flightStatus / audio / gsxSettings / appSettings) is opaque
// here — the panels that consume it know the DTO shapes. Phase 7B will
// strengthen the typing once the full DTO types are in.

// MessageLog mirror cap — matches the server's FlightStatusState ring
// buffer (set in Phase 1 to 500). Trimmed when WS log adds push past it.
const MESSAGE_LOG_CAP = 500;

export interface AppState {
  flightStatus: Record<string, unknown> | null;
  audio: Record<string, unknown> | null;
  gsxSettings: Record<string, unknown> | null;
  appSettings: Record<string, unknown> | null;
  ofp: Record<string, unknown> | null;
  connection: ConnectionStatus;
}

export type AppAction =
  | { type: "set"; channel: StateChannel; state: Record<string, unknown> }
  | { type: "patch"; channel: WsChannel; patch: Record<string, unknown> }
  | { type: "logAdded"; msg: string }
  | { type: "connection"; status: ConnectionStatus };

const initialState: AppState = {
  flightStatus: null,
  audio: null,
  gsxSettings: null,
  appSettings: null,
  ofp: null,
  connection: "closed",
};

function reducer(state: AppState, action: AppAction): AppState {
  switch (action.type) {
    case "set":
      return { ...state, [action.channel]: action.state };

    case "patch": {
      // The WS "gsx" channel patches into flightStatus.gsx (live runtime
      // is part of the FlightStatusDto's nested GsxLiveDto).
      if (action.channel === "gsx") {
        if (!state.flightStatus) return state;
        const currentGsx =
          (state.flightStatus.gsx as Record<string, unknown> | undefined) ?? {};
        return {
          ...state,
          flightStatus: {
            ...state.flightStatus,
            gsx: { ...currentGsx, ...action.patch },
          },
        };
      }

      const target = action.channel as keyof AppState;
      const current = state[target] as Record<string, unknown> | null;
      if (!current) return state;
      return { ...state, [target]: { ...current, ...action.patch } };
    }

    case "logAdded": {
      if (!state.flightStatus) return state;
      const log = (state.flightStatus.messageLog as string[] | undefined) ?? [];
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

interface ContextValue {
  state: AppState;
  dispatch: Dispatch<AppAction>;
}

const AppStateContext = createContext<ContextValue | null>(null);

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(reducer, initialState);
  return (
    <AppStateContext.Provider value={{ state, dispatch }}>
      {children}
    </AppStateContext.Provider>
  );
}

export function useAppState(): ContextValue {
  const ctx = useContext(AppStateContext);
  if (!ctx) {
    throw new Error("useAppState must be used inside <AppStateProvider>.");
  }
  return ctx;
}
