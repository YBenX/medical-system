using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.DTOs;
using MedicalSystem.Models;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public AppointmentsController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建预约（需先验证患者档案）
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        // 1. 验证患者是否存在
        var patient = await _context.Patients.FindAsync(request.PatientId);
        if (patient == null)
        {
            return BadRequest(new {
                error = "患者档案不存在",
                errorCode = "PATIENT_NOT_FOUND",
                message = "请先建立患者档案再进行预约"
            });
        }

        // 2. 验证排班是否存在
        var schedule = await _context.Schedules
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId);

        if (schedule == null)
        {
            return NotFound(new { error = "排班不存在" });
        }

        // 3. 验证是否有可用号源
        if (schedule.AvailableSlots <= 0)
        {
            return BadRequest(new { error = "该时段已满，无法预约" });
        }

        // 4. 检查是否已有相同预约
        var existingAppointment = await _context.Appointments
            .FirstOrDefaultAsync(a =>
                a.PatientId == request.PatientId &&
                a.ScheduleId == request.ScheduleId &&
                a.Status != "Cancelled");

        if (existingAppointment != null)
        {
            return BadRequest(new { error = "您已预约该时段，请勿重复预约" });
        }

        // 5. 创建预约
        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ScheduleId = request.ScheduleId,
            AppointmentTime = DateTime.Now,
            Status = "Scheduled",
            CreatedAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);

        // 6. 减少可用号源
        schedule.AvailableSlots--;

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, new AppointmentResponse
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patient.Name,
            DoctorId = appointment.DoctorId,
            DoctorName = schedule.Doctor.Name,
            Department = schedule.Doctor.Department,
            AppointmentDate = schedule.Date,
            TimeSlot = schedule.TimeSlot,
            Status = appointment.Status,
            CreatedAt = appointment.CreatedAt
        });
    }

    /// <summary>
    /// 获取预约详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentResponse>> GetAppointment(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
        {
            return NotFound(new { error = "预约不存在" });
        }

        return Ok(new AppointmentResponse
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient.Name,
            DoctorId = appointment.DoctorId,
            DoctorName = appointment.Doctor.Name,
            Department = appointment.Doctor.Department,
            AppointmentDate = appointment.Schedule.Date,
            TimeSlot = appointment.Schedule.TimeSlot,
            Status = appointment.Status,
            CreatedAt = appointment.CreatedAt
        });
    }

    /// <summary>
    /// 取消预约
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
        {
            return NotFound(new { error = "预约不存在" });
        }

        if (appointment.Status == "Cancelled")
        {
            return BadRequest(new { error = "预约已取消" });
        }

        appointment.Status = "Cancelled";
        appointment.Schedule.AvailableSlots++; // 恢复号源

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 获取患者的预约列表
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<AppointmentResponse>>> GetPatientAppointments(int patientId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Schedule)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AppointmentResponse
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.Name,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor.Name,
                Department = a.Doctor.Department,
                AppointmentDate = a.Schedule.Date,
                TimeSlot = a.Schedule.TimeSlot,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(appointments);
    }

    /// <summary>
    /// 验证患者并准备预约（通过手机号或姓名查询患者）
    /// </summary>
    [HttpPost("verify-patient")]
    public async Task<ActionResult<PatientVerificationDto>> VerifyPatientForAppointment([FromBody] PatientVerificationRequest request)
    {
        Patient? patient = null;

        // 优先通过手机号查询
        if (!string.IsNullOrEmpty(request.Phone))
        {
            patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Phone == request.Phone);
        }

        // 如果手机号未找到，尝试通过姓名和身份证查询
        if (patient == null && !string.IsNullOrEmpty(request.Name))
        {
            if (!string.IsNullOrEmpty(request.IdCard))
            {
                patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Name == request.Name && p.IdCard == request.IdCard);
            }
            else
            {
                // 只通过姓名模糊查询，返回匹配列表
                var patients = await _context.Patients
                    .Where(p => p.Name.Contains(request.Name))
                    .Select(p => new PatientSummaryDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Gender = p.Gender,
                        Phone = p.Phone,
                        DateOfBirth = p.DateOfBirth
                    })
                    .Take(5)
                    .ToListAsync();

                if (patients.Count > 1)
                {
                    return Ok(new PatientVerificationDto
                    {
                        Found = false,
                        MultipleMatches = true,
                        MatchedPatients = patients,
                        Message = "找到多个同名患者，请提供更多信息（手机号或身份证）以确认身份"
                    });
                }
                else if (patients.Count == 1)
                {
                    patient = await _context.Patients.FindAsync(patients[0].Id);
                }
            }
        }

        if (patient == null)
        {
            return Ok(new PatientVerificationDto
            {
                Found = false,
                Message = "未找到患者档案，请先建档"
            });
        }

        return Ok(new PatientVerificationDto
        {
            Found = true,
            Patient = new PatientSummaryDto
            {
                Id = patient.Id,
                Name = patient.Name,
                Gender = patient.Gender,
                Phone = patient.Phone,
                DateOfBirth = patient.DateOfBirth
            },
            Message = $"找到患者档案：{patient.Name}"
        });
    }
}
