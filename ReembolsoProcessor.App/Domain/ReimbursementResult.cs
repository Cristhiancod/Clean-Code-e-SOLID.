namespace ReembolsoProcessor.App.Domain;

public sealed record ReimbursementResult(
    int Id,
    ProcedureType ProcedureType,
    DateOnly ProcedureDate,
    decimal PaidAmount,
    decimal ReimbursedAmount,
    int ClientId,
    ReimbursementStatus Status);
