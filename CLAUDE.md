# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AI-powered medical clinic management system (门诊诊疗系统) with natural language conversation interface. The system handles patient registration, appointment booking, medical consultations, prescriptions, and billing through AI chat.

**Tech Stack:**
- Backend: .NET 9 (Minimal API), Entity Framework Core 9, SQLite
- Frontend: Vue 3 (Composition API), Element Plus UI, Vite
- AI Integration: DeepSeek/Qwen APIs for natural language processing

**Languages:** Backend code and comments are in English, frontend UI and user-facing content are in Chinese (医疗系统面向中文用户).

## Development Commands

### Backend (.NET 9)

```bash
# From /backend directory
dotnet restore                    # Restore dependencies
dotnet build                      # Build project
dotnet run                        # Run (http://localhost:5000)
"/c/Program Files/dotnet/dotnet.exe" run  # Windows Git Bash

# Database
# SQLite DB (medical.db) auto-creates on first run
# Initialize with: sqlite3 medical.db < init-data.sql
```

### Frontend (Vue 3 + Vite)

```bash
# From /frontend directory
npm install                       # Install dependencies
npm run dev                       # Dev server (http://localhost:5173)
npm run build                     # Production build
```

### Running Full Stack

Start both services concurrently:
1. Backend: `cd backend && dotnet run` (port 5000)
2. Frontend: `cd frontend && npm run dev` (port 5173)

## Architecture Overview

### Workflow System (核心架构)

The system uses a **state machine-based workflow architecture** for intelligent appointment booking:

**Two Main Workflows:**
1. **PatientRegistration (建档工作流)** - Collects patient info via conversation → Creates patient record
2. **Appointment (预约挂号工作流)** - Queries schedules → User selects time slot → Creates appointment

**Workflow States:**
- `Idle` → `CollectingPatientInfo` / `PatientSelection` / `QueryingSchedule` → `SelectingTimeSlot` → `Completed` / `Failed`

**Key Flow:**
```
User: "给张丽预约明天刘洋医生下午的号"
   ↓
AI parses intent (patient name, doctor, date, time slot)
   ↓
Check if patient exists:
  - Not found → Start PatientRegistration workflow → Continue to Appointment
  - Found (unique) → Retrieve record → Start Appointment workflow
  - Found (multiple) → Show candidates → User selects → Continue
   ↓
Query doctor schedules (filter by date, doctor name, time slot)
   ↓
Display available slots → User selects → Create appointment
```

**Implementation:**
- `WorkflowService` - Manages session context (in-memory, use Redis for production)
- `WorkflowController.ProcessWorkflow()` - Main entry point for all workflow actions
- `AIService.ParseAppointmentIntentAsync()` - NLP extracts structured data from natural language

### AI Integration Architecture

**AIService Design:**
- `GetSystemPrompt()` defines AI persona as medical assistant (医疗助理角色设定)
- Supports ACTION markers: `[ACTION:SAVE_PATIENT]`, `[ACTION:APPOINTMENT]` trigger UI buttons
- Returns Markdown tables for schedule display (rendered in ChatWindow.vue)
- `ExtractPatientInfoAsync()` - Extracts structured patient data from conversation history
- `ParseAppointmentIntentAsync()` - Parses natural language appointment requests

**Configuration:** Edit `appsettings.json` to switch AI providers:
```json
"AI": {
  "Provider": "DeepSeek",  // or "Qwen"
  "DeepSeek": { "ApiKey": "sk-...", "Model": "deepseek-chat" }
}
```

### Database Schema (10 Core Tables)

**Key Relationships:**
- `Patient` 1→N `Appointment` N→1 `Schedule` N→1 `Doctor`
- `Appointment` 1→1 `MedicalRecord` 1→N `Prescription` 1→N `PrescriptionDetail` N→1 `Medicine`
- `ConversationHistory` stores AI chat for context (sessionId, role, content)

**Important:**
- `Doctor.Department` is a **string field**, not a navigation property (don't use `.ThenInclude(d => d.Department)`)
- `Patient.DateOfBirth` is **non-nullable DateTime**
- `Appointment` does NOT have `AppointmentNumber` field (use `Id` instead)

### Frontend Architecture

**Core Components:**
- `ChatWindow.vue` - AI conversation interface with workflow handling
  - Detects appointment intent: `/预约|挂号|约.*号|明天|后天/i`
  - Calls `POST /api/workflow/process` for workflow-based interactions
  - Renders Markdown tables for schedules
  - Displays workflow options (patient/time slot selection)

**API Layer (`/frontend/src/api/`):**
- `axios.js` - Configured for `http://localhost:5000/api` with CORS
- `chat.js`, `patient.js`, `appointment.js`, `doctor.js` - API modules

### Key Controllers

**WorkflowController** (`POST /api/workflow/process`)
- Orchestrates PatientRegistration and Appointment workflows
- Handles patient verification, schedule queries, time slot selection
- Returns `WorkflowActionResponse` with state, options, and next action

**ChatController**
- `POST /api/chat/send` - AI conversation (uses AIService)
- `POST /api/chat/extract-patient` - Extract patient info from conversation
- `GET /api/chat/schedule-table` - Generate schedule Markdown table
- `POST /api/chat/smart-appointment` - (Legacy, replaced by WorkflowController)

**AppointmentsController**
- `POST /api/appointments/verify-patient` - Check patient exists before booking
- Prevents duplicate appointments

## Important Implementation Notes

### When Writing Backend Code

1. **EF Core Includes:** `Doctor.Department` is a string, NOT a navigation property:
   ```csharp
   // ❌ WRONG
   .Include(s => s.Doctor).ThenInclude(d => d.Department)

   // ✅ CORRECT
   .Include(s => s.Doctor)
   // Then access: schedule.Doctor.Department (string)
   ```

2. **Appointment Model:** No `AppointmentNumber` field exists, use `Id`:
   ```csharp
   // ❌ WRONG
   appointment.AppointmentNumber

   // ✅ CORRECT
   appointment.Id  // Use this as appointment identifier
   ```

3. **DateTime Handling:**
   ```csharp
   // Patient.DateOfBirth is DateTime (non-nullable)
   DateOfBirth = extractedInfo.DateOfBirth ?? DateTime.MinValue

   // For display:
   p.DateOfBirth.ToString("yyyy-MM-dd")
   ```

4. **AI Context Usage:** Always pass conversation history to AIService:
   ```csharp
   var conversation = await _context.ConversationHistories
       .Where(c => c.SessionId == sessionId)
       .OrderBy(c => c.CreatedAt)
       .Select(c => new ConversationMessage { Role = c.Role, Content = c.Content })
       .ToListAsync();

   var extracted = await _aiService.ExtractPatientInfoAsync(conversation);
   ```

### When Writing Frontend Code

1. **Workflow Integration:** Check if current workflow is active before sending to WorkflowController:
   ```javascript
   const isAppointmentIntent = /预约|挂号|约.*号|明天|后天/i.test(userMessage)

   if (isAppointmentIntent || currentWorkflow.value !== null) {
     await handleWorkflow(userMessage)  // Uses /api/workflow/process
   }
   ```

2. **Markdown Rendering:** ChatWindow.vue renders tables from AI responses:
   ```javascript
   // Tables in format: | Header1 | Header2 |\n|---------|---------|...
   // Automatically converted to <table class="schedule-table">
   ```

3. **CORS Configuration:** Backend allows `http://localhost:5173` and `5174`:
   ```csharp
   // Program.cs
   policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
   ```

## AI Prompt Engineering

**System Prompt Location:** `backend/Services/AIService.cs → GetSystemPrompt()`

**Current Persona:** Professional medical assistant (专业医疗助理) who:
- Guides patient registration with conversational prompts
- Displays doctor schedules as Markdown tables with date column
- Uses ACTION markers to trigger UI buttons: `[ACTION:SAVE_PATIENT]`, `[ACTION:APPOINTMENT]`

**Intent Parsing Prompt:** `ParseAppointmentIntentAsync()` uses specialized prompt:
```
从用户的预约请求中提取关键信息：
- patientName: 患者姓名
- doctorName: 医生姓名
- dateOffset: 0=今天, 1=明天, 2=后天
- timeSlot: 上午/下午/夜间
返回JSON格式...
```

## Configuration & Environment

### Required Setup

1. **AI API Key:** Edit `backend/appsettings.json`:
   ```json
   "AI": {
     "Provider": "DeepSeek",
     "DeepSeek": { "ApiKey": "sk-YOUR_KEY_HERE" }
   }
   ```

2. **Database:** SQLite (`medical.db`) auto-creates, initialize with:
   ```bash
   sqlite3 backend/medical.db < backend/init-data.sql
   ```

3. **Ports:** Backend:5000, Frontend:5173 (CORS configured)

### Production Considerations

- **WorkflowService:** Currently uses in-memory `ConcurrentDictionary` for session state. For production:
  - Switch to Redis/distributed cache
  - Implement session timeout/cleanup

- **Database:** Consider PostgreSQL/MySQL instead of SQLite for production

- **API Keys:** Move to environment variables/Azure Key Vault instead of appsettings.json

## Common Issues & Fixes

**CORS Errors:** Ensure backend CORS policy includes frontend origin (default: localhost:5173)

**AI Response Timeout:** Increase HttpClient timeout in AIService if DeepSeek API is slow

**Workflow State Stuck:** WorkflowService.ClearContext() clears session, or restart backend to reset all sessions

**Schedule Query Empty:** Test data uses future dates (today+1, today+2), query uses 7-day window from today
