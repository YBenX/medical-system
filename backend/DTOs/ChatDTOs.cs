namespace MedicalSystem.DTOs;

public class ChatMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public int? DoctorId { get; set; }
}

public class ChatMessageResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Role { get; set; } = "Assistant";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public object? Data { get; set; } // 可能包含表单数据或其他结构化信息
}

public class ConversationHistoryResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ExtractPatientRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public class PatientExtractionDto
{
    public bool HasData { get; set; }
    public int? ExistingPatientId { get; set; }
    public string? ExistingPatientName { get; set; }
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? IdCard { get; set; }
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? FamilyHistory { get; set; }
}

public class AvailableAppointmentsDto
{
    public DateTime Date { get; set; }
    public List<DepartmentInfoDto> Departments { get; set; } = new();
}

public class DepartmentInfoDto
{
    public string Name { get; set; } = string.Empty;
    public List<DoctorInfoDto> Doctors { get; set; } = new();
}

public class DoctorInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool HasAvailableSlots { get; set; }
}

public class SmartAppointmentRequest
{
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class SmartAppointmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Intent { get; set; }
    public bool NeedsPatientRegistration { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public List<PatientSummaryDto>? MultiplePatients { get; set; }
    public int? AppointmentId { get; set; }
    public List<ScheduleSummaryDto>? AlternativeSchedules { get; set; }
}

public class ScheduleSummaryDto
{
    public int ScheduleId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int AvailableSlots { get; set; }
}

// 工作流相关DTO
public enum WorkflowType
{
    None,
    PatientRegistration, // 建档工作流
    Appointment          // 预约挂号工作流
}

public enum WorkflowState
{
    Idle,
    CollectingPatientInfo,    // 收集患者信息
    PatientSelection,         // 选择患者(多个候选)
    QueryingSchedule,         // 查询排班
    SelectingTimeSlot,        // 选择时段
    Completed,
    Failed
}

public class WorkflowContext
{
    public string SessionId { get; set; } = string.Empty;
    public WorkflowType CurrentWorkflow { get; set; } = WorkflowType.None;
    public WorkflowState CurrentState { get; set; } = WorkflowState.Idle;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class WorkflowActionRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Action { get; set; } // 如: "SELECT_PATIENT", "SELECT_TIMESLOT", "CONFIRM"
    public Dictionary<string, object>? Data { get; set; } // 携带的数据
}

public class WorkflowActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public WorkflowType CurrentWorkflow { get; set; }
    public WorkflowState CurrentState { get; set; }
    public object? Data { get; set; }
    public List<WorkflowOption>? Options { get; set; } // 可选项(患者列表、时段列表等)
    public string? NextAction { get; set; } // 提示下一步操作
}

public class WorkflowOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public object? Data { get; set; }
}
