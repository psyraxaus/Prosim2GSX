using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX.Menu;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Services
{
    public enum GsxServiceType
    {
        Unknown = 0,
        Reposition = 1,
        Refuel = 2,
        Catering = 3,
        Boarding = 4,
        Pushback = 5,
        Deice = 6,
        Deboarding = 7,
        GPU = 8,
        Water = 9,
        Lavatory = 10,
        Jetway = 11,
        Stairs = 12,
        Cleaning = 13,
    }

    public enum GsxServiceActivation
    {
        Skip = 0,
        Manual = 1,
        AfterCalled = 2,
        AfterRequested = 3,
        AfterActive = 4,
        AfterPrevCompleted = 5,
        AfterAllCompleted = 6,
    }

    public enum GsxServiceConstraint
    {
        NoneAlways = 0,
        FirstLeg = 1,
        TurnAround = 2,
        CompanyHub = 3,
        NonCompanyHub = 4,
    }

    public enum GsxServiceState
    {
        Unknown = 0,
        Callable = 1,
        NotAvailable = 2,
        Bypassed = 3,
        Requested = 4,
        Active = 5,
        Completed = 6,
    }

    public abstract class GsxService
    {
        public abstract GsxServiceType Type { get; }
        public virtual GsxController Controller { get; }
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual ReceiverStore ReceiverStore => Controller.ReceiverStore;
        protected virtual AircraftProfile Profile => Controller.AircraftProfile;
        protected virtual bool IsProsimAircraft => AppService.Instance.IsProsimAircraft;

        public virtual bool IsCalled { get; protected set; } = false;
        protected virtual bool SequenceResult => CallSequence?.IsSuccess ?? false;
        protected virtual GsxMenuSequence CallSequence { get; }
        protected abstract ISimResourceSubscription SubStateVar { get; }
        public virtual GsxServiceState State => GetState();
        public virtual bool IsCalling => CallSequence.IsExecuting;
        public virtual bool IsRunning => State == GsxServiceState.Requested || State == GsxServiceState.Active;
        public virtual bool IsActive => State == GsxServiceState.Active;
        public virtual bool IsCompleted => State == GsxServiceState.Completed || WasCompleted;
        protected virtual double NumStateCompleted { get; } = 6;
        public virtual bool IsSkipped { get; set; } = false;
        public virtual bool WasActive { get; protected set; } = false;
        public virtual bool WasCompleted { get; protected set; } = false;

        public event Func<GsxService, Task> OnActive;
        public event Func<GsxService, Task> OnCompleted;
        public event Func<GsxService, Task> OnStateChanged;

        // Once-per-cycle notification guards. Reset in ResetState so the next service cycle
        // can fire each event again. Without these, NotifyStateChange can fire on every
        // ReconcileState tick (1Hz) — that drove the "Service callable" log spam observed in
        // long flights.
        protected GsxServiceState _lastNotifiedState = GsxServiceState.Unknown;
        protected bool _activeNotified = false;
        protected bool _completedNotified = false;

        // Tracked subscriptions registered via the helper methods below. Base FreeResources
        // unsubscribes and removes all of them, so subclasses no longer need bespoke cleanup.
        private readonly List<TrackedSubscription> _trackedSubscriptions = new();

        private sealed class TrackedSubscription
        {
            public ISimResourceSubscription Sub;
            public Action<ISimResourceSubscription, object> Handler;
            public string VarName;
        }

        public GsxService(GsxController controller)
        {
            Controller = controller;
            CallSequence = InitCallSequence();
            InitSubscriptions();
            Controller.GsxServices.Add(Type, this);
        }

        protected abstract GsxMenuSequence InitCallSequence();

        protected abstract void InitSubscriptions();

        /// <summary>
        /// Adds the service's primary state L-Var subscription and wires <see cref="OnStateChange"/>.
        /// Tracked for automatic teardown in <see cref="FreeResources"/>.
        /// </summary>
        protected ISimResourceSubscription RegisterStateSubscription(string varName)
        {
            var sub = SimStore.AddVariable(varName);
            sub.OnReceived += OnStateChange;
            _trackedSubscriptions.Add(new TrackedSubscription { Sub = sub, Handler = OnStateChange, VarName = varName });
            return sub;
        }

        /// <summary>
        /// Adds an L-Var subscription with a custom handler. Tracked for automatic teardown.
        /// </summary>
        protected ISimResourceSubscription RegisterChangeSubscription(string varName, Action<ISimResourceSubscription, object> handler)
        {
            var sub = SimStore.AddVariable(varName);
            if (handler != null)
                sub.OnReceived += handler;
            _trackedSubscriptions.Add(new TrackedSubscription { Sub = sub, Handler = handler, VarName = varName });
            return sub;
        }

        /// <summary>
        /// Adds an L-Var without wiring any callback (read-only access pattern). Tracked for teardown.
        /// </summary>
        protected ISimResourceSubscription RegisterReadOnlyVariable(string varName)
        {
            return RegisterChangeSubscription(varName, null);
        }

        protected virtual bool EvaluateComplete(ISimResourceSubscription sub)
        {
            return sub?.GetNumber() == NumStateCompleted && WasActive;
        }

        protected virtual void RunStateRequested()
        {

        }

        protected virtual void RunStateActive()
        {
            WasActive = true;
            NotifyActive();
        }

        protected virtual void RunStateCompleted()
        {

        }

        protected virtual void OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!IsProsimAircraft)
                return;

            if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
                RunStateRequested();
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                RunStateActive();
            }
            else if (sub.GetNumber() == NumStateCompleted && !WasCompleted && (WasActive || IsCalled))
            {
                if (!WasActive)
                {
                    Logger.Debug($"{Type} Service reached completion state without observed Active — reconciling");
                    WasActive = true;
                }
                Logger.Information($"{Type} Service completed");
                WasCompleted = true;
                RunStateCompleted();
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        public virtual void ReconcileState()
        {
            if (WasCompleted || !IsProsimAircraft || SubStateVar == null)
                return;
            if (SubStateVar.GetNumber() == NumStateCompleted)
                OnStateChange(SubStateVar, null);
        }

        public virtual void ResetState(bool resetVariable = false)
        {
            IsCalled = false;
            IsSkipped = false;
            WasActive = false;
            WasCompleted = false;
            _lastNotifiedState = GsxServiceState.Unknown;
            _activeNotified = false;
            _completedNotified = false;
            CallSequence.Reset();
            if (resetVariable)
                SetStateVariable(GsxServiceState.Callable);
            DoReset();
        }

        protected abstract void DoReset();

        public virtual void FreeResources()
        {
            foreach (var t in _trackedSubscriptions)
            {
                if (t.Handler != null && t.Sub != null)
                    t.Sub.OnReceived -= t.Handler;
            }
            foreach (var t in _trackedSubscriptions)
            {
                if (!string.IsNullOrEmpty(t.VarName))
                    SimStore.Remove(t.VarName);
            }
            _trackedSubscriptions.Clear();
            DoFreeResources();
        }

        /// <summary>
        /// Subclass hook for cleanup beyond the tracked subscription list.
        /// </summary>
        protected virtual void DoFreeResources() { }

        protected virtual GsxServiceState ReadState()
        {
            try
            {
                return (GsxServiceState)SubStateVar.GetValue<int>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected virtual GsxServiceState GetState()
        {
            var state = ReadState();

            if (NumStateCompleted == 6)
                return state;

            if ((state == GsxServiceState.Bypassed || state == (GsxServiceState)NumStateCompleted) && WasActive)
                return GsxServiceState.Completed;

            return state;
        }

        protected virtual void SetStateVariable(GsxServiceState state)
        {
            Logger.Debug($"Resetting State L-Var for Service {Type} to '{state}'");
            SubStateVar.WriteValue((int)state);
        }

        protected virtual bool CheckCalled()
        {
            return IsRunning;
        }

        public virtual async Task Call()
        {
            if (IsCalled)
                return;

            if (await DoCall() == false)
                return;
            await Task.Delay(Controller.Config.DelayServiceStateChange, Controller.Token);
            IsCalled = CheckCalled();
        }

        protected virtual async Task<bool> DoCall()
        {
            bool result = await Controller.Menu.RunSequence(CallSequence);
            Logger.Debug($"{Type} Sequence completed: Success {result}");
            return result;
        }

        protected virtual void NotifyStateChange()
        {
            if (!IsProsimAircraft)
                return;

            var current = State;
            if (current == GsxServiceState.Unknown)
                return;

            if (current == _lastNotifiedState)
                return;

            _lastNotifiedState = current;
            Logger.Debug($"Notify State Change for {Type}: {current}");
            TaskTools.RunLogged(() => OnStateChanged?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyActive()
        {
            if (!IsProsimAircraft)
                return;
            if (_activeNotified)
                return;

            _activeNotified = true;
            Logger.Debug($"Notify Active for {Type}: {State}");
            TaskTools.RunLogged(() => OnActive?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyCompleted()
        {
            if (!IsProsimAircraft)
                return;
            if (_completedNotified)
                return;

            _completedNotified = true;
            Logger.Debug($"Notify Completed for {Type}: {State}");
            TaskTools.RunLogged(() => OnCompleted?.Invoke(this), Controller.Token);
        }

        public virtual void ForceComplete()
        {
            if (!WasCompleted)
            {
                Logger.Debug($"Force Complete for {Type}");
                WasCompleted = true;
                NotifyCompleted();
                NotifyStateChange();
            }
        }
    }
}
