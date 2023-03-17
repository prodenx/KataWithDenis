using System;
namespace KataWithDenis.Domain
{
    public class InspectionSummary
    {
        public InspectionSummary()
        { 
        }

        public Guid Id { get; set; }

        public string State { get; set; }

        public int SampleSize { get; set; }

        public int TotalInspections { get; set; }
    }

    public static class InspectionSummaryState
    {
        public const string New = "New";
        public const string Running = "Running";
        public const string Completed = "Completed";
    }
}

