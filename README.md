# dotnet9-semantickernel-sqlserver-otel-grafana_consultaprodutos
Exemplo em .NET 9 de Console Application que faz uso do projeto Semantic Kernel, com integração com soluções de IA como Azure Open AI e Ollama na consulta de informações de produtos em uma base SQL Server. Inclui Docker Compose para criação do ambiente de testes com os dados + monitoramento com Grafana e OpenTelemetry.

O exemplo já faz uso do Grafana Alloy para a coleta de traces. Para saber mais sobre o Alloy acesse:
- https://grafana.com/docs/alloy/latest/
- https://github.com/grafana/grafana-opentelemetry-dotnet

---

## Traces

Exemplo de trace gerado utilizando Azure OpenAI:

![Trace com Azure OpenAI](img/trace-grafana-01.png)