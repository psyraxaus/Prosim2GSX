using System;
using System.Text;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    public class AcarsService : IAcarsService
    {
        private AcarsClient acarsClient;
        private bool isInitialized = false;

        private double prelimZfw = 0.0d;
        private double prelimTow = 0.0d;
        private int prelimPax = 0;
        private double prelimMacZfw = 0.0d;
        private double prelimMacTow = 0.0d;
        private double prelimFuel = 0.0d;

        public string Callsign { get; set; }
        public string AcarsNetworkUrl { get; set; }
        public string LogonSecret { get; set; }

        public AcarsService(string acarsNetworkUrl, string logonSecret)
        {
            AcarsNetworkUrl = acarsNetworkUrl;
            LogonSecret = logonSecret;
        }

        public bool Initialize(string flightNumber)
        {
            try
            {
                Callsign = FlightCallsignToOpsCallsign(flightNumber);
                acarsClient = new AcarsClient(Callsign, LogonSecret, AcarsNetworkUrl);
                isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AcarsService:Initialize", $"Unable to initialize ACARS service - Error: {ex.Message}");
                isInitialized = false;
                return false;
            }
        }

        public async Task SendMessageAsync(string toCallsign, string messageType, string packetData)
        {
            if (!isInitialized)
            {
                Logger.Log(LogLevel.Warning, "AcarsService:SendMessageAsync", "ACARS service not initialized");
                return;
            }

            try
            {
                await acarsClient.SendMessageToAcars(toCallsign, messageType, packetData);
                Logger.Log(LogLevel.Debug, "AcarsService:SendMessageAsync", $"Message sent to {toCallsign}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "AcarsService:SendMessageAsync", $"Error sending ACARS message: {ex.Message}");
            }
        }

        public async Task SendPreliminaryLoadsheetAsync(string flightNumber, (string time, string flightNumber, string tailNumber, string day, string date, string orig, string dest, double est_zfw, double max_zfw, double est_tow, double max_tow, double est_law, double max_law, int paxInfants, int paxAdults, double macZfw, double macTow, int paxZoneA, int paxZoneB, int paxZoneC, double fuelInTanks) loadsheetData)
        {
            prelimZfw = loadsheetData.est_zfw;
            prelimTow = loadsheetData.est_tow;
            prelimPax = loadsheetData.paxAdults;
            prelimMacZfw = loadsheetData.macZfw;
            prelimMacTow = loadsheetData.macTow;
            prelimFuel = Math.Round(loadsheetData.fuelInTanks);

            string prelimLoadsheet = FormatLoadSheet("prelim", loadsheetData.time, loadsheetData.flightNumber, loadsheetData.tailNumber, 
                loadsheetData.day, loadsheetData.date, loadsheetData.orig, loadsheetData.dest, loadsheetData.est_zfw, loadsheetData.max_zfw, 
                loadsheetData.est_tow, loadsheetData.max_tow, loadsheetData.est_law, loadsheetData.max_law, loadsheetData.paxInfants, 
                loadsheetData.paxAdults, loadsheetData.macZfw, loadsheetData.macTow, loadsheetData.paxZoneA, loadsheetData.paxZoneB, 
                loadsheetData.paxZoneC, loadsheetData.fuelInTanks);

            await SendMessageAsync(flightNumber, "telex", prelimLoadsheet);
        }

        public async Task SendFinalLoadsheetAsync(string flightNumber, (string time, string flightNumber, string tailNumber, string day, string date, string orig, string dest, double est_zfw, double max_zfw, double est_tow, double max_tow, double est_law, double max_law, int paxInfants, int paxAdults, double macZfw, double macTow, int paxZoneA, int paxZoneB, int paxZoneC, double fuelInTanks) loadsheetData, 
            (double zfw, double tow, int pax, double macZfw, double macTow, double fuel) preliminaryData)
        {
            double preZfw = preliminaryData.zfw != 0 ? preliminaryData.zfw : prelimZfw;
            double preTow = preliminaryData.tow != 0 ? preliminaryData.tow : prelimTow;
            int prePax = preliminaryData.pax != 0 ? preliminaryData.pax : prelimPax;
            double preMacZfw = preliminaryData.macZfw != 0 ? preliminaryData.macZfw : prelimMacZfw;
            double preMacTow = preliminaryData.macTow != 0 ? preliminaryData.macTow : prelimMacTow;
            double preFuel = preliminaryData.fuel != 0 ? preliminaryData.fuel : prelimFuel;

            string finalLoadsheet = FormatLoadSheet("final", loadsheetData.time, loadsheetData.flightNumber, loadsheetData.tailNumber, 
                loadsheetData.day, loadsheetData.date, loadsheetData.orig, loadsheetData.dest, loadsheetData.est_zfw, loadsheetData.max_zfw, 
                loadsheetData.est_tow, loadsheetData.max_tow, loadsheetData.est_law, loadsheetData.max_law, loadsheetData.paxInfants, 
                loadsheetData.paxAdults, loadsheetData.macZfw, loadsheetData.macTow, loadsheetData.paxZoneA, loadsheetData.paxZoneB, 
                loadsheetData.paxZoneC, loadsheetData.fuelInTanks);

            await SendMessageAsync(flightNumber, "telex", finalLoadsheet);
        }

        public string FlightCallsignToOpsCallsign(string flightNumber)
        {
            Logger.Log(LogLevel.Debug, "AcarsService:FlightCallsignToOpsCallsign", $"Flight Number obtained from flight plan: {flightNumber}");

            var count = 0;

            foreach (char c in flightNumber)
            {
                if (!char.IsLetter(c))
                {
                    break;
                }

                ++count;
            }

            StringBuilder sb = new StringBuilder(flightNumber, 8);
            sb.Remove(0, count);

            if (sb.Length >= 5)
            {
                sb.Remove(0, (sb.Length - 4));
            }

            sb.Insert(0, "OPS");
            Logger.Log(LogLevel.Debug, "AcarsService:FlightCallsignToOpsCallsign", $"Changed OPS callsign: {sb.ToString()}");

            return sb.ToString();
        }

        private string FormatLoadSheet(string loadsheetType, string time, string flightNumber, string tailNumber, string day, string date, string orig, string dest, double est_zfw, double max_zfw, double est_tow, double max_tow, double est_law, double max_law, int paxInfants, int paxAdults, double macZfw, double macTow, int paxZoneA, int paxZoneB, int paxZoneC, double fuelInTanks)
        {
            string formattedLoadSheet = "";
            var limitedBy = GetWeightLimitation(est_zfw, max_zfw, est_tow, max_tow, est_law, max_law);

            if (loadsheetType == "prelim")
            {
                string zfwLimited = limitedBy.Item1;
                string towLimited = limitedBy.Item2;
                string lawLimited = limitedBy.Item3;
                
                int zfwWhole = (int)Math.Round(est_zfw);
                int towWhole = (int)Math.Round(est_tow);
                int lawWhole = (int)Math.Round(est_law);
                int maxZfwWhole = (int)Math.Round(max_zfw);
                int maxTowWhole = (int)Math.Round(max_tow);
                int maxLawWhole = (int)Math.Round(max_law);
                int tofWhole = (int)Math.Round(est_tow - est_zfw);
                int tifWhole = (int)Math.Round(est_tow - est_law);
                int undloWhole = (int)Math.Round(max_law - est_law);
                int fuelInTanksWhole = (int)Math.Round(fuelInTanks);

                formattedLoadSheet = $"- LOADSHEET PRELIM {time}\nEDNO 1\n{flightNumber}/{day} {date}\n{orig} {dest} {tailNumber} 2/4\nZFW  {zfwWhole}  MAX  {maxZfwWhole}  {zfwLimited}\nTOF  {tofWhole}\nTOW  {towWhole}  MAX  {maxTowWhole}  {towLimited}\nTIF {tifWhole}\nLAW  {lawWhole}  MAX  {maxLawWhole}  {lawLimited}\nUNDLO  {undloWhole}\nPAX/{paxInfants}/{paxAdults} TTL {paxInfants + paxAdults}\nMACZFW  {macZfw}\nMACTOW  {macTow}\nA{paxZoneA}  B{paxZoneB}  C{paxZoneC}\nCABIN SECTION TRIM\nSI SERVICE WEIGHT\nADJUSTMENT WEIGHT/INDEX\nADD\n{dest} POTABLE WATER xx/10\n100PCT\n441 -0.5\nDEDUCTIONS\nNIL PANTRY EFFECT 2590/0.0\n......................\nPREPARED BY\n{GetRandomName()} +1 800 555 0199\nLICENCE {GetRandomLicenceNumber()}\nFUEL IN TANKS {fuelInTanksWhole}\nEND";
            }
            else if (loadsheetType == "final")
            {
                double finalZfw = est_zfw;
                double finalTow = est_tow;
                int finalPax = paxAdults;
                double finalMacZfw = macZfw;
                double finalMacTow = macTow;
                double finalFuel = Math.Round(fuelInTanks);

                var differentValues = GetLoadSheetDifferences(prelimZfw, prelimTow, prelimPax, prelimMacZfw, prelimMacTow, prelimFuel, finalZfw, finalTow, finalPax, finalMacZfw, finalMacTow, finalFuel);

                string zfwChanged = differentValues.Item1;
                string towChanged = differentValues.Item2;
                string paxChanged = differentValues.Item3;
                string macZfwChanged = differentValues.Item4;
                string macTowChanged = differentValues.Item5;
                string fuelChanged = differentValues.Item6;

                string finalTitle = differentValues.Item7 ? "REVISIONS TO EDNO 1" : "COMPLIANCE WITH EDNO 1";

                int paxDifference = finalPax - prelimPax;
                string paxDiffString = "";
                
                if (paxDifference > 0)
                {
                    paxDiffString = $"{finalPax} plus {paxDifference}";
                }
                else if (paxDifference < 0)
                {
                    paxDiffString = $"{finalPax} minus {-paxDifference}";
                }
                else
                {
                    paxDiffString = $"{finalPax} no change";
                }

                int finalZfwWhole = (int)Math.Round(finalZfw);
                int finalTowWhole = (int)Math.Round(finalTow);
                int finalFuelWhole = (int)Math.Round(finalFuel);

                formattedLoadSheet = $"{finalTitle}\n{flightNumber}/{day}  {date}\n{orig}  {dest}  {tailNumber}  2/4\n......................\nZFW  {finalZfwWhole}  {zfwChanged}\nTOW  {finalTowWhole}  {towChanged}\nPAX  {paxDiffString}\nMACZFW  {finalMacZfw}  {macZfwChanged}\nMACTOW  {finalMacTow}  {macTowChanged}\nFUEL IN TANKS  {finalFuelWhole}  {fuelChanged}\nEND";
            }
            return formattedLoadSheet;
        }

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
            {
                zfwLimited = "L";
            }

            if (towApproaches || towExceeds)
            {
                towLimited = "L";
            }

            if (lawApproaches || lawExceeds)
            {
                lawLimited = "L";
            }

            return (zfwLimited, towLimited, lawLimited);
        }

        private (string, string, string, string, string, string, bool) GetLoadSheetDifferences(double prezfw, double preTow, int prePax, double preMacZfw, double preMacTow, double prefuel, double finalZfw, double finalTow, int finalPax, double finalMacZfw, double finalMacTow, double finalfuel)
        {
            const double WeightTolerance = 1000.0;
            const double MacTolerance = 0.1;
            
            string zfwChanged = "";
            string towChanged = "";
            string paxChanged = "";
            string macZfwChanged = "";
            string macTowChanged = "";
            string fuelChanged = "";

            bool hasZfwChanged = Math.Abs(prezfw - finalZfw) > WeightTolerance;
            bool hasTowChanged = Math.Abs(preTow - finalTow) > WeightTolerance;
            bool hasPaxChanged = prePax != finalPax;
            bool hasMacZfwChanged = Math.Abs(preMacZfw - finalMacZfw) > MacTolerance;
            bool hasMacTowChanged = Math.Abs(preMacTow - finalMacTow) > MacTolerance;
            bool hasFuelChanged = Math.Abs(prefuel - finalfuel) > WeightTolerance;

            if (hasZfwChanged)
            {
                zfwChanged = "//";
                Logger.Log(LogLevel.Debug, "AcarsService:GetLoadSheetDifferences", $"ZFW changed: {prezfw} -> {finalZfw}");
            }

            if (hasTowChanged)
            {
                towChanged = "//";
                Logger.Log(LogLevel.Debug, "AcarsService:GetLoadSheetDifferences", $"TOW changed: {preTow} -> {finalTow}");
            }

            if (hasPaxChanged)
            {
                paxChanged = "//";
                Logger.Log(LogLevel.Debug, "AcarsService:GetLoadSheetDifferences", $"PAX changed: {prePax} -> {finalPax}");
            }

            if (hasMacZfwChanged)
            {
                macZfwChanged = "//";
                Logger.Log(LogLevel.Debug, "AcarsService:GetLoadSheetDifferences", $"MACZFW changed: {preMacZfw} -> {finalMacZfw}");
            }

            if (hasMacTowChanged)
            {
                macTowChanged = "//";
                Logger.Log(LogLevel.Debug, "AcarsService:GetLoadSheetDifferences", $"MACTOW changed: {preMacTow} -> {finalMacTow}");
            }

            if (hasFuelChanged)
            {
                fuelChanged = "//";
                Logger.Log(LogLevel.Debug, "AcarsService:GetLoadSheetDifferences", $"Fuel changed: {prefuel} -> {finalfuel}");
            }

            bool hasChanged = hasZfwChanged || hasTowChanged || hasPaxChanged || hasMacZfwChanged || hasMacTowChanged || hasFuelChanged;

            return (zfwChanged, towChanged, paxChanged, macZfwChanged, macTowChanged, fuelChanged, hasChanged);
        }

        private string GetRandomName()
        {
            string[] firstNames = {
                "John", "Jane", "Michael", "Emily", "David",
                "Sarah", "Christopher", "Jennifer", "Daniel", "Jessica"
            };

            string[] lastNames = {
                "Smith", "Johnson", "Williams", "Brown", "Jones",
                "Garcia", "Miller", "Davis", "Martinez", "Hernandez"
            };

            Random random = new Random();
            string firstName = firstNames[random.Next(firstNames.Length)];
            string lastName = lastNames[random.Next(lastNames.Length)];

            return $"{firstName}/{lastName}";
        }

        private string GetRandomLicenceNumber()
        {
            Random random = new Random();
            char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            char[] digits = "0123456789".ToCharArray();

            char letter1 = letters[random.Next(letters.Length)];
            char letter2 = letters[random.Next(letters.Length)];
            char letter3 = letters[random.Next(letters.Length)];

            char digit1 = digits[random.Next(digits.Length)];
            char digit2 = digits[random.Next(digits.Length)];
            char digit3 = digits[random.Next(digits.Length)];

            return $"{letter1}{letter2}{letter3}{digit1}{digit2}{digit3}";
        }
    }
}
