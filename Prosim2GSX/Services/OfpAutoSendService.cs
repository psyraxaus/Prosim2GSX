using CFIT.AppLogger;
using Prosim2GSX.Commands.Handlers;
using Prosim2GSX.GSX;
using ProsimInterface;
using System;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    // Watches GsxController.AutomationController.OnStateChange and fires the
    // arrival-gate Send-Now flow exactly once per pending gate when the
    // aircraft transitions into the Flight phase. Lives at the AppService
    // level so the trigger works regardless of which UI (WPF or web) the
    // user used to confirm the gate, and survives tab close/reopen.
    //
    // Replaces the previous WPF-only auto-fire that lived in ModelOfp.
    public class OfpAutoSendService
    {
        private readonly AppService _app;
        private GsxAutomationController _controller;

        public OfpAutoSendService(AppService app)
        {
            _app = app;
        }

        public virtual void Attach()
        {
            try
            {
                _controller = _app?.GsxService?.AutomationController;
                if (_controller == null)
                {
                    Logger.Debug("OfpAutoSendService: AutomationController unavailable; will not attach");
                    return;
                }
                _controller.OnStateChange += OnAutomationStateChanged;
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        public virtual void Detach()
        {
            try
            {
                if (_controller != null)
                    _controller.OnStateChange -= OnAutomationStateChanged;
                _controller = null;
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        protected virtual void OnAutomationStateChanged(AutomationState state)
        {
            try
            {
                if (state != AutomationState.Flight) return;
                var ofp = _app?.Ofp;
                if (ofp == null) return;
                if (string.IsNullOrWhiteSpace(ofp.PendingArrivalGate)) return;
                if (ofp.AutoFired) return;

                ofp.AutoFired = true;
                Logger.Information(
                    $"OfpAutoSendService: AutomationState=Flight + PendingArrivalGate='{ofp.PendingArrivalGate}' → firing SendNow");
                _ = Task.Run(async () =>
                {
                    try { await OfpHandlers.SendNowAsync(_app); }
                    catch (Exception ex) { Logger.LogException(ex); }
                });
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }
    }
}
