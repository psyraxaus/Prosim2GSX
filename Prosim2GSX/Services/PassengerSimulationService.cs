using CFIT.AppLogger;
using Prosim2GSX.Web.Contracts;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prosim2GSX.Services
{
    // Generates a synthetic passenger manifest (sequential seat assignment +
    // randomly-picked names) and writes the resulting seatOccupation string
    // to ProSim. Used to populate the cabin for headless / sim-PC workflows
    // where the user wants live boarding without GSX driving the count.
    //
    // Write path: SdkInterface.WriteDataRef(RefPaxCurrentString, "true,false,...")
    // — the same .string variant the boarding service writes during normal
    // GSX boarding (ProsimBoardingService.SendSeatString). The boolean[] form
    // (RefPaxCurrent) is also writable but the .string form is what the rest
    // of the codebase reads/writes, so we stick with it.
    //
    // Zone capacities are sourced from WeightBalanceState (already polled
    // each tick from aircraft.passengers.zone[1..4].capacity) with a fallback
    // to ProsimConstants.PaxZoneLimits {24,30,36,42} if the live values
    // haven't populated yet.
    //
    // Last-generated manifest is cached so GET /api/passengers/manifest can
    // re-serve it without regenerating (which would produce a different
    // random name set on every refresh).
    public class PassengerSimulationService
    {
        // Fixed RNG seed gives reproducibility within a session — successive
        // Simulate() calls produce a deterministic sequence of name picks
        // useful for reproducing test scenarios. Per-call freshness comes
        // from the shared RNG advancing, not from a non-deterministic seed.
        private const int RngSeed = 42;

        private readonly AppService _app;
        private readonly Random _rng = new Random(RngSeed);
        private readonly object _manifestLock = new();
        private PassengerManifestDto _lastManifest;

        public PassengerSimulationService(AppService app)
        {
            _app = app;
        }

        // Distribute totalPassengers across zones proportionally to capacity
        // using largest-remainder rounding, then fill seats sequentially
        // within each zone. Returns a PassengerManifestDto WITHOUT writing
        // to ProSim — pure data shaping so the caller can preview before
        // committing.
        public virtual PassengerManifestDto GenerateManifest(int totalPassengers, int[] zoneSizes)
        {
            if (zoneSizes == null || zoneSizes.Length == 0)
                zoneSizes = ProsimConstants.PaxZoneLimits;

            int totalCapacity = zoneSizes.Sum();
            if (totalPassengers < 0) totalPassengers = 0;
            if (totalPassengers > totalCapacity) totalPassengers = totalCapacity;

            var perZone = AllocateProportional(totalPassengers, zoneSizes);

            var passengers = new List<PassengerEntryDto>(totalPassengers);
            int seatOffset = 0;
            for (int z = 0; z < zoneSizes.Length; z++)
            {
                int zoneSize = zoneSizes[z];
                int paxInZone = perZone[z];
                for (int s = 0; s < paxInZone; s++)
                {
                    passengers.Add(new PassengerEntryDto
                    {
                        SeatNumber = seatOffset + s + 1,
                        Zone = z + 1,
                        FirstName = FIRST_NAMES[_rng.Next(FIRST_NAMES.Length)],
                        LastName = SURNAMES[_rng.Next(SURNAMES.Length)],
                    });
                }
                seatOffset += zoneSize;
            }

            return new PassengerManifestDto
            {
                TotalPassengers = totalPassengers,
                GeneratedAt = DateTime.UtcNow,
                SeatOccupationWritten = false,
                Passengers = passengers,
            };
        }

        // Build the comma-separated "true,false,..." string ProSim expects
        // (132 entries on standard A320). Mirrors ProsimSeatMap.SeatString
        // semantics — same lowercase "true"/"false" tokens, no spaces.
        public virtual string BuildSeatOccupationString(PassengerManifestDto manifest, int[] zoneSizes)
        {
            int totalCapacity = zoneSizes.Sum();
            var occupied = new bool[totalCapacity];
            if (manifest != null)
            {
                foreach (var entry in manifest.Passengers)
                {
                    int idx = entry.SeatNumber - 1;
                    if (idx >= 0 && idx < totalCapacity)
                        occupied[idx] = true;
                }
            }
            return JoinSeatString(occupied);
        }

        // Generates a manifest for `count` passengers (or full capacity when
        // null) and pushes the resulting seat-occupation string to ProSim.
        // Caches the manifest for subsequent GET requests. Returns a result
        // wrapper so the controller can surface SDK-disconnect failures
        // alongside the generated manifest.
        public virtual PassengerSimulationResultDto Simulate(int? count)
        {
            try
            {
                int[] zoneSizes = ResolveZoneSizes();
                int totalCapacity = zoneSizes.Sum();
                int target = count ?? totalCapacity;

                var manifest = GenerateManifest(target, zoneSizes);
                string seatString = BuildSeatOccupationString(manifest, zoneSizes);

                bool wrote = WriteSeatOccupation(seatString);
                manifest.SeatOccupationWritten = wrote;

                lock (_manifestLock)
                {
                    _lastManifest = manifest;
                }

                if (wrote)
                {
                    Logger.Information(
                        $"Passenger simulation: generated {manifest.TotalPassengers} pax across {zoneSizes.Length} zones and wrote seatOccupation to ProSim");
                }
                else
                {
                    Logger.Warning(
                        $"Passenger simulation: generated {manifest.TotalPassengers} pax but seatOccupation write failed (SDK disconnected?)");
                }

                return new PassengerSimulationResultDto
                {
                    Success = wrote,
                    ErrorMessage = wrote ? "" : "Failed to write seatOccupation to ProSim — SDK may be disconnected.",
                    Manifest = manifest,
                };
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "PassengerSimulationService.Simulate");
                return new PassengerSimulationResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Manifest = null,
                };
            }
        }

        // Writes an empty seatOccupation string (all 'false') and clears the
        // cached manifest. The W&B WS channel will broadcast the resulting
        // zero-pax state on the next tick.
        public virtual PassengerSimulationResultDto Clear()
        {
            try
            {
                int[] zoneSizes = ResolveZoneSizes();
                var empty = new bool[zoneSizes.Sum()];
                string seatString = JoinSeatString(empty);

                bool wrote = WriteSeatOccupation(seatString);

                lock (_manifestLock)
                {
                    _lastManifest = null;
                }

                if (wrote) Logger.Information("Passenger simulation: cleared seatOccupation");
                else Logger.Warning("Passenger simulation: clear failed (SDK disconnected?)");

                return new PassengerSimulationResultDto
                {
                    Success = wrote,
                    ErrorMessage = wrote ? "" : "Failed to write seatOccupation to ProSim — SDK may be disconnected.",
                    Manifest = null,
                };
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "PassengerSimulationService.Clear");
                return new PassengerSimulationResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Manifest = null,
                };
            }
        }

        public virtual PassengerManifestDto GetManifest()
        {
            lock (_manifestLock)
            {
                return _lastManifest ?? new PassengerManifestDto
                {
                    TotalPassengers = 0,
                    GeneratedAt = DateTime.MinValue,
                    SeatOccupationWritten = false,
                    Passengers = new List<PassengerEntryDto>(),
                };
            }
        }

        // ── Internals ────────────────────────────────────────────────────────

        private int[] ResolveZoneSizes()
        {
            var s = _app?.WeightBalance;
            int z1 = s?.Zone1Capacity ?? 0;
            int z2 = s?.Zone2Capacity ?? 0;
            int z3 = s?.Zone3Capacity ?? 0;
            int z4 = s?.Zone4Capacity ?? 0;
            if (z1 > 0 || z2 > 0 || z3 > 0 || z4 > 0)
                return new[] { z1, z2, z3, z4 };
            return ProsimConstants.PaxZoneLimits;
        }

        private bool WriteSeatOccupation(string seatString)
        {
            var sdk = _app?.GsxService?.AircraftInterface?.ProsimInterface?.SdkInterface;
            if (sdk == null || !sdk.IsConnected)
            {
                Logger.Warning("PassengerSimulationService: SDK not connected");
                return false;
            }
            return sdk.WriteDataRef(ProsimConstants.RefPaxCurrentString, seatString);
        }

        // Largest-remainder allocation. Hands out floor(count * size_i / total)
        // to each zone, then distributes the remainder one-by-one to the
        // zones with the largest fractional parts. Guarantees the per-zone
        // sum equals totalPassengers exactly and never exceeds zone capacity.
        private static int[] AllocateProportional(int totalPassengers, int[] zoneSizes)
        {
            int n = zoneSizes.Length;
            int totalCapacity = zoneSizes.Sum();
            var result = new int[n];
            if (totalPassengers == 0 || totalCapacity == 0) return result;

            var fractions = new double[n];
            int allocated = 0;
            for (int i = 0; i < n; i++)
            {
                double exact = (double)totalPassengers * zoneSizes[i] / totalCapacity;
                int floor = (int)Math.Floor(exact);
                result[i] = floor;
                fractions[i] = exact - floor;
                allocated += floor;
            }

            // Distribute remaining seats by descending fractional part, but
            // only to zones with capacity headroom — protects against the
            // edge case where rounding wants to push a zone over its cap.
            int remaining = totalPassengers - allocated;
            var order = Enumerable.Range(0, n)
                .OrderByDescending(i => fractions[i])
                .ToArray();
            int cursor = 0;
            while (remaining > 0 && cursor < n * 2)
            {
                int idx = order[cursor % n];
                if (result[idx] < zoneSizes[idx])
                {
                    result[idx]++;
                    remaining--;
                }
                cursor++;
            }
            return result;
        }

        private static string JoinSeatString(bool[] occupied)
        {
            var sb = new StringBuilder(occupied.Length * 6);
            for (int i = 0; i < occupied.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(occupied[i] ? "true" : "false");
            }
            return sb.ToString();
        }

        // ── Embedded name pools ──────────────────────────────────────────────
        // 200 surnames covering Anglo, Spanish/Portuguese, Chinese,
        // Korean/Japanese, Indian/South Asian, Arabic/Middle East, German/Dutch,
        // French, and Italian origins. Curated subset — the EFB-side full list
        // is ~5000 entries; this is enough variety to feel realistic without
        // bloating the binary.
        private static readonly string[] SURNAMES = new[]
        {
            // Anglo (40)
            "Smith", "Jones", "Williams", "Brown", "Wilson", "Taylor", "Johnson", "Martin",
            "Anderson", "Thompson", "Davis", "Miller", "Moore", "Walker", "White", "Harris",
            "Robinson", "Clark", "Lewis", "Hall", "Young", "King", "Wright", "Scott",
            "Green", "Adams", "Baker", "Hill", "Carter", "Mitchell", "Roberts", "Turner",
            "Phillips", "Campbell", "Parker", "Evans", "Edwards", "Collins", "Stewart", "Morris",
            // Spanish / Portuguese (20)
            "Garcia", "Martinez", "Rodriguez", "Lopez", "Gonzalez", "Hernandez", "Perez", "Sanchez",
            "Ramirez", "Torres", "Flores", "Rivera", "Gomez", "Diaz", "Reyes", "Cruz",
            "Morales", "Ortiz", "Silva", "Costa",
            // Chinese (20)
            "Chen", "Wang", "Li", "Zhang", "Liu", "Yang", "Huang", "Zhao",
            "Wu", "Zhou", "Xu", "Sun", "Ma", "Zhu", "Hu", "Lin",
            "Guo", "He", "Gao", "Tang",
            // Korean / Japanese (20)
            "Kim", "Park", "Lee", "Tanaka", "Suzuki", "Sato", "Watanabe", "Yamamoto",
            "Nakamura", "Kobayashi", "Kato", "Ito", "Saito", "Choi", "Jung", "Yoon",
            "Hwang", "Han", "Oh", "Shin",
            // Indian / South Asian (20)
            "Singh", "Kumar", "Patel", "Sharma", "Verma", "Gupta", "Reddy", "Iyer",
            "Joshi", "Nair", "Das", "Rao", "Bhatt", "Banerjee", "Chatterjee", "Mehta",
            "Saxena", "Agarwal", "Kapoor", "Chopra",
            // Arabic / Middle East (20)
            "Ali", "Khan", "Hassan", "Hussein", "Saleh", "Mahmoud", "Aziz", "Rahman",
            "Abbas", "Karim", "Yousef", "Fadel", "Najjar", "Haddad", "Khalil", "Mansour",
            "Sabri", "Khoury", "Ahmed", "Bakir",
            // German / Dutch (20)
            "Mueller", "Schmidt", "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz",
            "Hoffmann", "Schaefer", "Bauer", "Koch", "Richter", "Klein", "Wolf", "Schultz",
            "Krause", "Lange", "Hartmann", "Vogel",
            // French (20)
            "Dupont", "Bernard", "Moreau", "Laurent", "Simon", "Michel", "Lefebvre", "Leroy",
            "Roux", "David", "Petit", "Robert", "Richard", "Durand", "Dubois", "Garnier",
            "Faure", "Rousseau", "Blanc", "Girard",
            // Italian (20)
            "Rossi", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci", "Marino",
            "Greco", "Conti", "Russo", "Bruno", "Gallo", "Lombardi", "Moretti", "Barbieri",
            "Fontana", "Santoro", "Mariani", "Caruso",
        };

        // 100 first names — mixed gender, covering the same diversity bands as
        // SURNAMES so a randomly-paired (first, last) entry tends to read
        // plausibly without forcing strict cultural matching.
        private static readonly string[] FIRST_NAMES = new[]
        {
            // Anglo male (20)
            "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph",
            "Thomas", "Charles", "Christopher", "Daniel", "Matthew", "Anthony", "Mark", "Donald",
            "Steven", "Paul", "Andrew", "Joshua",
            // Anglo female (20)
            "Emma", "Olivia", "Ava", "Isabella", "Sophia", "Mia", "Charlotte", "Amelia",
            "Harper", "Evelyn", "Abigail", "Emily", "Elizabeth", "Avery", "Sofia", "Ella",
            "Madison", "Scarlett", "Victoria", "Aria",
            // Arabic (10)
            "Mohamed", "Fatima", "Omar", "Aisha", "Hassan", "Ahmed", "Layla", "Yusuf",
            "Maryam", "Khalid",
            // Chinese (10)
            "Wei", "Fang", "Ming", "Hui", "Yu", "Jing", "Lei", "Hong", "Xin", "Zhao",
            // Indian (10)
            "Raj", "Priya", "Anita", "Vikram", "Deepa", "Arjun", "Anjali", "Rohan",
            "Kavya", "Sanjay",
            // European (10)
            "Lucas", "Marie", "Pierre", "Sophie", "Hans", "Anna", "Klaus", "Ingrid",
            "Lars", "Astrid",
            // Italian (10)
            "Giuseppe", "Maria", "Paolo", "Lucia", "Marco", "Giulia", "Luca", "Francesca",
            "Antonio", "Sara",
            // Japanese (10)
            "Yuki", "Kenji", "Sakura", "Hiroshi", "Akiko", "Takeshi", "Naomi", "Daiki",
            "Mei", "Ren",
        };
    }
}
