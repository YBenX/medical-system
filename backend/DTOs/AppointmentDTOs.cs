namespace MedicalSystem.DTOs;

public class CreateAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int ScheduleId { get; set; }
}

public class AppointmentResponse
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ScheduleResponse
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
}

public class DoctorResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Introduction { get; set; }
    public bool IsActive { get; set; }
}

public class PatientVerificationRequest
{
    public string? Phone { get; set; }
    public string? Name { get; set; }
    public string? IdCard { get; set; }
}

public class PatientVerificationDto
{
    public bool Found { get; set; }
    public bool MultipleMatches { get; set; }
    public PatientSummaryDto? Patient { get; set; }
    public List<PatientSummaryDto>? MatchedPatients { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PatientSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}
