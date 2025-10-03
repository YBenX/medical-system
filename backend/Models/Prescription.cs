using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalSystem.Models;

/// <summary>
/// 处方实体模型
/// </summary>
public class Prescription
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MedicalRecordId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    public DateTime PrescriptionDate { get; set; } = DateTime.Now;

    /// <summary>
    /// 总金额（计算属性）
    /// </summary>
    public decimal TotalAmount => Details?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;

    /// <summary>
    /// 状态：Draft(草稿)、Submitted(已提交)、Dispensed(已发药)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("MedicalRecordId")]
    public MedicalRecord MedicalRecord { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public Doctor Doctor { get; set; } = null!;

    public ICollection<PrescriptionDetail> Details { get; set; } = new List<PrescriptionDetail>();
}
