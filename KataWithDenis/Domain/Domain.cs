using System;
using KataWithDenis.App;
using Microsoft.Extensions.Logging;

namespace KataWithDenis
{
    public class SigninResult
    {
        public SigninResult(
            string? sessionId,
            bool isError,
            string? errorMessage)
        {
            SessionId = sessionId;
            IsError = isError;
            ErrorMessage = errorMessage;
        }

        public string SessionId { get; }

        public bool IsError { get; }

        public string ErrorMessage { get; }

        static readonly public SigninResult BadInput = new SigninResult(null, true, "Bad input");

        static readonly public SigninResult UserNotFound = new SigninResult(null, true, "User not found");

        static public SigninResult Success(string sessionId) => new SigninResult(sessionId, false, null);
    }

    public class Dashboard
    {
        private List<Inspection> _inspections = new List<Inspection>();
        public List<Inspection> Inspections => _inspections.ToList();

        private List<Invoice> Invoices = new List<Invoice>();

        public Dashboard(
            IEnumerable<Inspection> inspections,
            IEnumerable<Invoice> invoices)
        {
            _inspections.AddRange(inspections);
            Invoices.AddRange(invoices);
        }

        public List<Invoice> GetInvoices()
        {
            return Invoices.ToList();
        }

    }

    public class UserRepository
    {
        private readonly List<Guid> _users = new List<Guid>()
        {
            AppConstants.AdminId
        };

        public async Task Add(Guid userId)
        {
            _users.Add(userId);
        }

        public async Task<bool> CheckIfUserIdExists(Guid userId)
        {
            return _users.Contains(userId);
        }
    }

    public class Inspection
    {
        private readonly List<InspectionEvent> _events = new List<InspectionEvent>();
        public List<InspectionEvent> Events => _events.ToList();

        private int _version = -1;

        private Inspection()
        {

        }

        public Inspection(
            Guid id,
            int sampleSize)
        {
            Id = id;
            SampleSize = sampleSize;

            var createdEvent = new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Created,
                Version = ++_version,
                Created = new InspectionCreated(id, sampleSize)
            };

            Apply(createdEvent);

            _events.Add(createdEvent);
        }

        public Guid Id { get; }

        public int SampleSize { get; }

        public bool IsCompleted { get; protected set; }

        public void Complete()
        {
            var @event = new InspectionEvent()
            {
                Id = Guid.NewGuid(),
                Type = InspectionEventType.Completed,
                Completed = new InspectionCompleted(),
                Version = ++_version
            };

            Apply(@event);

            _events.Add(@event);
        }

        public static Inspection Replay(List<InspectionEvent> eventStream)
        {
            Inspection inspection = null;

            foreach (var @event in eventStream)
            {
                if (@event.Type == InspectionEventType.Created)
                {
                    var inspectionCreatedEvent = @event.Created;

                    inspection = new Inspection(
                        inspectionCreatedEvent.Id,
                        inspectionCreatedEvent.SampleSize);
                }

                if (@event.Type == InspectionEventType.Completed)
                {
                    var inspectionCreatedEvent = @event.Completed;

                    inspection.Complete();
                }

                inspection._version = @event.Version;
            }

            return inspection;
        }

        private void Apply(InspectionEvent @event)
        {
            if (@event.Type == InspectionEventType.Created)
            {
                IsCompleted = false;

                return;
            }

            if (@event.Type == InspectionEventType.Completed)
            {
                IsCompleted = true;

                return;
            }

            throw new Exception();
        }
    }

    public class InspectionEventType
    {
        public const string Created = "Created";
        public const string Completed = "Completed";
    }

    public class InspectionEvent
    {
        public InspectionEvent()
        { }

        public Guid StreamId { get; set; }

        public Guid Id { get; set; }

        public string Type { get; set; }

        public int Version { get; set; }

        public InspectionCreated Created { get; set; }

        public InspectionCompleted Completed { get; set; }
    }

    public sealed record InspectionCreated ( Guid Id, int SampleSize );

    public sealed record InspectionCompleted();

    public class Invoice
    {
        public Guid Id { get; }

        public Guid InspectionId { get; }

        public Invoice(Guid inspectionId)
        {
            Id = Guid.NewGuid();
            InspectionId = inspectionId;
        }
    }

    public class CompleteInspectionCommand
    {
        private readonly Guid _inspectionId;

        public CompleteInspectionCommand(Guid inspectionId)
        {
            _inspectionId = inspectionId;
        }

        public async Task Run(
            InspectionRepository inspectionRepository,
            InvoiceRepository invoiceRepository,
            EventBasedInfrastructure<InspectionEvent> inspectionInfra,
            StateBasedInfrastructure<Invoice> invoiceInfra)
        {
            var inspection = await inspectionRepository.FindById(_inspectionId);

            if (inspection is null)
            {
                throw new Exception();
            }

            inspection.Complete();

            await inspectionInfra.Persist();
        }
    }

    public class InspectionRepository : IPersistableEventStream<InspectionEvent>
    {
        private readonly EventStore _eventStore;

        public InspectionRepository(EventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public void AddRange(ICollection<Inspection> inspections)
        {
            _eventStore.AddRange(inspections.SelectMany(i => i.Events).ToList());
        }

        public async Task Add(Inspection inspection)
        {
            _eventStore.AddRange(inspection.Events);
        }

        public async Task<Inspection> FindById(Guid id)
        {
            var stream =_eventStore.GetStream(id);
            return Inspection.Replay(stream.Events);
        }

        public FamilyOfStreams GetStreams()
        {
            return _eventStore.GetStreamFamily();
        }

        //public void Replay(List<List<InspectionEvent>> eventStreams)
        //{
        //    foreach (var eventStream in eventStreams)
        //    {
        //        _inspections.Add(Inspection.Replay(eventStream));
        //    }
        //}
    }

    public class UninvoicedInspectionsProjector
    {
        private List<Inspection> _inspections;

        private InspectionRepository _inspectionRepository;
        private InvoiceRepository _invoiceRepository;

        public UninvoicedInspectionsProjector(
            InspectionRepository inspectionRepository,
            InvoiceRepository invoiceRepository)
        {
            _inspectionRepository = inspectionRepository;
            _invoiceRepository = invoiceRepository;

            _inspections = new List<Inspection>();
        }

        public List<Inspection> GetList()
        {
            return _inspections.ToList();
        }
    }

    public class InvoiceRepository: IPersistable<Invoice>
    {
        private readonly List<Invoice> _invoices = new List<Invoice>();

        public void AddRange(ICollection<Invoice> invoices)
        {
            _invoices.AddRange(invoices);
        }

        public async Task Add(Invoice inspection)
        {
            _invoices.Add(inspection);
        }

        public async Task<List<Invoice>> GetAll()
        {
            return _invoices.ToList();
        }

        public async Task<Invoice> FindById(Guid id)
        {
            return _invoices.FirstOrDefault(i => i.Id == id);
        }
    }

    public interface IPersistable<T>
    {
        public void AddRange(ICollection<T> invoices);

        public Task<List<T>> GetAll();
    }

    public interface IPersistableEventStream<T>
    {
        public FamilyOfStreams GetStreams();

        //public void Replay(List<List<T>> eventStreams);
    }
}

