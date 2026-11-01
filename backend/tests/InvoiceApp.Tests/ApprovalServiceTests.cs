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
            new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_001m)),
            "INV-1",
            "demo.manager",
            "Manager",
            "Managers may only approve invoices up to 50,000.");
    }

    [Fact]
    public void Approve_DirectorFirstApprovalAboveLimitLeavesInvoicePending()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000.01m));
        var service = new ApprovalService(repository);

        var result = service.Approve("INV-1", "demo.director1", "Director");
        var storedInvoice = repository.GetById("INV-1")!;

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        var firstApproval = Assert.Single(storedInvoice.ApprovalHistory);
        Assert.Equal("demo.director1", firstApproval.Username);
        Assert.Equal("Director", firstApproval.Role);
        Assert.NotEqual(default, firstApproval.ApprovedAt);
        Assert.Null(storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCalls);
    }

    [Fact]
    public void Approve_DistinctDirectorSecondApprovalCompletesHighValueInvoice()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.director1", "Director");
        var result = service.Approve("INV-1", "demo.director2", "Director");
        var storedInvoice = repository.GetById("INV-1")!;

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Collection(
            storedInvoice.ApprovalHistory,
            first => Assert.Equal("demo.director1", first.Username),
            second => Assert.Equal("demo.director2", second.Username));
        Assert.All(storedInvoice.ApprovalHistory, approval =>
        {
            Assert.Equal("Director", approval.Role);
            Assert.NotEqual(default, approval.ApprovedAt);
        });
        Assert.Equal("demo.director2", storedInvoice.DecidedBy);
        Assert.Equal(storedInvoice.ApprovalHistory[1].ApprovedAt, storedInvoice.DecidedAt);
        Assert.Equal(2, repository.UpdateCalls);
    }

    [Fact]
    public void Approve_DuplicateSecondApproverDoesNotChangeInvoiceOrRepository()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        var service = new ApprovalService(repository);
        service.Approve("INV-1", "demo.director1", "Director");

        AssertApprovalDeniedWithoutMutation(
            repository,
            service,
            "INV-1",
            "demo.director1",
            "Director",
            "same user cannot approve this invoice twice");
    }

    [Fact]
    public void Approve_DuplicateSecondApproverIsCaseInsensitive()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        var service = new ApprovalService(repository);
        service.Approve("INV-1", "demo.director1", "Director");

        AssertApprovalDeniedWithoutMutation(
            repository,
            service,
            "INV-1",
            "DEMO.DIRECTOR1",
            "Director",
            "same user cannot approve this invoice twice");
    }

    [Fact]
    public void Approve_NonDirectorSecondApproverDoesNotChangeInvoiceOrRepository()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        var service = new ApprovalService(repository);
        service.Approve("INV-1", "demo.director1", "Director");

        AssertApprovalDeniedWithoutMutation(
            repository,
            service,
            "INV-1",
            "demo.manager",
            "Manager",
            "second approver for invoices above 500,000 must be a Director");
    }

    [Fact]
    public void Approve_ManagerCannotProvideFirstApprovalAbove500000WithoutMutation()
    {
        AssertApprovalDeniedWithoutMutation(
            new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m)),
            "INV-1",
            "demo.manager",
            "Manager",
            "Managers may only approve invoices up to 50,000");
    }

    [Theory]
    [InlineData("Approver")]
    [InlineData("Admin")]
    [InlineData("Clerk")]
    public void Approve_RejectsUnauthorizedRoleWithoutChangingInvoiceOrRepository(string role)
    {
        AssertApprovalDeniedWithoutMutation(
            new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval)),
            "INV-1",
            "demo.user",
            role,
            $"Role '{role}' is not authorized");
    }

    [Fact]
    public async Task Approve_ConcurrentDuplicateApprovalsCannotRecordSameUserTwice()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_001m));
        var service = new ApprovalService(repository);

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            try
            {
                service.Approve("INV-1", "demo.director1", "Director");
                return (Succeeded: true, Error: (string?)null);
            }
            catch (InvalidOperationException exception)
            {
                return (Succeeded: false, Error: exception.Message);
            }
        })));

        Assert.Single(outcomes, outcome => outcome.Succeeded);
        Assert.Contains(
            outcomes,
            outcome => outcome.Error?.Contains("same user cannot approve", StringComparison.Ordinal) == true);
        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        Assert.Collection(
            storedInvoice.ApprovalHistory,
            approval => Assert.Equal("demo.director1", approval.Username));
        Assert.Equal(1, repository.UpdateCalls);
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
        FakeInvoiceRepository repository,
        string invoiceId,
        string approver,
        string role,
        string expectedMessage)
    {
        var service = new ApprovalService(repository);
        AssertApprovalDeniedWithoutMutation(
            repository,
            service,
            invoiceId,
            approver,
            role,
            expectedMessage);
    }

    private static void AssertApprovalDeniedWithoutMutation(
        FakeInvoiceRepository repository,
        ApprovalService service,
        string invoiceId,
        string approver,
        string role,
        string expectedMessage)
    {
        var originalInvoice = repository.GetById(invoiceId)!;
        var originalUpdateCalls = repository.UpdateCalls;
        var originalInvoiceCount = repository.GetAll().Count;
        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoiceId, approver, role));
        var persistedInvoice = repository.GetById(invoiceId)!;

        Assert.Contains(expectedMessage, exception.Message);
        Assert.Equal(originalInvoice.Id, persistedInvoice.Id);
        Assert.Equal(originalInvoice.CustomerName, persistedInvoice.CustomerName);
        Assert.Equal(originalInvoice.Amount, persistedInvoice.Amount);
        Assert.Equal(originalInvoice.Status, persistedInvoice.Status);
        Assert.Equal(originalInvoice.CreatedAt, persistedInvoice.CreatedAt);
        Assert.Equal(originalInvoice.DecidedBy, persistedInvoice.DecidedBy);
        Assert.Equal(originalInvoice.DecidedAt, persistedInvoice.DecidedAt);
        Assert.Equal(originalInvoice.DecisionNote, persistedInvoice.DecisionNote);
        Assert.Equal(originalInvoice.ApprovalHistory, persistedInvoice.ApprovalHistory);
        Assert.Equal(originalUpdateCalls, repository.UpdateCalls);
        Assert.Equal(originalInvoiceCount, repository.GetAll().Count);
    }
}
