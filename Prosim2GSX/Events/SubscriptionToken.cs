using System;

namespace Prosim2GSX.Events
{
    public class SubscriptionToken : IEquatable<SubscriptionToken>
    {
        private readonly Guid _token;

        public SubscriptionToken()
        {
            _token = Guid.NewGuid();
        }

        public bool Equals(SubscriptionToken other)
        {
            if (other is null) return false;
            return _token.Equals(other._token);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SubscriptionToken);
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }
    }
}
