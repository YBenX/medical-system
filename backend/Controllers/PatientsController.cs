using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.DTOs;
using MedicalSystem.Models;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public PatientsController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有患者
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PatientResponse>>> GetPatients()
    {
        var patients = await _context.Patients
            .Select(p => new PatientResponse
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                Age = DateTime.Now.Year - p.DateOfBirth.Year,
                IdCard = p.IdCard,
                Phone = p.Phone,
                Address = p.Address,
                Allergies = p.Allergies,
                MedicalHistory = p.MedicalHistory,
                FamilyHistory = p.FamilyHistory,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(patients);
    }

    /// <summary>
    /// 根据ID获取患者
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PatientResponse>> GetPatient(int id)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound(new { error = "患者不存在" });
        }

        return Ok(new PatientResponse
        {
            Id = patient.Id,
            Name = patient.Name,
            Gender = patient.Gender,
            DateOfBirth = patient.DateOfBirth,
            Age = DateTime.Now.Year - patient.DateOfBirth.Year,
            IdCard = patient.IdCard,
            Phone = patient.Phone,
            Address = patient.Address,
            Allergies = patient.Allergies,
            MedicalHistory = patient.MedicalHistory,
            FamilyHistory = patient.FamilyHistory,
            CreatedAt = patient.CreatedAt
        });
    }

    /// <summary>
    /// 创建患者
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PatientResponse>> CreatePatient([FromBody] CreatePatientRequest request)
    {
        // 检查身份证号是否已存在
        if (!string.IsNullOrEmpty(request.IdCard))
        {
            var existing = await _context.Patients
                .FirstOrDefaultAsync(p => p.IdCard == request.IdCard);

            if (existing != null)
            {
                return BadRequest(new { error = "该身份证号已存在" });
            }
        }

        var patient = new Patient
        {
            Name = request.Name,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            IdCard = request.IdCard,
            Phone = request.Phone,
            Address = request.Address,
            Allergies = request.Allergies,
            MedicalHistory = request.MedicalHistory,
            FamilyHistory = request.FamilyHistory,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, new PatientResponse
        {
            Id = patient.Id,
            Name = patient.Name,
            Gender = patient.Gender,
            DateOfBirth = patient.DateOfBirth,
            Age = DateTime.Now.Year - patient.DateOfBirth.Year,
            IdCard = patient.IdCard,
            Phone = patient.Phone,
            Address = patient.Address,
            Allergies = patient.Allergies,
            MedicalHistory = patient.MedicalHistory,
            FamilyHistory = patient.FamilyHistory,
            CreatedAt = patient.CreatedAt
        });
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatient(int id, [FromBody] UpdatePatientRequest request)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound(new { error = "患者不存在" });
        }

        if (!string.IsNullOrEmpty(request.Name))
            patient.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Gender))
            patient.Gender = request.Gender;

        if (request.DateOfBirth.HasValue)
            patient.DateOfBirth = request.DateOfBirth.Value;

        if (request.IdCard != null)
            patient.IdCard = request.IdCard;

        if (!string.IsNullOrEmpty(request.Phone))
            patient.Phone = request.Phone;

        if (request.Address != null)
            patient.Address = request.Address;

        if (request.Allergies != null)
            patient.Allergies = request.Allergies;

        if (request.MedicalHistory != null)
            patient.MedicalHistory = request.MedicalHistory;

        if (request.FamilyHistory != null)
            patient.FamilyHistory = request.FamilyHistory;

        patient.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 搜索患者
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<PatientResponse>>> SearchPatients([FromQuery] string keyword)
    {
        var patients = await _context.Patients
            .Where(p => p.Name.Contains(keyword) ||
                       (p.Phone != null && p.Phone.Contains(keyword)) ||
                       (p.IdCard != null && p.IdCard.Contains(keyword)))
            .Select(p => new PatientResponse
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                Age = DateTime.Now.Year - p.DateOfBirth.Year,
                IdCard = p.IdCard,
                Phone = p.Phone,
                Address = p.Address,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(patients);
    }
}
