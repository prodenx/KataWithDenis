using System;
using KataWithDenis;
using KataWithDenis.App;
using KataWithDenis.Domain;

namespace Tests
{
    public class ProjectionUnitTests
    {
        [Fact]
        public async Task Dashboard_Should_Contain_Summary_of_one_Inspection()
        {
            EventStore eventStore = new EventStore();

            var inspection1Id = Guid.NewGuid();

            eventStore.Add(new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Created,
                Version = 1,
                Created = new InspectionCreated(inspection1Id, 10),
                StreamId = inspection1Id
            });

            eventStore.Add(new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Completed,
                Version = 2,
                Completed = new InspectionCompleted(),
                StreamId = inspection1Id
            });

            var inspection2Id = Guid.NewGuid();

            eventStore.Add(new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Created,
                Version = 1,
                Created = new InspectionCreated(inspection2Id, 10),
                StreamId = inspection2Id
            });

            var inspection1SummaryProjector = new InspectionSummaryProjector(eventStore.GetStreamFamily());

            InspectionSummary inspection1Summary = await inspection1SummaryProjector.GetSummary(inspection1Id);

            Assert.Equal(inspection1Id, inspection1Summary.Id);
            Assert.Equal(InspectionSummaryState.Completed, inspection1Summary.State);

            var inspection2SummaryProjector = new InspectionSummaryProjector(eventStore.GetStreamFamily());

            InspectionSummary inspection2Summary = await inspection2SummaryProjector.GetSummary(inspection2Id);

            Assert.Equal(inspection2Id, inspection2Summary.Id);
            Assert.Equal(InspectionSummaryState.Running, inspection2Summary.State);
        }

        [Fact]
        public async Task Dashboard_Should_Contain_Total_Number_Of_Inspections()
        {
            EventStore eventStore = new EventStore();

            var inspection1Id = Guid.NewGuid();

            eventStore.Add(new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Created,
                Version = 1,
                Created = new InspectionCreated(inspection1Id, 10),
                StreamId = inspection1Id
            });

            eventStore.Add(new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Completed,
                Version = 2,
                Completed = new InspectionCompleted(),
                StreamId = inspection1Id
            });

            var inspection1SummaryProjector = new InspectionSummaryProjector(eventStore.GetStreamFamily());

            InspectionSummary inspection1Summary = await inspection1SummaryProjector.GetSummary(inspection1Id);

            Assert.Equal(1, inspection1Summary.TotalInspections);

            var inspection2Id = Guid.NewGuid();

            eventStore.Add(new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Created,
                Version = 1,
                Created = new InspectionCreated(inspection2Id, 10),
                StreamId = inspection2Id
            });

            InspectionSummary inspection1SummaryNew = await inspection1SummaryProjector.GetSummary(inspection1Id);

            Assert.Equal(2, inspection1SummaryNew.TotalInspections);

            var inspection2SummaryProjector = new InspectionSummaryProjector(eventStore.GetStreamFamily());

            InspectionSummary inspection2Summary = await inspection2SummaryProjector.GetSummary(inspection2Id);

            Assert.Equal(2, inspection2Summary.TotalInspections);
        }
    }
}

