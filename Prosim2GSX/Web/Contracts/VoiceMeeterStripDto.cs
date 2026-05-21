namespace Prosim2GSX.Web.Contracts
{
    // One row in the Audio mapping VoiceMeeter strip combo. DisplayName is
    // formatted server-side ("Strip 1 — Hardware Input 1") so the web UI
    // doesn't need to know about VM-type-specific naming. Key matches the
    // server's encoded "strip:N" / "bus:N" form used by AudioMapping.VoiceMeeterKey.
    public class VoiceMeeterStripDto
    {
        public int Index { get; set; }
        public bool IsBus { get; set; }
        public string Label { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Key => $"{(IsBus ? "bus" : "strip")}:{Index}";
    }
}
