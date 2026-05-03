using Prosim2GSX.Audio;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Prosim2GSX.AppConfig
{
    // ACP channel → VoiceMeeter strip/bus binding, used when
    // Config.UseVoiceMeeter is true. Independent of AudioMapping —
    // VoiceMeeter routing is per-mixer-target, not per-process.
    public class VoiceMeeterMapping : IComparable<VoiceMeeterMapping>, INotifyPropertyChanged
    {
        private AudioChannel _channel;
        private int _stripIndex;
        private bool _isBus;
        private bool _useLatch = true;

        public virtual AudioChannel Channel
        {
            get => _channel;
            set { if (_channel == value) return; _channel = value; RaisePropertyChanged(); }
        }

        public virtual int StripIndex
        {
            get => _stripIndex;
            set { if (_stripIndex == value) return; _stripIndex = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TargetKey)); }
        }

        public virtual bool IsBus
        {
            get => _isBus;
            set { if (_isBus == value) return; _isBus = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TargetKey)); }
        }

        public virtual bool UseLatch
        {
            get => _useLatch;
            set { if (_useLatch == value) return; _useLatch = value; RaisePropertyChanged(); }
        }

        public VoiceMeeterMapping() { }

        public VoiceMeeterMapping(AudioChannel channel, int stripIndex, bool isBus = false, bool useLatch = true)
        {
            _channel = channel;
            _stripIndex = stripIndex;
            _isBus = isBus;
            _useLatch = useLatch;
        }

        // Combined "strip:N" / "bus:N" key for the WPF DataGrid combo column
        // (SelectedValuePath="Key"). Same encoding the Web UI uses.
        [JsonIgnore]
        public virtual string TargetKey
        {
            get => $"{(IsBus ? "bus" : "strip")}:{StripIndex.ToString(CultureInfo.InvariantCulture)}";
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                int colon = value.IndexOf(':');
                if (colon <= 0) return;
                bool isBus = value.Substring(0, colon).Equals("bus", StringComparison.OrdinalIgnoreCase);
                if (!int.TryParse(value.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)) return;
                IsBus = isBus;
                StripIndex = idx;
            }
        }

        // 1-based label for UI display ("Strip 1", "Bus 2"). Matches the
        // VoiceMeeterStrip.DisplayName head — VoiceMeeter itself uses 1-based
        // numbering on its UI even though the API takes 0-based indices.
        [JsonIgnore]
        public virtual string DisplayName => $"{(IsBus ? "Bus" : "Strip")} {StripIndex + 1}";

        public int CompareTo(VoiceMeeterMapping? other) => Channel.CompareTo(other.Channel);

        public override string ToString()
            => $"Channel: {Channel} - {(IsBus ? "Bus" : "Strip")}[{StripIndex}] (UseLatch: {UseLatch})";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
