using MedicalSystem.DTOs;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MedicalSystem.Services;

public interface IWorkflowService
{
    WorkflowContext GetOrCreateContext(string sessionId);
    void UpdateContext(WorkflowContext context);
    void ClearContext(string sessionId);
}

public class WorkflowService : IWorkflowService
{
    // 使用内存存储会话工作流上下文(生产环境应使用数据库或Redis)
    private static readonly ConcurrentDictionary<string, WorkflowContext> _contexts = new();

    public WorkflowContext GetOrCreateContext(string sessionId)
    {
        return _contexts.GetOrAdd(sessionId, _ => new WorkflowContext
        {
            SessionId = sessionId,
            CurrentWorkflow = WorkflowType.None,
            CurrentState = WorkflowState.Idle
        });
    }

    public void UpdateContext(WorkflowContext context)
    {
        context.UpdatedAt = DateTime.Now;
        _contexts.AddOrUpdate(context.SessionId, context, (_, _) => context);
    }

    public void ClearContext(string sessionId)
    {
        _contexts.TryRemove(sessionId, out _);
    }
}
