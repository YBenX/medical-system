import axios from './axios'

export const patientApi = {
  // 获取所有患者
  getPatients() {
    return axios.get('/patients')
  },

  // 获取患者详情
  getPatient(id) {
    return axios.get(`/patients/${id}`)
  },

  // 创建患者
  createPatient(data) {
    return axios.post('/patients', data)
  },

  // 更新患者
  updatePatient(id, data) {
    return axios.put(`/patients/${id}`, data)
  },

  // 搜索患者
  searchPatients(keyword) {
    return axios.get('/patients/search', { params: { keyword } })
  }
}
