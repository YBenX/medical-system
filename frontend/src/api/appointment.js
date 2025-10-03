import axios from './axios'

export const appointmentApi = {
  // 创建预约
  createAppointment(data) {
    return axios.post('/appointments', data)
  },

  // 获取预约详情
  getAppointment(id) {
    return axios.get(`/appointments/${id}`)
  },

  // 取消预约
  cancelAppointment(id) {
    return axios.put(`/appointments/${id}/cancel`)
  },

  // 获取患者的预约列表
  getPatientAppointments(patientId) {
    return axios.get(`/appointments/patient/${patientId}`)
  }
}
