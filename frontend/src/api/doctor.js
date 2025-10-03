import axios from './axios'

export const doctorApi = {
  // 获取医生列表
  getDoctors(department) {
    return axios.get('/doctors', { params: { department } })
  },

  // 获取排班信息
  getSchedules(doctorId, date) {
    return axios.get('/doctors/schedules', {
      params: { doctorId, date }
    })
  }
}
