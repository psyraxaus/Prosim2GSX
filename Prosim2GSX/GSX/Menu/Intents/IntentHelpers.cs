using Prosim2GSX.GSX.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Shared helpers used by intent <c>VerifyOutcomeAsync</c> implementations.
    /// </summary>
    internal static class IntentHelpers
    {
        /// <summary>
        /// Safely resolves a typed <see cref="GsxService"/> from the controller's
        /// service dictionary. Returns <c>null</c> if the controller, the service
        /// dictionary entry, or the cast is unavailable — every intent treats a
        /// null service as "preconditions not satisfied" rather than crashing.
        /// Used in preference to inline <c>?.TryGetValue(..., out var s)</c>
        /// patterns to keep definite-assignment rules simple.
        /// </summary>
        internal static T GetService<T>(GsxController controller, GsxServiceType type) where T : GsxService
        {
            if (controller == null) return null;
            if (!controller.GsxServices.TryGetValue(type, out var svc)) return null;
            return svc as T;
        }

        /// <summary>
        /// Polls <paramref name="condition"/> until it returns true, the
        /// <paramref name="token"/> cancels, or <paramref name="timeout"/> elapses.
        /// Returns <c>true</c> only when the condition was satisfied within the
        /// budget. The first probe happens immediately so cheap "already true"
        /// states return without waiting a full poll interval.
        /// </summary>
        internal static async Task<bool> PollUntilAsync(
            Func<bool> condition,
            TimeSpan timeout,
            TimeSpan pollInterval,
            CancellationToken token)
        {
            if (condition == null) return false;
            // Initial probe before the first wait — many state transitions land
            // before the polling helper is even reached.
            try
            {
                if (condition()) return true;
            }
            catch
            {
                // Treat probe exceptions as "not yet"; never propagate so the
                // resolver can decide via the surrounding outcome (verify failure
                // → GsxNoResponse rather than GsxError).
            }

            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (token.IsCancellationRequested) return false;
                try
                {
                    await Task.Delay(pollInterval, token);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }

                try
                {
                    if (condition()) return true;
                }
                catch
                {
                    // see above
                }
            }
            return false;
        }

        /// <summary>Default verification poll interval — 200 ms per Phase 2a spec.</summary>
        internal static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);
    }
}
