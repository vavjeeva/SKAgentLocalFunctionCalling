#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKAgentLocalFunctionCalling.Plugins;
using System.Reflection;
using System.Text;

var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

var modelId = config["modelId"];
var baseUrl = config["baseUrl"];
var weatherApiKey = config["weatherApiKey"];

var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(2)
};

var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: modelId!, apiKey: null, endpoint: new Uri(baseUrl!), httpClient: httpClient);
var kernel = builder.Build();

var HostName = "AI Assistant";
var HostInstructions = @"You are a helpful Assistant to answer their queries. Be respectful and precise in answering the queries.
If the queries are related to getting the time or weather, Use the available plugin functions to get the answer.";

var settings = new OpenAIPromptExecutionSettings() { Temperature = 0.0, ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

ChatCompletionAgent agent =
           new()
           {
               Instructions = HostInstructions,
               Name = HostName,
               Kernel = kernel,
               Arguments = new(settings),
           };

KernelPlugin localTimePlugin = KernelPluginFactory.CreateFromType<LocalTimePlugin>();
agent.Kernel.Plugins.Add(localTimePlugin);

KernelPlugin weatherPlugin = KernelPluginFactory.CreateFromObject(new WeatherPlugin(weatherApiKey!));
agent.Kernel.Plugins.Add(weatherPlugin);

Console.WriteLine("Assistant: Hello, I am your Assistant. How may i help you?");

AgentGroupChat chat = new();

while (true)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User: ");    
    await InvokeAgentAsync(Console.ReadLine()!);
}

// Local function to invoke agent and display the conversation messages.
async Task InvokeAgentAsync(string question)
{
    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, question));

    Console.ForegroundColor = ConsoleColor.Green;

    await foreach (ChatMessageContent content in chat.InvokeAsync(agent))
    {
        Console.WriteLine(content.Content);
    }
}

#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0110
