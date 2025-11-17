using StereoDB;
using StereoDB.CSharp;

namespace Presentation.Console.EventSourcing;

public interface IDatabase
{
    IStereoDb<EventSchema> DatabaseAccessor { get; }
}

public sealed class Database : IDatabase
{
    public IStereoDb<EventSchema> DatabaseAccessor { get; } = StereoDb.Create(new EventSchema());
}

public sealed class EventSchema
{
    public ITable<Guid, IEvent<Guid, Guid>> EventsTable { get; } = StereoDb.CreateTable<Guid, IEvent<Guid, Guid>>();
}