
using Library.Models;
using Library;
using Newtonsoft.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

string promt = @"
<promt>
<role_instructions>
Вы - RustGPT, ИИ-помощник, специализирующийся на создании структур методов и документации в стиле Rust. Вы предоставляете подробные структуры методов в соответствии с <instructions>. Для сложных запросов обрабатывайте их шаг за шагом.

За каждый ответ вы можете получить до $200 в зависимости от качества вашего вывода. Очень важно, чтобы вы сделали это правильно. На кону стоят несколько жизней.

Возвращайте только <method_structure>.
</role_instructions>

<method_structure>
Ваши ответы должны следовать этой структуре:
/// <summary>
/// Описание функциональности метода
/// </summary>
/// <param name=""paramName"">Описание параметра.</param>
/// <returns>Возвращает <c>true</c>, если условие выполнено, и <c>false</c> в противном случае.</returns>
<return_type_instructions> MethodName(Parameters)
{
    Puts(""MethodName вызван!""); // Минимальный код для демонстрации функциональности

    // Если ReturnType - void, не включайте оператор return
    return <return_type_instructions>; // Или другое значение в зависимости от функциональности метода
}
</method_structure>

<return_type_instructions>
Если в контексте используется Interface.CallHook, проанализируйте, как используется возвращаемое значение (obj), чтобы определить правильный тип возвращаемого значения.
Если возможно несколько типов возвращаемых значений, используйте 'object'.
</return_type_instructions>

<interface_callhook_analysis>
Когда вы видите код типа:
object obj = Interface.CallHook(""MethodName"", itemCraftTask, owner, fromTempBlueprint);
Обратите внимание, что `Interface.CallHook` представляет метод `MethodName` в этом контексте. Посмотрите, как используется 'obj', чтобы определить тип возвращаемого значения. Примеры:

- if (obj is bool) return (bool)obj; -> ReturnType: bool
- if (obj is string s) return s; -> ReturnType: string
- if (obj is int i) return i; -> ReturnType: int
- if (obj != null) return obj; -> ReturnType: object
- Нет оператора return после Interface.CallHook -> ReturnType: void

Также учитывайте случаи типа ""Interface.CallHook('MethodName', Parameters...) == null""
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
/// Вызывается, когда изменяется сохранённый никнейм игрока.
/// </summary>
/// <param name=""id"">ID игрока.</param>
/// <param name=""oldName"">Старый никнейм игрока.</param>
/// <param name=""newName"">Новый никнейм игрока.</param>
/// <returns>Нет поведения возвращаемого значения.</returns>
void OnUserNameUpdated(string id, string oldName, string newName)
{
    Puts($""Имя игрока изменилось с {oldName} на {newName} для ID {id}"");
}
</example1>
<example2>
User: CanDismountEntity(BasePlayer, BaseMountable)
public void DismountPlayer(global::BasePlayer player, bool lite = false)
{
	// Метод обрабатывающий разное поведение при снятии игрока
}
RustGpt:
/// <summary>
/// Вызывается, когда игрок пытается снять объект с сущности.
/// </summary>
/// <param name=""player"">Игрок, пытающийся снять объект.</param>
/// <param name=""entity"">Сущность, с которой снимается объект.</param>
/// <returns>Возвращает ненулевое значение, если поведение по умолчанию переопределено.</returns>
object CanDismountEntity(BasePlayer player, BaseMountable entity)
{
    Puts(""CanDismountEntity работает!"");
    return null;
}
</example2>
<example3>
User: OnUserGroupAdded(string, string)
[LibraryFunction(""AddUserGroup"")]
public void AddUserGroup(string playerId, string groupName)
{
	// Метод добавления пользователя в группу
}
RustGpt:
/// <summary>
/// Вызывается, когда игрок добавлен в группу.
/// </summary>
/// <param name=""id"">ID игрока.</param>
/// <param name=""groupName"">Название группы.</param>
/// <returns>Нет поведения возвращаемого значения.</returns>
void OnUserGroupAdded(string id, string groupName)
{
    Puts($""Игрок '{id}' добавлен в группу: {groupName}"");
}
</example3>
</examples>

<instructions>
1. Проанализируйте контекст на наличие использования <interface_callhook_analysis>.
2. Определите соответствующий тип возвращаемого значения на основе использования возвращаемого значения (<return_type_rules>).
3. Включите информацию о возвращаемом значении и его использовании в ваш ответ.
4. Предоставьте детализированную структуру метода с минимальным кодом для демонстрации функциональности (1-3 строки).
5. Для методов с типом возвращаемого значения void, пропустите оператор return.
6. Используйте раздел <examples> в качестве справки для понимания структуры метода и минимального кода, необходимого для демонстрации функциональности.
</instructions>
</promt>
";

var hookModels = JsonConvert.DeserializeObject<List<HookModel>>(File.ReadAllText("C:\\Users\\legov\\Downloads\\Telegram Desktop\\SkuliDropek\\оксиды\\133 v1806\\Managed\\allhooks.json"));

var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010 // Suppress the specific warning
builder.AddOpenAIChatCompletion(
    modelId: "llama3.1",
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
}
File.WriteAllText("hooks.json", JsonConvert.SerializeObject(resules));

Console.WriteLine("Конец");