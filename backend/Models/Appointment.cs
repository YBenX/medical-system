using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalSystem.Models;

/// <summary>
/// 预约实体模型
/// </summary>
public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    public int ScheduleId { get; set; }

    public DateTime AppointmentTime { get; set; } = DateTime.Now;

    public DateTime? VisitTime { get; set; }

    /// <summary>
    /// 状态：Scheduled(已预约)、Visited(已就诊)、Cancelled(已取消)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Scheduled";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public Doctor Doctor { get; set; } = null!;

    [ForeignKey("ScheduleId")]
    public Schedule Schedule { get; set; } = null!;

    public MedicalRecord? MedicalRecord { get; set; }
}
