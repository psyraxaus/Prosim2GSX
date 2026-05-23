using CFIT.AppFramework.MessageService;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Diagnostics;
using Prosim2GSX.GSX.Menu.Intents;
using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu
{
    public enum GsxMenuState
    {
        UNKNOWN = 0,
        READY = 1,
        HIDE = 2,
        TIMEOUT = 3,
        DISABLED = 4
    }

    public class GsxMenu(GsxController gsxController)
    {
        public virtual GsxController Controller { get; } = gsxController;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual Config Config => Controller.Config;
        protected virtual AircraftProfile AircraftProfile => Controller.AircraftProfile;
        public virtual string PathMenu { get { return Path.Join(Controller.PathInstallation, GsxConstants.RelativePathMenu); } }

        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual GsxMenuState MenuState { get; protected set; } = GsxMenuState.DISABLED;
        public virtual string MenuTitle { get; protected set; }
        public virtual bool HasTitle { get { return !string.IsNullOrWhiteSpace(MenuTitle); } }
        public virtual int MenuLineCount { get { return MenuLines.Count; } }
        public virtual List<string> MenuLines { get; } = [];

        public virtual bool FirstReadyReceived { get; protected set; } = false;
        public virtual bool IsMenuReady => MenuState == GsxMenuState.READY || MenuState == GsxMenuState.HIDE;
        public virtual bool IsGateMenu => MatchTitle(GsxConstants.MenuGate);
        public virtual bool IsOperatorMenu => MatchTitle(GsxConstants.MenuOperatorHandling) || MatchTitle(GsxConstants.MenuOperatorCater);
        protected virtual ConcurrentDictionary<string, Func<GsxMenu, Task>> MenuCallbacks { get; } = [];

        protected virtual ISimResourceSubscription SubMenuEvent { get; set; }
        protected virtual ISimResourceSubscription SubMenuOpen { get; set; }
        protected virtual ISimResourceSubscription SubMenuChoice { get; set; }
        protected virtual double LastMenuSelection { get; set; } = -2;
        protected virtual bool DeIceQuestionAnswered { get; set; } = false;
        protected virtual bool FollowMeAnswered { get; set; } = false;
        protected virtual bool MenuOpenRequesting { get; set; } = false;
        protected virtual bool MenuOpenAfterReady { get; set; } = false;
        // Grace window so the passive FSDT_GSX_MENU_CHOICE observer can tell
        // our own Select() writes apart from a real manual click. Set at the
        // top of Select(); if the LVAR callback fires inside the window the
        // write is ours and not a manual override.
        private DateTime _lastSelectInFlightUntil = DateTime.MinValue;
        public virtual bool WaitingForGate { get; protected set; } = false;
        public virtual bool WarpedToGate { get; protected set; } = false;
        public virtual bool SuppressMenuRefresh { get; set; } = false;
        public virtual MessageReceiver<MsgGsxMenuReady> MsgMenuReady { get; protected set; }


        public event Action<string> MenuTitleChanged;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubMenuEvent = Controller.SimStore.AddEvent(GsxConstants.EventMenu);
                SubMenuEvent.OnReceived += OnMenuEvent;
                SubMenuOpen = Controller.SimStore.AddVariable(GsxConstants.VarMenuOpen);
                SubMenuChoice = Controller.SimStore.AddVariable(GsxConstants.VarMenuChoice);
                SubMenuChoice.OnReceived += OnMenuSelection;

                MsgMenuReady = Controller.ReceiverStore.Add<MsgGsxMenuReady>();

                Controller.MsgCouatlStopped.OnMessage += OnCouatlStopped;

                MenuCallbacks.Add(GsxConstants.MenuTugAttach, OnTugQuestion);
                MenuCallbacks.Add(GsxConstants.MenuPushbackRequest, OnPushQuestion);
                MenuCallbacks.Add(GsxConstants.MenuPushbackDirection, OnPushbackDirection);
                MenuCallbacks.Add(GsxConstants.MenuFollowMe, OnFollowMeQuestion);
                MenuCallbacks.Add(GsxConstants.MenuDeiceOnPush, OnDeiceQuestion);
                MenuCallbacks.Add(GsxConstants.MenuDeiceType, OnDeiceTypeSelect);
                MenuCallbacks.Add(GsxConstants.MenuParkingChange, OnParking);
                MenuCallbacks.Add(GsxConstants.MenuParkingSelect, OnParking);
                MenuCallbacks.Add(GsxConstants.MenuBoardCrew, OnBoardCrew);
                MenuCallbacks.Add(GsxConstants.MenuDeboardCrew, OnDeboardCrew);

                IsInitialized = true;
            }
        }

        protected virtual void OnMenuSelection(ISimResourceSubscription sub, object value)
        {
            double num = sub.GetNumber();
            if (num != -2)
            {
                // Passive manual-override detection. If the LVAR change arrived
                // outside the in-flight grace window, the click came from the
                // user (or another tool), not from our Select(). Surface it to
                // the diagnostic log so the audit trail covers human input too.
                if (DateTime.UtcNow > _lastSelectInFlightUntil)
                {
                    try
                    {
                        int choiceIndex = (int)num;
                        string lineText = (choiceIndex >= 0 && choiceIndex < MenuLines.Count)
                            ? MenuLines[choiceIndex] : null;
                        Controller.GsxMenuDiagnosticLog?.LogManualOverride(MenuTitle, choiceIndex, lineText);
                    }
                    catch (Exception ex) { Logger.LogException(ex); }
                }

                if (MatchTitle(GsxConstants.MenuDeiceOnPush))
                {
                    Logger.Debug($"Deice Question was answered: {num}");
                    DeIceQuestionAnswered = true;
                }
                else if (MatchTitle(GsxConstants.MenuParkingChange) && WaitingForGate && num == 4)
                {
                    Logger.Debug($"Warped to Gate - trigger Menu Refresh");
                    WaitingForGate = false;
                    Task.Delay(2000, RequestToken).ContinueWith((_) => OpenHide()).ContinueWith((_) => WarpedToGate = true);
                }
                LastMenuSelection = num;
                Logger.Verbose($"Menu Selection {LastMenuSelection}");
            }
        }

        protected virtual async Task OnPushQuestion(GsxMenu menu)
        {
            Logger.Debug($"Request Pushback Question active");
            await Select(1, false, false, 2);
        }

        protected virtual async Task OnPushbackDirection(GsxMenu menu)
        {
            var preference = Controller.PushbackPreference;

            // GSX has been observed to re-open the direction menu after the
            // push has already started (field log 2026-05-23 captured this
            // on a TailLeft push at EFHK gate 24 — direction was selected
            // at 16:32:25, push reached PushingBack, then the direction
            // menu reopened at 16:33:02 and was auto-selected again). Once
            // the pushback phase is past direction-needed, suppress the
            // auto-select regardless of PushbackPreferenceReapplyOnChange —
            // re-selecting direction mid-push is never the right action.
            if (Controller != null
                && Controller.GsxServices != null
                && Controller.GsxServices.TryGetValue(GsxServiceType.Pushback, out var pushSvc)
                && pushSvc is GsxServicePushback pushback
                && pushback.Phase.IsPushInProgress())
            {
                Logger.Debug($"Pushback direction menu reopened while push already in progress (Phase={pushback.Phase}); skipping auto-select");
                return;
            }

            if (!Config.PushbackPreferenceReapplyOnChange && Controller.PushbackDirectionAutoSelected)
            {
                Logger.Debug($"Pushback direction menu reopened; preference already applied this cycle, skipping");
                return;
            }

            string searchToken = preference switch
            {
                PushbackPreference.TailLeft => "Tail Left",
                PushbackPreference.TailRight => "Tail Right",
                PushbackPreference.Straight => "Straight pushback",
                _ => null,
            };
            if (searchToken == null)
                return;

            int index = FindPushbackLineByText(searchToken);
            string strategy = "text";

            if (index < 0 && preference != PushbackPreference.Straight)
            {
                index = FindPushbackLineByFixedIndex(preference);
                strategy = "fixed-index";
            }

            // Phase 5 diagnostic emission — captured AFTER the existing
            // matching logic settles on its answer, BEFORE any side-effect
            // (Select / log spam / state mutation). Purely observational; the
            // matching algorithm above is unchanged.
            EmitPushbackDirectionDiagnostic(preference, searchToken, index, strategy);

            if (index < 0)
            {
                Logger.Information($"Pushback direction menu: could not auto-pick '{searchToken}' (text{(preference != PushbackPreference.Straight ? " and fixed-index" : "")} failed). Menu lines:");
                for (int i = 0; i < MenuLines.Count; i++)
                    Logger.Information($"  [{i + 1}] {MenuLines[i]}");
                Logger.Information("Leaving menu open for manual selection.");
                return;
            }

            Logger.Information($"Auto-selecting pushback direction (preference={preference}, strategy={strategy}, item {index + 1}: '{MenuLines[index]}')");
            await Select(index + 1, false, false);
            Controller.PushbackDirectionAutoSelected = true;
        }

        private void EmitPushbackDirectionDiagnostic(PushbackPreference preference, string searchToken, int selectedIndex, string strategy)
        {
            var diag = Controller?.GsxMenuDiagnosticLog;
            if (diag == null) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Pushback direction decision");
                sb.Append("  Airport: ")
                  .AppendLine(string.IsNullOrWhiteSpace(Controller?.AutomationController?.DepartureIcao)
                      ? "unknown" : Controller.AutomationController.DepartureIcao);
                sb.AppendLine("  Stand: unknown (not currently exposed through AircraftInterface)");
                sb.AppendLine("  Heading: unknown (no SimConnect heading source wired — bearing-delta analysis disabled)");
                sb.Append("  Preference: ").Append(preference)
                  .Append(" (search token: '").Append(searchToken ?? "<null>").Append("')").AppendLine();
                sb.Append("  MenuTitle: '").Append(MenuTitle).Append("'").AppendLine();
                sb.Append("  MenuLines (").Append(MenuLineCount).Append("):").AppendLine();
                for (int i = 0; i < MenuLines.Count; i++)
                {
                    var line = MenuLines[i] ?? string.Empty;
                    var compass = PushbackCompassParser.TryParse(line);
                    sb.Append("    [").Append(i + 1).Append("] ").Append(line);
                    if (compass.HasValue)
                        sb.Append("  -> parsed: ").Append(compass.Value.Token)
                          .Append("=").Append(compass.Value.Degrees.ToString("F1", System.Globalization.CultureInfo.InvariantCulture))
                          .Append("°");
                    else
                        sb.Append("  -> unparseable");
                    sb.AppendLine();
                }
                if (selectedIndex >= 0 && selectedIndex < MenuLines.Count)
                {
                    sb.Append("  Selected: entry ").Append(selectedIndex + 1)
                      .Append(" '").Append(MenuLines[selectedIndex])
                      .Append("' via strategy '").Append(strategy).Append("'").AppendLine();
                    sb.Append("  Verdict: ").AppendLine(strategy == "text"
                        ? "OK (keyword match)"
                        : strategy == "fixed-index"
                            ? "FALLBACK (no keyword match, fixed-index fallback used)"
                            : strategy);
                }
                else
                {
                    sb.AppendLine("  Selected: none");
                    sb.AppendLine("  Verdict: MANUAL (no match, no fallback, menu left open for user)");
                }

                diag.LogDiagnostic("pushback-direction", sb.ToString());

                // Record decision context so the pushback-completion observer
                // can emit the follow-up diagnostic. Best-effort cast — if the
                // service registry doesn't yet contain Pushback (degraded
                // startup), we silently skip the follow-up.
                if (Controller != null
                    && Controller.GsxServices != null
                    && Controller.GsxServices.TryGetValue(GsxServiceType.Pushback, out var svc)
                    && svc is GsxServicePushback pushback)
                {
                    double? parsedBearing = null;
                    if (selectedIndex >= 0 && selectedIndex < MenuLines.Count)
                    {
                        var parsed = PushbackCompassParser.TryParse(MenuLines[selectedIndex]);
                        if (parsed.HasValue) parsedBearing = parsed.Value.Degrees;
                    }
                    pushback.LastDirectionDecision = new PushbackDirectionDecision
                    {
                        At = DateTime.UtcNow,
                        Preference = preference,
                        SelectedEntryText = (selectedIndex >= 0 && selectedIndex < MenuLines.Count) ? MenuLines[selectedIndex] : null,
                        Strategy = strategy,
                        ParsedSelectedHeading = parsedBearing,
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual int FindPushbackLineByText(string tailToken)
        {
            for (int i = 0; i < MenuLines.Count; i++)
            {
                if (MenuLines[i] != null && MenuLines[i].Contains(tailToken, StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }
            return -1;
        }

        protected virtual int FindPushbackLineByFixedIndex(PushbackPreference preference)
        {
            int targetIndex = preference == PushbackPreference.TailLeft ? 0 : 1;
            if (MenuLines.Count <= targetIndex)
                return -1;

            string line = MenuLines[targetIndex];
            if (string.IsNullOrWhiteSpace(line) || IsPushbackMetaLine(line))
                return -1;

            if (MenuLines.Count > 1 && IsPushbackMetaLine(MenuLines[0]) || (MenuLines.Count > 1 && IsPushbackMetaLine(MenuLines[1])))
                return -1;

            return targetIndex;
        }

        protected static readonly string[] PushbackMetaPrefixes =
        {
            "Straight", "QuickEdit", "Customize", "GSX", "Restart", "SimBrief"
        };

        protected static bool IsPushbackMetaLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            foreach (var prefix in PushbackMetaPrefixes)
            {
                if (line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        protected virtual async Task OnTugQuestion(GsxMenu menu)
        {
            Logger.Debug($"Tug Question active");
            int hide = !AircraftProfile.SkipCrewQuestion ? 0 : 2;
            if (AircraftProfile.AttachTugDuringBoarding == 2)
                await Select(1, true, false, hide);
            else if (AircraftProfile.AttachTugDuringBoarding == 1)
                await Select(2, true, false, hide);

            if (AircraftProfile.SkipCrewQuestion && AircraftProfile.AttachTugDuringBoarding != 0)
                SuppressMenuRefresh = false;
            else if (AircraftProfile.AttachTugDuringBoarding == 0)
                SuppressMenuRefresh = true;
        }

        protected virtual async Task OnFollowMeQuestion(GsxMenu menu)
        {
            Logger.Debug($"FollowMe Question active");
            if (AircraftProfile.SkipFollowMe && !FollowMeAnswered)
            {
                // Phase 4 migration: replaces the Select(2)+Operator+Reset
                // legacy sequence with the AnswerFollowMe intent (accept=false
                // matches the legacy "item 2 = No" choice). The trailing
                // OpenHide preserves the legacy Reset command's forced-close
                // semantic; without it the verify can time out waiting for
                // GSX to dismiss the menu on its own.
                var phase = Controller.AutomationController.State;
                var result = await ExecuteIntent(new AnswerFollowMe(accept: false), phase, RequestToken);
                bool success = result.IsSuccess || result.IsBenignSkip;
                if (success)
                    await OpenHide();
                FollowMeAnswered = success;
            }
        }

        protected virtual async Task OnDeiceQuestion(GsxMenu menu)
        {
            Logger.Debug($"DeIce Question active");

            // Phase 4 migration: both branches replaced with the
            // AnswerDeIceQuestion intent. The legacy code did not OpenHide
            // after either Select (hide:0 default), so the migrated paths
            // also rely on GSX dismissing the menu naturally — the intent's
            // VerifyOutcomeAsync polls for that title transition.
            var phase = Controller.AutomationController.State;

            if (Config.AutoDeiceEnabled && !DeIceQuestionAnswered)
            {
                Logger.Information($"Auto-deice enabled: answering Yes to de-icing request");
                await ExecuteIntent(new AnswerDeIceQuestion(accept: true), phase, RequestToken);
                return;
            }

            if (AircraftProfile.KeepDirectionMenuOpen && DeIceQuestionAnswered)
                await ExecuteIntent(new AnswerDeIceQuestion(accept: false), phase, RequestToken);
        }

        protected static readonly System.Collections.Generic.Dictionary<AutoDeiceFluid, (string Type, string Concentration)> DeiceFluidTokens = new()
        {
            { AutoDeiceFluid.TypeI100,  ("Type I",   "100") },
            { AutoDeiceFluid.TypeI75,   ("Type I",   "75")  },
            { AutoDeiceFluid.TypeII100, ("Type II",  "100") },
            { AutoDeiceFluid.TypeII75,  ("Type II",  "75")  },
            { AutoDeiceFluid.TypeIV100, ("Type IV",  "100") },
            { AutoDeiceFluid.TypeIV75,  ("Type IV",  "75")  },
        };

        protected virtual async Task OnDeiceTypeSelect(GsxMenu menu)
        {
            Logger.Information($"DeIce Type Select menu active - AutoDeiceEnabled={Config.AutoDeiceEnabled}, AutoDeiceFluid={Config.AutoDeiceFluid}, MenuTitle='{MenuTitle}', Lines={MenuLineCount}");

            if (!Config.AutoDeiceEnabled)
                return;

            var (typeToken, concToken) = DeiceFluidTokens.TryGetValue(Config.AutoDeiceFluid, out var tokens)
                ? tokens
                : ("Type IV", "100");

            int index = -1;
            for (int i = 0; i < MenuLines.Count; i++)
            {
                var line = MenuLines[i];
                if (line == null) continue;
                if (line.Contains(typeToken, StringComparison.InvariantCultureIgnoreCase)
                    && line.Contains(concToken, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                Logger.Information($"Auto-deice: could not find '{typeToken}' + '{concToken}' in de-icing type menu. Menu lines:");
                for (int i = 0; i < MenuLines.Count; i++)
                    Logger.Information($"  [{i + 1}] {MenuLines[i]}");
                Logger.Information("Leaving menu open for manual selection.");
                return;
            }

            Logger.Information($"Auto-deice: selecting fluid '{typeToken} {concToken}%' (item {index + 1}: '{MenuLines[index]}')");
            await Select(index + 1, false, false);
        }

        protected virtual async Task OnParking(GsxMenu menu)
        {
            Logger.Debug($"Change/Select Parking active");
            FollowMeAnswered = false;

            if (MatchTitle(GsxConstants.MenuParkingChange) && Controller.AutomationController.State < AutomationState.Departure)
            {
                Logger.Debug($"App waiting for Gate");
                WaitingForGate = true;
            }
            await Task.Delay(25);
        }

        protected virtual async Task OnBoardCrew(GsxMenu menu)
        {
            Logger.Debug($"Board Crew Question active");
            if (AircraftProfile.SkipCrewQuestion)
            {
                // Phase 4 migration: replaces Select(1,..,hide:2) with the
                // OnBoardCrewIntent + explicit OpenHide. The hide:2 semantic
                // (force-close via OpenHide) is preserved because GSX does
                // not reliably dismiss this menu on its own after a Yes.
                var phase = Controller.AutomationController.State;
                var result = await ExecuteIntent(new OnBoardCrewIntent(), phase, RequestToken);
                if (result.IsSuccess || result.IsBenignSkip)
                    await OpenHide();
                SuppressMenuRefresh = false;
            }
        }

        protected virtual async Task OnDeboardCrew(GsxMenu menu)
        {
            Logger.Debug($"Deboard Crew Question active");
            if (AircraftProfile.SkipCrewQuestion)
            {
                // Phase 4 migration: see OnBoardCrew for the hide:2 rationale.
                var phase = Controller.AutomationController.State;
                var result = await ExecuteIntent(new OnDeboardCrewIntent(), phase, RequestToken);
                if (result.IsSuccess || result.IsBenignSkip)
                    await OpenHide();
                SuppressMenuRefresh = false;
            }
        }

        protected virtual void OnCouatlStopped(MsgGsxCouatlStopped msg)
        {
            FirstReadyReceived = false;
        }

        public virtual void Reset()
        {
            LastMenuSelection = -2;
            MenuTitle = "";
            MenuLines.Clear();
            MenuState = GsxMenuState.UNKNOWN;
            FirstReadyReceived = false;
            DeIceQuestionAnswered = false;
            FollowMeAnswered = false;
            MenuOpenRequesting = false;
            WaitingForGate = false;
            WarpedToGate = false;
        }

        public virtual void ResetFlight()
        {
            DeIceQuestionAnswered = false;
            FollowMeAnswered = false;
            WaitingForGate = false;
            WarpedToGate = false;
        }

        public virtual void AddMenuCallback(string title, Func<GsxMenu, Task> callback)
        {
            MenuCallbacks.TryAdd(title, callback);
        }

        public virtual void RemoveMenuCallback(string title)
        {
            MenuCallbacks.TryRemove(title, out _);
        }

        protected virtual async void OnMenuEvent(ISimResourceSubscription sub, object value)
        {
            try
            {
                if (!Controller.IsActive)
                    return;

                MenuState = (GsxMenuState)sub.GetValue<int>();
                Logger.Debug($"Received Menu Event: {MenuState}");

                if (!FirstReadyReceived && MenuState == GsxMenuState.READY)
                {
                    Logger.Debug($"First Menu Ready received");
                    FirstReadyReceived = true;
                }

                WaitingForGate = false;
                if (MenuState == GsxMenuState.READY)
                    await UpdateMenu();

                if (MenuState == GsxMenuState.READY)
                    Controller.MessageService.Send(MessageGsx.Create<MsgGsxMenuReady>(Controller, MenuState));
                else
                    Controller.MessageService.Send(MessageGsx.Create<MsgGsxMenuReceived>(Controller, MenuState));
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual async Task UpdateMenu()
        {
            string lastTitle = MenuTitle;
            MenuOpenAfterReady = false;
            if (File.Exists(PathMenu))
            {
                MenuLines.Clear();

                var fileLines = File.ReadAllLines(PathMenu).ToArray();
                var fileIndex = 0;
                MenuTitle = fileLines[fileIndex++];
                while (fileIndex < fileLines.Length)
                    MenuLines.Add(fileLines[fileIndex++]);
                Logger.Verbose($"Read {MenuLineCount} Lines");
            }
            else
                Logger.Warning($"GSX Menu files does not exist! ({PathMenu})");

            if (lastTitle != MenuTitle)
            {
                Logger.Debug($"Menu Title changed: '{MenuTitle}'");
                await TaskTools.RunLogged(() => MenuTitleChanged?.Invoke(MenuTitle), RequestToken);
                if (IsGateMenu)
                    FollowMeAnswered = false;

                if (DeIceQuestionAnswered && Config.AutoDeiceEnabled && !MatchTitle(GsxConstants.MenuDeiceType))
                {
                    Logger.Information($"Auto-deice diag: title changed after YES, did NOT match '{GsxConstants.MenuDeiceType}'. Title='{MenuTitle}', Lines={MenuLineCount}:");
                    for (int i = 0; i < MenuLines.Count; i++)
                        Logger.Information($"  [{i + 1}] {MenuLines[i]}");
                }

                // Phase 5 Part 3: passive menu snapshot. Legacy Select(N) paths
                // (OnTugQuestion, GsxController.ReloadSimbrief, etc.) bypass
                // the intent-execution log because the in-flight grace window
                // correctly attributes their writes as ours. This per-title-
                // change capture covers them — one diagnostic row per menu
                // transition, sufficient to unblock the deferred AnswerTugQuestion
                // and ReloadSimbrief intents once a turnaround flight runs.
                try
                {
                    var sb = new StringBuilder();
                    sb.Append("Title: '").Append(MenuTitle).Append("' (").Append(MenuLineCount).AppendLine(" entries)");
                    for (int i = 0; i < MenuLines.Count; i++)
                        sb.Append("  [").Append(i + 1).Append("] ").AppendLine(MenuLines[i]);
                    Controller.GsxMenuDiagnosticLog?.LogDiagnostic("menu-snapshot", sb.ToString());
                }
                catch (Exception ex) { Logger.LogException(ex); }
            }

            if (IsOperatorMenu && AircraftProfile.OperatorAutoSelect)
            {
                await SelectOperator();
                MenuOpenAfterReady = true;
            }

            var matchingCallbacks = MenuCallbacks.Where(c => MatchTitle(c.Key));
            foreach (var callback in matchingCallbacks)
                await TaskTools.RunLogged(() => callback.Value.Invoke(this), RequestToken);

            if (!SuppressMenuRefresh && MenuOpenAfterReady)
                _ = Task.Delay(1000, RequestToken).ContinueWith((_) => Open());
        }

        public virtual void Hide()
        {
            Logger.Debug($"Hide Menu");
            SubMenuEvent.WriteValue(2);
        }

        public virtual void Timeout()
        {
            Logger.Debug($"Timeout Menu");
            SubMenuEvent.WriteValue(3);
        }

        public virtual async Task<bool> Open(bool waitReady = false)
        {
            if (MenuOpenRequesting)
            {
                Logger.Debug($"Menu Open already requested - delaying ...");
                await Task.Delay(Config.MenuOpenTimeout, RequestToken);
            }
            MenuOpenRequesting = true;

            MsgGsxMenuReady msg = null;
            try
            {
                Logger.Debug($"Open Menu ...");
                MsgMenuReady.Clear();
                await SubMenuOpen.WriteValue(1);
                if (waitReady)
                {
                    msg = await MsgMenuReady.ReceiveAsync(false, Config.MenuOpenTimeout, RequestToken);
                    if (msg == null && !RequestToken.IsCancellationRequested)
                    {
                        Logger.Debug($"Retry Open ...");
                        await SubMenuOpen.WriteValue(1);
                        msg = await MsgMenuReady.ReceiveAsync(false, Config.MenuOpenTimeout, RequestToken);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            MenuOpenRequesting = false;

            return msg != null;
        }

        public virtual async Task<bool> OpenHide()
        {
            bool result = false;
            try
            {
                result = await Open(true);
                await Task.Delay(75);
                Hide();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            return result;
        }

        public virtual async Task Select(int number, bool waitReady = true, bool openMenu = false, int hide = 0)
        {
            if (openMenu && !IsMenuReady)
            {
                Logger.Verbose($"wait open");
                await Open(false);
            }

            if (waitReady && !IsMenuReady)
            {
                Logger.Verbose($"wait menu");
                await MsgMenuReady.ReceiveAsync(false, Config.MenuOpenTimeout, RequestToken);
            }

            // Mark a short window during which any inbound FSDT_GSX_MENU_CHOICE
            // change should be attributed to this write rather than to a user
            // click — see OnMenuSelection.
            _lastSelectInFlightUntil = DateTime.UtcNow + TimeSpan.FromSeconds(2);

            Logger.Debug($"Menu Select Item {number} => Value {number - 1}");
            await SubMenuChoice.WriteValue(number - 1);

            if (hide == 1)
                Hide();
            else if (hide == 2)
                await OpenHide();
        }

        public virtual async Task SelectOperator()
        {
            var gsxOperator = GsxOperator.OperatorSelection(AircraftProfile, MenuLines);
            if (gsxOperator != null)
            {
                Logger.Information($"Selecting Operator '{gsxOperator.Title}' (GSX Choice: {gsxOperator.GsxChoice})");
                await Select(gsxOperator.Number, false);
            }
            else
            {
                Logger.Warning($"Selecting Operator #1 - no Matches found");
                await Select(1, false);
            }
        }

        public virtual void FreeResources()
        {
            Controller.MsgCouatlStopped.OnMessage -= OnCouatlStopped;

            Controller.SimStore.Remove(GsxConstants.EventMenu).OnReceived -= OnMenuEvent;
            Controller.SimStore.Remove(GsxConstants.VarMenuOpen);
            SubMenuChoice.OnReceived -= OnMenuSelection;
            Controller.SimStore.Remove(GsxConstants.VarMenuChoice);
            Controller.ReceiverStore.Remove<MsgGsxMenuReady>();
        }

        public virtual bool MatchTitle(string match)
        {
            return MenuTitle?.StartsWith(match, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        /// <summary>
        /// Waits up to <paramref name="timeout"/> for the live menu to transition
        /// into an operator-selection picker (either handling or catering). Used
        /// by intent-migrated services after a service-request intent succeeds —
        /// the operator picker materialises asynchronously when GSX needs it,
        /// and the request intent's verify only confirms the LVAR state change,
        /// not the follow-up menu transition. Returns true when the operator
        /// menu is observed, false on timeout. Polls at <c>Config.MenuCheckInterval</c>.
        /// </summary>
        public virtual async Task<bool> WaitForOperatorMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            int waited = 0;
            int interval = Math.Max(50, Config.MenuCheckInterval);
            int budget = (int)timeout.TotalMilliseconds;
            while (waited < budget && !IsOperatorMenu)
            {
                if (token.IsCancellationRequested) return IsOperatorMenu;
                try { await Task.Delay(interval, token); }
                catch (OperationCanceledException) { return IsOperatorMenu; }
                waited += interval;
            }
            return IsOperatorMenu;
        }

        /// <summary>
        /// Waits for a human menu click on the open operator-selection picker,
        /// up to <c>Config.OperatorSelectTimeout</c>. On timeout, fires
        /// <see cref="Timeout"/>; on success, closes the menu via
        /// <see cref="OpenHide"/>. Returns true if a selection was observed.
        /// </summary>
        public virtual async Task<bool> WaitForManualOperatorSelectionAsync(CancellationToken token)
        {
            int waited = 0;
            int interval = Math.Max(50, Config.MenuCheckInterval);
            int budget = Config.OperatorSelectTimeout;
            LastMenuSelection = -2;
            Logger.Information($"Waiting for manual Operator Selection ... (Timeout {budget / 1000}s)");
            while (waited < budget && LastMenuSelection == -2)
            {
                if (token.IsCancellationRequested) return false;
                try { await Task.Delay(interval, token); }
                catch (OperationCanceledException) { return false; }
                waited += interval;
            }
            Logger.Debug($"Wait ended after {waited}ms - LastSelection {LastMenuSelection}");
            if (waited >= budget)
            {
                Timeout();
                return false;
            }
            await OpenHide();
            return true;
        }

        /// <summary>
        /// Resolves and executes a <see cref="GsxMenuIntent"/> against the live
        /// menu. The single entry point for menu interaction. The result is rich
        /// enough that callers don't need to consult any other state to know
        /// what happened, and the diagnostic log is emitted once per call
        /// (success or failure) at the single return path.
        /// </summary>
        public virtual async Task<GsxMenuResult> ExecuteIntent(GsxMenuIntent intent, AutomationState currentPhase, CancellationToken token = default)
        {
            if (intent == null) throw new ArgumentNullException(nameof(intent));

            // Phase 6.5.B: any UiMarshal.Post/PostBackground that fires while
            // this intent (or one of its callees) is executing is attributed
            // to "intent:{IntentName}" in the ResourceDiagnosticsWorker
            // top-N report. Nested intents (ParentMenu navigation) save and
            // restore the outer name, so attribution follows the call chain.
            using var _intentScope = UiMarshal.BeginIntentContext(intent.IntentName);

            var stopwatch = Stopwatch.StartNew();
            GsxMenuResult result;

            // Step 1: plausibility
            if (!intent.IsValidForPhase(currentPhase))
            {
                result = MakeResult(MenuOutcome.PhaseMismatch, intent, MenuTitle, Array.Empty<string>(),
                    null, null, currentPhase,
                    $"Phase {currentPhase} not valid for intent {intent.IntentName}",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            // Step 2: already-satisfied short-circuit
            if (intent.IsAlreadySatisfied(Controller))
            {
                result = MakeResult(MenuOutcome.StatePreconditionSatisfiedAlready, intent, MenuTitle, Array.Empty<string>(),
                    null, null, currentPhase,
                    $"Intent {intent.IntentName} reports already satisfied — no menu interaction needed",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            // Step 3: precondition
            if (!intent.ArePreconditionsSatisfied(Controller))
            {
                result = MakeResult(MenuOutcome.StatePreconditionFailed, intent, MenuTitle, Array.Empty<string>(),
                    null, null, currentPhase,
                    $"Preconditions for intent {intent.IntentName} not met",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            // Step 4: navigate to parent (recurse) or open at top level
            if (intent.ParentMenu != null)
            {
                var parent = await ExecuteIntent(intent.ParentMenu, currentPhase, token);
                if (!parent.IsSuccess && !parent.IsBenignSkip)
                {
                    result = MakeResult(MenuOutcome.NavigationFailed, intent, MenuTitle, Array.Empty<string>(),
                        null, null, currentPhase,
                        $"Parent navigation '{intent.ParentMenu.IntentName}' returned {parent.Outcome}: {parent.Reason}",
                        stopwatch);
                    EmitResult(result);
                    return result;
                }
            }
            else
            {
                bool opened = await Open(waitReady: true);
                if (!opened)
                {
                    result = MakeResult(MenuOutcome.NavigationFailed, intent, MenuTitle, Array.Empty<string>(),
                        null, null, currentPhase,
                        "Open(waitReady:true) returned false — menu did not become ready",
                        stopwatch);
                    EmitResult(result);
                    return result;
                }
            }

            // Step 5: defensive snapshot — every later check reads from snap*, never the live list.
            string snapTitle = MenuTitle;
            IReadOnlyList<string> snapLines = MenuLines.ToList();

            // Step 6: title check (multi-prefix; default impl wraps the single ExpectedMenuTitlePrefix)
            if (!MenuTitleMatchesAny(snapTitle, intent.ExpectedMenuTitlePrefixes))
            {
                string prefixes = string.Join(" | ", intent.ExpectedMenuTitlePrefixes ?? Array.Empty<string>());
                result = MakeResult(MenuOutcome.MenuTitleMismatch, intent, snapTitle, snapLines,
                    null, null, currentPhase,
                    $"Live menu title '{snapTitle}' does not match any of: [{prefixes}]",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            // Step 7: door-action-prompt guard — must run BEFORE resolution.
            int doorIdx = FindDoorActionPromptIndex(snapLines);
            if (doorIdx >= 0)
            {
                result = MakeResult(MenuOutcome.DoorActionPrompt, intent, snapTitle, snapLines,
                    null, null, currentPhase,
                    $"Menu shows door-action prompt at entry {doorIdx + 1}: '{snapLines[doorIdx]}'",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            // Step 8: resolve
            int idx;
            try
            {
                idx = intent.ResolveMenuLineIndex(snapLines);
            }
            catch (AmbiguousMatchException amx)
            {
                result = MakeResult(MenuOutcome.AmbiguousMatch, intent, snapTitle, snapLines,
                    null, null, currentPhase,
                    amx.Message,
                    stopwatch,
                    exception: amx);
                EmitResult(result);
                return result;
            }

            if (idx < 0)
            {
                // Navigation-intent special case: a top-level intent whose
                // ResolveMenuLineIndex returns -1 and whose live title already
                // matches the expected prefix is treated as success without a
                // write — the Open() in step 4 IS the operation. This is the
                // pattern OpenGateMenu uses; gating it on ParentMenu == null
                // keeps non-navigation intents from accidentally claiming
                // success when their pattern fails to match.
                if (intent.ParentMenu == null && MenuTitleMatchesAny(snapTitle, intent.ExpectedMenuTitlePrefixes))
                {
                    result = MakeResult(MenuOutcome.Success, intent, snapTitle, snapLines,
                        null, null, currentPhase,
                        "Navigation-only intent: live menu is the expected destination; no choice written",
                        stopwatch);
                    EmitResult(result);
                    return result;
                }

                result = MakeResult(MenuOutcome.ItemNotAvailable, intent, snapTitle, snapLines,
                    null, null, currentPhase,
                    $"No menu line matched intent {intent.IntentName}",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            string matchedText = snapLines[idx];

            // Step 9: write choice — Select(N) writes N-1, so the 1-based form is idx+1.
            try
            {
                await Select(idx + 1, waitReady: false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = MakeResult(MenuOutcome.GsxError, intent, snapTitle, snapLines,
                    idx, matchedText, currentPhase,
                    $"Select threw during write: {ex.Message}",
                    stopwatch,
                    exception: ex);
                EmitResult(result);
                return result;
            }

            // Step 10: verify
            bool verified;
            try
            {
                verified = await intent.VerifyOutcomeAsync(Controller,
                    TimeSpan.FromMilliseconds(Config.IntentVerificationTimeout), token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = MakeResult(MenuOutcome.GsxError, intent, snapTitle, snapLines,
                    idx, matchedText, currentPhase,
                    $"VerifyOutcomeAsync threw: {ex.Message}",
                    stopwatch,
                    exception: ex);
                EmitResult(result);
                return result;
            }

            if (!verified)
            {
                result = MakeResult(MenuOutcome.GsxNoResponse, intent, snapTitle, snapLines,
                    idx, matchedText, currentPhase,
                    $"Wrote choice {idx + 1} ('{matchedText}') but verification did not observe the expected change within {Config.IntentVerificationTimeout}ms",
                    stopwatch);
                EmitResult(result);
                return result;
            }

            // Step 11: success
            result = MakeResult(MenuOutcome.Success, intent, snapTitle, snapLines,
                idx, matchedText, currentPhase,
                $"Resolved '{matchedText}' at entry {idx + 1}; wrote and verified",
                stopwatch);
            EmitResult(result);
            return result;
        }

        private static GsxMenuResult MakeResult(
            MenuOutcome outcome,
            GsxMenuIntent intent,
            string title,
            IReadOnlyList<string> lines,
            int? resolvedIndex,
            string matchedEntryText,
            AutomationState phase,
            string reason,
            Stopwatch stopwatch,
            Exception exception = null,
            string postWriteStateObservation = null)
        {
            return new GsxMenuResult(
                outcome,
                intent,
                title,
                lines,
                resolvedIndex,
                matchedEntryText,
                phase,
                reason,
                exception,
                stopwatch?.Elapsed,
                postWriteStateObservation);
        }

        private void EmitResult(GsxMenuResult result)
        {
            try { Controller.GsxMenuDiagnosticLog?.LogIntentExecution(result); }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        private static bool MenuTitleMatchesAny(string title, IReadOnlyList<string> prefixes)
        {
            if (string.IsNullOrWhiteSpace(title) || prefixes == null || prefixes.Count == 0)
                return false;
            for (int i = 0; i < prefixes.Count; i++)
            {
                var p = prefixes[i];
                if (!string.IsNullOrWhiteSpace(p) && title.StartsWith(p, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private static int FindDoorActionPromptIndex(IReadOnlyList<string> lines)
        {
            const string prefix = "Waiting for your action:";
            if (lines == null) return -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (!string.IsNullOrEmpty(line)
                    && line.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}
