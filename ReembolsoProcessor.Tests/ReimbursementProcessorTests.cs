using ReembolsoProcessor.App.Core;
using ReembolsoProcessor.App.Domain;
using ReembolsoProcessor.App.Infrastructure;

namespace ReembolsoProcessor.Tests;

public class ReimbursementProcessorTests
{
    private static readonly DateOnly Today = new(2024, 5, 1);

    [Fact]
    public void Should_Process_Sample_From_Challenge()
    {
        var csvInput = """
                       Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
                       1,Consulta Médica,2024-02-10,400,123
                       2,Exame de Imagem,2024-03-01,600,124
                       3,Exame Laboratorial,2024-04-15,200,125
                       4,Outros,2024-01-20,150,126
                       """;

        var processor = CreateProcessor();
        var requests = CsvSerializer.ParseRequests(csvInput);

        var results = processor.Process(requests);

        Assert.Collection(results,
            r =>
            {
                Assert.Equal(320m, r.ReimbursedAmount);
                Assert.Equal(ReimbursementStatus.Aprovado, r.Status);
            },
            r =>
            {
                Assert.Equal(500m, r.ReimbursedAmount);
                Assert.Equal(ReimbursementStatus.Aprovado, r.Status);
            },
            r =>
            {
                Assert.Equal(140m, r.ReimbursedAmount);
                Assert.Equal(ReimbursementStatus.Aprovado, r.Status);
            },
            r =>
            {
                Assert.Equal(75m, r.ReimbursedAmount);
                Assert.Equal(ReimbursementStatus.Rejeitado, r.Status);
            });
    }

    [Fact]
    public void Should_Flag_Fraud_When_Request_Count_Exceeds_Limit()
    {
        var requests = Enumerable.Range(1, 6)
            .Select(id => new ReimbursementRequest(
                id,
                ProcedureType.ConsultaMedica,
                new DateOnly(2024, 4, 10 + id),
                100m,
                123))
            .ToList();

        var results = CreateProcessor().Process(requests);

        Assert.Equal(ReimbursementStatus.SuspeitoDeFraude, results.Last().Status);
    }

    [Fact]
    public void Should_Flag_Fraud_When_Total_Amount_Exceeds_Limit()
    {
        var requests = new[]
        {
            new ReimbursementRequest(1, ProcedureType.ExameDeImagem, new DateOnly(2024, 4, 1), 600m, 123),
            new ReimbursementRequest(2, ProcedureType.ExameDeImagem, new DateOnly(2024, 4, 2), 600m, 123),
            new ReimbursementRequest(3, ProcedureType.ExameDeImagem, new DateOnly(2024, 4, 3), 600m, 123),
            new ReimbursementRequest(4, ProcedureType.ExameDeImagem, new DateOnly(2024, 4, 4), 600m, 123)
        };

        var results = CreateProcessor().Process(requests);

        Assert.Equal(ReimbursementStatus.SuspeitoDeFraude, results[3].Status);
    }

    [Fact]
    public void Should_Process_10000_Requests_In_Reasonable_Time()
    {
        var requests = Enumerable.Range(1, 10_000)
            .Select(id => new ReimbursementRequest(
                id,
                id % 2 == 0 ? ProcedureType.ConsultaMedica : ProcedureType.ExameLaboratorial,
                new DateOnly(2024, 4, 1).AddDays(id % 30),
                300m,
                id % 200))
            .ToList();

        var start = DateTime.UtcNow;
        var results = CreateProcessor().Process(requests);
        var duration = DateTime.UtcNow - start;

        Assert.Equal(10_000, results.Count);
        Assert.True(duration < TimeSpan.FromSeconds(2), $"Processamento demorou {duration.TotalMilliseconds} ms");
    }

    private static ReimbursementProcessor CreateProcessor()
    {
        return new ReimbursementProcessor(new RulesConfiguration(), new FakeClock(Today));
    }

    private sealed class FakeClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; } = today;
    }
}
