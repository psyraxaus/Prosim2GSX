using System.Threading.Tasks;

namespace Prosim2GSX.SayIntentions
{
    public interface ISayIntentionsService
    {
        bool IsActive { get; }
        Task<bool> AssignGateAsync(string airportIcao, string gate);
    }
}
