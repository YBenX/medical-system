namespace MedicalSystem.Services;

/// <summary>
/// AI服务接口
/// </summary>
public interface IAIService
{
    /// <summary>
    /// 发送消息并获取AI回复
    /// </summary>
    Task<string> SendMessageAsync(string sessionId, string userMessage, List<ConversationMessage>? history = null);

    /// <summary>
    /// 流式发送消息
    /// </summary>
    IAsyncEnumerable<string> SendMessageStreamAsync(string sessionId, string userMessage, List<ConversationMessage>? history = null);

    /// <summary>
    /// 从对话中提取患者信息
    /// </summary>
    Task<PatientExtractionResult?> ExtractPatientInfoAsync(List<ConversationMessage> conversation);

    /// <summary>
    /// 解析预约意图（从自然语言中提取患者姓名、医生、日期、时段等信息）
    /// </summary>
    Task<AppointmentIntentResult?> ParseAppointmentIntentAsync(string userMessage);
}

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
