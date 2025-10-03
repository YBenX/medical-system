using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalSystem.Models;

/// <summary>
/// 医生排班实体模型
/// </summary>
public class Schedule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// 时段：上午、下午、晚上
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TimeSlot { get; set; } = string.Empty;

    /// <summary>
    /// 总号源数
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// 剩余号源数
    /// </summary>
    public int AvailableSlots { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("DoctorId")]
    public Doctor Doctor { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
