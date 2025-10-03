# AI智能门诊诊疗系统

基于AI会话的门诊诊疗管理系统，通过自然语言对话方式完成患者建档、预约挂号、医生看诊、病历记录、处方开具和收费等完整门诊流程。

## 技术栈

- **前端**: Vue 3 + Element UI Plus + Axios
- **后端**: .NET 9 + Entity Framework Core 9
- **数据库**: SQLite
- **AI集成**: DeepSeek / Qwen 大模型 API

## 项目结构

```
MZ/
├── backend/                 # 后端项目
│   ├── Models/             # 实体模型
│   ├── Data/               # 数据库上下文
│   ├── Services/           # 业务服务
│   ├── Controllers/        # API控制器
│   ├── DTOs/               # 数据传输对象
│   ├── Middleware/         # 中间件
│   ├── Program.cs          # 程序入口
│   └── appsettings.json    # 配置文件
│
├── frontend/               # 前端项目
│   ├── src/
│   │   ├── api/           # API接口
│   │   ├── components/    # Vue组件
│   │   ├── views/         # 页面视图
│   │   ├── utils/         # 工具函数
│   │   ├── App.vue        # 主应用组件
│   │   └── main.js        # 入口文件
│   └── package.json
│
└── 需求.md                 # 需求文档
```

## 快速开始

### 前置要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (v16+)
- DeepSeek 或 Qwen API 密钥

### 1. 配置AI API密钥

编辑 `backend/appsettings.json`，配置你的AI API密钥：

```json
{
  "AI": {
    "Provider": "DeepSeek",  // 或 "Qwen"
    "DeepSeek": {
      "ApiUrl": "https://api.deepseek.com/v1/chat/completions",
      "ApiKey": "YOUR_DEEPSEEK_API_KEY",  // 替换为你的密钥
      "Model": "deepseek-chat"
    }
  }
}
```

### 2. 启动后端

```bash
cd backend
dotnet restore
dotnet run
```

后端将在 `http://localhost:5001` 启动。

### 3. 启动前端

```bash
cd frontend
npm install
npm run dev
```

前端将在 `http://localhost:5173` 启动。

### 4. 初始化测试数据（可选）

为了测试系统功能，你可以手动在数据库中添加一些医生和排班数据。

打开 `backend/medical.db` 数据库，执行以下SQL（或使用API创建）：

```sql
-- 添加医生
INSERT INTO Doctors (Name, Title, Department, Specialization, IsActive, CreatedAt)
VALUES
('张医生', '主任医师', '内科', '心血管疾病', 1, datetime('now')),
('李医生', '副主任医师', '儿科', '儿童呼吸系统疾病', 1, datetime('now')),
('王医生', '主治医师', '外科', '普通外科手术', 1, datetime('now'));

-- 添加排班
INSERT INTO Schedules (DoctorId, Date, TimeSlot, TotalSlots, AvailableSlots, CreatedAt)
VALUES
(1, date('now', '+1 day'), '上午', 20, 20, datetime('now')),
(1, date('now', '+1 day'), '下午', 15, 15, datetime('now')),
(2, date('now', '+1 day'), '上午', 15, 15, datetime('now')),
(3, date('now', '+2 day'), '上午', 10, 10, datetime('now'));

-- 添加药品
INSERT INTO Medicines (Name, Specification, Unit, Price, Stock, Category, IsActive, CreatedAt)
VALUES
('阿莫西林胶囊', '0.25g*24粒', '盒', 15.50, 1000, '抗生素', 1, datetime('now')),
('布洛芬片', '0.2g*20片', '盒', 8.00, 800, '解热镇痛', 1, datetime('now')),
('维生素C片', '100mg*100片', '瓶', 12.00, 500, '维生素', 1, datetime('now'));
```

## 核心功能

### 1. AI会话交互
- 自然语言对话完成各项操作
- 智能识别用户意图
- 支持上下文记忆

### 2. 患者管理
- 患者建档（基本信息、病史）
- 患者信息查询和编辑
- 患者搜索

### 3. 预约挂号
- 选择科室和医生
- 查看医生排班
- 在线预约和取消

### 4. 医生看诊
- 查看患者档案
- 记录病历
- 开具处方
- 诊断记录

### 5. 收费管理
- 费用计算
- 收费记录
- 支付状态管理

## API接口文档

后端运行后，访问 `http://localhost:5001/swagger` 查看完整的API文档。

### 主要接口

#### 聊天相关
- `POST /api/chat/send` - 发送消息
- `GET /api/chat/history/{sessionId}` - 获取会话历史
- `DELETE /api/chat/history/{sessionId}` - 清除历史

#### 患者管理
- `GET /api/patients` - 获取患者列表
- `GET /api/patients/{id}` - 获取患者详情
- `POST /api/patients` - 创建患者
- `PUT /api/patients/{id}` - 更新患者
- `GET /api/patients/search?keyword=xxx` - 搜索患者

#### 预约挂号
- `POST /api/appointments` - 创建预约
- `GET /api/appointments/{id}` - 获取预约详情
- `PUT /api/appointments/{id}/cancel` - 取消预约
- `GET /api/appointments/patient/{patientId}` - 获取患者预约列表

#### 医生和排班
- `GET /api/doctors` - 获取医生列表
- `GET /api/doctors/schedules` - 获取排班信息

## 开发说明

### 添加新的实体模型

1. 在 `backend/Models/` 中创建实体类
2. 在 `backend/Data/MedicalDbContext.cs` 中添加 DbSet
3. 配置实体关系（如果需要）
4. 运行迁移：`dotnet ef migrations add MigrationName`
5. 更新数据库：`dotnet ef database update`

### 添加新的API接口

1. 在 `backend/Controllers/` 中创建控制器
2. 在 `frontend/src/api/` 中创建对应的API封装
3. 在组件中调用API

### 自定义AI Prompt

编辑 `backend/Services/AIService.cs` 中的 `GetSystemPrompt()` 方法来自定义AI的行为和角色。

## 常见问题

### 1. 后端无法启动
- 确保已安装 .NET 9 SDK
- 检查端口5001是否被占用
- 查看控制台错误信息

### 2. 前端无法连接后端
- 确认后端已正常运行
- 检查 `frontend/src/api/axios.js` 中的 baseURL 配置
- 检查浏览器控制台的网络请求

### 3. AI无法响应
- 确认已配置正确的API密钥
- 检查网络连接
- 查看后端日志中的错误信息
- 确认AI服务商API配额是否充足

### 4. 数据库错误
- 删除 `backend/medical.db` 文件，重新启动后端自动创建
- 或手动执行迁移：`dotnet ef database update`

## 贡献指南

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License
