using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Prosim2GSX.Audio
{
    public sealed record VoiceMeeterStrip(int Index, bool IsBus, string Label, string DisplayName)
    {
        // Encoded key matches AudioMapping.VoiceMeeterKey for two-way binding.
        public string Key => $"{(IsBus ? "bus" : "strip")}:{Index}";
    }

    // Wraps the VoiceMeeter Remote API (VoicemeeterRemote64.dll). The DLL is
    // user-supplied (Config.VoiceMeeterDllPath) — we don't redistribute it.
    // Loaded on-demand via LoadLibrary so the user can configure the path
    // post-install without app restart.
    //
    // All public methods are no-ops when IsAvailable is false (single-shot
    // warning logged on first failure). Call Login() when the user enables
    // VoiceMeeter mode; AudioController drives this from its tick loop.
    public class VoiceMeeterService : IDisposable
    {
        // VoiceMeeter strip/bus gain range. The lower bound (-60 dB) is
        // VoiceMeeter's hard floor; the upper bound (+12 dB) is its hard
        // ceiling. The ACP knob full-scale linear value (1.0) maps to +12 dB
        // so the user can drive the strip past unity gain when they want.
        // Unity / pass-through is 0 dB and is what we restore on backend
        // handover (see SetStripGainDb / ResetStripsToNeutral).
        private const float MinDb = -60f;
        private const float MaxDb = 12f;
        private const float DbSpan = MaxDb - MinDb;

        // VoiceMeeter type → (max strip count, max bus count). VBVMR_GetVoicemeeterType returns:
        //   1 = VoiceMeeter (3 strips, 2 buses)
        //   2 = Banana       (5 strips, 5 buses)
        //   3 = Potato       (8 strips, 8 buses)
        private static readonly Dictionary<int, (int Strips, int Buses)> TypeCounts = new()
        {
            [1] = (3, 2),
            [2] = (5, 5),
            [3] = (8, 8),
        };

        private IntPtr _module = IntPtr.Zero;
        private bool _loginCalled = false;
        private bool _loadFailureLogged = false;
        private bool _writesSuspended = false;
        private (int Strips, int Buses)? _counts;

        private LoginDelegate _login;
        private LogoutDelegate _logout;
        private SetParameterFloatDelegate _setParamFloat;
        private GetParameterFloatDelegate _getParamFloat;
        private GetVoicemeeterTypeDelegate _getType;
        private GetParameterStringADelegate _getParamStringA;

        // DLL loaded and Login succeeded — read-only API calls (GetStrips,
        // GetStripVolume, GetStripMute) are valid regardless of suspend state,
        // so callers that just want the strip list use IsLoaded.
        public virtual bool IsLoaded => _module != IntPtr.Zero && _login != null && _loginCalled;
        // IsLoaded plus writes are not suspended. Volume / mute writes gate
        // on this so a backend swap to CoreAudio cleanly stops VM writes.
        public virtual bool IsAvailable => IsLoaded && !_writesSuspended;

        public virtual bool Login(string dllPath)
        {
            // If we already have a live session, just resume writes — repeated
            // VBVMR_Login on the same process is flaky on some VoiceMeeter
            // versions, and there's no benefit to round-tripping the API.
            if (_loginCalled)
            {
                _writesSuspended = false;
                return true;
            }
            // Fresh attempt — clear the previous-session warning gate so a
            // path-correction retry surfaces its own message instead of
            // staying silent.
            _loadFailureLogged = false;
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
            {
                if (!_loadFailureLogged)
                {
                    Logger.Warning($"VoiceMeeter Remote DLL not found at '{dllPath ?? "(empty)"}'. VoiceMeeter integration disabled.");
                    _loadFailureLogged = true;
                }
                return false;
            }

            try
            {
                _module = NativeMethods.LoadLibrary(dllPath);
                if (_module == IntPtr.Zero)
                {
                    if (!_loadFailureLogged)
                    {
                        int err = Marshal.GetLastWin32Error();
                        Logger.Warning($"LoadLibrary failed for '{dllPath}' (Win32 error {err}). VoiceMeeter integration disabled.");
                        _loadFailureLogged = true;
                    }
                    return false;
                }

                _login = ResolveDelegate<LoginDelegate>("VBVMR_Login");
                _logout = ResolveDelegate<LogoutDelegate>("VBVMR_Logout");
                _setParamFloat = ResolveDelegate<SetParameterFloatDelegate>("VBVMR_SetParameterFloat");
                _getParamFloat = ResolveDelegate<GetParameterFloatDelegate>("VBVMR_GetParameterFloat");
                _getType = ResolveDelegate<GetVoicemeeterTypeDelegate>("VBVMR_GetVoicemeeterType");
                _getParamStringA = ResolveDelegate<GetParameterStringADelegate>("VBVMR_GetParameterStringA");

                if (_login == null || _logout == null || _setParamFloat == null || _getParamFloat == null || _getType == null || _getParamStringA == null)
                {
                    Logger.Warning($"VoiceMeeter Remote DLL '{dllPath}' is missing one or more expected exports. VoiceMeeter integration disabled.");
                    UnloadInternal();
                    return false;
                }

                int rc = _login();
                if (rc != 0 && rc != 1)
                {
                    // 0 = OK, 1 = OK with VoiceMeeter not running yet (still
                    // a usable login). Anything else is an error.
                    Logger.Warning($"VBVMR_Login returned {rc}. VoiceMeeter integration disabled.");
                    UnloadInternal();
                    return false;
                }

                _loginCalled = true;
                _writesSuspended = false;
                Logger.Information($"VoiceMeeter Remote API initialised from '{dllPath}'.");
                _counts = ResolveTypeCounts();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"VoiceMeeter Login failed for '{dllPath}'");
                UnloadInternal();
                return false;
            }
        }

        // Pause writes without unloading. Used on backend transitions
        // (VM → CoreAudio) so the next CoreAudio → VM transition doesn't
        // round-trip the VBVMR_Login/Logout API, which is flaky on some
        // VoiceMeeter versions when called repeatedly in one process.
        public virtual void SuspendWrites()
        {
            if (!_loginCalled) return;
            _writesSuspended = true;
        }

        // Full teardown — unload the DLL and reset all state. Intended for
        // app shutdown / audio-service stop, not for backend toggles.
        public virtual void Logout()
        {
            if (!_loginCalled) return;
            try { _logout?.Invoke(); }
            catch (Exception ex) { Logger.LogException(ex, "VBVMR_Logout"); }
            UnloadInternal();
        }

        public virtual IReadOnlyList<VoiceMeeterStrip> GetStrips()
        {
            if (!IsLoaded) return Array.Empty<VoiceMeeterStrip>();
            var counts = _counts ??= ResolveTypeCounts();
            var list = new List<VoiceMeeterStrip>(counts.Strips + counts.Buses);
            for (int i = 0; i < counts.Strips; i++)
            {
                string label = ReadLabel($"Strip[{i}].Label");
                list.Add(new VoiceMeeterStrip(i, false, label, FormatDisplay(i, false, label)));
            }
            for (int i = 0; i < counts.Buses; i++)
            {
                string label = ReadLabel($"Bus[{i}].Label");
                list.Add(new VoiceMeeterStrip(i, true, label, FormatDisplay(i, true, label)));
            }
            return list;
        }

        public virtual void SetStripVolume(int index, bool isBus, float normalised)
        {
            if (!IsAvailable) return;
            float dB = Math.Clamp(MinDb + normalised * DbSpan, MinDb, MaxDb);
            try { _setParamFloat($"{(isBus ? "Bus" : "Strip")}[{index}].Gain", dB); }
            catch (Exception ex) { Logger.Verbose($"SetStripVolume failed ({(isBus ? "Bus" : "Strip")}[{index}]): {ex.GetType().Name}"); }
        }

        // Set an explicit dB value (clamped to VoiceMeeter's range). Used to
        // restore strips to 0 dB unity when handing audio control back to
        // CoreAudio — that's the neutral pass-through, not the user's max
        // knob position which now maps to +12 dB.
        public virtual void SetStripGainDb(int index, bool isBus, float dB)
        {
            if (!IsAvailable) return;
            float clamped = Math.Clamp(dB, MinDb, MaxDb);
            try { _setParamFloat($"{(isBus ? "Bus" : "Strip")}[{index}].Gain", clamped); }
            catch (Exception ex) { Logger.Verbose($"SetStripGainDb failed ({(isBus ? "Bus" : "Strip")}[{index}]): {ex.GetType().Name}"); }
        }

        public virtual void SetStripMute(int index, bool isBus, bool mute)
        {
            if (!IsAvailable) return;
            try { _setParamFloat($"{(isBus ? "Bus" : "Strip")}[{index}].Mute", mute ? 1f : 0f); }
            catch (Exception ex) { Logger.Verbose($"SetStripMute failed ({(isBus ? "Bus" : "Strip")}[{index}]): {ex.GetType().Name}"); }
        }

        public virtual float GetStripVolume(int index, bool isBus)
        {
            if (!IsLoaded) return 0f;
            try
            {
                if (_getParamFloat($"{(isBus ? "Bus" : "Strip")}[{index}].Gain", out float dB) == 0)
                    return Math.Clamp((dB - MinDb) / DbSpan, 0f, 1f);
            }
            catch (Exception ex) { Logger.Verbose($"GetStripVolume failed: {ex.GetType().Name}"); }
            return 0f;
        }

        public virtual bool GetStripMute(int index, bool isBus)
        {
            if (!IsLoaded) return false;
            try
            {
                if (_getParamFloat($"{(isBus ? "Bus" : "Strip")}[{index}].Mute", out float v) == 0)
                    return v >= 0.5f;
            }
            catch (Exception ex) { Logger.Verbose($"GetStripMute failed: {ex.GetType().Name}"); }
            return false;
        }

        public void Dispose() => Logout();

        private (int Strips, int Buses) ResolveTypeCounts()
        {
            try
            {
                if (_getType != null && _getType(out int type) == 0 && TypeCounts.TryGetValue(type, out var v))
                    return v;
            }
            catch { }
            return TypeCounts[1]; // safest fallback (basic VoiceMeeter)
        }

        private string ReadLabel(string param)
        {
            try
            {
                var sb = new StringBuilder(512);
                if (_getParamStringA(param, sb) == 0) return sb.ToString();
            }
            catch { }
            return string.Empty;
        }

        private static string FormatDisplay(int index, bool isBus, string label)
        {
            string head = isBus ? $"Bus {index + 1}" : $"Strip {index + 1}";
            return string.IsNullOrEmpty(label) ? head : $"{head} — {label}";
        }

        private TDelegate ResolveDelegate<TDelegate>(string name) where TDelegate : Delegate
        {
            IntPtr addr = NativeMethods.GetProcAddress(_module, name);
            if (addr == IntPtr.Zero) return null;
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(addr);
        }

        private void UnloadInternal()
        {
            try { if (_module != IntPtr.Zero) NativeMethods.FreeLibrary(_module); } catch { }
            _module = IntPtr.Zero;
            _login = null; _logout = null;
            _setParamFloat = null; _getParamFloat = null;
            _getType = null; _getParamStringA = null;
            _loginCalled = false;
            _writesSuspended = false;
            _loadFailureLogged = false;
            _counts = null;
        }

        // VBVMR_* signatures — minimum set the audio path needs.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LoginDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LogoutDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int SetParameterFloatDelegate([MarshalAs(UnmanagedType.LPStr)] string paramName, float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int GetParameterFloatDelegate([MarshalAs(UnmanagedType.LPStr)] string paramName, out float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetVoicemeeterTypeDelegate(out int type);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int GetParameterStringADelegate([MarshalAs(UnmanagedType.LPStr)] string paramName, StringBuilder result);

        private static class NativeMethods
        {
            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr LoadLibrary(string lpFileName);
            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
            internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeLibrary(IntPtr hModule);
        }
    }
}
