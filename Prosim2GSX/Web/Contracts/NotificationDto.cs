using Prosim2GSX.State;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Web.Contracts
{
    // Wire-shape mirror of State.Notification. Kept as a separate type so
    // any future server-only fields on Notification (channel routing,
    // origin metadata, etc.) don't leak onto the wire.
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "";
        public string Severity { get; set; } = "info";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool Dismissed { get; set; }

        public static NotificationDto From(Notification n) => new()
        {
            Id = n.Id,
            Type = n.Type ?? "",
            Severity = n.Severity ?? "info",
            Message = n.Message ?? "",
            Timestamp = n.Timestamp,
            Dismissed = n.Dismissed,
        };
    }

    // Combined snapshot — the WS broadcast and GET endpoint both ride on
    // this shape so the React reducer's "set" branch and the panel's
    // initial fetch share one type.
    public class NotificationsSnapshotDto
    {
        public List<NotificationDto> Items { get; set; } = new();

        public static NotificationsSnapshotDto From(AppService app)
        {
            var src = app?.Notifications?.Items ?? new List<Notification>();
            return new NotificationsSnapshotDto
            {
                Items = src.Select(NotificationDto.From).ToList(),
            };
        }
    }
}
