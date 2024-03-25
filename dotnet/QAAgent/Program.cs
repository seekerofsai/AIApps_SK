using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets("aiapps")
    .AddEnvironmentVariables()
    .Build();

// Read config 
string apiKey = configuration["AzureOpenAI:ApiKey"];
string deploymentChatName = configuration["AzureOpenAI:DeploymentChatName"];
string deploymentEmbeddingName = configuration["AzureOpenAI:DeploymentEmbeddingName"];
string endpoint = configuration["AzureOpenAI:Endpoint"];
string searchApiKey = configuration["AzureAISeach:ApiKey"];
string searchEndpoint = configuration["AzureAISeach:Endpoint"];


// Build Serverless Memory 
var embeddingsConfig = new AzureOpenAIConfig
{
    APIKey = apiKey,
    Deployment = deploymentEmbeddingName,
    Endpoint = endpoint,
    APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
    Auth = AzureOpenAIConfig.AuthTypes.APIKey
};

var chatConfig = new AzureOpenAIConfig
{
    APIKey = apiKey,
    Deployment = deploymentChatName,
    Endpoint = endpoint,
    APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
    Auth = AzureOpenAIConfig.AuthTypes.APIKey    
};

var memory = new KernelMemoryBuilder()
    .WithAzureOpenAITextGeneration(chatConfig)
    .WithAzureOpenAITextEmbeddingGeneration(embeddingsConfig)
    .WithAzureAISearchMemoryDb(searchEndpoint, searchApiKey)
    .Build<MemoryServerless>();


// Wrapper async methods to upload and ask question using memory service
async Task<bool> StoreText(string text)
{
    try
    {
        await memory.ImportTextAsync(text);
        return true;
    }
    catch
    {
        return false;
    }
}


async Task<bool> StoreFile(string path, string filename)
{
    try{
        await memory.ImportDocumentAsync(path, documentId: filename);
        return true;
    }
    catch{
        return false;
    }

}

async Task<bool> StoreWebsite(string url)
{
    try{
        await memory.ImportWebPageAsync(url);
        return true;
    }
    catch{
        return false;
    }

}

async Task<MemoryAnswer> AskQuestion(string question)
{
    var answer = await memory.AskAsync(question);
    return answer;

}


// Ingest files
bool ingestion = true;
bool purge = true;
var toDelete = new List<string>();

if (ingestion)
{
    Console.WriteLine("\n====================================\n");

    // Uploading some text, without using files. Hold a copy of the ID to delete it later.
    Console.WriteLine("Uploading text about E=mc^2");
    var docId = await memory.ImportTextAsync("In physics, mass–energy equivalence is the relationship between mass and energy " +
                                             "in a system's rest frame, where the two quantities differ only by a multiplicative " +
                                             "constant and the units of measurement. The principle is described by the physicist " +
                                             "Albert Einstein's formula: E = m*c^2");
    toDelete.Add(docId);

    // Simple file upload, with document ID
    toDelete.Add("doc001");
    Console.WriteLine("Uploading article file about Carbon");
    await memory.ImportDocumentAsync("./Documents/file1-Wikipedia-Carbon.txt", documentId: "doc001");

    
    // Uploading multiple files and adding a user tag, checking if the document already exists
    toDelete.Add("doc002");
    if (!await memory.IsDocumentReadyAsync(documentId: "doc002"))
    {
        Console.WriteLine("Uploading a text file, a Word doc, and a PDF about Kernel Memory");
        await memory.ImportDocumentAsync(new Document("doc002")
            .AddFiles(new[] { "./Documents/file2-Wikipedia-Moon.txt", "./Documents/file3-lorem-ipsum.docx", "./Documents/file4-KM-Readme.pdf" })
            .AddTag("user", "Blake"));
    }
    else
    {
        Console.WriteLine("doc002 already uploaded.");
    }

    // Categorizing files with several tags
    toDelete.Add("doc003");
    if (!await memory.IsDocumentReadyAsync(documentId: "doc003"))
    {
        Console.WriteLine("Uploading a PDF with a news about NASA and Orion");
        await memory.ImportDocumentAsync(new Document("doc003")
            .AddFile("./Documents/file5-NASA-news.pdf")
            .AddTag("user", "Taylor")
            .AddTag("collection", "meetings")
            .AddTag("collection", "NASA")
            .AddTag("collection", "space")
            .AddTag("type", "news"));
    }
    else
    {
        Console.WriteLine("doc003 already uploaded.");
    }

    // Downloading web pages
    toDelete.Add("webPage1");
    if (!await memory.IsDocumentReadyAsync("webPage1"))
    {
        Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md");
        await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md", documentId: "webPage1");
    }
    else
    {
        Console.WriteLine("webPage1 already uploaded.");
    }

    // Custom pipelines, e.g. excluding summarization
    toDelete.Add("webPage2");
    if (!await memory.IsDocumentReadyAsync("webPage2"))
    {
        Console.WriteLine("Uploading https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md");
        await memory.ImportWebPageAsync("https://raw.githubusercontent.com/microsoft/kernel-memory/main/docs/security/security-filters.md",
            documentId: "webPage2",
            steps: Constants.PipelineWithoutSummary);
    }
    else
    {
        Console.WriteLine("webPage2 already uploaded.");
    }
}

Console.WriteLine("\n====================================\n");

foreach (var docId in toDelete)
{
    while (!await memory.IsDocumentReadyAsync(documentId: docId))
    {
        Console.WriteLine("Waiting for memory ingestion to complete...");
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}


// === retrieval =============
string question; 
do
{
    Console.WriteLine("Ask me a question?");
    question = Console.ReadLine();

    var kernelrespone = await AskQuestion(question);
    Console.WriteLine("The Answer is : " + kernelrespone.Result);
    var citations = kernelrespone.RelevantSources;

    foreach (var citation in kernelrespone.RelevantSources)
    {
        Console.WriteLine("File Name: " + citation.SourceName);
        Console.WriteLine("File Type: " + citation.SourceContentType);
    }
}while(!string.IsNullOrWhiteSpace(question));





// === PURGE =============

if (purge)
{
    Console.WriteLine("====================================");

    foreach (var docId in toDelete)
    {
        Console.WriteLine($"Deleting memories derived from {docId}");
        await memory.DeleteDocumentAsync(docId);
    }
}
