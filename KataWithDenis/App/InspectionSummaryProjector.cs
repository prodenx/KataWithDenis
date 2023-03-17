using System;
using KataWithDenis.Domain;
using Microsoft.Extensions.Logging;

namespace KataWithDenis.App
{
    public class InspectionSummaryProjector
    {
        private FamilyOfStreams _streamFamily;

        public InspectionSummaryProjector(
            FamilyOfStreams streamFamily)
        {
            _streamFamily = streamFamily;
        }

        public async Task<InspectionSummary> GetSummary(Guid inspectionId)
        {
            var summary = new InspectionSummary()
            {
                Id = inspectionId,
            };

            var inspectionStream = _streamFamily.GetStream(inspectionId);

            if (inspectionStream.Events.Any(i => i.Type == InspectionEventType.Completed))
            {
                summary.State = InspectionSummaryState.Completed;
            }
            else
            {
                summary.State = InspectionSummaryState.Running;
            }

            summary.TotalInspections = _streamFamily
                .Count(i => i.Type == InspectionEventType.Created);

            return summary;
        }
    }
}

