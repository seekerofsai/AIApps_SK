// Copyright (c) Kevin BEAUGRAND. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernel.Assistants;
using SemanticKernel.Assistants.AutoGen;
using SemanticKernel.Assistants.AutoGen.Plugins;
using SemanticKernel.Assistants.Extensions;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets("aiapps")
    .AddEnvironmentVariables()
    .Build();



using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging
        .AddDebug()
        .AddConfiguration(configuration.GetSection("Logging"));
});

AnsiConsole.Write(new FigletText($"AutoGen").Color(Color.Green));
AnsiConsole.WriteLine("");

IAssistant assistant = null!;

AnsiConsole.Status().Start("Initializing...", ctx =>
{
    string azureOpenAIKey = configuration["AzureOpenAI:ApiKey"]!;
    string azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"]!;
    string azureOpenAIGPT35DeploymentName = configuration["AzureOpenAI:GPT35DeploymentName"]!;
    string azureOpenAIGPT4DeploymentName = configuration["AzureOpenAI:GPT4DeploymentName"]!;

    var codeInterpretionOptions = new CodeInterpretionPluginOptions();
    configuration!.Bind("CodeInterpreter", codeInterpretionOptions);

    var butlerKernelBuilder = Kernel.CreateBuilder()
                            .AddAzureOpenAIChatCompletion(azureOpenAIGPT4DeploymentName, azureOpenAIEndpoint, azureOpenAIKey);

    butlerKernelBuilder.Services.AddSingleton(loggerFactory);

    var butlerKernel = butlerKernelBuilder
                            .Build();


    butlerKernel.ImportPluginFromObject(new FileAccessPlugin(codeInterpretionOptions.OutputFilePath, loggerFactory), "file");
    butlerKernel.ImportPluginFromAssistant(CreateCodeInterpreter(codeInterpretionOptions, azureOpenAIGPT35DeploymentName, azureOpenAIEndpoint, azureOpenAIKey));

    assistant = AssistantAgentBuilder.CreateBuilder()
        .WithKernel(butlerKernel)
        .Build();
});

var options = configuration.GetRequiredSection("CodeInterpreter");

var thread = assistant.CreateThread();

while (true)
{
    var prompt = AnsiConsole.Prompt(new TextPrompt<string>("User > ").PromptStyle("teal"));

    await AnsiConsole.Status().StartAsync("Creating...", async ctx =>
    {
        ctx.Spinner(Spinner.Known.Star);
        ctx.SpinnerStyle(Style.Parse("green"));
        ctx.Status($"Processing ...");

        var answer = await thread.InvokeAsync(prompt).ConfigureAwait(true);

        AnsiConsole.MarkupInterpolated($"AutoGen > [cyan]{answer.Content!}[/]");
    });
}

IAssistant CreateCodeInterpreter(CodeInterpretionPluginOptions codeInterpretionOptions, string azureOpenAIDeploymentName, string azureOpenAIEndpoint, string azureOpenAIKey)
{
    var kernel = Kernel.CreateBuilder()
                        .AddAzureOpenAIChatCompletion(azureOpenAIDeploymentName, azureOpenAIEndpoint, azureOpenAIKey)
                        .Build();

    kernel.ImportPluginFromObject(new CodeInterpretionPlugin(codeInterpretionOptions, loggerFactory), "code");

    return CodeInterpreterBuilder.CreateBuilder()
                                .WithKernel(kernel)
                                .Build();
}