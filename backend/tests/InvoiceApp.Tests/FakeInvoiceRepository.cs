using InvoiceApp.Domain;

namespace InvoiceApp.Tests;

internal class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> _invoices = new();
    private readonly object _lock = new();

    public int UpdateCalls { get; private set; }

    public FakeInvoiceRepository(params Invoice[] seed) =>
        _invoices.AddRange(seed.Select(Copy));

    public IReadOnlyList<Invoice> GetAll()
    {
        lock (_lock) return _invoices.Select(Copy).ToList();
    }

    public Invoice? GetById(string id)
    {
        lock (_lock) return _invoices.FirstOrDefault(i => i.Id == id) is { } invoice ? Copy(invoice) : null;
    }

    public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids)
    {
        var wanted = ids.ToHashSet();
        lock (_lock) return _invoices.Where(i => wanted.Contains(i.Id)).Select(Copy).ToList();
    }

    public T ExecuteAtomically<T>(string id, Func<Invoice?, T> operation)
    {
        lock (_lock)
        {
            var storedInvoice = _invoices.FirstOrDefault(i => i.Id == id);
            return operation(storedInvoice is null ? null : Copy(storedInvoice));
        }
    }

    public Invoice Add(Invoice invoice)
    {
        lock (_lock)
        {
            _invoices.Add(Copy(invoice));
            return invoice;
        }
    }

    public void Update(Invoice invoice)
    {
        lock (_lock)
        {
            UpdateCalls++;
            var index = _invoices.FindIndex(storedInvoice => storedInvoice.Id == invoice.Id);
            if (index >= 0) _invoices[index] = Copy(invoice);
        }
    }

    public bool Remove(string id)
    {
        lock (_lock) return _invoices.RemoveAll(i => i.Id == id) > 0;
    }

    private static Invoice Copy(Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            CustomerName = invoice.CustomerName,
            Amount = invoice.Amount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt,
            ApprovalHistory = invoice.ApprovalHistory.ToList(),
            DecidedBy = invoice.DecidedBy,
            DecidedAt = invoice.DecidedAt,
            DecisionNote = invoice.DecisionNote
        };
}
