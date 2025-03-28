using System;
using System.Collections.Generic;
using System.Text;

namespace Prosim2GSX.Services.WeightAndBalance
{
    /// <summary>
    /// Formats weight and balance data into airline-style loadsheets
    /// </summary>
    public class LoadsheetFormatter
    {
        /// <summary>
        /// Format loadsheet data as text
        /// </summary>
        public string FormatLoadSheet(string loadsheetType, string time, LoadsheetData loadData,
            string flightNumber, string tailNumber, string day, string date, string orig, string dest,
            double max_zfw, double max_tow, double max_law, int paxInfants,
            LoadsheetData prelimData = null)
        {
            StringBuilder sb = new StringBuilder();

            if (loadsheetType == "prelim")
            {
                var limitedBy = GetWeightLimitation(loadData.ZeroFuelWeight, max_zfw,
                                                    loadData.TakeoffWeight, max_tow,
                                                    loadData.LandingWeight, max_law);

                string zfwLimited = limitedBy.Item1;
                string towLimited = limitedBy.Item2;
                string lawLimited = limitedBy.Item3;

                // Format weights as whole numbers
                int zfwWhole = (int)Math.Round(loadData.ZeroFuelWeight);
                int towWhole = (int)Math.Round(loadData.TakeoffWeight);
                int lawWhole = (int)Math.Round(loadData.LandingWeight);
                int maxZfwWhole = (int)Math.Round(max_zfw);
                int maxTowWhole = (int)Math.Round(max_tow);
                int maxLawWhole = (int)Math.Round(max_law);
                int tofWhole = (int)Math.Round(loadData.TakeoffWeight - loadData.ZeroFuelWeight);
                int tifWhole = (int)Math.Round(loadData.TakeoffWeight - loadData.LandingWeight);
                int undloWhole = (int)Math.Round(max_law - loadData.LandingWeight);
                int fuelInTanksWhole = (int)Math.Round(loadData.FuelWeight);

                // Extract passenger counts by zone
                int paxZoneA = loadData.PassengersByZone.ContainsKey(1) ? loadData.PassengersByZone[1] : 0;
                int paxZoneB = loadData.PassengersByZone.ContainsKey(2) ? loadData.PassengersByZone[2] : 0;
                int paxZoneC = loadData.PassengersByZone.ContainsKey(3) ? loadData.PassengersByZone[3] : 0;
                paxZoneC += loadData.PassengersByZone.ContainsKey(4) ? loadData.PassengersByZone[4] : 0;

                sb.AppendLine($"- LOADSHEET PRELIM {time}");
                sb.AppendLine("EDNO 1");
                sb.AppendLine($"{flightNumber}/{day} {date}");
                sb.AppendLine($"{orig} {dest} {tailNumber} 2/4");
                sb.AppendLine($"ZFW  {zfwWhole}  MAX  {maxZfwWhole}  {zfwLimited}");
                sb.AppendLine($"TOF  {tofWhole}");
                sb.AppendLine($"TOW  {towWhole}  MAX  {maxTowWhole}  {towLimited}");
                sb.AppendLine($"TIF {tifWhole}");
                sb.AppendLine($"LAW  {lawWhole}  MAX  {maxLawWhole}  {lawLimited}");
                sb.AppendLine($"UNDLO  {undloWhole}");
                sb.AppendLine($"PAX/{paxInfants}/{loadData.TotalPassengers} TTL {paxInfants + loadData.TotalPassengers}");
                sb.AppendLine($"MACZFW  {loadData.ZeroFuelWeightMac:F2}");
                sb.AppendLine($"MACTOW  {loadData.TakeoffWeightMac:F2}");
                sb.AppendLine($"A{paxZoneA}  B{paxZoneB}  C{paxZoneC}");
                sb.AppendLine("CABIN SECTION TRIM");
                sb.AppendLine("SI SERVICE WEIGHT");
                sb.AppendLine("ADJUSTMENT WEIGHT/INDEX");
                sb.AppendLine("ADD");
                sb.AppendLine($"{dest} POTABLE WATER xx/10");
                sb.AppendLine("100PCT");
                sb.AppendLine("441 -0.5");
                sb.AppendLine("DEDUCTIONS");
                sb.AppendLine("NIL PANTRY EFFECT 2590/0.0");
                sb.AppendLine("......................");
                sb.AppendLine("PREPARED BY");
                sb.AppendLine($"{GetRandomName()} +1 800 555 0199");
                sb.AppendLine($"LICENCE {GetRandomLicenceNumber()}");
                sb.AppendLine($"FUEL IN TANKS {fuelInTanksWhole}");
                sb.Append("END");
            }
            else if (loadsheetType == "final" && prelimData != null)
            {
                var differentValues = GetLoadSheetDifferences(
                    prelimData.ZeroFuelWeight, prelimData.TakeoffWeight, prelimData.TotalPassengers,
                    prelimData.ZeroFuelWeightMac, prelimData.TakeoffWeightMac, prelimData.FuelWeight,
                    loadData.ZeroFuelWeight, loadData.TakeoffWeight, loadData.TotalPassengers,
                    loadData.ZeroFuelWeightMac, loadData.TakeoffWeightMac, loadData.FuelWeight);

                string zfwChanged = differentValues.Item1;
                string towChanged = differentValues.Item2;
                string paxChanged = differentValues.Item3;
                string macZfwChanged = differentValues.Item4;
                string macTowChanged = differentValues.Item5;
                string fuelChanged = differentValues.Item6;

                string finalTitle = differentValues.Item7 ? "REVISIONS TO EDNO 1" : "COMPLIANCE WITH EDNO 1";

                // Calculate the difference between preliminary and final passenger numbers
                int paxDifference = loadData.TotalPassengers - prelimData.TotalPassengers;
                string paxDiffString;
                if (paxDifference > 0)
                    paxDiffString = $"{loadData.TotalPassengers} plus {paxDifference}";
                else if (paxDifference < 0)
                    paxDiffString = $"{loadData.TotalPassengers} minus {-paxDifference}";
                else
                    paxDiffString = $"{loadData.TotalPassengers} no change";

                // Format weights as whole numbers
                int finalZfwWhole = (int)Math.Round(loadData.ZeroFuelWeight);
                int finalTowWhole = (int)Math.Round(loadData.TakeoffWeight);
                int finalFuelWhole = (int)Math.Round(loadData.FuelWeight);

                sb.AppendLine(finalTitle);
                sb.AppendLine($"{flightNumber}/{day}  {date}");
                sb.AppendLine($"{orig}  {dest}  {tailNumber}  2/4");
                sb.AppendLine("......................");
                sb.AppendLine($"ZFW  {finalZfwWhole}  {zfwChanged}");
                sb.AppendLine($"TOW  {finalTowWhole}  {towChanged}");
                sb.AppendLine($"PAX  {paxDiffString}");
                sb.AppendLine($"MACZFW  {loadData.ZeroFuelWeightMac:F2}  {macZfwChanged}");
                sb.AppendLine($"MACTOW  {loadData.TakeoffWeightMac:F2}  {macTowChanged}");
                sb.AppendLine($"FUEL IN TANKS  {finalFuelWhole}  {fuelChanged}");
                sb.Append("END");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check for weight limitations
        /// </summary>
        private (string, string, string) GetWeightLimitation(double est_zfw, double max_zfw, double est_tow, double max_tow, double est_law, double max_law)
        {
            const int WeightThreshold = 1000;
            string zfwLimited = "";
            string towLimited = "";
            string lawLimited = "";
            bool zfwExceeds = est_zfw > max_zfw;
            bool towExceeds = est_tow > max_tow;
            bool lawExceeds = est_law > max_law;
            bool zfwApproaches = !zfwExceeds && (max_zfw - est_zfw <= WeightThreshold);
            bool towApproaches = !towExceeds && (max_tow - est_tow <= WeightThreshold);
            bool lawApproaches = !lawExceeds && (max_law - est_law <= WeightThreshold);

            if (zfwApproaches || zfwExceeds)
                zfwLimited = "L";

            if (towApproaches || towExceeds)
                towLimited = "L";

            if (lawApproaches || lawExceeds)
                lawLimited = "L";

            return (zfwLimited, towLimited, lawLimited);
        }

        /// <summary>
        /// Compare preliminary and final loadsheet values
        /// </summary>
        private (string, string, string, string, string, string, bool) GetLoadSheetDifferences(
            double prezfw, double preTow, int prePax, double preMacZfw, double preMacTow, double prefuel,
            double finalZfw, double finalTow, int finalPax, double finalMacZfw, double finalMacTow, double finalfuel)
        {
            // Tolerance values for detecting changes
            const double WeightTolerance = 1000.0; // 1000 kg tolerance for weight values
            const double MacTolerance = 0.5;     // 0.5% tolerance for MAC values
            string zfwChanged = "";
            string towChanged = "";
            string paxChanged = "";
            string macZfwChanged = "";
            string macTowChanged = "";
            string fuelChanged = "";

            // Check if values have changed beyond tolerance
            bool hasZfwChanged = Math.Abs(prezfw - finalZfw) > WeightTolerance;
            bool hasTowChanged = Math.Abs(preTow - finalTow) > WeightTolerance;
            bool hasPaxChanged = prePax != finalPax;
            bool hasMacZfwChanged = Math.Abs(preMacZfw - finalMacZfw) > MacTolerance;
            bool hasMacTowChanged = Math.Abs(preMacTow - finalMacTow) > MacTolerance;
            bool hasFuelChanged = Math.Abs(prefuel - finalfuel) > WeightTolerance;

            // Mark changes with "//"
            if (hasZfwChanged)
                zfwChanged = "//";
            if (hasTowChanged)
                towChanged = "//";
            if (hasPaxChanged)
                paxChanged = "//";
            if (hasMacZfwChanged)
                macZfwChanged = "//";
            if (hasMacTowChanged)
                macTowChanged = "//";
            if (hasFuelChanged)
                fuelChanged = "//";

            // Determine if any values have changed
            bool hasChanged = hasZfwChanged || hasTowChanged || hasPaxChanged ||
                              hasMacZfwChanged || hasMacTowChanged || hasFuelChanged;

            return (zfwChanged, towChanged, paxChanged, macZfwChanged, macTowChanged, fuelChanged, hasChanged);
        }

        /// <summary>
        /// Generates a random crew name for the loadsheet
        /// </summary>
        private string GetRandomName()
        {
            // Lists of first names and last names
            string[] firstNames = {
                "John", "Jane", "Michael", "Emily", "David",
                "Sarah", "Christopher", "Jennifer", "Daniel", "Jessica"
            };

            string[] lastNames = {
                "Smith", "Johnson", "Williams", "Brown", "Jones",
                "Garcia", "Miller", "Davis", "Martinez", "Hernandez"
            };

            // Random number generator
            Random random = new Random();

            // Select a random first name and last name
            string firstName = firstNames[random.Next(firstNames.Length)];
            string lastName = lastNames[random.Next(lastNames.Length)];

            // Return the formatted name
            return $"{firstName}/{lastName}";
        }

        /// <summary>
        /// Generates a random license number for the loadsheet
        /// </summary>
        private string GetRandomLicenceNumber()
        {
            // Random number generator
            Random random = new Random();
            // Arrays of uppercase letters and digits
            char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            char[] digits = "0123456789".ToCharArray();

            // Generate 3 random letters
            char letter1 = letters[random.Next(letters.Length)];
            char letter2 = letters[random.Next(letters.Length)];
            char letter3 = letters[random.Next(letters.Length)];

            // Generate 3 random digits
            char digit1 = digits[random.Next(digits.Length)];
            char digit2 = digits[random.Next(digits.Length)];
            char digit3 = digits[random.Next(digits.Length)];

            // Return the formatted license number
            return $"{letter1}{letter2}{letter3}{digit1}{digit2}{digit3}";
        }
    }
}