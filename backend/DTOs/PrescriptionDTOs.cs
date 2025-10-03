namespace MedicalSystem.DTOs;

/// <summary>
/// 处方明细DTO
/// </summary>
public class PrescriptionDetailItemDto
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty; // 用法用量
    public int Quantity { get; set; } // 数量
    public decimal UnitPrice { get; set; } // 单价
    public decimal Subtotal => Quantity * UnitPrice; // 小计
}

/// <summary>
/// 创建处方请求DTO
/// </summary>
public class CreatePrescriptionDto
{
    public int MedicalRecordId { get; set; }
    public int DoctorId { get; set; }
    public List<PrescriptionDetailItemDto> Items { get; set; } = new();
    public string? Notes { get; set; } // 备注
}

/// <summary>
/// 处方响应DTO
/// </summary>
public class PrescriptionDto
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<PrescriptionDetailItemDto> Items { get; set; } = new();
}

/// <summary>
/// 更新处方状态DTO
/// </summary>
public class UpdatePrescriptionStatusDto
{
    public string Status { get; set; } = string.Empty; // 待审核、已审核、已配药、已取消
}
