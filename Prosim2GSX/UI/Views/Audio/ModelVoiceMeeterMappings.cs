using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Prosim2GSX.UI.Views.Audio
{
    public partial class ModelVoiceMeeterMappings(ModelAudio modelAudio)
        : ViewModelCollection<VoiceMeeterMapping, VoiceMeeterMapping>(modelAudio.Source.VoiceMeeterMappings, (s) => s, (s) => s != null)
    {
        protected virtual ModelAudio ModelAudio { get; } = modelAudio;
        public override ICollection<VoiceMeeterMapping> Source => ModelAudio.Source.VoiceMeeterMappings;

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();
            CreateMemberBinding<AudioChannel, AudioChannel>(nameof(VoiceMeeterMapping.Channel), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(VoiceMeeterMapping.TargetKey), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(VoiceMeeterMapping.UseLatch), new NoneConverter());
        }

        public override bool UpdateSource(VoiceMeeterMapping oldItem, VoiceMeeterMapping newItem)
        {
            try
            {
                oldItem.Channel = newItem.Channel;
                oldItem.StripIndex = newItem.StripIndex;
                oldItem.IsBus = newItem.IsBus;
                oldItem.UseLatch = newItem.UseLatch;
                return true;
            }
            catch { }
            return false;
        }

        public override void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e = null)
        {
            ModelAudio.Source.VoiceMeeterMappings.Sort();
            base.NotifyCollectionChanged(e);
        }
    }
}
