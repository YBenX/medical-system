using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.Models;
using MedicalSystem.DTOs;
using System.Text.Json;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChargesController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public ChargesController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建收费记录
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChargeDto>> CreateCharge([FromBody] CreateChargeDto dto)
    {
        // 验证患者是否存在
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
            return NotFound($"患者ID {dto.PatientId} 不存在");

        // 计算总金额
        decimal totalAmount = dto.Items.Sum(item => item.Amount);

        // 创建收费记录
        var charge = new Charge
        {
            PatientId = dto.PatientId,
            MedicalRecordId = dto.MedicalRecordId,
            PrescriptionId = dto.PrescriptionId,
            ChargeDate = DateTime.Now,
            TotalAmount = totalAmount,
            PaymentStatus = "未支付",
            ItemsJson = JsonSerializer.Serialize(dto.Items),
            Notes = dto.Notes
        };

        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var result = new ChargeDto
        {
            Id = charge.Id,
            PatientId = charge.PatientId,
            PatientName = patient.Name,
            MedicalRecordId = charge.MedicalRecordId,
            PrescriptionId = charge.PrescriptionId,
            ChargeDate = charge.ChargeDate,
            TotalAmount = charge.TotalAmount,
            PaymentStatus = charge.PaymentStatus,
            PaymentTime = charge.PaymentTime,
            PaymentMethod = charge.PaymentMethod,
            Items = dto.Items,
            Notes = charge.Notes
        };

        return CreatedAtAction(nameof(GetCharge), new { id = charge.Id }, result);
    }

    /// <summary>
    /// 根据处方自动创建收费记录
    /// </summary>
    [HttpPost("from-prescription/{prescriptionId}")]
    public async Task<ActionResult<ChargeDto>> CreateChargeFromPrescription(int prescriptionId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.MedicalRecord)
                .ThenInclude(r => r.Patient)
            .Include(p => p.Details)
                .ThenInclude(d => d.Medicine)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription == null)
            return NotFound($"处方ID {prescriptionId} 不存在");

        // 检查是否已经创建过收费记录
        var existingCharge = await _context.Charges
            .FirstOrDefaultAsync(c => c.PrescriptionId == prescriptionId);

        if (existingCharge != null)
            return BadRequest($"处方ID {prescriptionId} 已经创建过收费记录");

        // 创建收费明细
        var items = new List<ChargeItemDto>();

        // 添加药品费用
        foreach (var detail in prescription.Details)
        {
            items.Add(new ChargeItemDto
            {
                ItemName = detail.Medicine.Name,
                ItemType = "药品费",
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice
            });
        }

        // 创建收费记录
        var dto = new CreateChargeDto
        {
            PatientId = prescription.MedicalRecord.PatientId,
            MedicalRecordId = prescription.MedicalRecordId,
            PrescriptionId = prescriptionId,
            Items = items,
            Notes = "根据处方自动生成"
        };

        return await CreateCharge(dto);
    }

    /// <summary>
    /// 获取收费记录详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChargeDto>> GetCharge(int id)
    {
        var charge = await _context.Charges
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (charge == null)
            return NotFound($"收费记录ID {id} 不存在");

        var items = string.IsNullOrEmpty(charge.ItemsJson)
            ? new List<ChargeItemDto>()
            : JsonSerializer.Deserialize<List<ChargeItemDto>>(charge.ItemsJson) ?? new List<ChargeItemDto>();

        var result = new ChargeDto
        {
            Id = charge.Id,
            PatientId = charge.PatientId,
            PatientName = charge.Patient.Name,
            MedicalRecordId = charge.MedicalRecordId,
            PrescriptionId = charge.PrescriptionId,
            ChargeDate = charge.ChargeDate,
            TotalAmount = charge.TotalAmount,
            PaymentStatus = charge.PaymentStatus,
            PaymentTime = charge.PaymentTime,
            PaymentMethod = charge.PaymentMethod,
            Items = items,
            Notes = charge.Notes
        };

        return result;
    }

    /// <summary>
    /// 支付
    /// </summary>
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayCharge(int id, [FromBody] PaymentDto dto)
    {
        var charge = await _context.Charges.FindAsync(id);
        if (charge == null)
            return NotFound($"收费记录ID {id} 不存在");

        if (charge.PaymentStatus == "已支付")
            return BadRequest("该收费记录已支付");

        if (dto.Amount != charge.TotalAmount)
            return BadRequest($"支付金额不匹配。应支付: {charge.TotalAmount}, 实际支付: {dto.Amount}");

        charge.PaymentStatus = "已支付";
        charge.PaymentTime = DateTime.Now;
        charge.PaymentMethod = dto.PaymentMethod;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 退费
    /// </summary>
    [HttpPost("{id}/refund")]
    public async Task<IActionResult> RefundCharge(int id, [FromBody] RefundDto dto)
    {
        var charge = await _context.Charges.FindAsync(id);
        if (charge == null)
            return NotFound($"收费记录ID {id} 不存在");

        if (charge.PaymentStatus != "已支付")
            return BadRequest("只有已支付的记录才能退费");

        charge.PaymentStatus = "已退款";
        charge.Notes = string.IsNullOrEmpty(charge.Notes)
            ? $"退费原因: {dto.Reason}"
            : $"{charge.Notes}; 退费原因: {dto.Reason}";

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 获取患者的收费记录
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<ChargeDto>>> GetPatientCharges(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null)
            return NotFound($"患者ID {patientId} 不存在");

        var charges = await _context.Charges
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ChargeDate)
            .ToListAsync();

        var result = charges.Select(c =>
        {
            var items = string.IsNullOrEmpty(c.ItemsJson)
                ? new List<ChargeItemDto>()
                : JsonSerializer.Deserialize<List<ChargeItemDto>>(c.ItemsJson) ?? new List<ChargeItemDto>();

            return new ChargeDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                PatientName = patient.Name,
                MedicalRecordId = c.MedicalRecordId,
                PrescriptionId = c.PrescriptionId,
                ChargeDate = c.ChargeDate,
                TotalAmount = c.TotalAmount,
                PaymentStatus = c.PaymentStatus,
                PaymentTime = c.PaymentTime,
                PaymentMethod = c.PaymentMethod,
                Items = items,
                Notes = c.Notes
            };
        }).ToList();

        return result;
    }

    /// <summary>
    /// 获取所有收费记录（支持分页和状态筛选）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ChargeDto>>> GetAllCharges(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var query = _context.Charges
            .Include(c => c.Patient)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.PaymentStatus == status);
        }

        var charges = await query
            .OrderByDescending(c => c.ChargeDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = charges.Select(c =>
        {
            var items = string.IsNullOrEmpty(c.ItemsJson)
                ? new List<ChargeItemDto>()
                : JsonSerializer.Deserialize<List<ChargeItemDto>>(c.ItemsJson) ?? new List<ChargeItemDto>();

            return new ChargeDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                PatientName = c.Patient.Name,
                MedicalRecordId = c.MedicalRecordId,
                PrescriptionId = c.PrescriptionId,
                ChargeDate = c.ChargeDate,
                TotalAmount = c.TotalAmount,
                PaymentStatus = c.PaymentStatus,
                PaymentTime = c.PaymentTime,
                PaymentMethod = c.PaymentMethod,
                Items = items,
                Notes = c.Notes
            };
        }).ToList();

        return result;
    }

    /// <summary>
    /// 删除收费记录（仅限未支付）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCharge(int id)
    {
        var charge = await _context.Charges.FindAsync(id);
        if (charge == null)
            return NotFound($"收费记录ID {id} 不存在");

        if (charge.PaymentStatus == "已支付")
            return BadRequest("已支付的记录不能删除，请使用退费功能");

        _context.Charges.Remove(charge);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
