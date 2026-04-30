import axios from 'axios';

export const apiClient = axios.create({
  baseURL: 'http://localhost:5000/gateway',
  // baseURL: '/gateway',
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('token');

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('user');
      window.location.href = '/auth/login';
    }

    return Promise.reject(error);
  }
);