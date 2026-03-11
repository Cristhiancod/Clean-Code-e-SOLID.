using ReembolsoProcessor.App.Domain;

namespace ReembolsoProcessor.App.Core;

public sealed class ProcedureReimbursementCalculator
{
    private readonly RulesConfiguration _rules;

    public ProcedureReimbursementCalculator(RulesConfiguration rules)
    {
        _rules = rules;
    }

    public decimal Calculate(ProcedureType procedureType, decimal paidAmount)
    {
        var percentage = procedureType switch
        {
            ProcedureType.ConsultaMedica => 0.80m,
            ProcedureType.ExameDeImagem => 0.90m,
            ProcedureType.ExameLaboratorial => 0.70m,
            ProcedureType.Outros => 0.50m,
            _ => throw new ArgumentOutOfRangeException(nameof(procedureType), procedureType, "Tipo de procedimento inválido")
        };

        var reimbursed = paidAmount * percentage;
        return decimal.Min(reimbursed, _rules.MaxReimbursementPerProcedure);
    }
}
