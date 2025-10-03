using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.Models;
using MedicalSystem.DTOs;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicalRecordsController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public MedicalRecordsController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建病历
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MedicalRecordDto>> CreateMedicalRecord([FromBody] CreateMedicalRecordDto dto)
    {
        // 验证患者和医生是否存在
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
            return NotFound($"患者ID {dto.PatientId} 不存在");

        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null)
            return NotFound($"医生ID {dto.DoctorId} 不存在");

        var record = new MedicalRecord
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentId = dto.AppointmentId,
            VisitDate = DateTime.Now,
            ChiefComplaint = dto.ChiefComplaint,
            PresentIllness = dto.PresentIllness,
            PhysicalExam = dto.PhysicalExam,
            Diagnosis = dto.Diagnosis,
            ICD10Code = dto.ICD10Code,
            TreatmentPlan = dto.TreatmentPlan,
            Notes = dto.Notes,
            CreatedAt = DateTime.Now
        };

        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync();

        // 如果关联了预约，更新预约状态
        if (dto.AppointmentId.HasValue)
        {
            var appointment = await _context.Appointments.FindAsync(dto.AppointmentId.Value);
            if (appointment != null)
            {
                appointment.Status = "已完成";
                await _context.SaveChangesAsync();
            }
        }

        var result = new MedicalRecordDto
        {
            Id = record.Id,
            PatientId = record.PatientId,
            PatientName = patient.Name,
            DoctorId = record.DoctorId,
            DoctorName = doctor.Name,
            AppointmentId = record.AppointmentId,
            VisitDate = record.VisitDate,
            ChiefComplaint = record.ChiefComplaint,
            PresentIllness = record.PresentIllness,
            PhysicalExam = record.PhysicalExam,
            Diagnosis = record.Diagnosis,
            ICD10Code = record.ICD10Code,
            TreatmentPlan = record.TreatmentPlan,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt
        };

        return CreatedAtAction(nameof(GetMedicalRecord), new { id = record.Id }, result);
    }

    /// <summary>
    /// 获取病历详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalRecordDto>> GetMedicalRecord(int id)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
            return NotFound($"病历ID {id} 不存在");

        var result = new MedicalRecordDto
        {
            Id = record.Id,
            PatientId = record.PatientId,
            PatientName = record.Patient.Name,
            DoctorId = record.DoctorId,
            DoctorName = record.Doctor.Name,
            AppointmentId = record.AppointmentId,
            VisitDate = record.VisitDate,
            ChiefComplaint = record.ChiefComplaint,
            PresentIllness = record.PresentIllness,
            PhysicalExam = record.PhysicalExam,
            Diagnosis = record.Diagnosis,
            ICD10Code = record.ICD10Code,
            TreatmentPlan = record.TreatmentPlan,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt
        };

        return result;
    }

    /// <summary>
    /// 更新病历
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMedicalRecord(int id, [FromBody] UpdateMedicalRecordDto dto)
    {
        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null)
            return NotFound($"病历ID {id} 不存在");

        if (!string.IsNullOrEmpty(dto.ChiefComplaint))
            record.ChiefComplaint = dto.ChiefComplaint;
        if (!string.IsNullOrEmpty(dto.PresentIllness))
            record.PresentIllness = dto.PresentIllness;
        if (!string.IsNullOrEmpty(dto.PhysicalExam))
            record.PhysicalExam = dto.PhysicalExam;
        if (!string.IsNullOrEmpty(dto.Diagnosis))
            record.Diagnosis = dto.Diagnosis;
        if (dto.ICD10Code != null)
            record.ICD10Code = dto.ICD10Code;
        if (dto.TreatmentPlan != null)
            record.TreatmentPlan = dto.TreatmentPlan;
        if (dto.Notes != null)
            record.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 获取患者的病历历史
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<PatientMedicalHistoryDto>> GetPatientMedicalHistory(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null)
            return NotFound($"患者ID {patientId} 不存在");

        var records = await _context.MedicalRecords
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.VisitDate)
            .Select(r => new MedicalRecordDto
            {
                Id = r.Id,
                PatientId = r.PatientId,
                PatientName = patient.Name,
                DoctorId = r.DoctorId,
                DoctorName = r.Doctor.Name,
                AppointmentId = r.AppointmentId,
                VisitDate = r.VisitDate,
                ChiefComplaint = r.ChiefComplaint,
                PresentIllness = r.PresentIllness,
                PhysicalExam = r.PhysicalExam,
                Diagnosis = r.Diagnosis,
                ICD10Code = r.ICD10Code,
                TreatmentPlan = r.TreatmentPlan,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        var result = new PatientMedicalHistoryDto
        {
            PatientId = patientId,
            PatientName = patient.Name,
            Records = records
        };

        return result;
    }

    /// <summary>
    /// 删除病历
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedicalRecord(int id)
    {
        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null)
            return NotFound($"病历ID {id} 不存在");

        _context.MedicalRecords.Remove(record);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 获取所有病历（支持分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MedicalRecordDto>>> GetAllMedicalRecords(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var records = await _context.MedicalRecords
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .OrderByDescending(r => r.VisitDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MedicalRecordDto
            {
                Id = r.Id,
                PatientId = r.PatientId,
                PatientName = r.Patient.Name,
                DoctorId = r.DoctorId,
                DoctorName = r.Doctor.Name,
                AppointmentId = r.AppointmentId,
                VisitDate = r.VisitDate,
                ChiefComplaint = r.ChiefComplaint,
                PresentIllness = r.PresentIllness,
                PhysicalExam = r.PhysicalExam,
                Diagnosis = r.Diagnosis,
                ICD10Code = r.ICD10Code,
                TreatmentPlan = r.TreatmentPlan,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return records;
    }
}
