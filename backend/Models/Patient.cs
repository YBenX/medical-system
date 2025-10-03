using System.ComponentModel.DataAnnotations;

namespace MedicalSystem.Models;

/// <summary>
/// 患者实体模型
/// </summary>
public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    [MaxLength(18)]
    public string? IdCard { get; set; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// 过敏史
    /// </summary>
    [MaxLength(500)]
    public string? Allergies { get; set; }

    /// <summary>
    /// 既往病史
    /// </summary>
    [MaxLength(1000)]
    public string? MedicalHistory { get; set; }

    /// <summary>
    /// 家族病史
    /// </summary>
    [MaxLength(1000)]
    public string? FamilyHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
