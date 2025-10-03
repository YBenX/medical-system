using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.DTOs;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public DoctorsController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有医生
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DoctorResponse>>> GetDoctors([FromQuery] string? department = null)
    {
        var query = _context.Doctors.Where(d => d.IsActive);

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(d => d.Department == department);
        }

        var doctors = await query
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                Name = d.Name,
                Title = d.Title,
                Department = d.Department,
                Specialization = d.Specialization,
                Introduction = d.Introduction,
                IsActive = d.IsActive
            })
            .ToListAsync();

        return Ok(doctors);
    }

    /// <summary>
    /// 获取排班信息
    /// </summary>
    [HttpGet("schedules")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetSchedules(
        [FromQuery] int? doctorId = null,
        [FromQuery] DateTime? date = null)
    {
        var query = _context.Schedules
            .Include(s => s.Doctor)
            .AsQueryable();

        if (doctorId.HasValue)
        {
            query = query.Where(s => s.DoctorId == doctorId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(s => s.Date.Date == date.Value.Date);
        }
        else
        {
            // 默认只显示未来的排班
            query = query.Where(s => s.Date >= DateTime.Today);
        }

        var schedules = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.TimeSlot)
            .Select(s => new ScheduleResponse
            {
                Id = s.Id,
                DoctorId = s.DoctorId,
                DoctorName = s.Doctor.Name,
                Department = s.Doctor.Department,
                Title = s.Doctor.Title,
                Date = s.Date,
                TimeSlot = s.TimeSlot,
                TotalSlots = s.TotalSlots,
                AvailableSlots = s.AvailableSlots
            })
            .ToListAsync();

        return Ok(schedules);
    }
}
