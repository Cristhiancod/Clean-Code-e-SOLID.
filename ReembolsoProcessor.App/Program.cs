using ReembolsoProcessor.App.Core;
using ReembolsoProcessor.App.Infrastructure;

var input = Console.In.ReadToEnd();
if (string.IsNullOrWhiteSpace(input))
{
    return;
}

var rules = new RulesConfiguration();
var processor = new ReimbursementProcessor(rules, new SystemClock());
var requests = CsvSerializer.ParseRequests(input);
var results = processor.Process(requests, Console.Error);
var output = CsvSerializer.ToCsv(results);

Console.Out.Write(output);
