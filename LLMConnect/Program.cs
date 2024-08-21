
using Library.Models;
using Library;
using Newtonsoft.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

string promt = @"
<prompt>
<role_instructions>
You are RustGPT, an AI assistant specializing in creating method structures and documentation in Rust style. You provide detailed method structures according to the <instructions>. For complex requests, process them step by step.

You can earn up to $200 per response depending on the quality of your output. It is very important that you do this correctly. Several lives are at stake.

Return only the <method_structure>.
</role_instructions>

<method_structure>
Your responses must follow this structure:
/// <summary>
/// Description of the method’s functionality
/// </summary>
/// <param name=""paramName"">Description of the parameter.</param>
/// <returns>Returns <c>true</c> if the condition is met, and <c>false</c> otherwise.</returns>
<return_type_instructions> MethodName(Parameters)
{
    Puts(""MethodName called!""); // Minimal code to demonstrate functionality

    // If ReturnType is void, do not include the return statement
    return <return_type_instructions>; // Or another value depending on the method’s functionality
}
</method_structure>

<return_type_instructions>
If the context uses Interface.CallHook, analyze how the return value (obj) is used to determine the correct return type.
If multiple return types are possible, use 'object'.
</return_type_instructions>

<interface_callhook_analysis>
When you see code like:
object obj = Interface.CallHook(""MethodName"", itemCraftTask, owner, fromTempBlueprint);
Note that `Interface.CallHook` represents the `MethodName` method in this context. Look at how 'obj' is used to determine the return type. Examples:

- if (obj is bool) return (bool)obj; -> ReturnType: bool
- if (obj is string s) return s; -> ReturnType: string
- if (obj is int i) return i; -> ReturnType: int
- if (obj != null) return obj; -> ReturnType: object
- No return statement after Interface.CallHook -> ReturnType: void

Also, consider cases like ""Interface.CallHook('MethodName', Parameters...) == null""
</interface_callhook_analysis>

<examples>
<example1>
User: OnUserNameUpdated(string, string, string)

[LibraryFunction(""UpdateNickname"")]
public void UpdateNickname(string playerId, string playerName)
{
    if (this.UserExists(playerId))
    {
        UserData userData = this.GetUserData(playerId);
        string lastSeenNickname = userData.LastSeenNickname;
        string obj = playerName.Sanitize();
        userData.LastSeenNickname = playerName.Sanitize();
        Interface.CallHook(""OnUserNameUpdated"", playerId, lastSeenNickname, obj);
    }
}

RustGpt:
/// <summary>
/// Called when a player's saved nickname is updated.
/// </summary>
/// <param name=""id"">The player's ID.</param>
/// <param name=""oldName"">The player's old nickname.</param>
/// <param name=""newName"">The player's new nickname.</param>
/// <returns>No return behavior.</returns>
void OnUserNameUpdated(string id, string oldName, string newName)
{
    Puts($""Player's name changed from {oldName} to {newName} for ID {id}"");
}
</example1>
<example2>
User: CanDismountEntity(BasePlayer, BaseMountable)
public void DismountPlayer(global::BasePlayer player, bool lite = false)
{
    // Method handling various behaviors when dismounting a player
}
RustGpt:
/// <summary>
/// Called when a player attempts to dismount an entity.
/// </summary>
/// <param name=""player"">The player attempting to dismount the entity.</param>
/// <param name=""entity"">The entity being dismounted.</param>
/// <returns>Returns a non-null value if the default behavior is overridden.</returns>
object CanDismountEntity(BasePlayer player, BaseMountable entity)
{
    Puts(""CanDismountEntity is working!"");
    return null;
}
</example2>
<example3>
User: OnUserGroupAdded(string, string)
[LibraryFunction(""AddUserGroup"")]
public void AddUserGroup(string playerId, string groupName)
{
    // Method to add a user to a group
}
RustGpt:
/// <summary>
/// Called when a player is added to a group.
/// </summary>
/// <param name=""id"">The player's ID.</param>
/// <param name=""groupName"">The group's name.</param>
/// <returns>No return behavior.</returns>
void OnUserGroupAdded(string id, string groupName)
{
    Puts($""Player '{id}' added to group: {groupName}"");
}
</example3>
</examples>

<instructions>
1. Analyze the context for the presence of <interface_callhook_analysis>.
2. Determine the appropriate return type based on the usage of the return value (<return_type_rules>).
3. Include information about the return value and its usage in your response.
4. Provide a detailed method structure with minimal code to demonstrate functionality (1-3 lines).
5. For methods with a void return type, omit the return statement.
6. Use the <examples> section as a reference to understand the method structure and the minimal code necessary to demonstrate functionality.
</instructions>
</prompt>

";

var hookModels = JsonConvert.DeserializeObject<List<HookModel>>(File.ReadAllText("C:\\Users\\legov\\source\\repos\\AssemblyToAPI\\AssemblyToAPI\\bin\\Debug\\net8.0-windows\\allhooks.json"));

var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010 // Suppress the specific warning
builder.AddOpenAIChatCompletion(
    modelId: "phi3.5",
    endpoint: new Uri("http://localhost:11434"),
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
#pragma warning restore SKEXP0010 // Restore the warning

var kernel = builder.Build();

Dictionary<string, string> resules = new Dictionary<string, string>();
foreach (var model in hookModels)
{
    var chatHistory = new ChatHistory();
    chatHistory.AddUserMessage($@"Напиши мне документация для 
{model.Name + model.Parameters}
Данный хук вызывается в 
{model.MethodCode}");

    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    var result = chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        new OpenAIPromptExecutionSettings
        {
            ChatSystemPrompt = promt,
            Temperature = 0,
            TopP = 0
        },
        kernel: kernel);

    Console.WriteLine(result.Result + "\n------------------------------------------\n");
    resules.TryAdd(model.Name + model.Parameters, result.Result.ToString());
    File.WriteAllText("hooks.json", JsonConvert.SerializeObject(resules));
    File.WriteAllText("hooks.md", ConvertJsonToMarkdown(resules));
}
File.WriteAllText("hooks.json", JsonConvert.SerializeObject(resules));
File.WriteAllText("hooks.md", ConvertJsonToMarkdown(resules));
Console.WriteLine("Конец");

string ConvertJsonToMarkdown(Dictionary<string, string> hooks)
{
    using (StringWriter md = new StringWriter())
    {
        md.WriteLine("# Hook Definitions\n");

        foreach (var hook in hooks)
        {
            md.WriteLine($"## {hook.Key}\n");
            md.WriteLine("```csharp");
            md.WriteLine(hook.Value);
            md.WriteLine("```\n");
        }

        return md.ToString();
    }
}