using System;

namespace Prosim2GSX.Events
{
    public interface IEventAggregator
    {
        void Publish<TEvent>(TEvent eventToPublish) where TEvent : EventBase;
        SubscriptionToken Subscribe<TEvent>(Action<TEvent> action) where TEvent : EventBase;
        void Unsubscribe<TEvent>(SubscriptionToken token) where TEvent : EventBase;
    }
}
