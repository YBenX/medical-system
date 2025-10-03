namespace MedicalSystem.DTOs;

public class CreatePatientRequest
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? IdCard { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? FamilyHistory { get; set; }
}

public class UpdatePatientRequest
{
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? IdCard { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? FamilyHistory { get; set; }
}

public class PatientResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public string? IdCard { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalHistory { get; set; }
    public string? FamilyHistory { get; set; }
    public DateTime CreatedAt { get; set; }
}
