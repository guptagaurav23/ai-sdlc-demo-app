namespace InvoiceApp.Domain;

public interface IInvoiceRepository
{
    IReadOnlyList<Invoice> GetAll();
    Invoice? GetById(string id);
    IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids);
    /// <summary>Runs an operation against an invoice while preventing concurrent repository operations.</summary>
    T ExecuteAtomically<T>(string id, Func<Invoice?, T> operation);
    Invoice Add(Invoice invoice);
    void Update(Invoice invoice);
    bool Remove(string id);
}
