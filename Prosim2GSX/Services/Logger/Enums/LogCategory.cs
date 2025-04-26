using System;

namespace Prosim2GSX.Services.Logger.Enums
{
    /// <summary>
    /// Defines logging categories for filtering
    /// </summary>
    public enum LogCategory
    {
        /// <summary>All categories (default)</summary>
        All = 0,

        /// <summary>GSX Controller</summary>
        GsxController = 1,

        /// <summary>Refueling services</summary>
        Refueling = 2,

        /// <summary>Boarding services</summary>
        Boarding = 3,

        /// <summary>Catering services</summary>
        Catering = 1 << 3, // 8

        /// <summary>Ground services</summary>
        GroundServices = 1 << 4, // 16

        /// <summary>SimConnect interface</summary>
        SimConnect = 1 << 5, // 32

        /// <summary>Prosim interface</summary>
        Prosim = 1 << 6, // 64

        /// <summary>Event system</summary>
        Events = 1 << 7, // 128

        /// <summary>Menu system</summary>
        Menu = 1 << 8, // 256

        /// <summary>Audio system</summary>
        Audio = 1 << 9, // 512

        /// <summary>Configuration</summary>
        Configuration = 1 << 10, // 1024

        /// <summary>Door operations</summary>
        Doors = 1 << 11, // 2048

        /// <summary>Cargo operations</summary>
        Cargo = 1 << 12, // 4096

        /// <summary>Loadsheet operations</summary>
        Loadsheet = 1 << 13 // 8192
    }
}