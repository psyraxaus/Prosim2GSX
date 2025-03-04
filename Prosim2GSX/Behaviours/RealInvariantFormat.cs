﻿using System;
using System.Globalization;

namespace Prosim2GSX.Behaviours
{
    public class RealInvariantFormat : IFormatProvider
    {
        public NumberFormatInfo formatInfo = CultureInfo.InvariantCulture.NumberFormat;

        public RealInvariantFormat(string value)
        {
            if (value == null)
            {
                // Use CultureInfo.GetCultureInfo for better performance in .NET 8.0
                formatInfo = CultureInfo.GetCultureInfo("en-US").NumberFormat;
                return;
            }

            int lastPoint = value.LastIndexOf('.');
            int lastComma = value.LastIndexOf(',');
            if (lastComma > lastPoint)
            {
                // Use CultureInfo.GetCultureInfo for better performance in .NET 8.0
                formatInfo = CultureInfo.GetCultureInfo("de-DE").NumberFormat;
            }
            else
            {
                // Use CultureInfo.GetCultureInfo for better performance in .NET 8.0
                formatInfo = CultureInfo.GetCultureInfo("en-US").NumberFormat;
            }
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(NumberFormatInfo))
            {
                return formatInfo;
            }
            else
                return null;
        }
    }
}
