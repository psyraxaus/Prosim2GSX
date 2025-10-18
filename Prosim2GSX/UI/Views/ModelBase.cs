using CFIT.AppFramework.UI.ViewModels;
using CFIT.SimConnectLib;
using Prosim2GSX.Aircraft;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using Prosim2GSX.GSX;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Prosim2GSX.UI.Views
{
    public abstract partial class ModelBase<TObject>(TObject source, AppService appService) : ViewModelBase<TObject>(source) where TObject : class
    {
        protected virtual AppService AppService { get; } = appService;
        protected virtual Config Config => AppService.Config;
        protected virtual SimConnectController SimConnectController => AppService.SimService.Controller;
        protected virtual SimConnectManager SimConnect => AppService.SimConnect;
        protected virtual GsxController GsxController => AppService.GsxService;
        protected virtual AudioController AudioController => AppService.AudioService;
        protected virtual AircraftInterface AircraftInterface => GsxController?.AircraftInterface;
        protected virtual AircraftProfile AircraftProfile => GsxController?.AircraftProfile;
        public virtual bool InhibitConfigSave { get; set; } = false;

        public virtual void SaveConfig()
        {
            if (!InhibitConfigSave)
                Config.SaveConfiguration();
        }

        public virtual void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceValue(value, validator, callback, propertyName);
            SaveConfig();
        }
    }
}
