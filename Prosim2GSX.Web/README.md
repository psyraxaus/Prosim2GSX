# Prosim2GSX.Web

React + Vite + TypeScript frontend for the Prosim2GSX desktop app's
embedded web server (Phase 6 backend, Phase 7 frontend).

## Setup

```bash
cd Prosim2GSX.Web
npm install      # one-time; review package.json + package-lock.json first
```

## Develop

```bash
npm run dev
```

Vite dev server runs on `http://localhost:5173` with `/api` and `/ws`
proxied to `http://localhost:5001` (the embedded Kestrel host). The
desktop app must be running with the web server enabled.

## Build

```bash
npm run build
```

Output is written to `../Prosim2GSX/wwwroot/`. The WPF csproj's
`<Content Include="wwwroot\**\*">` rule then copies it into the
desktop app's bin directory at build time, so the embedded host
serves the React bundle automatically.

The WPF project's pre-build target also runs `npm run build` if
Node is available — but it does NOT run `npm install`. Run
`npm install` manually after auditing dependencies.

## Stack

- React 18 (function components + hooks)
- TypeScript (compile-time wire-contract validation)
- Vite (bundler + dev server)
- Plain CSS modules (one per component)
- No state library, no router (single AppStateContext + useReducer,
  useState-driven tab switching)
