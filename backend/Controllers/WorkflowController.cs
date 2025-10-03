using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.DTOs;
using MedicalSystem.Services;
using MedicalSystem.Models;
using System.Text.Json;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly MedicalDbContext _context;
    private readonly IWorkflowService _workflowService;
    private readonly IAIService _aiService;

    public WorkflowController(MedicalDbContext context, IWorkflowService workflowService, IAIService aiService)
    {
        _context = context;
        _workflowService = workflowService;
        _aiService = aiService;
    }

    [HttpPost("process")]
    public async Task<ActionResult<WorkflowActionResponse>> ProcessWorkflow([FromBody] WorkflowActionRequest request)
    {
        var context = _workflowService.GetOrCreateContext(request.SessionId);

        // 如果是新会话或空闲状态,解析用户意图
        if (context.CurrentState == WorkflowState.Idle)
        {
            var intent = await _aiService.ParseAppointmentIntentAsync(request.Message);

            if (intent?.HasIntent == true)
            {
                // 保存意图到上下文
                context.Data["intent"] = JsonSerializer.Serialize(intent);

                // 查询患者是否存在
                var patients = await _context.Patients
                    .Where(p => p.Name.Contains(intent.PatientName!))
                    .ToListAsync();

                if (patients.Count == 0)
                {
                    // 启动建档工作流
                    return await StartPatientRegistrationWorkflow(context, intent.PatientName!);
                }
                else if (patients.Count == 1)
                {
                    // 患者唯一,直接进入预约流程
                    return await StartAppointmentWorkflow(context, patients[0], intent);
                }
                else
                {
                    // 多个患者,进入选择状态
                    context.CurrentWorkflow = WorkflowType.Appointment;
                    context.CurrentState = WorkflowState.PatientSelection;
                    context.Data["candidates"] = JsonSerializer.Serialize(patients);
                    _workflowService.UpdateContext(context);

                    var options = patients.Select(p => new WorkflowOption
                    {
                        Id = p.Id.ToString(),
                        Label = $"{p.Name} - {p.Phone} - {p.IdCard}",
                        Data = new
                        {
                            p.Id,
                            p.Name,
                            p.Phone,
                            p.IdCard,
                            p.Gender,
                            DateOfBirth = p.DateOfBirth.ToString("yyyy-MM-dd")
                        }
                    }).ToList();

                    return new WorkflowActionResponse
                    {
                        Success = true,
                        Message = $"找到{patients.Count}位名为\"{intent.PatientName}\"的患者,请选择:",
                        CurrentWorkflow = context.CurrentWorkflow,
                        CurrentState = context.CurrentState,
                        Options = options,
                        NextAction = "请选择患者或回复序号"
                    };
                }
            }
            else
            {
                return new WorkflowActionResponse
                {
                    Success = false,
                    Message = "未能识别您的预约意图,请说明患者姓名、医生姓名和预约时间",
                    CurrentWorkflow = WorkflowType.None,
                    CurrentState = WorkflowState.Idle
                };
            }
        }

        // 处理当前工作流的各个状态
        return context.CurrentWorkflow switch
        {
            WorkflowType.PatientRegistration => await HandlePatientRegistrationWorkflow(context, request),
            WorkflowType.Appointment => await HandleAppointmentWorkflow(context, request),
            _ => new WorkflowActionResponse
            {
                Success = false,
                Message = "未知的工作流状态",
                CurrentWorkflow = context.CurrentWorkflow,
                CurrentState = context.CurrentState
            }
        };
    }

    private async Task<WorkflowActionResponse> StartPatientRegistrationWorkflow(WorkflowContext context, string patientName)
    {
        context.CurrentWorkflow = WorkflowType.PatientRegistration;
        context.CurrentState = WorkflowState.CollectingPatientInfo;
        context.Data["patientName"] = patientName;
        _workflowService.UpdateContext(context);

        return new WorkflowActionResponse
        {
            Success = true,
            Message = $"未找到患者\"{patientName}\"的档案,需要先建档。\n\n请提供以下信息:\n- 姓名: {patientName}\n- 性别\n- 出生日期\n- 手机号\n- 身份证号\n- 住址\n- 过敏史(如无请说无)\n- 既往病史(如无请说无)",
            CurrentWorkflow = context.CurrentWorkflow,
            CurrentState = context.CurrentState,
            NextAction = "请提供患者的详细信息"
        };
    }

    private async Task<WorkflowActionResponse> HandlePatientRegistrationWorkflow(WorkflowContext context, WorkflowActionRequest request)
    {
        if (context.CurrentState == WorkflowState.CollectingPatientInfo)
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

            // 使用AI提取患者信息
            var extractedInfo = await _aiService.ExtractPatientInfoAsync(conversation);

            if (extractedInfo == null || !extractedInfo.HasData || string.IsNullOrEmpty(extractedInfo.Name))
            {
                return new WorkflowActionResponse
                {
                    Success = false,
                    Message = "信息不完整,请继续补充患者信息",
                    CurrentWorkflow = context.CurrentWorkflow,
                    CurrentState = context.CurrentState,
                    NextAction = "请提供姓名、性别、出生日期、手机号等信息"
                };
            }

            // 保存患者档案
            var patient = new Patient
            {
                Name = extractedInfo.Name,
                Gender = extractedInfo.Gender ?? "未知",
                DateOfBirth = extractedInfo.DateOfBirth ?? DateTime.MinValue,
                Phone = extractedInfo.Phone ?? "",
                IdCard = extractedInfo.IdCard ?? "",
                Address = extractedInfo.Address ?? "",
                Allergies = extractedInfo.Allergies,
                MedicalHistory = extractedInfo.MedicalHistory,
                FamilyHistory = extractedInfo.FamilyHistory
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // 建档完成,检查是否有预约意图
            if (context.Data.ContainsKey("intent"))
            {
                var intentJson = context.Data["intent"].ToString()!;
                var intent = JsonSerializer.Deserialize<AppointmentIntentResult>(intentJson);

                // 转入预约工作流
                return await StartAppointmentWorkflow(context, patient, intent!);
            }
            else
            {
                // 只是建档,完成
                _workflowService.ClearContext(context.SessionId);
                return new WorkflowActionResponse
                {
                    Success = true,
                    Message = $"患者档案建立成功!\n\n档案编号: {patient.Id}\n姓名: {patient.Name}\n性别: {patient.Gender}\n手机: {patient.Phone}",
                    CurrentWorkflow = WorkflowType.None,
                    CurrentState = WorkflowState.Completed,
                    Data = new { PatientId = patient.Id }
                };
            }
        }

        return new WorkflowActionResponse
        {
            Success = false,
            Message = "未知状态",
            CurrentWorkflow = context.CurrentWorkflow,
            CurrentState = context.CurrentState
        };
    }

    private async Task<WorkflowActionResponse> HandleAppointmentWorkflow(WorkflowContext context, WorkflowActionRequest request)
    {
        // 处理患者选择
        if (context.CurrentState == WorkflowState.PatientSelection)
        {
            // 提取选择的患者ID
            int patientId;
            if (request.Data?.ContainsKey("patientId") == true)
            {
                patientId = Convert.ToInt32(request.Data["patientId"]);
            }
            else
            {
                // 尝试从消息中解析数字
                var match = System.Text.RegularExpressions.Regex.Match(request.Message, @"\d+");
                if (!match.Success)
                {
                    return new WorkflowActionResponse
                    {
                        Success = false,
                        Message = "请选择一个患者或回复患者序号",
                        CurrentWorkflow = context.CurrentWorkflow,
                        CurrentState = context.CurrentState
                    };
                }
                patientId = int.Parse(match.Value);
            }

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return new WorkflowActionResponse
                {
                    Success = false,
                    Message = "未找到该患者,请重新选择",
                    CurrentWorkflow = context.CurrentWorkflow,
                    CurrentState = context.CurrentState
                };
            }

            // 获取原始意图
            var intentJson = context.Data["intent"].ToString()!;
            var intent = JsonSerializer.Deserialize<AppointmentIntentResult>(intentJson);

            return await StartAppointmentWorkflow(context, patient, intent!);
        }

        // 处理时段选择
        if (context.CurrentState == WorkflowState.SelectingTimeSlot)
        {
            // 提取选择的排班ID
            int scheduleId;
            if (request.Data?.ContainsKey("scheduleId") == true)
            {
                scheduleId = Convert.ToInt32(request.Data["scheduleId"]);
            }
            else
            {
                var match = System.Text.RegularExpressions.Regex.Match(request.Message, @"\d+");
                if (!match.Success)
                {
                    return new WorkflowActionResponse
                    {
                        Success = false,
                        Message = "请选择一个时段或回复序号",
                        CurrentWorkflow = context.CurrentWorkflow,
                        CurrentState = context.CurrentState
                    };
                }
                scheduleId = int.Parse(match.Value);
            }

            var patientId = Convert.ToInt32(context.Data["patientId"]);

            // 创建预约
            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null || schedule.AvailableSlots <= 0)
            {
                return new WorkflowActionResponse
                {
                    Success = false,
                    Message = "该时段已无可用号源,请重新选择",
                    CurrentWorkflow = context.CurrentWorkflow,
                    CurrentState = context.CurrentState
                };
            }

            // 检查重复预约
            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.PatientId == patientId &&
                                         a.ScheduleId == scheduleId &&
                                         a.Status != "已取消");

            if (existingAppointment != null)
            {
                return new WorkflowActionResponse
                {
                    Success = false,
                    Message = "您已经预约了该时段,请勿重复预约",
                    CurrentWorkflow = context.CurrentWorkflow,
                    CurrentState = context.CurrentState
                };
            }

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = schedule.DoctorId,
                ScheduleId = scheduleId,
                AppointmentTime = schedule.Date,
                Status = "已预约",
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);

            schedule.AvailableSlots--;
            await _context.SaveChangesAsync();

            // 完成工作流
            _workflowService.ClearContext(context.SessionId);

            var patient = await _context.Patients.FindAsync(patientId);

            return new WorkflowActionResponse
            {
                Success = true,
                Message = $"✅ 预约成功!\n\n预约编号: {appointment.Id}\n患者: {patient!.Name}\n医生: {schedule.Doctor!.Name}\n日期: {schedule.Date:yyyy-MM-dd}\n时段: {schedule.TimeSlot}\n请按时就诊!",
                CurrentWorkflow = WorkflowType.None,
                CurrentState = WorkflowState.Completed,
                Data = new
                {
                    AppointmentId = appointment.Id
                }
            };
        }

        return new WorkflowActionResponse
        {
            Success = false,
            Message = "未知状态",
            CurrentWorkflow = context.CurrentWorkflow,
            CurrentState = context.CurrentState
        };
    }

    private async Task<WorkflowActionResponse> StartAppointmentWorkflow(WorkflowContext context, Patient patient, AppointmentIntentResult intent)
    {
        context.CurrentWorkflow = WorkflowType.Appointment;
        context.CurrentState = WorkflowState.QueryingSchedule;
        context.Data["patientId"] = patient.Id;
        _workflowService.UpdateContext(context);

        // 计算目标日期
        var targetDate = DateTime.Today.AddDays(intent.DateOffset ?? 0);

        // 映射时段
        var timeSlot = intent.TimeSlot switch
        {
            "上午" => "上午",
            "下午" => "下午",
            "晚上" or "夜间" => "夜间",
            _ => null
        };

        // 查询医生排班
        var query = _context.Schedules
            .Include(s => s.Doctor)
            .Where(s => s.Date.Date == targetDate.Date && s.AvailableSlots > 0);

        if (!string.IsNullOrEmpty(intent.DoctorName))
        {
            query = query.Where(s => s.Doctor!.Name.Contains(intent.DoctorName));
        }

        if (!string.IsNullOrEmpty(timeSlot))
        {
            query = query.Where(s => s.TimeSlot == timeSlot);
        }

        var schedules = await query.OrderBy(s => s.TimeSlot).ToListAsync();

        if (schedules.Count == 0)
        {
            // 没有找到匹配的排班,查询该医生的其他可用时段
            var alternativeSchedules = await _context.Schedules
                .Include(s => s.Doctor)
                .Where(s => s.Doctor!.Name.Contains(intent.DoctorName!) &&
                           s.Date >= DateTime.Today &&
                           s.AvailableSlots > 0)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.TimeSlot)
                .Take(5)
                .ToListAsync();

            if (alternativeSchedules.Count == 0)
            {
                _workflowService.ClearContext(context.SessionId);
                return new WorkflowActionResponse
                {
                    Success = false,
                    Message = $"抱歉,未找到{intent.DoctorName}医生的可用排班",
                    CurrentWorkflow = WorkflowType.None,
                    CurrentState = WorkflowState.Failed
                };
            }

            context.CurrentState = WorkflowState.SelectingTimeSlot;
            _workflowService.UpdateContext(context);

            var options = alternativeSchedules.Select((s, index) => new WorkflowOption
            {
                Id = s.Id.ToString(),
                Label = $"[{index + 1}] {s.Date:MM月dd日} {s.TimeSlot} - {s.Doctor!.Name} - 剩余{s.AvailableSlots}号",
                Data = new
                {
                    ScheduleId = s.Id,
                    DoctorName = s.Doctor.Name,
                    Date = s.Date.ToString("yyyy-MM-dd"),
                    s.TimeSlot,
                    s.AvailableSlots
                }
            }).ToList();

            return new WorkflowActionResponse
            {
                Success = true,
                Message = $"{targetDate:MM月dd日}{timeSlot}没有{intent.DoctorName}医生的号源,以下是其他可选时段:",
                CurrentWorkflow = context.CurrentWorkflow,
                CurrentState = context.CurrentState,
                Options = options,
                NextAction = "请选择一个时段或回复序号"
            };
        }
        else if (schedules.Count == 1)
        {
            // 唯一匹配,直接进入时段选择(确认)
            var schedule = schedules[0];
            context.CurrentState = WorkflowState.SelectingTimeSlot;
            context.Data["scheduleId"] = schedule.Id;
            _workflowService.UpdateContext(context);

            var options = new List<WorkflowOption>
            {
                new WorkflowOption
                {
                    Id = schedule.Id.ToString(),
                    Label = $"{schedule.Date:MM月dd日} {schedule.TimeSlot} - {schedule.Doctor!.Name} - 剩余{schedule.AvailableSlots}号",
                    Data = new
                    {
                        ScheduleId = schedule.Id,
                        DoctorName = schedule.Doctor.Name,
                        Date = schedule.Date.ToString("yyyy-MM-dd"),
                        schedule.TimeSlot,
                        schedule.AvailableSlots
                    }
                }
            };

            return new WorkflowActionResponse
            {
                Success = true,
                Message = $"为您找到了以下排班:\n\n患者: {patient.Name}\n医生: {schedule.Doctor!.Name}\n科室: {schedule.Doctor.Department}\n日期: {schedule.Date:yyyy-MM-dd}\n时段: {schedule.TimeSlot}\n剩余号数: {schedule.AvailableSlots}\n\n请确认预约 (回复序号1或'确认')",
                CurrentWorkflow = context.CurrentWorkflow,
                CurrentState = context.CurrentState,
                Options = options,
                NextAction = "请确认预约"
            };
        }
        else
        {
            // 多个匹配,让用户选择
            context.CurrentState = WorkflowState.SelectingTimeSlot;
            _workflowService.UpdateContext(context);

            var options = schedules.Select((s, index) => new WorkflowOption
            {
                Id = s.Id.ToString(),
                Label = $"[{index + 1}] {s.Date:MM月dd日} {s.TimeSlot} - {s.Doctor!.Name} ({s.Doctor.Department}) - 剩余{s.AvailableSlots}号",
                Data = new
                {
                    ScheduleId = s.Id,
                    DoctorName = s.Doctor.Name,
                    Department = s.Doctor.Department,
                    Date = s.Date.ToString("yyyy-MM-dd"),
                    s.TimeSlot,
                    s.AvailableSlots
                }
            }).ToList();

            return new WorkflowActionResponse
            {
                Success = true,
                Message = $"找到{schedules.Count}个可选时段,请选择:",
                CurrentWorkflow = context.CurrentWorkflow,
                CurrentState = context.CurrentState,
                Options = options,
                NextAction = "请选择时段或回复序号"
            };
        }
    }
}
