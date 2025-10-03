using System.ComponentModel.DataAnnotations;

namespace MedicalSystem.Models;

/// <summary>
/// 医生实体模型
/// </summary>
public class Doctor
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Specialization { get; set; }

    [MaxLength(500)]
    public string? Introduction { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
