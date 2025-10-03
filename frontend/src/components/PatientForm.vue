<template>
  <el-dialog
    :model-value="visible"
    :title="isEdit ? '编辑患者信息' : '患者建档'"
    width="700px"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="120px"
    >
      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="姓名" prop="name">
            <el-input v-model="form.name" placeholder="请输入姓名" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="性别" prop="gender">
            <el-select v-model="form.gender" placeholder="请选择性别" style="width: 100%">
              <el-option label="男" value="男" />
              <el-option label="女" value="女" />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item label="出生日期" prop="dateOfBirth">
            <el-date-picker
              v-model="form.dateOfBirth"
              type="date"
              placeholder="选择日期"
              style="width: 100%"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="联系电话" prop="phone">
            <el-input v-model="form.phone" placeholder="请输入电话" />
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item label="身份证号" prop="idCard">
        <el-input v-model="form.idCard" placeholder="请输入身份证号" />
      </el-form-item>

      <el-form-item label="地址">
        <el-input v-model="form.address" placeholder="请输入地址" />
      </el-form-item>

      <el-form-item label="过敏史">
        <el-input
          v-model="form.allergies"
          type="textarea"
          :rows="2"
          placeholder="请输入过敏史"
        />
      </el-form-item>

      <el-form-item label="既往病史">
        <el-input
          v-model="form.medicalHistory"
          type="textarea"
          :rows="2"
          placeholder="请输入既往病史"
        />
      </el-form-item>

      <el-form-item label="家族病史">
        <el-input
          v-model="form.familyHistory"
          type="textarea"
          :rows="2"
          placeholder="请输入家族病史"
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="loading">
        确定
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { patientApi } from '../api/patient'

const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  patientData: {
    type: Object,
    default: null
  }
})

const emit = defineEmits(['update:visible', 'success'])

const formRef = ref(null)
const loading = ref(false)
const isEdit = ref(false)

const form = ref({
  name: '',
  gender: '',
  dateOfBirth: '',
  phone: '',
  idCard: '',
  address: '',
  allergies: '',
  medicalHistory: '',
  familyHistory: ''
})

const rules = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  gender: [{ required: true, message: '请选择性别', trigger: 'change' }],
  dateOfBirth: [{ required: true, message: '请选择出生日期', trigger: 'change' }],
  phone: [
    { required: true, message: '请输入联系电话', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }
  ]
}

const resetForm = () => {
  form.value = {
    name: '',
    gender: '',
    dateOfBirth: '',
    phone: '',
    idCard: '',
    address: '',
    allergies: '',
    medicalHistory: '',
    familyHistory: ''
  }
  formRef.value?.clearValidate()
}

// 监听patientData变化
watch(() => props.patientData, (data) => {
  if (data) {
    isEdit.value = true
    form.value = {
      name: data.name,
      gender: data.gender,
      dateOfBirth: new Date(data.dateOfBirth),
      phone: data.phone,
      idCard: data.idCard || '',
      address: data.address || '',
      allergies: data.allergies || '',
      medicalHistory: data.medicalHistory || '',
      familyHistory: data.familyHistory || ''
    }
  } else {
    isEdit.value = false
    resetForm()
  }
}, { immediate: true })

const handleClose = () => {
  emit('update:visible', false)
  resetForm()
}

const handleSubmit = async () => {
  try {
    await formRef.value.validate()
    loading.value = true

    if (isEdit.value) {
      await patientApi.updatePatient(props.patientData.id, form.value)
      ElMessage.success('更新成功')
    } else {
      await patientApi.createPatient(form.value)
      ElMessage.success('建档成功')
    }

    emit('success')
    handleClose()
  } catch (error) {
    console.error('提交失败:', error)
  } finally {
    loading.value = false
  }
}
</script>
