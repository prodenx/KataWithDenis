using System;
namespace KataWithDenis.App
{
    public class EventStore
    {
        private HashSet<InspectionEvent> _events;

        public EventStore()
        {
            _events = new HashSet<InspectionEvent>();
        }

        public void Add(InspectionEvent @event)
        {
            _events.Add(@event);
        }

        public void AddRange(ICollection<InspectionEvent> events)
        {
            _events.UnionWith(events);
        }

        public EventStream GetStream(Guid id)
        {
            return new EventStream(
                id,
                _events
                    .Where(i => i.StreamId == id)
                    .OrderBy(i => i.Version)
                    .ToList());
        }

        public FamilyOfStreams GetStreamFamily()
        {
            return new FamilyOfStreams(_events);
        }
    }
}

