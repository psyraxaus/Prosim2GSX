using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the Prosim EFB loadsheet datarefs
    // (efb.prelimLoadsheet, efb.finalLoadsheet). Owned by AppService for the
    // app's lifetime; populated by LoadsheetService each StateUpdateWorker
    // tick. Equality-based compare-and-skip in the [ObservableProperty]
    // setters keeps WS broadcasts cheap (INPC only fires on actual change).
    //
    // Two parallel slots — Prelim and Final — mirror the EFB's two-stage
    // loadsheet flow: a preliminary loadsheet generated when the OFP is
    // accepted, then a final loadsheet generated before pushback. The
    // Status field tracks lifecycle (pending → received | error) and the
    // Type field is constant per slot ("prelim" | "final" | "none" when
    // reset). MacTowError is computed against MinMacTow/MaxMacTow on each
    // receipt.
    public partial class LoadsheetState : ObservableObject
    {
        // Prelim slot. macZfw + zfw mirror the loadsheet's pre-fuel CG &
        // weight; macTow + tow mirror the take-off snapshot. Both pairs are
        // surfaced so the W&B page can show a "loadsheet" row alongside the
        // live aircraft datarefs (the live values can drift after the
        // loadsheet is signed if pax/cargo move).
        [ObservableProperty] private string _PrelimType = "none";
        [ObservableProperty] private string _PrelimStatus = "pending";
        [ObservableProperty] private double _PrelimMacTow;
        [ObservableProperty] private bool _PrelimMacTowError;
        [ObservableProperty] private double _PrelimTowKg;
        [ObservableProperty] private double _PrelimMacZfw;
        [ObservableProperty] private double _PrelimZfwKg;
        [ObservableProperty] private string _PrelimLoadsheetIdent = "";
        [ObservableProperty] private string _PrelimRawJson = "";
        [ObservableProperty] private DateTime? _PrelimReceivedAt;

        // Final slot.
        [ObservableProperty] private string _FinalType = "none";
        [ObservableProperty] private string _FinalStatus = "pending";
        [ObservableProperty] private double _FinalMacTow;
        [ObservableProperty] private bool _FinalMacTowError;
        [ObservableProperty] private double _FinalTowKg;
        [ObservableProperty] private double _FinalMacZfw;
        [ObservableProperty] private double _FinalZfwKg;
        [ObservableProperty] private string _FinalLoadsheetIdent = "";
        [ObservableProperty] private string _FinalRawJson = "";
        [ObservableProperty] private DateTime? _FinalReceivedAt;

        // A320-family MAC% envelope. Sourced from the Prosim EFB's
        // config.limits initialiser (minMacTow: 10.5, maxMacTow: 45). Public
        // so LoadsheetDto can project them onto the wire without reflection.
        public double MinMacTow => 10.5;
        public double MaxMacTow => 45.0;
    }
}
