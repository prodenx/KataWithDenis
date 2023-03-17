using System;
using System.Collections;

namespace KataWithDenis.App
{
    public class FamilyOfStreams: IEnumerable<InspectionEvent>
    {
        private HashSet<InspectionEvent> _events;

        public FamilyOfStreams(HashSet<InspectionEvent> events)
        {
            _events = events;
        }

        public IEnumerator<InspectionEvent> GetEnumerator()
        {
            return _events.GetEnumerator();
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

        public ICollection<Guid> GetAllUniqueIds()
        {
            return _events.GroupBy(i => i.StreamId).Select(i => i.Key).ToList();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _events.GetEnumerator();
        }
    }
}

