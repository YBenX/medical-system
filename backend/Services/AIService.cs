using System.Text;
using System.Text.Json;

namespace MedicalSystem.Services;

/// <summary>
/// AI服务实现（支持DeepSeek和Qwen）
/// </summary>
public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;

    public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(string sessionId, string userMessage, List<ConversationMessage>? history = null)
    {
        var provider = _configuration["AI:Provider"] ?? "DeepSeek";
        var apiKey = _configuration[$"AI:{provider}:ApiKey"];
        var apiUrl = _configuration[$"AI:{provider}:ApiUrl"];
        var model = _configuration[$"AI:{provider}:Model"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
        {
            throw new InvalidOperationException($"AI配置不完整，请检查 AI:{provider} 配置项");
        }

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = GetSystemPrompt()
            }
        };

        // 添加历史消息
        if (history != null && history.Any())
        {
            foreach (var msg in history.TakeLast(10)) // 只取最近10条
            {
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
            }
        }

        // 添加当前用户消息
        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model = model ?? "deepseek-chat",
            messages = messages,
            temperature = 0.7,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        try
        {
            var response = await _httpClient.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var assistantMessage = result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return assistantMessage ?? "抱歉，我无法回答这个问题。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用AI服务失败");
            return "抱歉，AI服务暂时不可用，请稍后再试。";
        }
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(string sessionId, string userMessage, List<ConversationMessage>? history = null)
    {
        var provider = _configuration["AI:Provider"] ?? "DeepSeek";
        var apiKey = _configuration[$"AI:{provider}:ApiKey"];
        var apiUrl = _configuration[$"AI:{provider}:ApiUrl"];
        var model = _configuration[$"AI:{provider}:Model"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
        {
            yield return "AI配置不完整";
            yield break;
        }

        var messages = new List<object>
        {
            new { role = "system", content = GetSystemPrompt() }
        };

        if (history != null && history.Any())
        {
            foreach (var msg in history.TakeLast(10))
            {
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
            }
        }

        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model = model ?? "deepseek-chat",
            messages = messages,
            temperature = 0.7,
            max_tokens = 2000,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl) { Content = content };
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]") break;

            string? chunk = null;
            try
            {
                var jsonData = JsonSerializer.Deserialize<JsonElement>(data);
                var delta = jsonData
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var contentElement))
                {
                    chunk = contentElement.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析流式响应失败: {Line}", line);
            }

            if (!string.IsNullOrEmpty(chunk))
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// 从对话中提取结构化的患者信息
    /// </summary>
    public async Task<PatientExtractionResult?> ExtractPatientInfoAsync(List<ConversationMessage> conversation)
    {
        var provider = _configuration["AI:Provider"] ?? "DeepSeek";
        var apiKey = _configuration[$"AI:{provider}:ApiKey"];
        var apiUrl = _configuration[$"AI:{provider}:ApiUrl"];
        var model = _configuration[$"AI:{provider}:Model"];

        var extractionPrompt = @"请从以下对话中提取患者的基本信息和病史信息。如果某些信息没有提及，请设置为null。

要求：
1. 仔细分析对话内容，提取所有可用的患者信息
2. 返回JSON格式的结构化数据
3. 如果对话中没有足够的患者信息，返回 {""hasData"": false}
4. 年龄可以从出生日期推算，或直接使用对话中提到的年龄

返回格式：
{
  ""hasData"": true,
  ""name"": ""姓名"",
  ""gender"": ""男/女"",
  ""dateOfBirth"": ""1990-01-01"",
  ""phone"": ""13800138000"",
  ""idCard"": ""身份证号"",
  ""address"": ""地址"",
  ""allergies"": ""过敏史"",
  ""medicalHistory"": ""既往病史"",
  ""familyHistory"": ""家族病史""
}";

        var messages = new List<object>
        {
            new { role = "system", content = extractionPrompt }
        };

        // 添加对话历史
        foreach (var msg in conversation)
        {
            messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
        }

        messages.Add(new { role = "user", content = "请提取以上对话中的患者信息，返回JSON格式" });

        var requestBody = new
        {
            model = model ?? "deepseek-chat",
            messages = messages,
            temperature = 0.3, // 降低温度以获得更准确的提取
            max_tokens = 1000
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var extractedContent = result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(extractedContent))
                return null;

            // 尝试从返回内容中提取JSON
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(extractedContent, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
                return null;

            var extractedData = JsonSerializer.Deserialize<PatientExtractionResult>(jsonMatch.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return extractedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取患者信息失败");
            return null;
        }
    }

    private string GetSystemPrompt()
    {
        return @"你是一位专业的医疗助理，负责协助患者完成门诊诊疗流程。你的职责包括：

1. 患者建档：收集患者基本信息（姓名、性别、年龄、联系方式等）和病史信息（过敏史、既往病史、家族病史）
2. 预约挂号：帮助患者选择合适的科室和医生，安排就诊时间
3. 辅助看诊：协助医生记录主诉、现病史、体格检查结果和诊断
4. 处方管理：协助医生开具处方，记录用药信息
5. 收费指导：说明各项费用，引导患者完成缴费

交互要求：
- 使用专业但易懂的语言与患者沟通
- 通过自然对话收集患者信息，询问姓名、性别、年龄、联系方式、身份证号、地址等
- 询问过敏史、既往病史、家族病史等重要医疗信息
- 当收集到足够的患者基本信息后，在回复末尾包含 [ACTION:SAVE_PATIENT] 提示保存患者档案
- 当用户询问预约/挂号/看病时，系统会自动提供实时排班信息（Markdown表格格式），你需要：
  1. 将排班表格原样展示给用户
  2. 解释表格内容，帮助用户选择合适的医生
  3. 在回复末尾包含 [ACTION:APPOINTMENT] 按钮
- 识别用户意图（建档/挂号/看诊/查询等），提供针对性的帮助
- 保护患者隐私，确保信息安全
- 如果遇到专业医疗问题，建议患者咨询医生

特殊标记说明：
- [ACTION:SAVE_PATIENT] - 触发保存患者档案（当收集到姓名、性别、联系方式等基本信息时）
- [ACTION:APPOINTMENT] - 触发预约挂号功能

信息收集示例：
用户：我想看病
助理：好的，我来帮您。请问您贵姓？

用户：我姓张，叫张三
助理：张三您好，请问您的性别和年龄？

用户：男，35岁
助理：好的。请问您的联系电话是多少？

用户：13800138000
助理：收到。请问您有什么不舒服的症状吗？另外，您有什么过敏史或既往病史需要告知吗？

用户：头疼，没有过敏
助理：了解了。我已经收集到您的基本信息，是否现在为您建立患者档案？[ACTION:SAVE_PATIENT]

预约示例：
用户：我要挂号
系统自动提供：[系统提供的实时排班信息]
近期排班信息（2025-10-03 - 2025-10-10）：

【内科】
| 医生姓名 | 职称 | 日期 | 时段 | 剩余号源 |
|---------|------|--------|------|---------|
| 李明 | 主任医师 | 10-04 | 上午 | 10/10 |
| 李明 | 主任医师 | 10-05 | 上午 | 10/10 |
| 张伟 | 副主任医师 | 10-04 | 下午 | 15/15 |

助理：好的，以下是近期的排班信息：

【内科】
| 医生姓名 | 职称 | 日期 | 时段 | 剩余号源 |
|---------|------|--------|------|---------|
| 李明 | 主任医师 | 10-04 | 上午 | 10/10 |
| 李明 | 主任医师 | 10-05 | 上午 | 10/10 |
| 张伟 | 副主任医师 | 10-04 | 下午 | 15/15 |

内科近期有以下医生可预约：
- 李明医生（主任医师）在10月4日、5日上午有号
- 张伟医生（副主任医师）在10月4日下午有号

请问您想预约哪位医生的哪个时段？[ACTION:APPOINTMENT]

请始终保持友好、专业、高效的服务态度。注意：当系统提供排班信息时，务必将表格完整展示给用户。";
    }

    /// <summary>
    /// 解析预约意图
    /// </summary>
    public async Task<AppointmentIntentResult?> ParseAppointmentIntentAsync(string userMessage)
    {
        var provider = _configuration["AI:Provider"] ?? "DeepSeek";
        var apiKey = _configuration[$"AI:{provider}:ApiKey"];
        var apiUrl = _configuration[$"AI:{provider}:ApiUrl"];
        var model = _configuration[$"AI:{provider}:Model"];

        var intentPrompt = @"从用户的预约请求中提取关键信息。返回JSON格式，如果无法提取则返回 {""hasIntent"": false}

用户输入示例：
- ""给张丽预约明天刘洋医生下午的号""
- ""我要预约李明医生，明天上午""
- ""帮我妈妈挂王医生后天的号""

提取以下信息：
1. 患者姓名
2. 医生姓名
3. 日期（今天、明天、后天、具体日期等，转为相对天数offset）
4. 时段（上午/下午/晚上）

返回格式：
{
  ""hasIntent"": true,
  ""patientName"": ""张丽"",
  ""doctorName"": ""刘洋"",
  ""dateOffset"": 1,  // 0=今天, 1=明天, 2=后天
  ""timeSlot"": ""下午""
}

如果信息不全，仍然返回已提取到的部分，但标注缺失的字段为null。";

        var messages = new List<object>
        {
            new { role = "system", content = intentPrompt },
            new { role = "user", content = userMessage }
        };

        var requestBody = new
        {
            model = model ?? "deepseek-chat",
            messages = messages,
            temperature = 0.2, // 降低温度以获得更精确的提取
            max_tokens = 500
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var extractedContent = result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(extractedContent))
                return null;

            // 提取JSON
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(extractedContent, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
                return null;

            var intentData = JsonSerializer.Deserialize<AppointmentIntentResult>(jsonMatch.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return intentData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析预约意图失败");
            return null;
        }
    }
}

/// <summary>
/// 患者信息提取结果
/// </summary>
public class PatientExtractionResult
{
    public bool HasData { get; set; }
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? IdCard { get; set; }
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? FamilyHistory { get; set; }
}

/// <summary>
/// 预约意图解析结果
/// </summary>
public class AppointmentIntentResult
{
    public bool HasIntent { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public int? DateOffset { get; set; } // 0=今天, 1=明天, 2=后天
    public string? TimeSlot { get; set; } // 上午/下午/晚上
}
