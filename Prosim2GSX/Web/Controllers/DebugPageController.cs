using Microsoft.AspNetCore.Mvc;

namespace Prosim2GSX.Web.Controllers
{
    // Self-contained HTML page mirroring the WPF Debug tab. Served at /debug
    // (outside /api/* so BearerTokenMiddleware does not gate the page itself).
    // The actual data fetch is /api/debug — that IS bearer-gated, so the page
    // reads the token from the URL fragment (#token=...) the same way the
    // SPA does and forwards it on the fetch.
    //
    // No JS bundle, no external assets — the entire UI is inlined so the page
    // works regardless of whether the React build has been deployed to wwwroot.
    // Styled to match the SPA palette (theme.css :root variables) for visual
    // continuity.
    [ApiController]
    [Route("debug")]
    public class DebugPageController : ControllerBase
    {
        private readonly AppService _app;

        public DebugPageController(AppService app) => _app = app;

        [HttpGet]
        public IActionResult Get()
        {
            if (_app?.Config?.ShowDebugTab != true)
                return NotFound();

            int refreshMs = _app.Config?.DebugRefreshMs ?? 500;
            return Content(BuildHtml(refreshMs), "text/html; charset=utf-8");
        }

        private static string BuildHtml(int refreshMs)
        {
            // Embedded HTML kept as a single verbatim string so the only
            // server-side substitution is the refresh interval. The braces in
            // CSS/JS are doubled for string.Format semantics.
            const string template = @"<!doctype html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<meta name=""theme-color"" content=""#1a2035"" />
<title>Prosim2GSX — Debug</title>
<style>
:root {{
  --bg-primary: #1a2035;
  --bg-card: #232a47;
  --border: #2a3357;
  --text-primary: #ffffff;
  --text-secondary: #a8b0c8;
  --text-muted: #6b7390;
  --accent: #2196f3;
  --danger: #f44336;
  --font-mono: ""Consolas"", ""Courier New"", ""Liberation Mono"", monospace;
  --font-sans: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
}}
* {{ box-sizing: border-box; }}
html, body {{ margin: 0; padding: 0; background: var(--bg-primary); color: var(--text-primary); font-family: var(--font-sans); }}
header {{ padding: 12px 20px; border-bottom: 1px solid var(--border); display: flex; align-items: center; justify-content: space-between; }}
header h1 {{ font-size: 16px; font-weight: 600; margin: 0; letter-spacing: 0.5px; }}
header .meta {{ font-size: 12px; color: var(--text-muted); }}
header .status {{ font-size: 12px; font-family: var(--font-mono); }}
header .status.ok {{ color: var(--accent); }}
header .status.err {{ color: var(--danger); }}
main {{ padding: 16px 20px; max-width: 1100px; margin: 0 auto; }}
.group {{ margin-bottom: 18px; }}
.group h2 {{ font-size: 13px; font-weight: 600; color: var(--text-secondary); letter-spacing: 1px; text-transform: uppercase; margin: 0 0 6px 0; }}
.group .card {{ background: var(--bg-card); border: 1px solid var(--border); border-radius: 6px; padding: 10px 14px; }}
table {{ width: 100%; border-collapse: collapse; font-family: var(--font-mono); font-size: 13px; }}
td {{ padding: 3px 6px; vertical-align: top; }}
td.k {{ color: var(--text-secondary); width: 320px; word-break: break-word; }}
td.v {{ color: var(--text-primary); font-weight: 600; word-break: break-all; }}
.banner {{ background: var(--bg-card); border: 1px solid var(--danger); border-radius: 6px; padding: 14px 18px; margin: 16px 0; color: var(--text-primary); }}
.banner code {{ background: rgba(0,0,0,0.3); padding: 1px 6px; border-radius: 3px; font-family: var(--font-mono); }}
</style>
</head>
<body>
<header>
  <h1>PROSIM2GSX — DEBUG</h1>
  <div class=""meta"">refresh <span id=""interval""></span> ms · <span id=""status"" class=""status"">connecting…</span></div>
</header>
<main id=""root"">
  <div class=""banner"" id=""initBanner"" style=""display:none""></div>
</main>
<script>
(function() {{
  var REFRESH_MS = {0};
  var TOKEN_KEY = ""prosim2gsx.auth.token"";

  document.getElementById(""interval"").textContent = REFRESH_MS;

  function readTokenFromHash() {{
    var h = window.location.hash;
    if (!h) return null;
    var m = h.match(/[#&]token=([^&]+)/);
    return m ? decodeURIComponent(m[1]) : null;
  }}

  function getToken() {{
    var fromHash = readTokenFromHash();
    if (fromHash) {{
      try {{ localStorage.setItem(TOKEN_KEY, fromHash); }} catch (e) {{}}
      history.replaceState(null, """", window.location.pathname + window.location.search);
      return fromHash;
    }}
    try {{ return localStorage.getItem(TOKEN_KEY); }} catch (e) {{ return null; }}
  }}

  function showAuthBanner() {{
    var b = document.getElementById(""initBanner"");
    b.style.display = ""block"";
    b.innerHTML = ""<strong>Auth required.</strong> Append the auth token to the URL as <code>/debug#token=YOUR_TOKEN</code> (the same token used by the main web UI) and reload."";
  }}

  function setStatus(text, ok) {{
    var s = document.getElementById(""status"");
    s.textContent = text;
    s.className = ""status "" + (ok ? ""ok"" : ""err"");
  }}

  function render(snapshot) {{
    var root = document.getElementById(""root"");
    var existing = {{}};
    // Cache sections by group name so we can update text in place rather
    // than rebuilding the DOM every tick (preserves text-selection state).
    var nodes = root.querySelectorAll("".group"");
    for (var i = 0; i < nodes.length; i++) {{
      var key = nodes[i].getAttribute(""data-group"");
      if (key) existing[key] = nodes[i];
    }}

    var seen = {{}};
    Object.keys(snapshot).forEach(function(group) {{
      seen[group] = true;
      var entries = snapshot[group] || {{}};
      var section = existing[group];
      if (!section) {{
        section = document.createElement(""div"");
        section.className = ""group"";
        section.setAttribute(""data-group"", group);
        var h = document.createElement(""h2"");
        h.textContent = group;
        section.appendChild(h);
        var card = document.createElement(""div"");
        card.className = ""card"";
        var t = document.createElement(""table"");
        var tb = document.createElement(""tbody"");
        t.appendChild(tb);
        card.appendChild(t);
        section.appendChild(card);
        root.appendChild(section);
      }}
      var tbody = section.querySelector(""tbody"");
      var rows = {{}};
      var trs = tbody.querySelectorAll(""tr"");
      for (var j = 0; j < trs.length; j++) {{
        var k = trs[j].getAttribute(""data-key"");
        if (k) rows[k] = trs[j];
      }}
      Object.keys(entries).forEach(function(k) {{
        var v = entries[k];
        var tr = rows[k];
        if (!tr) {{
          tr = document.createElement(""tr"");
          tr.setAttribute(""data-key"", k);
          var tdK = document.createElement(""td"");
          tdK.className = ""k"";
          tdK.textContent = k;
          var tdV = document.createElement(""td"");
          tdV.className = ""v"";
          tdV.textContent = v;
          tr.appendChild(tdK);
          tr.appendChild(tdV);
          tbody.appendChild(tr);
        }} else {{
          var cell = tr.querySelector(""td.v"");
          if (cell.textContent !== v) cell.textContent = v;
        }}
        delete rows[k];
      }});
      // Drop rows that disappeared from this group's snapshot.
      Object.keys(rows).forEach(function(k) {{ rows[k].parentNode.removeChild(rows[k]); }});
    }});

    // Drop any sections no longer present.
    Object.keys(existing).forEach(function(k) {{
      if (!seen[k]) existing[k].parentNode.removeChild(existing[k]);
    }});
  }}

  var token = getToken();
  if (!token) {{
    showAuthBanner();
    setStatus(""no token"", false);
    return;
  }}

  function tick() {{
    fetch(""/api/debug"", {{
      headers: {{ ""Authorization"": ""Bearer "" + token }},
      cache: ""no-store""
    }}).then(function(r) {{
      if (r.status === 401) {{
        try {{ localStorage.removeItem(TOKEN_KEY); }} catch (e) {{}}
        showAuthBanner();
        setStatus(""401 unauthorized"", false);
        return null;
      }}
      if (r.status === 404) {{
        setStatus(""404 — debug surface disabled"", false);
        return null;
      }}
      if (!r.ok) {{
        setStatus(""HTTP "" + r.status, false);
        return null;
      }}
      return r.json();
    }}).then(function(data) {{
      if (!data) return;
      render(data);
      setStatus(""ok "" + new Date().toLocaleTimeString(), true);
    }}).catch(function(err) {{
      setStatus(""error: "" + err.message, false);
    }});
  }}

  tick();
  setInterval(tick, REFRESH_MS);
}})();
</script>
</body>
</html>";
            return string.Format(template, refreshMs);
        }
    }
}
