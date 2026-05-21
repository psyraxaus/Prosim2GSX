using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.State
{
    // Long-lived observable store for transient operational notifications
    // (loadsheet timing alerts in Phase 1, future surfaces can reuse). The
    // list is replaced wholesale on every mutation so [ObservableProperty]
    // INPC fires once per change — same pattern the override dicts in
    // EfbFlightPlanState use, lets the WS handler broadcast a single
    // snapshot per change.
    //
    // Sized cap with FIFO eviction so a long session can't accumulate
    // unbounded entries; matches the bounded-log idea used elsewhere.
    public partial class NotificationsState : ObservableObject
    {
        public const int MaxItems = 50;

        [ObservableProperty] private List<Notification> _Items = new();

        public virtual void Add(Notification n)
        {
            if (n == null) return;
            var next = new List<Notification>(Items) { n };
            if (next.Count > MaxItems)
                next.RemoveRange(0, next.Count - MaxItems);
            Items = next;
        }

        public virtual void Dismiss(Guid id)
        {
            var idx = Items.FindIndex(x => x.Id == id);
            if (idx < 0) return;
            if (Items[idx].Dismissed) return;

            var next = new List<Notification>(Items);
            next[idx] = next[idx] with { Dismissed = true };
            Items = next;
        }

        public virtual void Clear()
        {
            if (Items.Count == 0) return;
            Items = new List<Notification>();
        }
    }

    // Single notification entry. Record so the dismiss path can use a
    // with-expression to flip Dismissed without mutating the existing
    // instance (the wire-side serialisation might be reading concurrently).
    public record Notification
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Type { get; init; } = "";
        public string Severity { get; init; } = "info"; // "info" | "warning" | "error"
        public string Message { get; init; } = "";
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public bool Dismissed { get; init; }
    }
}
