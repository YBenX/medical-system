using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.Models;
using MedicalSystem.DTOs;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public PrescriptionsController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建处方
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PrescriptionDto>> CreatePrescription([FromBody] CreatePrescriptionDto dto)
    {
        // 验证病历是否存在
        var medicalRecord = await _context.MedicalRecords
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == dto.MedicalRecordId);

        if (medicalRecord == null)
            return NotFound($"病历ID {dto.MedicalRecordId} 不存在");

        // 验证医生是否存在
        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null)
            return NotFound($"医生ID {dto.DoctorId} 不存在");

        // 验证所有药品是否存在
        foreach (var item in dto.Items)
        {
            var medicine = await _context.Medicines.FindAsync(item.MedicineId);
            if (medicine == null)
                return NotFound($"药品ID {item.MedicineId} 不存在");
        }

        // 创建处方
        var prescription = new Prescription
        {
            MedicalRecordId = dto.MedicalRecordId,
            DoctorId = dto.DoctorId,
            PrescriptionDate = DateTime.Now,
            Status = "待审核",
            Notes = dto.Notes
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        // 创建处方明细
        decimal totalAmount = 0;
        var detailDtos = new List<PrescriptionDetailItemDto>();

        foreach (var item in dto.Items)
        {
            var medicine = await _context.Medicines.FindAsync(item.MedicineId);
            var detail = new PrescriptionDetail
            {
                PrescriptionId = prescription.Id,
                MedicineId = item.MedicineId,
                Dosage = item.Dosage,
                Quantity = item.Quantity,
                UnitPrice = medicine!.Price
            };

            _context.PrescriptionDetails.Add(detail);
            totalAmount += detail.Quantity * detail.UnitPrice;

            detailDtos.Add(new PrescriptionDetailItemDto
            {
                MedicineId = medicine.Id,
                MedicineName = medicine.Name,
                Dosage = detail.Dosage,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice
            });
        }

        await _context.SaveChangesAsync();

        var result = new PrescriptionDto
        {
            Id = prescription.Id,
            MedicalRecordId = prescription.MedicalRecordId,
            DoctorId = prescription.DoctorId,
            DoctorName = doctor.Name,
            PatientId = medicalRecord.PatientId,
            PatientName = medicalRecord.Patient.Name,
            PrescriptionDate = prescription.PrescriptionDate,
            Status = prescription.Status,
            TotalAmount = prescription.TotalAmount,
            Notes = prescription.Notes,
            Items = detailDtos
        };

        return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, result);
    }

    /// <summary>
    /// 获取处方详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PrescriptionDto>> GetPrescription(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.MedicalRecord)
                .ThenInclude(r => r.Patient)
            .Include(p => p.Details)
                .ThenInclude(d => d.Medicine)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
            return NotFound($"处方ID {id} 不存在");

        var result = new PrescriptionDto
        {
            Id = prescription.Id,
            MedicalRecordId = prescription.MedicalRecordId,
            DoctorId = prescription.DoctorId,
            DoctorName = prescription.Doctor.Name,
            PatientId = prescription.MedicalRecord.PatientId,
            PatientName = prescription.MedicalRecord.Patient.Name,
            PrescriptionDate = prescription.PrescriptionDate,
            Status = prescription.Status,
            TotalAmount = prescription.TotalAmount,
            Notes = prescription.Notes,
            Items = prescription.Details.Select(d => new PrescriptionDetailItemDto
            {
                MedicineId = d.MedicineId,
                MedicineName = d.Medicine.Name,
                Dosage = d.Dosage,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList()
        };

        return result;
    }

    /// <summary>
    /// 更新处方状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdatePrescriptionStatus(int id, [FromBody] UpdatePrescriptionStatusDto dto)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription == null)
            return NotFound($"处方ID {id} 不存在");

        // 验证状态值
        var validStatuses = new[] { "待审核", "已审核", "已配药", "已取消" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest($"无效的状态值。有效值为: {string.Join(", ", validStatuses)}");

        prescription.Status = dto.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 获取患者的处方列表
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<PrescriptionDto>>> GetPatientPrescriptions(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null)
            return NotFound($"患者ID {patientId} 不存在");

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.MedicalRecord)
            .Include(p => p.Details)
                .ThenInclude(d => d.Medicine)
            .Where(p => p.MedicalRecord.PatientId == patientId)
            .OrderByDescending(p => p.PrescriptionDate)
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                MedicalRecordId = p.MedicalRecordId,
                DoctorId = p.DoctorId,
                DoctorName = p.Doctor.Name,
                PatientId = p.MedicalRecord.PatientId,
                PatientName = patient.Name,
                PrescriptionDate = p.PrescriptionDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Notes = p.Notes,
                Items = p.Details.Select(d => new PrescriptionDetailItemDto
                {
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine.Name,
                    Dosage = d.Dosage,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return prescriptions;
    }

    /// <summary>
    /// 获取病历的处方
    /// </summary>
    [HttpGet("medical-record/{medicalRecordId}")]
    public async Task<ActionResult<List<PrescriptionDto>>> GetMedicalRecordPrescriptions(int medicalRecordId)
    {
        var medicalRecord = await _context.MedicalRecords
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == medicalRecordId);

        if (medicalRecord == null)
            return NotFound($"病历ID {medicalRecordId} 不存在");

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Details)
                .ThenInclude(d => d.Medicine)
            .Where(p => p.MedicalRecordId == medicalRecordId)
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                MedicalRecordId = p.MedicalRecordId,
                DoctorId = p.DoctorId,
                DoctorName = p.Doctor.Name,
                PatientId = medicalRecord.PatientId,
                PatientName = medicalRecord.Patient.Name,
                PrescriptionDate = p.PrescriptionDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Notes = p.Notes,
                Items = p.Details.Select(d => new PrescriptionDetailItemDto
                {
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine.Name,
                    Dosage = d.Dosage,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return prescriptions;
    }

    /// <summary>
    /// 删除处方
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrescription(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
            return NotFound($"处方ID {id} 不存在");

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
