# wwwroot

This directory holds the compiled React (Vite) bundle that the embedded
ASP.NET Core / Kestrel host serves at `http://<host>:<port>/`.

It is populated by the `Prosim2GSX.Web/` project's build step
(`npm run build`), which writes `index.html` plus an `assets/` folder
into here. The csproj's `<Content Include="wwwroot\**\*">` rule picks up
whatever lands here and copies it into the WPF app's bin directory at
build time.

When this directory is empty (no `index.html`), the Kestrel host
serves a 404 with a "run npm run build" hint for any non-API path —
the rest of the web layer (REST endpoints, WebSocket) keeps working.
