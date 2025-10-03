using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.DTOs;
using MedicalSystem.Models;
using MedicalSystem.Services;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly MedicalDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(MedicalDbContext context, IAIService aiService, ILogger<ChatController> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage([FromBody] ChatMessageRequest request)
    {
        try
        {
            // 保存用户消息
            var userMessage = new ConversationHistory
            {
                SessionId = request.SessionId,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                Content = request.Message,
                Role = "User",
                CreatedAt = DateTime.Now
            };
            _context.ConversationHistories.Add(userMessage);

            // 获取历史消息
            var history = await _context.ConversationHistories
                .Where(c => c.SessionId == request.SessionId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ConversationMessage
                {
                    Role = c.Role,
                    Content = c.Content
                })
                .ToListAsync();

            // 检查用户是否查询患者信息
            var isPatientQuery = (request.Message.Contains("查找") ||
                                 request.Message.Contains("查询") ||
                                 request.Message.Contains("搜索") ||
                                 request.Message.Contains("有没有")) &&
                                (request.Message.Contains("患者") ||
                                 request.Message.Contains("病人") ||
                                 System.Text.RegularExpressions.Regex.IsMatch(request.Message, @"[\u4e00-\u9fa5]{2,4}")); // 包含中文姓名

            // 检查用户是否询问预约/挂号相关信息
            var isAppointmentQuery = (request.Message.Contains("预约") ||
                                    request.Message.Contains("挂号") ||
                                    request.Message.Contains("看病") ||
                                    request.Message.Contains("医生") ||
                                    request.Message.Contains("科室")) &&
                                    !isPatientQuery; // 排除患者查询

            string aiResponse;

            if (isPatientQuery)
            {
                // 提取可能的患者姓名
                var nameMatch = System.Text.RegularExpressions.Regex.Match(request.Message, @"(患者|病人)?([（(])?([\u4e00-\u9fa5]{2,4})([)）])?");
                string patientName = nameMatch.Success && nameMatch.Groups.Count >= 4 ? nameMatch.Groups[3].Value : "";

                if (string.IsNullOrEmpty(patientName))
                {
                    // 尝试直接提取中文名字（排除"查找"、"查询"等关键词）
                    var simpleMatch = System.Text.RegularExpressions.Regex.Match(request.Message, @"(?<!查找|查询|搜索|显示|列出)([\u4e00-\u9fa5]{2,4})");
                    patientName = simpleMatch.Success ? simpleMatch.Value : "";
                }

                var patientInfo = "";
                if (!string.IsNullOrEmpty(patientName))
                {
                    var patients = await _context.Patients
                        .Where(p => p.Name.Contains(patientName))
                        .ToListAsync();

                    if (patients.Any())
                    {
                        patientInfo = $"\n\n[系统查询到的患者信息]\n找到 {patients.Count} 位患者:\n";
                        foreach (var p in patients)
                        {
                            var age = p.DateOfBirth != DateTime.MinValue ?
                                DateTime.Now.Year - p.DateOfBirth.Year : 0;
                            patientInfo += $"- {p.Name}, {p.Gender}, {age}岁, 电话: {p.Phone}, 身份证: {p.IdCard}\n";
                            if (!string.IsNullOrEmpty(p.Allergies))
                                patientInfo += $"  过敏史: {p.Allergies}\n";
                            if (!string.IsNullOrEmpty(p.MedicalHistory))
                                patientInfo += $"  既往病史: {p.MedicalHistory}\n";
                        }
                    }
                    else
                    {
                        patientInfo = $"\n\n[系统查询结果]\n未找到名为\"{patientName}\"的患者档案。";
                    }
                }

                var enhancedMessage = request.Message + patientInfo;
                aiResponse = await _aiService.SendMessageAsync(request.SessionId, enhancedMessage, history);
            }
            else if (isAppointmentQuery)
            {
                // 获取实时排班信息
                var scheduleData = await GetScheduleTableData(DateTime.Today);

                // 将排班信息附加到用户消息中，让AI使用
                var enhancedMessage = $"{request.Message}\n\n[系统提供的实时排班信息]\n{scheduleData}";
                aiResponse = await _aiService.SendMessageAsync(request.SessionId, enhancedMessage, history);
            }
            else
            {
                aiResponse = await _aiService.SendMessageAsync(request.SessionId, request.Message, history);
            }

            // 保存AI响应
            var assistantMessage = new ConversationHistory
            {
                SessionId = request.SessionId,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                Content = aiResponse,
                Role = "Assistant",
                CreatedAt = DateTime.Now
            };
            _context.ConversationHistories.Add(assistantMessage);

            await _context.SaveChangesAsync();

            return Ok(new ChatMessageResponse
            {
                SessionId = request.SessionId,
                Message = aiResponse,
                Role = "Assistant",
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理聊天消息时发生错误");
            return StatusCode(500, new { error = "处理消息失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取排班表格数据（近7天）
    /// </summary>
    private async Task<string> GetScheduleTableData(DateTime startDate)
    {
        var endDate = startDate.AddDays(7);

        var departments = await _context.Doctors
            .Where(d => d.IsActive)
            .Select(d => d.Department)
            .Distinct()
            .ToListAsync();

        var result = new System.Text.StringBuilder();
        result.AppendLine($"近期排班信息（{startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}）：\n");

        var hasAnySchedule = false;

        foreach (var dept in departments)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Doctor)
                .Where(s => s.Doctor!.Department == dept &&
                           s.Date >= startDate &&
                           s.Date <= endDate &&
                           s.AvailableSlots > 0)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.TimeSlot)
                .ToListAsync();

            if (schedules.Any())
            {
                hasAnySchedule = true;
                result.AppendLine($"【{dept}】");
                result.AppendLine("| 医生姓名 | 职称 | 日期 | 时段 | 剩余号源 |");
                result.AppendLine("|---------|------|--------|------|---------|");

                foreach (var schedule in schedules)
                {
                    result.AppendLine($"| {schedule.Doctor!.Name} | {schedule.Doctor.Title} | {schedule.Date:MM-dd} | {schedule.TimeSlot} | {schedule.AvailableSlots}/{schedule.TotalSlots} |");
                }
                result.AppendLine();
            }
        }

        if (!hasAnySchedule)
        {
            return "近期暂无可预约的排班信息。";
        }

        return result.ToString();
    }

    /// <summary>
    /// 获取会话历史
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public async Task<ActionResult<List<ConversationHistoryResponse>>> GetHistory(string sessionId)
    {
        var history = await _context.ConversationHistories
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ConversationHistoryResponse
            {
                Id = c.Id,
                Content = c.Content,
                Role = c.Role,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>
    /// 清除会话历史
    /// </summary>
    [HttpDelete("history/{sessionId}")]
    public async Task<IActionResult> ClearHistory(string sessionId)
    {
        var messages = await _context.ConversationHistories
            .Where(c => c.SessionId == sessionId)
            .ToListAsync();

        _context.ConversationHistories.RemoveRange(messages);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 从会话中提取患者信息
    /// </summary>
    [HttpPost("extract-patient")]
    public async Task<ActionResult<PatientExtractionDto>> ExtractPatientInfo([FromBody] ExtractPatientRequest request)
    {
        try
        {
            // 获取会话历史
            var conversation = await _context.ConversationHistories
                .Where(c => c.SessionId == request.SessionId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ConversationMessage
                {
                    Role = c.Role,
                    Content = c.Content
                })
                .ToListAsync();

            if (!conversation.Any())
            {
                return BadRequest(new { error = "会话历史为空" });
            }

            // 调用AI提取患者信息
            var extractedInfo = await _aiService.ExtractPatientInfoAsync(conversation);

            if (extractedInfo == null || !extractedInfo.HasData)
            {
                return Ok(new PatientExtractionDto { HasData = false });
            }

            // 检查是否已存在患者（通过手机号或身份证）
            Patient? existingPatient = null;
            if (!string.IsNullOrEmpty(extractedInfo.Phone))
            {
                existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Phone == extractedInfo.Phone);
            }
            else if (!string.IsNullOrEmpty(extractedInfo.IdCard))
            {
                existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdCard == extractedInfo.IdCard);
            }

            return Ok(new PatientExtractionDto
            {
                HasData = true,
                ExistingPatientId = existingPatient?.Id,
                ExistingPatientName = existingPatient?.Name,
                Name = extractedInfo.Name,
                Gender = extractedInfo.Gender,
                DateOfBirth = extractedInfo.DateOfBirth,
                Phone = extractedInfo.Phone,
                IdCard = extractedInfo.IdCard,
                Address = extractedInfo.Address,
                Allergies = extractedInfo.Allergies,
                MedicalHistory = extractedInfo.MedicalHistory,
                FamilyHistory = extractedInfo.FamilyHistory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取患者信息时发生错误");
            return StatusCode(500, new { error = "提取信息失败" });
        }
    }

    /// <summary>
    /// 智能预约处理（解析自然语言预约请求）
    /// </summary>
    [HttpPost("smart-appointment")]
    public async Task<ActionResult<SmartAppointmentResponse>> ProcessSmartAppointment([FromBody] SmartAppointmentRequest request)
    {
        try
        {
            // 1. 解析预约意图
            var intent = await _aiService.ParseAppointmentIntentAsync(request.Message);

            if (intent == null || !intent.HasIntent)
            {
                return Ok(new SmartAppointmentResponse
                {
                    Success = false,
                    Message = "未能理解您的预约请求，请明确说明患者姓名、医生和时间"
                });
            }

            var response = new SmartAppointmentResponse { Intent = intent };

            // 2. 查询患者
            if (string.IsNullOrEmpty(intent.PatientName))
            {
                response.Success = false;
                response.Message = "请提供患者姓名";
                return Ok(response);
            }

            var patients = await _context.Patients
                .Where(p => p.Name.Contains(intent.PatientName))
                .ToListAsync();

            if (patients.Count == 0)
            {
                response.Success = false;
                response.NeedsPatientRegistration = true;
                response.Message = $"未找到患者\"{intent.PatientName}\"的档案，需要先建档";
                return Ok(response);
            }
            else if (patients.Count > 1)
            {
                response.Success = false;
                response.MultiplePatients = patients.Select(p => new PatientSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Phone = p.Phone,
                    DateOfBirth = p.DateOfBirth
                }).ToList();
                response.Message = $"找到{patients.Count}位名为\"{intent.PatientName}\"的患者，请提供更多信息（如手机号）以确认";
                return Ok(response);
            }

            var patient = patients[0];
            response.PatientId = patient.Id;
            response.PatientName = patient.Name;

            // 3. 查询医生和排班
            if (string.IsNullOrEmpty(intent.DoctorName))
            {
                response.Success = false;
                response.Message = "请指定要预约的医生";
                return Ok(response);
            }

            var targetDate = DateTime.Today.AddDays(intent.DateOffset ?? 0);
            var timeSlot = intent.TimeSlot ?? "上午";

            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s =>
                    s.Doctor!.Name.Contains(intent.DoctorName) &&
                    s.Date.Date == targetDate.Date &&
                    s.TimeSlot == timeSlot &&
                    s.AvailableSlots > 0);

            if (schedule == null)
            {
                response.Success = false;
                response.Message = $"未找到{intent.DoctorName}医生在{targetDate:MM月dd日}{timeSlot}的可用排班";

                // 查找该医生的其他可用时段
                var alternativeSchedules = await _context.Schedules
                    .Include(s => s.Doctor)
                    .Where(s =>
                        s.Doctor!.Name.Contains(intent.DoctorName) &&
                        s.Date >= DateTime.Today &&
                        s.Date <= DateTime.Today.AddDays(7) &&
                        s.AvailableSlots > 0)
                    .OrderBy(s => s.Date)
                    .Take(5)
                    .ToListAsync();

                if (alternativeSchedules.Any())
                {
                    response.AlternativeSchedules = alternativeSchedules.Select(s => new ScheduleSummaryDto
                    {
                        ScheduleId = s.Id,
                        DoctorName = s.Doctor!.Name,
                        Date = s.Date,
                        TimeSlot = s.TimeSlot,
                        AvailableSlots = s.AvailableSlots
                    }).ToList();
                    response.Message += "\n\n但该医生有以下可预约时段：\n" +
                        string.Join("\n", alternativeSchedules.Select(s =>
                            $"- {s.Date:MM月dd日} {s.TimeSlot} (剩余{s.AvailableSlots}个号)"));
                }

                return Ok(response);
            }

            // 4. 检查是否已预约
            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.PatientId == patient.Id &&
                    a.ScheduleId == schedule.Id &&
                    a.Status != "Cancelled");

            if (existingAppointment != null)
            {
                response.Success = false;
                response.Message = $"{patient.Name}已预约该时段，请勿重复预约";
                return Ok(response);
            }

            // 5. 创建预约
            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = schedule.DoctorId,
                ScheduleId = schedule.Id,
                AppointmentTime = DateTime.Now,
                Status = "Scheduled",
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            schedule.AvailableSlots--;
            await _context.SaveChangesAsync();

            response.Success = true;
            response.AppointmentId = appointment.Id;
            response.Message = $"预约成功！\n患者：{patient.Name}\n医生：{schedule.Doctor!.Name}\n时间：{schedule.Date:yyyy年MM月dd日} {schedule.TimeSlot}\n剩余号源：{schedule.AvailableSlots}";

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能预约处理失败");
            return StatusCode(500, new { error = "预约处理失败" });
        }
    }

    /// <summary>
    /// 获取可预约的科室和医生
    /// </summary>
    [HttpGet("available-appointments")]
    public async Task<ActionResult<AvailableAppointmentsDto>> GetAvailableAppointments([FromQuery] DateTime? date)
    {
        try
        {
            var targetDate = date ?? DateTime.Today;

            // 获取所有科室
            var departments = await _context.Doctors
                .Where(d => d.IsActive)
                .Select(d => d.Department)
                .Distinct()
                .ToListAsync();

            // 获取每个科室的医生和排班信息
            var departmentInfos = new List<DepartmentInfoDto>();

            foreach (var dept in departments)
            {
                var doctors = await _context.Doctors
                    .Where(d => d.IsActive && d.Department == dept)
                    .Select(d => new DoctorInfoDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Title = d.Title,
                        Specialization = d.Specialization,
                        Department = d.Department
                    })
                    .ToListAsync();

                // 获取该科室今日有排班的医生
                var doctorIds = doctors.Select(d => d.Id).ToList();
                var availableDoctorIds = await _context.Schedules
                    .Where(s => doctorIds.Contains(s.DoctorId) &&
                                s.Date.Date == targetDate.Date &&
                                s.AvailableSlots > 0)
                    .Select(s => s.DoctorId)
                    .Distinct()
                    .ToListAsync();

                // 标记可预约的医生
                foreach (var doctor in doctors)
                {
                    doctor.HasAvailableSlots = availableDoctorIds.Contains(doctor.Id);
                }

                departmentInfos.Add(new DepartmentInfoDto
                {
                    Name = dept,
                    Doctors = doctors
                });
            }

            return Ok(new AvailableAppointmentsDto
            {
                Date = targetDate,
                Departments = departmentInfos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可预约信息时发生错误");
            return StatusCode(500, new { error = "获取信息失败" });
        }
    }
}
