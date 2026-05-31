using System;
using System.Runtime.InteropServices;

namespace Prosim2GSX.Diagnostics
{
    /// <summary>
    /// Shared P/Invoke wrappers used by the diagnostics layer
    /// (<see cref="ResourceSnapshot"/> and <c>ResourceDiagnosticsWorker</c>).
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        /// <summary>Flag for <see cref="GetGuiResources"/> — count of GDI objects owned by the process.</summary>
        internal const uint GR_GDIOBJECTS = 0;
        /// <summary>Flag for <see cref="GetGuiResources"/> — count of USER objects owned by the process.</summary>
        internal const uint GR_USEROBJECTS = 1;
    }
}
