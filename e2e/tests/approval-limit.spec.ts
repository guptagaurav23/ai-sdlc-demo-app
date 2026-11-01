import { expect, test, type APIRequestContext } from '@playwright/test';

type Invoice = {
  id: string;
  customerName: string;
  amount: number;
  status: string;
  approvalHistory: Array<{
    username: string;
    role: string;
    approvedAt: string;
  }>;
  decidedBy: string | null;
  decidedAt: string | null;
  decisionNote: string | null;
};

type AuthHeaders = { Authorization: string };

async function signIn(
  request: APIRequestContext,
  username: string,
): Promise<AuthHeaders> {
  const response = await request.post('/api/auth/login', {
    data: { username, password: 'ChangeMe123!' },
  });
  expect(response.status()).toBe(200);
  const login = (await response.json()) as { token: string };
  expect(login.token).toBeTruthy();
  return { Authorization: `Bearer ${login.token}` };
}

async function createPendingInvoice(
  request: APIRequestContext,
  headers: AuthHeaders,
  amount: number,
): Promise<Invoice> {
  const createResponse = await request.post('/api/invoices', {
    headers,
    data: {
      customerName: `Approval E2E ${Date.now()} ${Math.random()}`,
      amount,
    },
  });
  expect(createResponse.status()).toBe(201);
  const created = (await createResponse.json()) as Invoice;

  const submitResponse = await request.post(
    `/api/invoices/${created.id}/submit`,
    { headers },
  );
  expect(submitResponse.status()).toBe(200);

  const invoiceResponse = await request.get(`/api/invoices/${created.id}`, {
    headers,
  });
  expect(invoiceResponse.status()).toBe(200);
  const invoice = (await invoiceResponse.json()) as Invoice;
  expect(invoice.status).toBe('PendingApproval');
  expect(invoice.approvalHistory).toEqual([]);
  return invoice;
}

async function getInvoice(
  request: APIRequestContext,
  invoiceId: string,
  headers: AuthHeaders,
): Promise<Invoice> {
  const response = await request.get(`/api/invoices/${invoiceId}`, { headers });
  expect(response.status()).toBe(200);
  return (await response.json()) as Invoice;
}

test('allows a Manager to approve an invoice of exactly 50,000', async ({
  request,
}) => {
  const manager = await signIn(request, 'demo.manager');
  const invoice = await createPendingInvoice(request, manager, 50_000);

  const response = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: manager },
  );

  expect(response.status()).toBe(200);
  const approved = (await response.json()) as Invoice;
  expect(approved.status).toBe('Approved');
  expect(approved.approvalHistory).toHaveLength(1);
  expect(approved.approvalHistory[0]).toMatchObject({
    username: 'demo.manager',
    role: 'Manager',
  });
});

test('rejects a Manager approval above 50,000 without changing the invoice', async ({
  request,
}) => {
  const manager = await signIn(request, 'demo.manager');
  const invoice = await createPendingInvoice(request, manager, 50_001);
  const beforeApproval = await getInvoice(request, invoice.id, manager);

  const response = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: manager },
  );

  expect(response.status()).toBe(409);
  await expect(response.json()).resolves.toEqual({
    error: 'Managers may only approve invoices up to 50,000.',
  });
  await expect(getInvoice(request, invoice.id, manager)).resolves.toEqual(
    beforeApproval,
  );
});

test('allows a Director to approve an invoice of exactly 500,000', async ({
  request,
}) => {
  const director = await signIn(request, 'demo.director');
  const invoice = await createPendingInvoice(request, director, 500_000);

  const response = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: director },
  );

  expect(response.status()).toBe(200);
  const approved = (await response.json()) as Invoice;
  expect(approved.status).toBe('Approved');
  expect(approved.approvalHistory).toHaveLength(1);
  expect(approved.approvalHistory[0]).toMatchObject({
    username: 'demo.director',
    role: 'Director',
  });
});

test('keeps an invoice above 500,000 pending after the first Director approval', async ({
  request,
}) => {
  const director = await signIn(request, 'demo.director');
  const invoice = await createPendingInvoice(request, director, 500_001);

  const response = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: director },
  );

  expect(response.status()).toBe(200);
  const afterFirstApproval = (await response.json()) as Invoice;
  expect(afterFirstApproval.status).toBe('PendingApproval');
  expect(afterFirstApproval.approvalHistory).toHaveLength(1);
  expect(afterFirstApproval.approvalHistory[0]).toMatchObject({
    username: 'demo.director',
    role: 'Director',
  });
});

test('approves an invoice above 500,000 after a second, different Director approves', async ({
  request,
}) => {
  const firstDirector = await signIn(request, 'demo.director');
  const secondDirector = await signIn(request, 'demo.director2');
  const invoice = await createPendingInvoice(request, firstDirector, 500_001);

  const firstResponse = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: firstDirector },
  );
  expect(firstResponse.status()).toBe(200);
  expect(((await firstResponse.json()) as Invoice).status).toBe(
    'PendingApproval',
  );

  const secondResponse = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: secondDirector },
  );

  expect(secondResponse.status()).toBe(200);
  const approved = (await secondResponse.json()) as Invoice;
  expect(approved.status).toBe('Approved');
  expect(approved.approvalHistory).toHaveLength(2);
  expect(approved.approvalHistory.map((approval) => approval.username)).toEqual([
    'demo.director',
    'demo.director2',
  ]);
  expect(approved.approvalHistory.every((approval) => approval.role === 'Director'))
    .toBe(true);
});

test('rejects the same Director approving twice without changing the invoice', async ({
  request,
}) => {
  const director = await signIn(request, 'demo.director');
  const invoice = await createPendingInvoice(request, director, 500_001);

  const firstResponse = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: director },
  );
  expect(firstResponse.status()).toBe(200);
  const beforeDuplicate = (await firstResponse.json()) as Invoice;
  expect(beforeDuplicate.status).toBe('PendingApproval');

  const duplicateResponse = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: director },
  );

  expect(duplicateResponse.status()).toBe(409);
  await expect(duplicateResponse.json()).resolves.toEqual({
    error: 'The same user cannot approve this invoice twice.',
  });
  await expect(getInvoice(request, invoice.id, director)).resolves.toEqual(
    beforeDuplicate,
  );
});

test('rejects a non-Director second approval without changing the invoice', async ({
  request,
}) => {
  const director = await signIn(request, 'demo.director');
  const approver = await signIn(request, 'demo.approver');
  const invoice = await createPendingInvoice(request, director, 500_001);

  const firstResponse = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: director },
  );
  expect(firstResponse.status()).toBe(200);
  expect(((await firstResponse.json()) as Invoice).status).toBe(
    'PendingApproval',
  );
  const beforeSecondApproval = await getInvoice(request, invoice.id, director);

  const secondResponse = await request.post(
    `/api/approvals/${invoice.id}/approve`,
    { headers: approver },
  );

  expect(secondResponse.status()).toBe(409);
  await expect(secondResponse.json()).resolves.toEqual({
    error: 'The second approver for invoices above 500,000 must be a Director.',
  });
  await expect(getInvoice(request, invoice.id, director)).resolves.toEqual(
    beforeSecondApproval,
  );
});
