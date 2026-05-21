namespace Prosim2GSX.Web.Contracts
{
    // One suggestion item for the Audio mapping autocomplete. Sourced from
    // AudioSessionRegistry — only processes that currently own a CoreAudio
    // session are surfaced, so the dropdown shows controllable apps only.
    public class AudioSessionSuggestionDto
    {
        public string ProcessName { get; set; }
        public bool IsAccessible { get; set; }
    }
}
