using System;
namespace KataWithDenis.App
{
    public class EventStream
    {
        public List<InspectionEvent> Events { get; }

        public Guid Id { get; }

        public EventStream(
            Guid id,
            List<InspectionEvent> events)
        {
            Id = id;
            Events = events;
        }
    }
}

