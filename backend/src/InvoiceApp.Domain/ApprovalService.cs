namespace InvoiceApp.Domain;

/// <summary>
/// Moves invoices through their lifecycle and applies role-based approval rules.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        return _repository.ExecuteAtomically(invoiceId, storedInvoice =>
        {
            var invoice = storedInvoice
                ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
            }

            invoice.Status = InvoiceStatus.PendingApproval;
            _repository.Update(invoice);
            return invoice;
        });
    }

    public Invoice Approve(string invoiceId, string approver, string approverRole)
    {
        return _repository.ExecuteAtomically(invoiceId, storedInvoice =>
        {
            var invoice = storedInvoice
                ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");
            EnsurePending(invoice);

            if (invoice.Amount > 500_000m)
            {
                return ApproveHighValueInvoice(invoice, approver, approverRole);
            }

            EnsureCanApprove(invoice, approverRole);
            var approvedAt = DateTime.UtcNow;

            invoice.ApprovalHistory.Add(new InvoiceApproval(approver, approverRole, approvedAt));
            invoice.Status = InvoiceStatus.Approved;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = approvedAt;
            _repository.Update(invoice);
            return invoice;
        });
    }

    private Invoice ApproveHighValueInvoice(Invoice invoice, string approver, string approverRole)
    {
        if (invoice.ApprovalHistory.Count == 0)
        {
            if (approverRole == "Manager")
            {
                throw new InvalidOperationException(
                    "Managers may only approve invoices up to 50,000; a Director must provide the first approval above 500,000.");
            }

            if (approverRole != "Director")
            {
                throw UnauthorizedRole(approverRole);
            }

            invoice.ApprovalHistory.Add(
                new InvoiceApproval(approver, approverRole, DateTime.UtcNow));
            _repository.Update(invoice);
            return invoice;
        }

        if (invoice.ApprovalHistory.Count != 1)
        {
            throw new InvalidOperationException(
                "A pending invoice above 500,000 must have exactly one recorded approval before its second approval.");
        }

        var firstApproval = invoice.ApprovalHistory[0];
        if (firstApproval.Role != "Director")
        {
            throw new InvalidOperationException(
                "The first approver for invoices above 500,000 must be a Director.");
        }

        if (string.Equals(firstApproval.Username, approver, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The same user cannot approve this invoice twice.");
        }

        if (approverRole != "Director")
        {
            throw new InvalidOperationException(
                "The second approver for invoices above 500,000 must be a Director.");
        }

        var secondApprovedAt = DateTime.UtcNow;
        invoice.ApprovalHistory.Add(new InvoiceApproval(approver, approverRole, secondApprovedAt));
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = secondApprovedAt;
        _repository.Update(invoice);
        return invoice;
    }

    private static void EnsureCanApprove(Invoice invoice, string approverRole)
    {
        switch (approverRole)
        {
            case "Manager" when invoice.Amount <= 50_000m:
                return;
            case "Manager":
                throw new InvalidOperationException(
                    "Managers may only approve invoices up to 50,000.");
            case "Director" when invoice.Amount <= 500_000m:
                return;
            case "Director":
                throw new InvalidOperationException(
                    "Directors may only single-approve invoices up to 500,000.");
            default:
                throw UnauthorizedRole(approverRole);
        }
    }

    private static InvalidOperationException UnauthorizedRole(string approverRole) =>
        new(
            $"Role '{approverRole}' is not authorized to approve invoices. " +
            "Only Managers and Directors may approve invoices.");

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        return _repository.ExecuteAtomically(invoiceId, storedInvoice =>
        {
            var invoice = storedInvoice
                ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");
            EnsurePending(invoice);

            invoice.Status = InvoiceStatus.Rejected;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            invoice.DecisionNote = reason;
            _repository.Update(invoice);
            return invoice;
        });
    }

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
