import type {
  LandingInputsDto,
  LandingPerfStateDto,
  TakeoffInputsDto,
  TakeoffPerfStateDto,
} from "../types";

// Thin typed wrappers over the /api/perf REST surface. Panels pull
// `{ get, post } = useApi()` and pass them into these helpers — the
// network plumbing (bearer token, 401 routing) stays in useApi, the
// typing of every endpoint stays here. Wire-shape matches
// Prosim2GSX/Web/Controllers/PerfController.cs and PerfDtos.cs.

type Api = {
  get: <T,>(path: string) => Promise<T>;
  post: <T,>(path: string, body?: unknown) => Promise<T>;
};

// ─────────────────────────────────────────────────────────────────
//  TAKEOFF
// ─────────────────────────────────────────────────────────────────

export function getTakeoff(api: Api): Promise<TakeoffPerfStateDto> {
  return api.get<TakeoffPerfStateDto>("/perf/takeoff");
}

export function postTakeoffInputs(
  api: Api,
  inputs: TakeoffInputsDto,
): Promise<TakeoffPerfStateDto> {
  return api.post<TakeoffPerfStateDto>("/perf/takeoff/inputs", inputs);
}

export function loadTakeoffRunways(
  api: Api,
  icao: string,
): Promise<TakeoffPerfStateDto> {
  return api.post<TakeoffPerfStateDto>(
    `/perf/takeoff/load-runways?icao=${encodeURIComponent(icao)}`,
  );
}

export function syncTakeoffLoadsheet(api: Api): Promise<TakeoffPerfStateDto> {
  return api.post<TakeoffPerfStateDto>("/perf/takeoff/sync-loadsheet");
}

export function calculateTakeoff(api: Api): Promise<TakeoffPerfStateDto> {
  return api.post<TakeoffPerfStateDto>("/perf/takeoff/calculate");
}

export function uplinkTakeoff(api: Api): Promise<TakeoffPerfStateDto> {
  return api.post<TakeoffPerfStateDto>("/perf/takeoff/uplink");
}

export function resetTakeoff(api: Api): Promise<TakeoffPerfStateDto> {
  return api.post<TakeoffPerfStateDto>("/perf/takeoff/reset");
}

// ─────────────────────────────────────────────────────────────────
//  LANDING
// ─────────────────────────────────────────────────────────────────

export function getLanding(api: Api): Promise<LandingPerfStateDto> {
  return api.get<LandingPerfStateDto>("/perf/landing");
}

export function postLandingInputs(
  api: Api,
  inputs: LandingInputsDto,
): Promise<LandingPerfStateDto> {
  return api.post<LandingPerfStateDto>("/perf/landing/inputs", inputs);
}

export function loadLandingRunways(
  api: Api,
  icao: string,
): Promise<LandingPerfStateDto> {
  return api.post<LandingPerfStateDto>(
    `/perf/landing/load-runways?icao=${encodeURIComponent(icao)}`,
  );
}

export function calculateLanding(api: Api): Promise<LandingPerfStateDto> {
  return api.post<LandingPerfStateDto>("/perf/landing/calculate");
}

export function resetLanding(api: Api): Promise<LandingPerfStateDto> {
  return api.post<LandingPerfStateDto>("/perf/landing/reset");
}
