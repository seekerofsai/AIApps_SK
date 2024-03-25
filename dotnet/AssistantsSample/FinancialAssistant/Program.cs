// Copyright (c) Kevin BEAUGRAND. All rights reserved.

using FinancialAssistant.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernel.Assistants;
using SemanticKernel.Assistants.Extensions;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets("aiapps")
    .AddEnvironmentVariables()
    .Build();

// Read config 
string azureOpenAIKey = configuration["AzureOpenAI:ApiKey"];
string azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
string azureOpenAIDeploymentName = configuration["AzureOpenAI:GPT35Endpoint"];

using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging
        .AddConsole(opts =>
        {
            opts.FormatterName = "simple";
        })
        .AddConfiguration(configuration.GetSection("Logging"));
});

AnsiConsole.Write(new FigletText($"Copilot").Color(Color.Green));
AnsiConsole.WriteLine("");

IAssistant assistant = null!;

AnsiConsole.Status().Start("Initializing...", ctx =>
{

    var financialKernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(azureOpenAIDeploymentName, azureOpenAIEndpoint, azureOpenAIKey)
                    .Build();

    financialKernel.ImportPluginFromObject(new FinancialPlugin(), "financial");

    var financialCalculator = AssistantBuilder.FromTemplate("./Assistants/FinancialCalculator.yaml")
                                                .WithKernel(financialKernel)
                                                .Build();

    var butlerKernel = Kernel.CreateBuilder()
                            .AddAzureOpenAIChatCompletion(azureOpenAIDeploymentName, azureOpenAIEndpoint, azureOpenAIKey)
                            .Build();

    butlerKernel.ImportPluginFromAssistant(financialCalculator);

    assistant = AssistantBuilder.FromTemplate("./Assistants/Butler.yaml")
        .WithKernel(butlerKernel)
        .Build();
});

var thread = assistant.CreateThread();

while (true)
{
    var prompt = AnsiConsole.Prompt(new TextPrompt<string>("User > ").PromptStyle("teal"));

    await AnsiConsole.Status().StartAsync("Processing...", async ctx =>
    {
        ctx.Spinner(Spinner.Known.Star);
        ctx.SpinnerStyle(Style.Parse("green"));
        ctx.Status($"Processing ...");

        var answer = await thread.InvokeAsync(prompt).ConfigureAwait(true);

        AnsiConsole.MarkupLine($"[cyan]Copilot > {answer.Content!}\n[/]");
    });
}