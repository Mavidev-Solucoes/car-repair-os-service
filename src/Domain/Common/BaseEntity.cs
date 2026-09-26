namespace Domain.Common;

public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public Guid? CreatedUserId { get; private set; }
    public Guid? LastUpdatedUserId { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void SetCreatedBy(Guid? userId) => CreatedUserId = userId;

    protected void Touch(Guid? userId = null)
    {
        UpdatedAt = DateTime.UtcNow;
        LastUpdatedUserId = userId;
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
