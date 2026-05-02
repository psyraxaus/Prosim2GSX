import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Build output goes into the WPF project's wwwroot so the csproj's
// <Content Include="wwwroot\**\*"> rule picks it up at build time.
// Dev server proxies /api and /ws to the embedded Kestrel host on :5001
// (CORS already allows localhost:5173 in DEBUG builds). Port moved from
// 5000 to 5001 because ProSim's own services occupy 5000 on the same host.
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "../Prosim2GSX/wwwroot",
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": {
        target: "http://localhost:5001",
        changeOrigin: true,
      },
      "/ws": {
        target: "ws://localhost:5001",
        ws: true,
        changeOrigin: true,
      },
    },
  },
});
