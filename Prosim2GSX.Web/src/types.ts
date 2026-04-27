// Wire-contract types. Phase 7A keeps this minimal — full DTO type
// definitions land in 7B alongside the panels that consume them.

export type ConnectionStatus = "connecting" | "open" | "reconnecting" | "closed";

// Channels that the WebSocket envelope uses (and that AppState is keyed by,
// except "gsx" which patches into flightStatus.gsx).
export type WsChannel = "flightStatus" | "gsx" | "audio" | "appSettings";

// Top-level state-store keys (the four REST tabs).
export type StateChannel = "flightStatus" | "audio" | "gsxSettings" | "appSettings";

// Server → client envelope shapes (matches StateWebSocketHandler):
//   { channel, patch: { propertyName: value } }
//   { channel: "flightStatus", logAdded: "..." }
export interface PatchEnvelope {
  channel: WsChannel;
  patch: Record<string, unknown>;
}

export interface LogAddedEnvelope {
  channel: "flightStatus";
  logAdded: string;
}

export type WsEnvelope = PatchEnvelope | LogAddedEnvelope;
