using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerCanApproveInvoiceAtLimit()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000m));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
    }

    [Fact]
    public void Approve_DirectorCanApproveInvoiceAtLimit()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000m));

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
    }

    [Theory]
    [InlineData(50_000.01)]
    [InlineData(499_999.99)]
    public void Approve_DirectorCanApproveAmountsAboveManagerLimitThroughDirectorLimit(decimal amount)
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount));

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
    }

    [Fact]
    public void Approve_ManagerCanApproveAmountBelowLimit()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 49_999.99m));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
    }

    [Fact]
    public void Approve_ManagerCannotApproveAboveLimitWithoutChangingInvoiceOrRepository()
    {
        AssertApprovalDeniedWithoutMutation(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_001m),
            "Manager",
            "Managers may only approve invoices up to 50,000.");
    }

    [Fact]
    public void Approve_DirectorCannotSingleApproveAboveLimitWithoutChangingInvoiceOrRepository()
    {
        AssertApprovalDeniedWithoutMutation(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m),
            "Director",
            "Directors may only single-approve invoices up to 500,000.");
    }

    [Theory]
    [InlineData("Approver")]
    [InlineData("Admin")]
    [InlineData("Clerk")]
    public void Approve_RejectsUnauthorizedRoleWithoutChangingInvoiceOrRepository(string role)
    {
        AssertApprovalDeniedWithoutMutation(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval),
            role,
            $"Role '{role}' is not authorized");
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.manager", "Manager"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.manager", "Manager"));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }

    private static void AssertApprovalDeniedWithoutMutation(
        Invoice invoice,
        string role,
        string expectedMessage)
    {
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);
        var originalState = (
            invoice.Id,
            invoice.CustomerName,
            invoice.Amount,
            invoice.Status,
            invoice.CreatedAt,
            invoice.DecidedBy,
            invoice.DecidedAt,
            invoice.DecisionNote);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "demo.user", role));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.Equal(originalState, (
            invoice.Id,
            invoice.CustomerName,
            invoice.Amount,
            invoice.Status,
            invoice.CreatedAt,
            invoice.DecidedBy,
            invoice.DecidedAt,
            invoice.DecisionNote));
        Assert.Same(invoice, repository.GetById(invoice.Id));
        Assert.Equal(0, repository.UpdateCalls);
    }
}
