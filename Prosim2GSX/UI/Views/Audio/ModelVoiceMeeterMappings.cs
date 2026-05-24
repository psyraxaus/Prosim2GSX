using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Prosim2GSX.UI.Views.Audio
{
    // Per-ACP wrapper around Config.VoiceMeeterMappingsByAcp[Acp]. One
    // instance per active ACP (CPT/FO/OBS); ModelAudio owns all three and
    // the WPF tab binds each to its own DataGrid card.
    public partial class ModelVoiceMeeterMappings(ModelAudio modelAudio, AcpSide acp)
        : ViewModelCollection<VoiceMeeterMapping, VoiceMeeterMapping>(GetOrCreateBucket(modelAudio.Source, acp), (s) => s, (s) => s != null)
    {
        protected virtual ModelAudio ModelAudio { get; } = modelAudio;
        public virtual AcpSide Acp { get; } = acp;
        public override ICollection<VoiceMeeterMapping> Source => GetOrCreateBucket(ModelAudio.Source, Acp);

        // Returns the list for this ACP, creating an empty one in the dict
        // if absent. Ensures the ViewModelCollection always has a live target
        // even for ACPs the user just enabled for the first time.
        private static List<VoiceMeeterMapping> GetOrCreateBucket(Config cfg, AcpSide acp)
        {
            if (cfg.VoiceMeeterMappingsByAcp == null)
                cfg.VoiceMeeterMappingsByAcp = new Dictionary<AcpSide, List<VoiceMeeterMapping>>();
            if (!cfg.VoiceMeeterMappingsByAcp.TryGetValue(acp, out var list) || list == null)
            {
                list = new List<VoiceMeeterMapping>();
                cfg.VoiceMeeterMappingsByAcp[acp] = list;
            }
            return list;
        }

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
            GetOrCreateBucket(ModelAudio.Source, Acp).Sort();
            base.NotifyCollectionChanged(e);
        }
    }
}
