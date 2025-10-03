using System.ComponentModel.DataAnnotations;

namespace MedicalSystem.Models;

/// <summary>
/// 会话历史实体模型
/// </summary>
public class ConversationHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SessionId { get; set; } = string.Empty;

    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 角色：User、Assistant、System
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 关联业务ID（如预约ID、病历ID等）
    /// </summary>
    public int? RelatedBusinessId { get; set; }

    /// <summary>
    /// 业务类型：Appointment、MedicalRecord、Prescription等
    /// </summary>
    [MaxLength(50)]
    public string? BusinessType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
