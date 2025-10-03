<template>
  <div id="app">
    <el-container class="main-container">
      <!-- 顶部导航 -->
      <el-header class="header">
        <div class="header-content">
          <h1>AI智能门诊诊疗系统</h1>
          <el-menu mode="horizontal" :default-active="activeMenu" @select="handleMenuSelect">
            <el-menu-item index="chat">AI会话</el-menu-item>
            <el-menu-item index="patients">患者管理</el-menu-item>
            <el-menu-item index="appointments">预约挂号</el-menu-item>
          </el-menu>
        </div>
      </el-header>

      <!-- 主内容区 -->
      <el-main class="main-content">
        <!-- AI会话页面 -->
        <div v-show="activeMenu === 'chat'" class="page-container">
          <el-row :gutter="20">
            <el-col :span="6">
              <el-card>
                <template #header>
                  <div class="card-header">
                    <span>快捷操作</span>
                  </div>
                </template>
                <el-button @click="showPatientForm = true" type="primary" style="width: 100%; margin-bottom: 10px">
                  患者建档
                </el-button>
                <el-button @click="handleQuickAction('appointment')" style="width: 100%; margin-bottom: 10px">
                  预约挂号
                </el-button>
                <el-button @click="handleQuickAction('query')" style="width: 100%">
                  查询记录
                </el-button>
              </el-card>
            </el-col>
            <el-col :span="18">
              <ChatWindow
                :session-id="sessionId"
                :patient-id="currentPatientId"
                @show-patient-form="showPatientForm = true"
                @show-appointment="activeMenu = 'appointments'"
                @patient-extracted="handlePatientExtracted"
                @appointments-available="handleAppointmentsAvailable"
                @patient-selected="handlePatientSelected"
              />
            </el-col>
          </el-row>
        </div>

        <!-- 患者管理页面 -->
        <div v-show="activeMenu === 'patients'" class="page-container">
          <el-card>
            <template #header>
              <div class="card-header">
                <span>患者管理</span>
                <el-button type="primary" @click="showPatientForm = true">新建患者</el-button>
              </div>
            </template>

            <el-input
              v-model="searchKeyword"
              placeholder="搜索患者（姓名/电话/身份证）"
              class="search-input"
              @input="handleSearch"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>

            <el-table :data="patients" style="width: 100%; margin-top: 20px">
              <el-table-column prop="id" label="ID" width="80" />
              <el-table-column prop="name" label="姓名" width="120" />
              <el-table-column prop="gender" label="性别" width="80" />
              <el-table-column prop="age" label="年龄" width="80" />
              <el-table-column prop="phone" label="电话" width="150" />
              <el-table-column prop="idCard" label="身份证号" width="180" />
              <el-table-column label="操作" width="180">
                <template #default="scope">
                  <el-button size="small" @click="handleEdit(scope.row)">编辑</el-button>
                  <el-button size="small" type="primary" @click="handleViewRecords(scope.row)">病历</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-card>
        </div>

        <!-- 预约挂号页面 -->
        <div v-show="activeMenu === 'appointments'" class="page-container">
          <el-card>
            <template #header>
              <div class="card-header">
                <span>预约挂号</span>
              </div>
            </template>

            <el-row :gutter="20">
              <el-col :span="12">
                <h3>选择科室和医生</h3>
                <el-select v-model="selectedDepartment" placeholder="选择科室" style="width: 100%; margin-bottom: 20px" @change="loadDoctors">
                  <el-option label="内科" value="内科" />
                  <el-option label="外科" value="外科" />
                  <el-option label="儿科" value="儿科" />
                  <el-option label="妇产科" value="妇产科" />
                  <el-option label="骨科" value="骨科" />
                </el-select>

                <el-table :data="doctors" @row-click="handleDoctorSelect" highlight-current-row>
                  <el-table-column prop="name" label="医生" />
                  <el-table-column prop="title" label="职称" />
                  <el-table-column prop="specialization" label="擅长" />
                </el-table>
              </el-col>

              <el-col :span="12">
                <h3>选择时间</h3>
                <el-date-picker
                  v-model="selectedDate"
                  type="date"
                  placeholder="选择日期"
                  style="width: 100%; margin-bottom: 20px"
                  :disabled-date="disabledDate"
                  @change="loadSchedules"
                />

                <el-table :data="schedules" @row-click="handleScheduleSelect" highlight-current-row>
                  <el-table-column prop="timeSlot" label="时段" />
                  <el-table-column label="剩余号源">
                    <template #default="scope">
                      {{ scope.row.availableSlots }} / {{ scope.row.totalSlots }}
                    </template>
                  </el-table-column>
                </el-table>

                <el-button
                  type="primary"
                  style="width: 100%; margin-top: 20px"
                  :disabled="!selectedSchedule"
                  @click="confirmAppointment"
                >
                  确认预约
                </el-button>
              </el-col>
            </el-row>
          </el-card>
        </div>
      </el-main>
    </el-container>

    <!-- 患者表单对话框 -->
    <PatientForm
      v-model:visible="showPatientForm"
      :patient-data="editingPatient"
      @success="handlePatientFormSuccess"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search } from '@element-plus/icons-vue'
import ChatWindow from './components/ChatWindow.vue'
import PatientForm from './components/PatientForm.vue'
import { patientApi } from './api/patient'
import { doctorApi } from './api/doctor'
import { appointmentApi } from './api/appointment'

// 生成会话ID
const sessionId = ref(`session-${Date.now()}`)
const currentPatientId = ref(null)
const activeMenu = ref('chat')

// 患者管理相关
const patients = ref([])
const searchKeyword = ref('')
const showPatientForm = ref(false)
const editingPatient = ref(null)

// 预约相关
const selectedDepartment = ref('')
const selectedDate = ref(new Date())
const selectedDoctor = ref(null)
const selectedSchedule = ref(null)
const doctors = ref([])
const schedules = ref([])

// 菜单切换
const handleMenuSelect = (index) => {
  activeMenu.value = index
  if (index === 'patients') {
    loadPatients()
  }
}

// 快捷操作
const handleQuickAction = (action) => {
  ElMessage.info(`请在会话中输入：${action === 'appointment' ? '我要预约挂号' : '查询我的就诊记录'}`)
}

// 加载患者列表
const loadPatients = async () => {
  try {
    patients.value = await patientApi.getPatients()
  } catch (error) {
    console.error('加载患者列表失败:', error)
  }
}

// 搜索患者
const handleSearch = async () => {
  if (!searchKeyword.value.trim()) {
    loadPatients()
    return
  }

  try {
    patients.value = await patientApi.searchPatients(searchKeyword.value)
  } catch (error) {
    console.error('搜索失败:', error)
  }
}

// 编辑患者
const handleEdit = (patient) => {
  editingPatient.value = patient
  showPatientForm.value = true
}

// 查看病历
const handleViewRecords = (patient) => {
  currentPatientId.value = patient.id
  activeMenu.value = 'chat'
  ElMessage.success(`已切换到患者：${patient.name}`)
}

// 患者表单提交成功
const handlePatientFormSuccess = () => {
  loadPatients()
  editingPatient.value = null
}

// 处理从会话中提取的患者信息
const handlePatientExtracted = async (data) => {
  try {
    if (data.isUpdate) {
      // 更新患者信息
      const updateData = {
        name: data.name,
        gender: data.gender,
        dateOfBirth: data.dateOfBirth,
        phone: data.phone,
        idCard: data.idCard,
        address: data.address,
        allergies: data.allergies,
        medicalHistory: data.medicalHistory,
        familyHistory: data.familyHistory
      }
      await patientApi.updatePatient(data.existingPatientId, updateData)
      ElMessage.success('患者信息已更新')
      currentPatientId.value = data.existingPatientId
    } else {
      // 创建新患者 - 确保dateOfBirth不为空
      const createData = {
        name: data.name || '',
        gender: data.gender || '',
        dateOfBirth: data.dateOfBirth || new Date('2000-01-01'),
        phone: data.phone || '',
        idCard: data.idCard,
        address: data.address,
        allergies: data.allergies,
        medicalHistory: data.medicalHistory,
        familyHistory: data.familyHistory
      }
      const result = await patientApi.createPatient(createData)
      ElMessage.success('患者档案已创建')
      currentPatientId.value = result.id
    }

    loadPatients()
  } catch (error) {
    console.error('保存患者信息失败:', error)
    ElMessage.error(error.response?.data?.error || '保存患者信息失败')
  }
}

// 处理患者选择
const handlePatientSelected = (patient) => {
  currentPatientId.value = patient.id
  ElMessage.success(`已选择患者：${patient.name}`)
}

// 处理可预约信息
const handleAppointmentsAvailable = (data) => {
  // 显示可预约信息的对话框或切换到预约页面
  ElMessageBox.alert(
    `<div>
      <p><strong>可预约科室和医生（${data.date}）：</strong></p>
      ${data.departments.map(dept => `
        <p><strong>${dept.name}：</strong></p>
        <ul>
          ${dept.doctors.map(doc => `
            <li>${doc.name} - ${doc.title} ${doc.hasAvailableSlots ? '(有号)' : '(无号)'}</li>
          `).join('')}
        </ul>
      `).join('')}
    </div>`,
    '可预约信息',
    {
      dangerouslyUseHTMLString: true,
      confirmButtonText: '去预约',
      showCancelButton: true,
      cancelButtonText: '关闭'
    }
  ).then(() => {
    activeMenu.value = 'appointments'
  }).catch(() => {
    // 用户点击关闭
  })
}

// 加载医生列表
const loadDoctors = async () => {
  try {
    doctors.value = await doctorApi.getDoctors(selectedDepartment.value)
    selectedDoctor.value = null
    schedules.value = []
  } catch (error) {
    console.error('加载医生列表失败:', error)
  }
}

// 选择医生
const handleDoctorSelect = (row) => {
  selectedDoctor.value = row
  loadSchedules()
}

// 加载排班
const loadSchedules = async () => {
  if (!selectedDoctor.value || !selectedDate.value) return

  try {
    schedules.value = await doctorApi.getSchedules(
      selectedDoctor.value.id,
      selectedDate.value
    )
  } catch (error) {
    console.error('加载排班失败:', error)
  }
}

// 选择排班
const handleScheduleSelect = (row) => {
  selectedSchedule.value = row
}

// 禁用过去的日期
const disabledDate = (time) => {
  return time.getTime() < Date.now() - 24 * 60 * 60 * 1000
}

// 确认预约
const confirmAppointment = async () => {
  if (!selectedSchedule.value) {
    ElMessage.warning('请选择时间段')
    return
  }

  // 检查是否有当前患者
  if (!currentPatientId.value) {
    ElMessageBox.confirm(
      '您还没有建立患者档案，是否现在建档？',
      '需要先建档',
      {
        confirmButtonText: '去建档',
        cancelButtonText: '取消',
        type: 'warning'
      }
    ).then(() => {
      showPatientForm.value = true
    }).catch(() => {
      // 用户取消
    })
    return
  }

  try {
    await appointmentApi.createAppointment({
      patientId: currentPatientId.value,
      doctorId: selectedDoctor.value.id,
      scheduleId: selectedSchedule.value.id
    })
    ElMessage.success('预约成功')
    loadSchedules() // 刷新排班信息
  } catch (error) {
    console.error('预约失败:', error)

    // 处理患者不存在的错误
    if (error.response?.data?.errorCode === 'PATIENT_NOT_FOUND') {
      ElMessageBox.confirm(
        error.response.data.message,
        '患者档案不存在',
        {
          confirmButtonText: '去建档',
          cancelButtonText: '取消',
          type: 'error'
        }
      ).then(() => {
        showPatientForm.value = true
      })
    } else {
      ElMessage.error(error.response?.data?.error || '预约失败')
    }
  }
}

onMounted(() => {
  loadPatients()
})
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

#app {
  font-family: 'Helvetica Neue', Helvetica, 'PingFang SC', 'Hiragino Sans GB', 'Microsoft YaHei', Arial, sans-serif;
  height: 100vh;
}

.main-container {
  height: 100%;
}

.header {
  background-color: #fff;
  border-bottom: 1px solid #e4e7ed;
  padding: 0;
}

.header-content {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  height: 100%;
  padding: 0 20px;
}

.header-content h1 {
  font-size: 20px;
  margin-right: 50px;
  color: #303133;
}

.main-content {
  background-color: #f5f7fa;
  padding: 20px;
  overflow-y: auto;
}

.page-container {
  max-width: 1400px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-input {
  margin-bottom: 20px;
}
</style>
