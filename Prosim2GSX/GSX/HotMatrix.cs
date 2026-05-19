using System;

namespace Prosim2GSX.GSX
{
    // Precipitation condition for a holdover-time lookup. These mirror the
    // column headings of the published FAA / Transport Canada / EASA HOT
    // guideline tables. The crew picks the condition (Prosim2GSX has no
    // reliable precip dataref — ProSim doesn't expose one) the same way a
    // real flight crew reads it off the HOT card.
    public enum HotPrecip
    {
        None = 0,                  // No active precipitation — deiced only, no HOT
        ActiveFrost,
        FreezingFog,
        Snow,                      // Snow / snow grains / snow pellets
        FreezingDrizzleLight,
        FreezingDrizzleModerate,
        LightFreezingRain,
        RainOnColdSoakedWing,
    }

    // Holdover-time lookup.
    //
    // ⚠ SIM IMMERSION ONLY. The figures below are representative of the
    // structure of the published FAA Holdover Time Guidelines (fluid type
    // × concentration × OAT band × precipitation), rounded into plausible
    // ranges. They are NOT a certified HOT table and MUST NOT be used for
    // real-world dispatch or flight planning. Always use the current
    // official tables operationally.
    //
    // Returns the (low, high) holdover window in minutes, or null when no
    // holdover applies (no precipitation, or OAT below the lowest
    // operational use temperature for the fluid family).
    public static class HotMatrix
    {
        public readonly record struct HotWindow(double LowMinutes, double HighMinutes);

        // OAT bands shared by the table. Index by GetBand().
        //   0: OAT >= -3            1: -3 > OAT >= -14
        //   2: -14 > OAT >= -25     3: OAT < -25 (Type I/II only, below
        //                              Type IV LOUT)
        private static int GetBand(double oatC)
        {
            if (oatC >= -3) return 0;
            if (oatC >= -14) return 1;
            if (oatC >= -25) return 2;
            return 3;
        }

        // fluidType: 1=Type I, 2=Type II, 3=Type III, 4=Type IV (GSX enum).
        // concentration: 100 / 75 / 50 (percent fluid). Type I is heated /
        // single-table — concentration is ignored for it.
        public static HotWindow? Lookup(int fluidType, int concentration, double oatC, HotPrecip precip)
        {
            if (precip == HotPrecip.None) return null;
            int band = GetBand(oatC);

            // Active frost is independent of OAT band and fluid potency.
            if (precip == HotPrecip.ActiveFrost)
                return fluidType == 1 ? new HotWindow(45, 45) : new HotWindow(480, 480);

            // Type I: heated, single table, no concentration / OAT-band 3.
            if (fluidType == 1)
            {
                if (band == 3) return null; // below Type I generic LOUT
                return precip switch
                {
                    HotPrecip.FreezingFog            => Band(band, (11, 17), (8, 13), (5, 9)),
                    HotPrecip.Snow                   => Band(band, (6, 11), (4, 6), (4, 6)),
                    HotPrecip.FreezingDrizzleLight   => Band(band, (5, 9), (2, 5), null),
                    HotPrecip.FreezingDrizzleModerate=> Band(band, (4, 7), (2, 4), null),
                    HotPrecip.LightFreezingRain      => Band(band, (3, 6), (2, 5), null),
                    HotPrecip.RainOnColdSoakedWing   => Band(band, (2, 5), null, null),
                    _ => (HotWindow?)null,
                };
            }

            // Thickened fluids (Type II / III / IV). Type IV is the
            // reference set; II/III are scaled down. Below -25 °C is the
            // generic LOUT for thickened fluids.
            if (band == 3) return null;

            HotWindow? baseWin = precip switch
            {
                HotPrecip.FreezingFog             => Band(band, (75, 160), (45, 110), (20, 55)),
                HotPrecip.Snow                    => Band(band, (35, 75), (20, 45), (15, 40)),
                HotPrecip.FreezingDrizzleLight    => Band(band, (50, 110), (25, 55), null),
                HotPrecip.FreezingDrizzleModerate => Band(band, (20, 45), (10, 25), null),
                HotPrecip.LightFreezingRain       => Band(band, (25, 55), (10, 30), null),
                HotPrecip.RainOnColdSoakedWing    => Band(band, (20, 45), null, null),
                _ => (HotWindow?)null,
            };
            if (baseWin == null) return null;

            // Family potency relative to Type IV @ 100/100.
            double family = fluidType == 4 ? 1.0 : fluidType == 2 ? 0.7 : 0.6; // III ≈ Type II band
            // Concentration scaling: 100 → 1.0, 75 → 0.6, 50 → 0.35.
            double conc = concentration >= 100 ? 1.0 : concentration >= 75 ? 0.6 : 0.35;
            double f = family * conc;

            var w = baseWin.Value;
            return new HotWindow(
                Math.Round(w.LowMinutes * f),
                Math.Round(w.HighMinutes * f));
        }

        // Pick the band's window; null means "not protected in this band".
        private static HotWindow? Band(
            int band,
            (double lo, double hi)? b0,
            (double lo, double hi)? b1,
            (double lo, double hi)? b2)
        {
            var sel = band switch { 0 => b0, 1 => b1, _ => b2 };
            return sel == null ? (HotWindow?)null : new HotWindow(sel.Value.lo, sel.Value.hi);
        }
    }
}
