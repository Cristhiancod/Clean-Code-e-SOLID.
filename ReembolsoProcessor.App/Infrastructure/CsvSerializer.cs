using System.Globalization;
using ReembolsoProcessor.App.Domain;

namespace ReembolsoProcessor.App.Infrastructure;

public static class CsvSerializer
{
    public static IReadOnlyList<ReimbursementRequest> ParseRequests(string csvContent)
    {
        var lines = csvContent
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (lines.Count == 0)
        {
            return [];
        }

        var requests = new List<ReimbursementRequest>(capacity: lines.Count - 1);

        foreach (var line in lines.Skip(1))
        {
            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 5)
            {
                throw new FormatException($"Linha inválida: '{line}'. Esperado 5 colunas.");
            }

            requests.Add(new ReimbursementRequest(
                Id: int.Parse(columns[0], CultureInfo.InvariantCulture),
                ProcedureType: ParseProcedureType(columns[1]),
                ProcedureDate: DateOnly.ParseExact(columns[2], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                PaidAmount: decimal.Parse(columns[3], CultureInfo.InvariantCulture),
                ClientId: int.Parse(columns[4], CultureInfo.InvariantCulture)
            ));
        }

        return requests;
    }

    public static string ToCsv(IReadOnlyList<ReimbursementResult> results)
    {
        var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine("Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status");

        foreach (var result in results)
        {
            writer.WriteLine(string.Join(',',
                result.Id,
                ToProcedureDisplayName(result.ProcedureType),
                result.ProcedureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                result.PaidAmount.ToString("0.##", CultureInfo.InvariantCulture),
                result.ReimbursedAmount.ToString("0.##", CultureInfo.InvariantCulture),
                result.ClientId,
                ToStatusDisplayName(result.Status)));
        }

        return writer.ToString();
    }

    private static ProcedureType ParseProcedureType(string raw) => raw.Trim() switch
    {
        "Consulta Médica" => ProcedureType.ConsultaMedica,
        "Exame de Imagem" => ProcedureType.ExameDeImagem,
        "Exame Laboratorial" => ProcedureType.ExameLaboratorial,
        "Outros" => ProcedureType.Outros,
        _ => throw new FormatException($"Tipo de procedimento inválido: '{raw}'")
    };

    private static string ToProcedureDisplayName(ProcedureType procedureType) => procedureType switch
    {
        ProcedureType.ConsultaMedica => "Consulta Médica",
        ProcedureType.ExameDeImagem => "Exame de Imagem",
        ProcedureType.ExameLaboratorial => "Exame Laboratorial",
        ProcedureType.Outros => "Outros",
        _ => throw new ArgumentOutOfRangeException(nameof(procedureType), procedureType, null)
    };

    private static string ToStatusDisplayName(ReimbursementStatus status) => status switch
    {
        ReimbursementStatus.Aprovado => "Aprovado",
        ReimbursementStatus.Rejeitado => "Rejeitado",
        ReimbursementStatus.SuspeitoDeFraude => "Suspeito de Fraude",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
