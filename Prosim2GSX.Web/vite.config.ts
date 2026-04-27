import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Build output goes into the WPF project's wwwroot so the csproj's
// <Content Include="wwwroot\**\*"> rule picks it up at build time.
// Dev server proxies /api and /ws to the embedded Kestrel host on :5000
// (CORS already allows localhost:5173 in DEBUG builds).
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
        target: "http://localhost:5000",
        changeOrigin: true,
      },
      "/ws": {
        target: "ws://localhost:5000",
        ws: true,
        changeOrigin: true,
      },
    },
  },
});
