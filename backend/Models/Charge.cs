using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalSystem.Models;

/// <summary>
/// 收费实体模型
/// </summary>
public class Charge
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    public int? MedicalRecordId { get; set; }

    public int? PrescriptionId { get; set; }

    public DateTime ChargeDate { get; set; } = DateTime.Now;

    /// <summary>
    /// 费用明细（JSON格式）
    /// </summary>
    [Required]
    public string ItemsJson { get; set; } = "[]";

    /// <summary>
    /// 总金额
    /// </summary>
    [Required]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 支付状态：Unpaid(未支付)、Paid(已支付)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Unpaid";

    public DateTime? PaymentTime { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性
    [ForeignKey("PatientId")]
    public Patient Patient { get; set; } = null!;

    [ForeignKey("MedicalRecordId")]
    public MedicalRecord MedicalRecord { get; set; } = null!;
}
