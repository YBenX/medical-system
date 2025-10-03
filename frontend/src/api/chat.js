import axios from './axios'

export const chatApi = {
  // 发送消息
  sendMessage(data) {
    return axios.post('/chat/send', data)
  },

  // 获取会话历史
  getHistory(sessionId) {
    return axios.get(`/chat/history/${sessionId}`)
  },

  // 清除会话历史
  clearHistory(sessionId) {
    return axios.delete(`/chat/history/${sessionId}`)
  }
}
