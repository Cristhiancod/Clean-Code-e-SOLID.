# Code Challenge - Processamento de Reembolsos

Aplicação de linha de comando em **.NET 8** que lê pedidos CSV via `stdin`, aplica regras de negócio e prevenção de fraude, e retorna o resultado processado via `stdout`.

## Decisões técnicas e arquiteturais

- **Separação por responsabilidades**:
  - `Domain`: modelos e enums do domínio.
  - `Core`: regras de negócio, cálculo e processamento.
  - `Infrastructure`: parsing/serialização CSV e relógio do sistema.
- **Extensibilidade (OCP/SOLID)**:
  - regras centralizadas em `RulesConfiguration`.
  - cálculo de reembolso isolado em `ProcedureReimbursementCalculator`.
  - dependência de data via `IClock` para facilitar testes.
- **Sem dependências externas** além do que já vem com o .NET.

## Regras implementadas

1. **Percentual por tipo de procedimento**
   - Consulta Médica: 80%
   - Exame de Imagem: 90%
   - Exame Laboratorial: 70%
   - Outros: 50%
2. **Teto por procedimento**: R$ 500,00
3. **Validade**: só processa pedidos dentro dos últimos 90 dias.
   - Fora da janela: status `Rejeitado`.
4. **Fraude em janela de 30 dias por cliente**:
   - Mais de 5 pedidos: `Suspeito de Fraude`.
   - Soma de reembolsos > R$ 1.500,00: `Suspeito de Fraude`.

## Entrada e saída

### Entrada (stdin)
```csv
Id,TipoProcedimento,DataProcedimento,ValorPago,ClienteId
1,Consulta Médica,2024-02-10,400,123
```

### Saída (stdout)
```csv
Id,TipoProcedimento,DataProcedimento,ValorPago,ValorReembolsado,ClienteId,Status
1,Consulta Médica,2024-02-10,400,320,123,Aprovado
```

Logs de auditoria para pedidos `Rejeitado` e `Suspeito de Fraude` são emitidos em `stderr`.

## Como compilar e executar

```bash
dotnet build ReembolsoProcessor.sln
cat entrada.csv | dotnet run --project ReembolsoProcessor.App
```

## Como rodar os testes

```bash
dotnet test ReembolsoProcessor.sln
```

## Notas adicionais

- Inclui teste de carga com 10.000 pedidos para validar o requisito opcional de escalabilidade.
- O formato numérico usa ponto (`.`) como separador decimal no CSV.
