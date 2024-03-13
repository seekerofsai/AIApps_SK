using Azure.Identity;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Plugins.MsGraph;
using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors;
using System.Text.Json;


var configuration = new ConfigurationBuilder()
    .AddUserSecrets("aiapps")
    .AddEnvironmentVariables()
    .Build();

string apiKey = configuration["AzureOpenAI:ApiKey"];
string deploymentName = configuration["AzureOpenAI:DeploymentChatName"];
string endpoint = configuration["AzureOpenAI:Endpoint"];
string bingKey = configuration["Bing:ApiKey"];
string tenantId = configuration["MSGraph:TenantId"];
string clientId = configuration["MSGraph:ClientId"];
string clientSecret = configuration["MsGraph:clientSecret"];

var scopes = new[] { "Calendars.Read"};
var options = new DeviceCodeCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
    ClientId = clientId,
    TenantId  = tenantId,

    DeviceCodeCallback = (code, cancellation) =>
    {
        Console.WriteLine(code.Message);
        return Task.FromResult(0);
    },

};

var deviceCodeCredential = new DeviceCodeCredential(options);
var graphClient = new GraphServiceClient(deviceCodeCredential, scopes);



var builder = Kernel.CreateBuilder();
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName,
    endpoint,
    apiKey);
var kernel = builder.Build();


#pragma warning disable SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var bingConnector = new BingConnector(bingKey);
var bingplugin = new WebSearchEnginePlugin(bingConnector);
kernel.ImportPluginFromObject(bingplugin, "BingPlugin");

#pragma warning restore SKEXP0054 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0053 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
OutlookCalendarConnector connector = new OutlookCalendarConnector(graphClient);
CalendarPlugin calplugin = new CalendarPlugin(connector);
kernel.ImportPluginFromObject(calplugin, "CalendarPlugin");
#pragma warning restore SKEXP0053 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};


// MSGraph Plugin
string prompt = "What is my next meeting?";
var results = kernel.InvokePromptStreamingAsync(prompt, new KernelArguments(settings));
await foreach (var message in results)
{
    Console.Write(message);
}


// Bing Plugin
var results = kernel.InvokePromptStreamingAsync("What is Semantic Kernel from Microsoft?", new KernelArguments(settings));
await foreach (var message in results)
{
    Console.WriteLine(message);
}

Console.WriteLine();
Console.ReadLine();


// var chatHistory = new ChatHistory();
// chatHistory.AddMessage(AuthorRole.User, "What is Industrial Solutions Engineering at Microsoft?");

// var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
// var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);

// var functionCalls = ((OpenAIChatMessageContent)result).GetOpenAIFunctionToolCalls();
// foreach (var functionCall in functionCalls)
// {
//     KernelFunction pluginFunction;
//     KernelArguments arguments;
//     kernel.Plugins.TryGetFunctionAndArguments(functionCall, out pluginFunction, out arguments);
//     var functionResult = await kernel.InvokeAsync(pluginFunction!, arguments!);
//     var jsonResponse = functionResult.GetValue<object>();
//     var json = JsonSerializer.Serialize(jsonResponse);
//     Console.WriteLine(json);
//     chatHistory.AddMessage(AuthorRole.Tool, json);
// }

// result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);

// Console.WriteLine(result.Content);
// Console.ReadLine();




