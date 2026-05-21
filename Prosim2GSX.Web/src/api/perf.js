// ─────────────────────────────────────────────────────────────────
//  TAKEOFF
// ─────────────────────────────────────────────────────────────────
export function getTakeoff(api) {
    return api.get("/perf/takeoff");
}
export function postTakeoffInputs(api, inputs) {
    return api.post("/perf/takeoff/inputs", inputs);
}
export function loadTakeoffRunways(api, icao) {
    return api.post(`/perf/takeoff/load-runways?icao=${encodeURIComponent(icao)}`);
}
export function syncTakeoffLoadsheet(api) {
    return api.post("/perf/takeoff/sync-loadsheet");
}
export function calculateTakeoff(api) {
    return api.post("/perf/takeoff/calculate");
}
export function uplinkTakeoff(api) {
    return api.post("/perf/takeoff/uplink");
}
export function resetTakeoff(api) {
    return api.post("/perf/takeoff/reset");
}
// ─────────────────────────────────────────────────────────────────
//  LANDING
// ─────────────────────────────────────────────────────────────────
export function getLanding(api) {
    return api.get("/perf/landing");
}
export function postLandingInputs(api, inputs) {
    return api.post("/perf/landing/inputs", inputs);
}
export function loadLandingRunways(api, icao) {
    return api.post(`/perf/landing/load-runways?icao=${encodeURIComponent(icao)}`);
}
export function calculateLanding(api) {
    return api.post("/perf/landing/calculate");
}
export function resetLanding(api) {
    return api.post("/perf/landing/reset");
}
