using ReembolsoProcessor.App.Domain;

namespace ReembolsoProcessor.App.Core;

public sealed class ReimbursementProcessor
{
    private readonly RulesConfiguration _rules;
    private readonly IClock _clock;
    private readonly ProcedureReimbursementCalculator _calculator;

    public ReimbursementProcessor(RulesConfiguration rules, IClock clock)
    {
        _rules = rules;
        _clock = clock;
        _calculator = new ProcedureReimbursementCalculator(_rules);
    }

    public IReadOnlyList<ReimbursementResult> Process(IEnumerable<ReimbursementRequest> requests, TextWriter? auditLog = null)
    {
        var historyByClient = new Dictionary<int, Queue<(DateOnly date, decimal reimbursed)>>();
        var results = new List<ReimbursementResult>();

        foreach (var request in requests)
        {
            var reimbursed = _calculator.Calculate(request.ProcedureType, request.PaidAmount);
            var status = DetermineStatus(request, reimbursed, historyByClient);

            if (status is ReimbursementStatus.Rejeitado or ReimbursementStatus.SuspeitoDeFraude)
            {
                auditLog?.WriteLine($"Pedido {request.Id} do cliente {request.ClientId} marcado como {status}.");
            }

            if (status != ReimbursementStatus.Rejeitado)
            {
                RegisterRequest(historyByClient, request.ClientId, request.ProcedureDate, reimbursed);
            }

            results.Add(new ReimbursementResult(
                request.Id,
                request.ProcedureType,
                request.ProcedureDate,
                request.PaidAmount,
                reimbursed,
                request.ClientId,
                status));
        }

        return results;
    }

    private ReimbursementStatus DetermineStatus(
        ReimbursementRequest request,
        decimal reimbursed,
        Dictionary<int, Queue<(DateOnly date, decimal reimbursed)>> historyByClient)
    {
        if (IsOutsideProcessingWindow(request.ProcedureDate))
        {
            return ReimbursementStatus.Rejeitado;
        }

        var history = GetClientWindowHistory(historyByClient, request.ClientId, request.ProcedureDate);
        var projectedCount = history.Count + 1;
        var projectedTotal = history.Sum(item => item.reimbursed) + reimbursed;

        if (projectedCount > _rules.MaxRequestsPerClientInWindow ||
            projectedTotal > _rules.MaxTotalReimbursementPerClientInWindow)
        {
            return ReimbursementStatus.SuspeitoDeFraude;
        }

        return ReimbursementStatus.Aprovado;
    }

    private bool IsOutsideProcessingWindow(DateOnly procedureDate)
    {
        var days = _clock.Today.DayNumber - procedureDate.DayNumber;
        return days < 0 || days > _rules.MaxProcessingDays;
    }

    private Queue<(DateOnly date, decimal reimbursed)> GetClientWindowHistory(
        Dictionary<int, Queue<(DateOnly date, decimal reimbursed)>> historyByClient,
        int clientId,
        DateOnly currentProcedureDate)
    {
        if (!historyByClient.TryGetValue(clientId, out var history))
        {
            history = new Queue<(DateOnly date, decimal reimbursed)>();
            historyByClient[clientId] = history;
            return history;
        }

        while (history.TryPeek(out var item) && (currentProcedureDate.DayNumber - item.date.DayNumber) > _rules.FraudWindowDays)
        {
            history.Dequeue();
        }

        return history;
    }

    private static void RegisterRequest(
        Dictionary<int, Queue<(DateOnly date, decimal reimbursed)>> historyByClient,
        int clientId,
        DateOnly date,
        decimal reimbursed)
    {
        if (!historyByClient.TryGetValue(clientId, out var history))
        {
            history = new Queue<(DateOnly date, decimal reimbursed)>();
            historyByClient[clientId] = history;
        }

        history.Enqueue((date, reimbursed));
    }
}
