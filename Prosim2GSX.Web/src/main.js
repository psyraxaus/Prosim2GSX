import { jsx as _jsx } from "react/jsx-runtime";
import React from "react";
import ReactDOM from "react-dom/client";
import { App } from "./App";
import { bootstrapAuth } from "./auth/auth";
import "./styles/global.css";
// If the URL carried a token in its hash fragment (e.g. user just
// scanned the QR code from the desktop app's App Settings tab) move
// it into localStorage and scrub the address bar BEFORE React mounts,
// so AuthGate sees a token already in place and skips the prompt.
bootstrapAuth();
const root = document.getElementById("root");
if (!root)
    throw new Error("#root element missing from index.html");
ReactDOM.createRoot(root).render(_jsx(React.StrictMode, { children: _jsx(App, {}) }));
