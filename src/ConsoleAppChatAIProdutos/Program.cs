using Bogus;
using ConsoleAppChatAIProdutos.Data;
using ConsoleAppChatAIProdutos.Inputs;
using ConsoleAppChatAIProdutos.Plugins;
using ConsoleAppChatAIProdutos.Tracing;
using DbUp;
using Grafana.OpenTelemetry;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

var standardForegroundColor = ConsoleColor.White;
Console.ForegroundColor = standardForegroundColor;

Console.WriteLine("***** Testes com Semantic Kernel + Plugins (Kernel Functions) + SQL Server *****");
Console.WriteLine();

var aiSolution = InputHelper.GetAISolution();

var numberOfRecords = InputHelper.GetNumberOfNewProducts();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("BaseCatalogo")!;
CatalogoContext.ConnectionString = connectionString;

Console.WriteLine("Analisando execucao de Migrations com DbUp...");

// Aguarda 7 segundos para se assegurar de que
// a instancia do SQL Server esteja ativa 
Thread.Sleep(7_000);

var upgrader = DeployChanges
    .To.SqlDatabase(configuration.GetConnectionString("BaseInicializacao"))
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();
var result = upgrader.PerformUpgrade();

if (result.Successful)
{
    Console.WriteLine("Analises do DbUp concluidas com sucesso!");
}
else
{
    Environment.ExitCode = 1;
    Console.WriteLine($"Falha na execucao das Migrations do DbUp: {result.Error.Message}");
    return;
}

var db = new DataConnection(new DataOptions().UseSqlServer(connectionString));

var random = new Random();
var fakeEmpresas = new Faker<ConsoleAppChatAIProdutos.Data.Fake.Produto>("pt_BR").StrictMode(false)
            .RuleFor(p => p.Nome, f => f.Commerce.Product())
            .RuleFor(p => p.CodigoBarras, f => f.Commerce.Ean13())
            .RuleFor(p => p.Preco, f => random.Next(10, 30))
            .Generate(numberOfRecords);

if (numberOfRecords > 0)
{
    Console.WriteLine($"Gerando {numberOfRecords} produto(s)...");
    await db.BulkCopyAsync<ConsoleAppChatAIProdutos.Data.Fake.Produto>(fakeEmpresas);
    Console.WriteLine($"Produto(s) gerado(s) com sucesso!");
}
else
    Console.WriteLine($"Nenhum novo foi produto gerado!");
Console.WriteLine();

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(OpenTelemetryExtensions.ServiceName);

AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(OpenTelemetryExtensions.ServiceName)
    .AddSource("Microsoft.SemanticKernel*")
    .AddSqlClientInstrumentation(options =>
    {
        options.SetDbStatementForText = true;
    })
    .AddHttpClientInstrumentation()
    .UseGrafana()
    .Build();

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var kernelBuilder = Kernel.CreateBuilder();
PromptExecutionSettings settings;

if (aiSolution == InputHelper.OLLAMA)
{
    kernelBuilder.AddOllamaChatCompletion(
        modelId: configuration["Ollama:Model"]!,
        endpoint: new Uri(configuration["Ollama:Endpoint"]!),
        serviceId: "chat");
    settings = new OllamaPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };
}
else if (aiSolution == InputHelper.AZURE_OPEN_AI)
{
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: configuration["AzureOpenAI:DeploymentName"]!,
        endpoint: configuration["AzureOpenAI:Endpoint"]!,
        apiKey: configuration["AzureOpenAI:ApiKey"]!,
        serviceId: "chat");
    settings = new OpenAIPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };
}
else
    throw new Exception($"Solucao de AI invalida: {aiSolution}");

kernelBuilder.Plugins.AddFromType<ProdutosPlugin>();
Kernel kernel = kernelBuilder.Build();

#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var aiChatService = kernel.GetRequiredService<IChatCompletionService>();
var chatHistory = new ChatHistory();
while (true)
{    
    Console.WriteLine("Sua pergunta:");
    Console.ForegroundColor = ConsoleColor.Cyan;
    var userPrompt = Console.ReadLine();
    Console.ForegroundColor = standardForegroundColor;

    using var activity1 = OpenTelemetryExtensions.ActivitySource
        .StartActivity("PerguntaChatIAProdutos")!;

    chatHistory.Add(new ChatMessageContent(AuthorRole.User, userPrompt));

    Console.WriteLine();
    Console.WriteLine("Resposta da IA:");
    Console.WriteLine();

    ChatMessageContent chatResult = await aiChatService
        .GetChatMessageContentAsync(chatHistory, settings, kernel);
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(chatResult.Content);
    Console.ForegroundColor = standardForegroundColor;
    chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, chatResult.Content));

    Console.WriteLine();
    Console.WriteLine();
    
    activity1.Stop();
}