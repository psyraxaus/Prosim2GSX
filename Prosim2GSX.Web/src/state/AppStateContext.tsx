import {
  Dispatch,
  ReactNode,
  createContext,
  useContext,
  useRef,
  useSyncExternalStore,
} from "react";
import {
  ConnectionStatus,
  StateChannel,
  WsChannel,
  FlightStatusDto,
  AudioDto,
  GsxSettingsDto,
  AppSettingsDto,
  OfpDto,
  ChecklistDto,
  WeightBalanceDto,
  LoadsheetSnapshotDto,
  EfbFlightPlanDto,
  NotificationsSnapshotDto,
  FuelDto,
  TakeoffPerfStateDto,
  LandingPerfStateDto,
} from "../types";

// Single source of truth for live state. WS deltas dispatch "patch"
// actions; REST initial loads dispatch "set" actions.
//
// Backed by an external store (useSyncExternalStore) so consumers subscribe to
// ONE channel via useChannel(...) and re-render only when that channel's
// reference changes — the reducer produces a fresh object solely for the
// patched channel, so an unrelated channel's tick never re-renders a panel.
// Channels are stored opaquely (the wire shape is a partial-merge target); the
// typed useChannel<K> hook is the single place the DTO cast lives.

// MessageLog mirror cap — matches the server's FlightStatusState ring
// buffer (set in Phase 1 to 500). Trimmed when WS log adds push past it.
const MESSAGE_LOG_CAP = 500;

export interface AppState {
  flightStatus: Record<string, unknown> | null;
  audio: Record<string, unknown> | null;
  gsxSettings: Record<string, unknown> | null;
  appSettings: Record<string, unknown> | null;
  ofp: Record<string, unknown> | null;
  checklists: Record<string, unknown> | null;
  weightBalance: Record<string, unknown> | null;
  loadsheet: Record<string, unknown> | null;
  efbFlightPlan: Record<string, unknown> | null;
  notifications: Record<string, unknown> | null;
  fuel: Record<string, unknown> | null;
  takeoffPerf: Record<string, unknown> | null;
  landingPerf: Record<string, unknown> | null;
  connection: ConnectionStatus;
}

// Channel → DTO map. useChannel<K> returns ChannelTypes[K] | null, so panels
// get a typed value with no per-call cast.
export interface ChannelTypes {
  flightStatus: FlightStatusDto;
  audio: AudioDto;
  gsxSettings: GsxSettingsDto;
  appSettings: AppSettingsDto;
  ofp: OfpDto;
  checklists: ChecklistDto;
  weightBalance: WeightBalanceDto;
  loadsheet: LoadsheetSnapshotDto;
  efbFlightPlan: EfbFlightPlanDto;
  notifications: NotificationsSnapshotDto;
  fuel: FuelDto;
  takeoffPerf: TakeoffPerfStateDto;
  landingPerf: LandingPerfStateDto;
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
  checklists: null,
  weightBalance: null,
  loadsheet: null,
  efbFlightPlan: null,
  notifications: null,
  fuel: null,
  takeoffPerf: null,
  landingPerf: null,
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

      // Same nesting as "gsx", but the deice HOT card is OfpDto's nested
      // DeiceHoldoverDto — it merges into ofp.deiceHoldover.
      if (action.channel === "deiceHoldover") {
        if (!state.ofp) return state;
        const currentHot =
          (state.ofp.deiceHoldover as Record<string, unknown> | undefined) ?? {};
        return {
          ...state,
          ofp: {
            ...state.ofp,
            deiceHoldover: { ...currentHot, ...action.patch },
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

// ── External store ──────────────────────────────────────────────────────────

interface Store {
  getState(): AppState;
  subscribe(listener: () => void): () => void;
  dispatch: Dispatch<AppAction>;
}

function createStore(): Store {
  let state = initialState;
  const listeners = new Set<() => void>();
  return {
    getState: () => state,
    subscribe(listener) {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    dispatch(action) {
      const next = reducer(state, action);
      // The reducer returns the same reference for no-op actions (e.g. a patch
      // on a still-null channel) — skip the notify so nothing re-renders.
      if (next !== state) {
        state = next;
        listeners.forEach((l) => l());
      }
    },
  };
}

const StoreContext = createContext<Store | null>(null);

export function AppStateProvider({ children }: { children: ReactNode }) {
  const storeRef = useRef<Store | null>(null);
  if (storeRef.current === null) storeRef.current = createStore();
  return (
    <StoreContext.Provider value={storeRef.current}>
      {children}
    </StoreContext.Provider>
  );
}

function useStore(): Store {
  const store = useContext(StoreContext);
  if (!store) {
    throw new Error("store hooks must be used inside <AppStateProvider>.");
  }
  return store;
}

// Subscribe to a single channel; re-renders only when that channel changes.
export function useChannel<K extends keyof ChannelTypes>(key: K): ChannelTypes[K] | null {
  const store = useStore();
  return useSyncExternalStore(
    store.subscribe,
    () => store.getState()[key] as ChannelTypes[K] | null,
  );
}

// Connection status (non-nullable channel).
export function useConnection(): ConnectionStatus {
  const store = useStore();
  return useSyncExternalStore(store.subscribe, () => store.getState().connection);
}

// Stable dispatch — never causes a re-render.
export function useDispatch(): Dispatch<AppAction> {
  return useStore().dispatch;
}
