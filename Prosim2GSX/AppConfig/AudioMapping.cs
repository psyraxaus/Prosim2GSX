using Prosim2GSX.Audio;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Prosim2GSX.AppConfig
{
    public class AudioMapping : IComparable<AudioMapping>, INotifyPropertyChanged
    {
        public virtual AudioChannel Channel { get; set; }
        public virtual string Device { get; set; }
        public virtual string Binary { get; set; }
        public virtual bool UseLatch { get; set; }
        public virtual bool OnlyActive { get; set; } = true;

        public AudioMapping() { }

        public AudioMapping(AudioChannel channel, string device, string binary, bool useLatch = true, bool onlyActive = true)
        {
            Channel = channel;
            Device = device;
            Binary = binary;
            UseLatch = useLatch;
            OnlyActive = onlyActive;
        }

        [JsonIgnore]
        public virtual string DeviceName { get => string.IsNullOrWhiteSpace(Device) ? "All" : Device; set { Device = string.IsNullOrWhiteSpace(value) || value == "All" ? "" : value; } }

        // Runtime-only diagnostic surfaced in the UI when an AudioSession is
        // skipped (e.g. the mapped binary runs at higher integrity than us).
        // Not persisted.
        private string _status;
        [JsonIgnore]
        public virtual string Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasStatus));
            }
        }

        [JsonIgnore]
        public virtual bool HasStatus => !string.IsNullOrEmpty(_status);

        public int CompareTo(AudioMapping? other)
        {
            return Channel.CompareTo(other.Channel);
        }

        public override string ToString()
        {
            return $"Channel: {Channel} - Binary '{Binary}' @ Device '{DeviceName}' (UseLatch: {UseLatch} | OnlyActive: {OnlyActive})";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
