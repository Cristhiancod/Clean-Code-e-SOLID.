namespace ReembolsoProcessor.App.Domain;

public sealed record ReimbursementRequest(
    int Id,
    ProcedureType ProcedureType,
    DateOnly ProcedureDate,
    decimal PaidAmount,
    int ClientId);
