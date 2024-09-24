
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

<instructions>
1. Analyze the context for the presence of <interface_callhook_analysis>.
2. Determine the appropriate return type based on the usage of the return value (<return_type_rules>).
3. Include information about the return value and its usage in your response.
4. Provide a detailed method structure with minimal code to demonstrate functionality (1-3 lines).
5. For methods with a void return type, omit the return statement.
6. Use the <examples> section as a reference to understand the method structure and the minimal code necessary to demonstrate functionality.
7. Keep your response concise, within a limit of 300 tokens.
8. To output a message to the console, use Puts(""Message"")
</instructions>
</prompt>
";

var hookModels = JsonConvert.DeserializeObject<List<HookModel>>(File.ReadAllText("C:\\RustServer 2.0\\rustserver\\RustDedicated_Data\\Managed\\allhooks.json"));

var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010 // Suppress the specific warning
builder.AddOpenAIChatCompletion(
    modelId: "granite-code:8b",
    endpoint: new Uri("http://localhost:11434"),
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
#pragma warning restore SKEXP0010 // Restore the warning

var kernel = builder.Build();

Dictionary<string, (string, HookModel)> resules = new Dictionary<string, (string, HookModel)>();

resules = JsonConvert.DeserializeObject< Dictionary<string, (string, HookModel)> >(File.ReadAllText("hooks.json"));

int index = 0;
foreach (var model in hookModels)
{
	index++;
	if(resules.ContainsKey(model.Name + model.Parameters))
	{
		continue;
	}

    var chatHistory = new ChatHistory();


    #region DataSet OnUserNameUpdated
    chatHistory.AddUserMessage(@"OnUserNameUpdated(string, string, string)

```csharp
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
```");

    chatHistory.AddAssistantMessage(@"```csharp
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
```");
    #endregion

    #region DataSet OnPlayerSpectate
    chatHistory.AddUserMessage(@"OnPlayerSpectate(BasePlayer,string)

```csharp
public void StartSpectating()
{
	if (!IsSpectating() && Interface.CallHook(""OnPlayerSpectate"", this, spectateFilter) == null)
	{
		SetPlayerFlag(PlayerFlags.Spectating, b: true);
		UnityEngine.TransformEx.SetLayerRecursive(base.gameObject, 10);
		CancelInvoke(InventoryUpdate);
		ChatMessage(""Becoming Spectator"");
		UpdateSpectateTarget(spectateFilter);
	}
}
```");

    chatHistory.AddAssistantMessage(@"```csharp
/// <summary>
/// Called when a player starts spectating.
/// </summary>
/// <param name=""player"">The player starting to spectate.</param>
/// <param name=""filter"">The filter used for spectating, which could be a player name or other identifier.</param>
/// <returns>
/// Returns `null` to allow the player to start spectating, or any non-null value to prevent them from spectating.
/// </returns>
object OnPlayerSpectate(BasePlayer player, string filter)
{
    Puts($""Player {player.UserIDString} started spectating with filter: {filter}"");
    if (filter == ""restricted"")
    {
        Puts($""Player {player.displayName} is not allowed to spectate with filter: {filter}"");
        return true;
    }
    return null;
}
```");

    #endregion

    #region DataSet OnUserApproved
    chatHistory.AddUserMessage(@"OnUserApproved(string,string,string)

```csharp
[HookMethod(""IOnUserApprove"")]
private object IOnUserApprove(Connection connection)
{
	string username = connection.username;
	string text = connection.userid.ToString();
	string obj = Regex.Replace(connection.ipaddress, ipPattern, """");
	uint authLevel = connection.authLevel;
	if (permission.IsLoaded)
	{
		permission.UpdateNickname(text, username);
		OxideConfig.DefaultGroups defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
		if (!permission.UserHasGroup(text, defaultGroups.Players))
		{
			permission.AddUserGroup(text, defaultGroups.Players);
		}
		if (authLevel >= 2 && !permission.UserHasGroup(text, defaultGroups.Administrators))
		{
			permission.AddUserGroup(text, defaultGroups.Administrators);
		}
	}
	Covalence.PlayerManager.PlayerJoin(connection.userid, username);
	object obj2 = Interface.CallHook(""CanClientLogin"", connection);
	object obj3 = Interface.CallHook(""CanUserLogin"", username, text, obj);
	object obj4 = ((obj2 == null) ? obj3 : obj2);
	if (obj4 is string || (obj4 is bool && !(bool)obj4))
	{
		ConnectionAuth.Reject(connection, (obj4 is string) ? obj4.ToString() : lang.GetMessage(""ConnectionRejected"", this, text));
		return true;
	}
	object obj5 = Interface.CallHook(""OnUserApprove"", connection);
	object result = Interface.CallHook(""OnUserApproved"", username, text, obj);
	if (obj5 != null)
	{
		return obj5;
	}
	return result;
}");
    chatHistory.AddAssistantMessage(@"```csharp
/// <summary>
/// Called when a user is approved to join the server.
/// </summary>
/// <param name=""username"">The username of the approved user.</param>
/// <param name=""steamId"">The Steam ID of the approved user.</param>
/// <param name=""ipAddress"">The IP address of the approved user.</param>
/// <returns>
/// Returns a non-null value to override the default approval behavior. 
/// If a string is returned, the user will be kicked with the provided message as the reason. 
/// If `null` is returned, the user is approved as normal.
/// </returns>
object OnUserApproved(string username, ulong steamId, string ipAddress)
{
    Puts($""User {username} with SteamID {steamId} and IP {ipAddress} has been approved to join the server."");
    if (ipAddress == ""192.168.1.1"")
    {
        Puts($""User {username} with IP {ipAddress} has been blocked from joining."");
        return ""Blocked: Unauthorized IP address."";
    }
    return null;
}
```");
	#endregion

	#region DataSet CanBeHomingTargeted

	chatHistory.AddUserMessage(@"CanBeHomingTargeted(PlayerHelicopter)

```csharp
public override bool IsValidHomingTarget()
{
	object obj = Interface.CallHook(""CanBeHomingTargeted"", this);
	if (obj is bool)
	{
		return (bool)obj;
	}
	return IsOn();
}
```
");

	chatHistory.AddAssistantMessage(@"
```csharp
/// <summary>
/// Determines whether the specified player-controlled helicopter can be targeted by homing missiles.
/// </summary>
/// <param name=""helicopter"">The player-controlled helicopter to check.</param>
/// <returns>
/// Returns `true` if the helicopter can be targeted by homing missiles; otherwise, returns `false`.
/// If the method returns `null`, the default game logic will be used to determine if the helicopter can be targeted.
/// </returns>
bool? CanBeHomingTargeted(PlayerHelicopter helicopter)
{
    Puts($""Checking homing target for Helicopter ID: {helicopter.net.ID}, Position: {helicopter.transform.position}, OwnerID: {helicopter.OwnerID}"");
    return true;
}
```");
	#endregion

	#region DataSet OnItemCraftFinished

	chatHistory.AddUserMessage(@"OnItemCraftFinished(ItemCraftTask,Item,ItemCrafter)

```csharp
public void FinishCrafting(ItemCraftTask task)
	{
		task.amount--;
		task.numCrafted++;
		ulong skin = ItemDefinition.FindSkin(task.blueprint.targetItem.itemid, task.skinID);
		Item item = ItemManager.CreateByItemID(task.blueprint.targetItem.itemid, 1, skin);
		item.amount = task.blueprint.amountToCreate;
		int amount = item.amount;
		_ = owner.currentCraftLevel;
		bool inSafezone = owner.InSafeZone();
		if (item.hasCondition && task.conditionScale != 1f)
		{
			item.maxCondition *= task.conditionScale;
			item.condition = item.maxCondition;
		}
		item.OnVirginSpawn();
		foreach (ItemAmount ingredient in task.blueprint.ingredients)
		{
			int num = (int)ingredient.amount;
			if (task.takenItems == null)
			{
				continue;
			}
			foreach (Item takenItem in task.takenItems)
			{
				if (takenItem.info == ingredient.itemDef)
				{
					int num2 = Mathf.Min(takenItem.amount, num);
					Facepunch.Rust.Analytics.Azure.OnCraftMaterialConsumed(takenItem.info.shortname, num, base.baseEntity, task.workbenchEntity, inSafezone, item.info.shortname);
					takenItem.UseItem(num);
					num -= num2;
				}
				_ = 0;
			}
		}
		Facepunch.Rust.Analytics.Server.Crafting(task.blueprint.targetItem.shortname, task.skinID);
		Facepunch.Rust.Analytics.Azure.OnCraftItem(item.info.shortname, item.amount, base.baseEntity, task.workbenchEntity, inSafezone);
		owner.Command(""note.craft_done"", task.taskUID, 1, task.amount);
		Interface.CallHook(""OnItemCraftFinished"", task, item, this);
		if (task.instanceData != null)
		{
			item.instanceData = task.instanceData;
		}
		if (!string.IsNullOrEmpty(task.blueprint.UnlockAchievment))
		{
			owner.GiveAchievement(task.blueprint.UnlockAchievment);
		}
		owner.ProcessMissionEvent(BaseMission.MissionEventType.CRAFT_ITEM, item.info.itemid, amount);
		if (owner.inventory.GiveItem(item))
		{
			owner.Command(""note.inv"", item.info.itemid, amount);
			return;
		}
		ItemContainer itemContainer = containers.First();
		owner.Command(""note.inv"", item.info.itemid, amount);
		owner.Command(""note.inv"", item.info.itemid, -item.amount);
		item.Drop(itemContainer.dropPosition, itemContainer.dropVelocity);
	}
```");

	chatHistory.AddAssistantMessage(@"```csharp
/// <summary>
/// Called when an item crafting task is finished.
/// </summary>
/// <param name=""task"">The item crafting task that has been completed.</param>
/// <param name=""item"">The item that was crafted.</param>
/// <param name=""craftingStation"">The entity or station that performed the crafting.</param>
/// <returns>No return behavior.</returns>
void OnItemCraftFinished(ItemCraftTask task, Item item, ItemCrafter craftingStation)
{
    Puts($""Item crafting finished: {item.info.displayName.english} (Shortname: {item.info.shortname}) "" +
         $""crafted at station: {craftingStation?.gameObject.name ?? ""Player inventory""}."");

    if (item.info.shortname == ""explosive.timed"")
    {
        Puts(""A timed explosive has been crafted! Watch out!"");
    }
}
```");

	#endregion

	#region DataSet CanAffordToPlace
	chatHistory.AddUserMessage(@"CanAffordToPlace(BasePlayer,Construction)

public bool CanAffordToPlace(Construction component)
{
	if (isTypeDeployable)
	{
		return true;
	}
	BasePlayer ownerPlayer = GetOwnerPlayer();
	if (!ownerPlayer)
	{
		return false;
	}
	object obj = Interface.CallHook(""CanAffordToPlace"", ownerPlayer, this, component);
	if (obj is bool)
	{
		return (bool)obj;
	}
	if (ownerPlayer.IsInCreativeMode && Creative.freeBuild)
	{
		return true;
	}
	foreach (ItemAmount item in component.defaultGrade.CostToBuild())
	{
		if ((float)ownerPlayer.inventory.GetAmount(item.itemDef.itemid) < item.amount)
		{
			return false;
		}
	}
	return true;
}
");
	chatHistory.AddAssistantMessage(@"```csharp
/// <summary>
/// Called to determine if a player can afford to place a construction.
/// </summary>
/// <param name=""player"">The player attempting to place the construction.</param>
/// <param name=""construction"">The construction being placed.</param>
/// <returns>
/// Returns `true` if the player can afford to place the construction, or `false` if they cannot.
/// If the method returns `null`, the default game logic will determine if the player can afford the placement.
/// </returns>
bool? CanAffordToPlace(BasePlayer player, Construction construction)
{
    Puts($""Player {player.displayName} (ID: {player.UserIDString}) is attempting to place a {construction.fullName}."");

    if (construction.fullName == ""wall.external.high.stone"")
    {
        Puts($""Player {player.displayName} is not allowed to place a high stone wall."");
        return false;
    }

    if (player.IsInCreativeMode)
    {
        Puts($""Player {player.displayName} is in creative mode and can place constructions without resource checks."");
        return true;
    }

    return null;
}
```");
	#endregion

	chatHistory.AddUserMessage($@" 
{model.Name + model.Parameters}

```csharp
{model.MethodCode}
```");

    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    var result = chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        new OpenAIPromptExecutionSettings
        {
            ChatSystemPrompt = promt,
            Temperature = 0,
            TopP = 0,
			//StopSequences = new List<string>() { "```" }
			//MaxTokens = 400,
			//PresencePenalty = 2,
			//Seed = 1
			//FrequencyPenalty = 1,
		},
        kernel: kernel);

    Console.WriteLine($"\n[{index}/{hookModels.Count}]-------------------{model.Name + model.Parameters}-----------------------\n" + result.Result);
    resules.TryAdd(model.Name + model.Parameters, (result.Result.ToString(), model));
    File.WriteAllText("hooks.json", JsonConvert.SerializeObject(resules));
    //File.WriteAllText("hooks.md", ConvertJsonToMarkdown(resules));
}
File.WriteAllText("hooks.json", JsonConvert.SerializeObject(resules.ToDictionary(s => s.Key, s => s.Value.Item1)));
File.WriteAllText("hooks.md", ConvertJsonToMarkdown(resules));
Console.WriteLine("Конец");

string ConvertJsonToMarkdown(Dictionary<string, (string, HookModel)> hooks)
{
    using (StringWriter md = new StringWriter())
    {
        md.WriteLine("# Hook Definitions\n");

        foreach (var hook in hooks)
        {
            md.WriteLine($"## {hook.Key}\n");
            md.WriteLine("```csharp");
            md.WriteLine(hook.Value.Item1);
            md.WriteLine("```\n");

            md.WriteLine("### Source Code from the Library\n");
            md.WriteLine("```csharp");
            md.WriteLine(hook.Value.Item2.MethodCode);
            md.WriteLine("```\n");
        }

        return md.ToString();
    }
}