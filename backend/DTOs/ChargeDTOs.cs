namespace MedicalSystem.DTOs;

/// <summary>
/// 费用明细项
/// </summary>
public class ChargeItemDto
{
    public string ItemName { get; set; } = string.Empty; // 费用项名称
    public string ItemType { get; set; } = string.Empty; // 费用类型: 挂号费、诊疗费、药品费、检查费
    public int Quantity { get; set; } = 1; // 数量
    public decimal UnitPrice { get; set; } // 单价
    public decimal Amount => Quantity * UnitPrice; // 金额
}

/// <summary>
/// 创建收费记录请求DTO
/// </summary>
public class CreateChargeDto
{
    public int PatientId { get; set; }
    public int? MedicalRecordId { get; set; }
    public int? PrescriptionId { get; set; }
    public List<ChargeItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// 收费记录响应DTO
/// </summary>
public class ChargeDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? MedicalRecordId { get; set; }
    public int? PrescriptionId { get; set; }
    public DateTime ChargeDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty; // 已支付、未支付、已退款
    public DateTime? PaymentTime { get; set; }
    public string? PaymentMethod { get; set; } // 现金、支付宝、微信、银行卡
    public List<ChargeItemDto> Items { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// 支付DTO
/// </summary>
public class PaymentDto
{
    public string PaymentMethod { get; set; } = string.Empty; // 现金、支付宝、微信、银行卡
    public decimal Amount { get; set; } // 支付金额
}

/// <summary>
/// 退费DTO
/// </summary>
public class RefundDto
{
    public string Reason { get; set; } = string.Empty; // 退费原因
}
