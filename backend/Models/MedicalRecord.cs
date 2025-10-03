using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalSystem.Models;

/// <summary>
/// 病历实体模型
/// </summary>
public class MedicalRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    public int? AppointmentId { get; set; }

    public DateTime VisitDate { get; set; } = DateTime.Now;

    /// <summary>
    /// 主诉
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ChiefComplaint { get; set; } = string.Empty;

    /// <summary>
    /// 现病史
    /// </summary>
    [MaxLength(1000)]
    public string? PresentIllness { get; set; }

    /// <summary>
    /// 体格检查
    /// </summary>
    [MaxLength(1000)]
    public string? PhysicalExamination { get; set; }

    /// <summary>
    /// 体格检查 (别名)
    /// </summary>
    public string? PhysicalExam
    {
        get => PhysicalExamination;
        set => PhysicalExamination = value;
    }

    /// <summary>
    /// 诊断
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Diagnosis { get; set; } = string.Empty;

    /// <summary>
    /// ICD-10编码
    /// </summary>
    [MaxLength(20)]
    public string? IcdCode { get; set; }

    /// <summary>
    /// ICD-10编码 (别名)
    /// </summary>
    public string? ICD10Code
    {
        get => IcdCode;
        set => IcdCode = value;
    }

    /// <summary>
    /// 治疗建议
    /// </summary>
    [MaxLength(1000)]
    public string? Treatment { get; set; }

    /// <summary>
    /// 治疗方案 (别名)
    /// </summary>
    public string? TreatmentPlan
    {
        get => Treatment;
        set => Treatment = value;
    }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public Doctor Doctor { get; set; } = null!;

    [ForeignKey("AppointmentId")]
    public Appointment? Appointment { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public Charge? Charge { get; set; }
}
