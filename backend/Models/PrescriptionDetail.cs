using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalSystem.Models;

/// <summary>
/// 处方明细实体模型
/// </summary>
public class PrescriptionDetail
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PrescriptionId { get; set; }

    [Required]
    public int MedicineId { get; set; }

    /// <summary>
    /// 用法
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Usage { get; set; } = string.Empty;

    /// <summary>
    /// 用量
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Dosage { get; set; } = string.Empty;

    /// <summary>
    /// 数量
    /// </summary>
    [Required]
    public int Quantity { get; set; }

    /// <summary>
    /// 单价
    /// </summary>
    [Required]
    public decimal UnitPrice { get; set; }

    // 导航属性
    [ForeignKey("PrescriptionId")]
    public Prescription Prescription { get; set; } = null!;

    [ForeignKey("MedicineId")]
    public Medicine Medicine { get; set; } = null!;
}
