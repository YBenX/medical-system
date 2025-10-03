namespace MedicalSystem.DTOs;

/// <summary>
/// 创建病历请求DTO
/// </summary>
public class CreateMedicalRecordDto
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int? AppointmentId { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty; // 主诉
    public string PresentIllness { get; set; } = string.Empty; // 现病史
    public string PhysicalExam { get; set; } = string.Empty; // 体格检查
    public string Diagnosis { get; set; } = string.Empty; // 诊断
    public string? ICD10Code { get; set; } // ICD-10编码
    public string? TreatmentPlan { get; set; } // 治疗方案
    public string? Notes { get; set; } // 备注
}

/// <summary>
/// 更新病历请求DTO
/// </summary>
public class UpdateMedicalRecordDto
{
    public string? ChiefComplaint { get; set; }
    public string? PresentIllness { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Diagnosis { get; set; }
    public string? ICD10Code { get; set; }
    public string? TreatmentPlan { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// 病历响应DTO
/// </summary>
public class MedicalRecordDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public DateTime VisitDate { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string PresentIllness { get; set; } = string.Empty;
    public string PhysicalExam { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string? ICD10Code { get; set; }
    public string? TreatmentPlan { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 患者病历历史查询DTO
/// </summary>
public class PatientMedicalHistoryDto
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public List<MedicalRecordDto> Records { get; set; } = new();
}
