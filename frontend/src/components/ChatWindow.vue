<template>
  <div class="chat-window">
    <el-card class="chat-container">
      <!-- 消息列表 -->
      <div class="message-list" ref="messageList">
        <div
          v-for="msg in messages"
          :key="msg.id"
          :class="['message-item', msg.role === 'User' ? 'user-message' : 'assistant-message']"
        >
          <div class="message-avatar">
            {{ msg.role === 'User' ? '我' : 'AI' }}
          </div>
          <div class="message-content">
            <div class="message-text" v-html="formatMessage(msg.content)"></div>
            <div v-if="msg.role === 'Assistant' && hasActions(msg.content)" class="message-actions">
              <el-button
                v-if="msg.content.includes('[ACTION:SAVE_PATIENT]')"
                type="primary"
                size="small"
                @click="handleSavePatient"
              >
                保存患者档案
              </el-button>
              <el-button
                v-if="msg.content.includes('[ACTION:APPOINTMENT]')"
                type="success"
                size="small"
                @click="handleShowAppointments"
              >
                查看可预约信息
              </el-button>
            </div>
            <div class="message-time">{{ formatTime(msg.createdAt) }}</div>
          </div>
        </div>
        <div v-if="loading" class="message-item assistant-message">
          <div class="message-avatar">AI</div>
          <div class="message-content">
            <el-icon class="is-loading"><Loading /></el-icon>
            <span class="loading-text">AI正在思考...</span>
          </div>
        </div>
      </div>

      <!-- 输入框 -->
      <div class="input-area">
        <el-input
          v-model="inputMessage"
          type="textarea"
          :rows="3"
          placeholder="请输入您的问题..."
          @keydown.enter.ctrl="sendMessage"
        />
        <div class="input-actions">
          <el-button @click="clearHistory" text>清除历史</el-button>
          <el-button type="primary" @click="sendMessage" :loading="loading">
            发送 (Ctrl+Enter)
          </el-button>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import { chatApi } from '../api/chat'

const props = defineProps({
  sessionId: {
    type: String,
    required: true
  },
  patientId: {
    type: Number,
    default: null
  }
})

const emit = defineEmits(['showPatientForm', 'showAppointment', 'patientExtracted', 'appointmentsAvailable', 'patientSelected'])

const messages = ref([])
const inputMessage = ref('')
const loading = ref(false)
const messageList = ref(null)

// 加载历史消息
const loadHistory = async () => {
  try {
    const history = await chatApi.getHistory(props.sessionId)
    messages.value = history
    scrollToBottom()
  } catch (error) {
    console.error('加载历史消息失败:', error)
  }
}

// 当前工作流状态
const currentWorkflow = ref(null)
const currentWorkflowState = ref(null)
const workflowOptions = ref([])

// 发送消息
const sendMessage = async () => {
  if (!inputMessage.value.trim() || loading.value) return

  const userMessage = inputMessage.value
  inputMessage.value = ''

  // 添加用户消息到列表
  messages.value.push({
    id: Date.now(),
    content: userMessage,
    role: 'User',
    createdAt: new Date()
  })

  scrollToBottom()
  loading.value = true

  try {
    // 检测是否是预约意图 - 必须包含明确的预约关键词
    const isAppointmentIntent = /预约|挂号|约.*号|给.*预约.*号/i.test(userMessage) &&
                                !/查找|查询|搜索|显示|列出|看看|有没有/i.test(userMessage)

    if (isAppointmentIntent || currentWorkflow.value !== null) {
      // 使用工作流处理
      await handleWorkflow(userMessage)
    } else {
      const response = await chatApi.sendMessage({
        sessionId: props.sessionId,
        message: userMessage,
        patientId: props.patientId
      })

      messages.value.push({
        id: Date.now() + 1,
        content: response.message,
        role: 'Assistant',
        createdAt: new Date(response.timestamp)
      })
    }

    scrollToBottom()
  } catch (error) {
    ElMessage.error('发送消息失败')
  } finally {
    loading.value = false
  }
}

// 处理工作流
const handleWorkflow = async (userMessage) => {
  try {
    const response = await fetch('http://localhost:5000/api/workflow/process', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        sessionId: props.sessionId,
        message: userMessage
      })
    })

    const data = await response.json()

    // 更新工作流状态
    currentWorkflow.value = data.currentWorkflow
    currentWorkflowState.value = data.currentState
    workflowOptions.value = data.options || []

    // 显示消息
    let messageContent = data.message

    // 如果有选项，添加选项列表到消息
    if (data.options && data.options.length > 0) {
      const optionsList = data.options.map((opt, index) => opt.label).join('\n')
      messageContent += '\n\n' + optionsList
    }

    messages.value.push({
      id: Date.now() + 1,
      content: data.success ? messageContent : `❌ ${messageContent}`,
      role: 'Assistant',
      createdAt: new Date()
    })

    // 如果工作流完成，清除状态
    if (data.currentState === 'Completed' || data.currentState === 'Failed') {
      if (data.success) {
        ElMessage.success(data.currentWorkflow === 'Appointment' ? '预约成功' : '建档成功')
      }
      currentWorkflow.value = null
      currentWorkflowState.value = null
      workflowOptions.value = []
    }
  } catch (error) {
    console.error('工作流处理失败:', error)
    ElMessage.error('处理失败')
    currentWorkflow.value = null
    currentWorkflowState.value = null
    workflowOptions.value = []
  }
}

// 清除历史
const clearHistory = async () => {
  try {
    await chatApi.clearHistory(props.sessionId)
    messages.value = []
    ElMessage.success('历史记录已清除')
  } catch (error) {
    ElMessage.error('清除历史失败')
  }
}

// 检查消息是否包含操作按钮
const hasActions = (content) => {
  return content.includes('[ACTION:')
}

// 保存患者档案
const handleSavePatient = async () => {
  try {
    loading.value = true
    const response = await fetch('http://localhost:5000/api/chat/extract-patient', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sessionId: props.sessionId })
    })

    const data = await response.json()

    if (!data.hasData) {
      ElMessage.warning('对话中没有足够的患者信息')
      return
    }

    // 检查是否存在患者
    if (data.existingPatientId) {
      ElMessageBox.confirm(
        `检测到患者"${data.existingPatientName}"(手机号: ${data.phone})已存在档案，是否更新信息？`,
        '患者已存在',
        {
          confirmButtonText: '更新信息',
          cancelButtonText: '取消',
          type: 'warning'
        }
      ).then(() => {
        emit('patientExtracted', { ...data, isUpdate: true })
      }).catch(() => {
        ElMessage.info('已取消')
      })
    } else {
      ElMessageBox.confirm(
        `是否为"${data.name}"创建患者档案？`,
        '确认创建档案',
        {
          confirmButtonText: '创建',
          cancelButtonText: '取消',
          type: 'info'
        }
      ).then(() => {
        emit('patientExtracted', { ...data, isUpdate: false })
      }).catch(() => {
        ElMessage.info('已取消')
      })
    }
  } catch (error) {
    ElMessage.error('提取患者信息失败')
  } finally {
    loading.value = false
  }
}

// 显示可预约信息（先验证患者）
const handleShowAppointments = async () => {
  try {
    loading.value = true

    // 先检查是否有患者ID
    if (!props.patientId) {
      ElMessageBox.confirm(
        '预约需要先建立患者档案。您可以：\n1. 通过手机号查询已有档案\n2. 创建新的患者档案',
        '需要患者信息',
        {
          distinguishCancelAndClose: true,
          confirmButtonText: '查询档案',
          cancelButtonText: '新建档案',
          type: 'info'
        }
      ).then(async () => {
        // 用户选择查询档案
        const { value: phone } = await ElMessageBox.prompt('请输入患者手机号', '查询患者档案', {
          confirmButtonText: '查询',
          cancelButtonText: '取消',
          inputPattern: /^1[3-9]\d{9}$/,
          inputErrorMessage: '请输入有效的手机号'
        })

        // 调用验证接口
        const verifyResponse = await fetch('http://localhost:5000/api/appointments/verify-patient', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ phone })
        })

        const verifyData = await verifyResponse.json()

        if (verifyData.found) {
          ElMessage.success(verifyData.message)
          // 设置当前患者并显示预约信息
          emit('patientSelected', verifyData.patient)
          showAppointmentsDialog()
        } else if (verifyData.multipleMatches) {
          // 显示多个匹配的患者供选择
          ElMessage.warning(verifyData.message)
        } else {
          ElMessageBox.confirm(
            verifyData.message,
            '未找到患者',
            {
              confirmButtonText: '去建档',
              cancelButtonText: '取消',
              type: 'warning'
            }
          ).then(() => {
            emit('showPatientForm')
          })
        }
      }).catch((action) => {
        if (action === 'cancel') {
          // 用户选择新建档案
          emit('showPatientForm')
        }
      })
      return
    }

    // 有患者ID，直接显示预约信息
    await showAppointmentsDialog()
  } catch (error) {
    console.error('预约流程错误:', error)
    ElMessage.error('操作失败，请重试')
  } finally {
    loading.value = false
  }
}

// 显示预约信息对话框
const showAppointmentsDialog = async () => {
  const response = await fetch('http://localhost:5000/api/chat/available-appointments')
  const data = await response.json()
  emit('appointmentsAvailable', data)
}

// 格式化消息（支持Markdown表格和基本语法，并移除ACTION标记）
const formatMessage = (content) => {
  let formatted = content.replace(/\[ACTION:[^\]]+\]/g, '') // 移除ACTION标记

  // 处理Markdown表格
  const tableRegex = /\|(.+)\|\n\|[-:\s|]+\|\n((?:\|.+\|\n?)+)/g
  formatted = formatted.replace(tableRegex, (match) => {
    const lines = match.trim().split('\n')
    const headers = lines[0].split('|').filter(cell => cell.trim())
    const rows = lines.slice(2).map(row =>
      row.split('|').filter(cell => cell.trim())
    )

    let table = '<table class="schedule-table"><thead><tr>'
    headers.forEach(header => {
      table += `<th>${header.trim()}</th>`
    })
    table += '</tr></thead><tbody>'

    rows.forEach(row => {
      if (row.length > 0) {
        table += '<tr>'
        row.forEach((cell, index) => {
          const trimmedCell = cell.trim()
          // 如果是日期列（MM-dd格式），高亮显示
          if (trimmedCell.match(/^\d{2}-\d{2}$/)) {
            table += `<td class="date-cell">${trimmedCell}</td>`
          } else {
            table += `<td>${trimmedCell}</td>`
          }
        })
        table += '</tr>'
      }
    })
    table += '</tbody></table>'
    return table
  })

  // 处理其他Markdown语法
  formatted = formatted
    .replace(/【([^】]+)】/g, '<div class="department-title">【$1】</div>') // 科室标题
    .replace(/\n/g, '<br>')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')

  return formatted
}

// 格式化时间
const formatTime = (date) => {
  const d = new Date(date)
  return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`
}

// 滚动到底部
const scrollToBottom = () => {
  nextTick(() => {
    if (messageList.value) {
      messageList.value.scrollTop = messageList.value.scrollHeight
    }
  })
}

// 监听sessionId变化，重新加载历史
watch(() => props.sessionId, () => {
  loadHistory()
})

onMounted(() => {
  loadHistory()
})
</script>

<style scoped>
.chat-window {
  height: 100%;
}

.chat-container {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.message-list {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  min-height: 400px;
  max-height: 600px;
}

.message-item {
  display: flex;
  margin-bottom: 20px;
  align-items: flex-start;
}

.user-message {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  font-weight: bold;
  flex-shrink: 0;
}

.user-message .message-avatar {
  background-color: #409eff;
  color: white;
  margin-left: 12px;
}

.assistant-message .message-avatar {
  background-color: #67c23a;
  color: white;
  margin-right: 12px;
}

.message-content {
  max-width: 70%;
  background-color: #f5f7fa;
  padding: 12px 16px;
  border-radius: 8px;
}

.user-message .message-content {
  background-color: #409eff;
  color: white;
}

.message-text {
  line-height: 1.6;
  word-wrap: break-word;
}

.message-time {
  font-size: 12px;
  color: #909399;
  margin-top: 8px;
}

.user-message .message-time {
  color: rgba(255, 255, 255, 0.8);
  text-align: right;
}

.loading-text {
  margin-left: 8px;
}

.message-actions {
  margin-top: 12px;
  display: flex;
  gap: 8px;
}

.department-title {
  font-weight: bold;
  color: #409eff;
  margin: 10px 0 5px 0;
  font-size: 15px;
}

.schedule-table {
  width: 100%;
  border-collapse: collapse;
  margin: 10px 0;
  font-size: 14px;
  background-color: white;
  border-radius: 4px;
  overflow: hidden;
}

.schedule-table th,
.schedule-table td {
  border: 1px solid #e4e7ed;
  padding: 8px 12px;
  text-align: left;
}

.schedule-table th {
  background-color: #f5f7fa;
  color: #606266;
  font-weight: 600;
}

.schedule-table tbody tr:hover {
  background-color: #f5f7fa;
}

.schedule-table td {
  color: #303133;
}

.schedule-table td.date-cell {
  font-weight: 600;
  color: #409eff;
}

.input-area {
  border-top: 1px solid #e4e7ed;
  padding-top: 16px;
  margin-top: 16px;
}

.input-actions {
  display: flex;
  justify-content: space-between;
  margin-top: 12px;
}
</style>
