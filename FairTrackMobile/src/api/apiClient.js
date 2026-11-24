import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform, NativeModules } from 'react-native';
import { API_CONFIG, DEFAULT_BASE_URL } from '../constants/config';

export { DEFAULT_BASE_URL };

export const resolveServerUrl = async (path) => {
  if (!path) return '';
  if (path.startsWith('data:') || path.startsWith('file:') || path.startsWith('blob:') || path.startsWith('content:')) {
    return path;
  }
  let base = DEFAULT_BASE_URL.replace(/\/+$/, '');
  try {
    const saved = await AsyncStorage.getItem('@fairtrack_server_url');
    if (saved && saved.trim()) {
      const cleanSaved = saved.trim().replace(/\/+$/, '');
      const isInvalid = Platform.OS !== 'web' && (cleanSaved.includes('localhost') || cleanSaved.includes('10.0.2.2'));
      if (!isInvalid) {
        base = cleanSaved;
      }
    }
  } catch (e) {}

  if (path.startsWith('http://') || path.startsWith('https://')) {
    // If the path contains localhost or 10.0.2.2 on physical device, re-base it onto active base
    if (Platform.OS !== 'web' && (path.includes('localhost') || path.includes('10.0.2.2'))) {
      try {
        const urlObj = new URL(path);
        return `${base}${urlObj.pathname}${urlObj.search}`;
      } catch (_) {}
    }
    return path;
  }

  const cleanPath = path.startsWith('/') ? path : '/' + path;
  return `${base}${cleanPath}`;
};

const apiClient = axios.create({
  baseURL: DEFAULT_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// Dynamic Base URL loader
apiClient.interceptors.request.use(async (config) => {
  try {
    const savedUrl = await AsyncStorage.getItem('@fairtrack_server_url');
    if (savedUrl) {
      const cleanBase = savedUrl.trim().replace(/\/+$/, '');
      // Avoid using localhost, 56018, or old stale LAN IPs on physical mobile devices if previously cached
      const isInvalidMobileUrl =
        Platform.OS !== 'web' &&
        (cleanBase.includes('localhost') || cleanBase.includes('10.0.2.2') || cleanBase.includes(':56018') || cleanBase.includes('192.168.50.9'));

      config.baseURL = (isInvalidMobileUrl ? DEFAULT_BASE_URL : cleanBase) + '/';
      if (config.url && config.url.startsWith('/')) {
        config.url = config.url.replace(/^\/+/, '');
      }
    } else {
      config.baseURL = DEFAULT_BASE_URL.replace(/\/+$/, '') + '/';
      if (config.url && config.url.startsWith('/')) {
        config.url = config.url.replace(/^\/+/, '');
      }
    }

    const token = await AsyncStorage.getItem('@fairtrack_jwt_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  } catch (err) {
    console.warn('Error reading from AsyncStorage:', err);
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

let onUnauthorizedCallback = null;

export const setOnUnauthorizedCallback = (callback) => {
  onUnauthorizedCallback = callback;
};

// Response interceptor for session expiration (401) with automatic offline-to-online token upgrade
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    // Never intercept login or logout requests
    if (originalRequest && originalRequest.url) {
      if (originalRequest.url.includes('logout')) {
        return Promise.resolve({ data: { success: true } });
      }
      if (originalRequest.url.includes('login')) {
        return Promise.reject(error);
      }
    }
    if (error.response && error.response.status === 401 && originalRequest && !originalRequest._retried) {
      originalRequest._retried = true;
      try {
        const rawUser = await AsyncStorage.getItem('@fairtrack_user');
        const rawAccounts = await AsyncStorage.getItem('@fairtrack_offline_accounts');
        const userObj = rawUser ? JSON.parse(rawUser) : null;
        const accounts = rawAccounts ? JSON.parse(rawAccounts) : {};
        const userKey = (userObj?.username || '').toLowerCase();
        const cachedAccount = userKey ? accounts[userKey] : null;

        if (cachedAccount && cachedAccount.password) {
          const base = (originalRequest.baseURL || DEFAULT_BASE_URL).replace(/\/+$/, '');
          const targetUrl = `${base}/api/auth/login`;
          const loginRes = await axios.post(
            targetUrl,
            { username: userObj?.username || cachedAccount.user?.username || userKey, password: cachedAccount.password }
          );
          if (loginRes.data && loginRes.data.success && loginRes.data.accessToken) {
            const newToken = loginRes.data.accessToken;
            await AsyncStorage.setItem('@fairtrack_jwt_token', newToken);
            if (loginRes.data.user) {
              await AsyncStorage.setItem('@fairtrack_user', JSON.stringify(loginRes.data.user));
              await AsyncStorage.setItem('@fairtrack_last_user', JSON.stringify(loginRes.data.user));
            }
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return apiClient(originalRequest);
          }
        }
      } catch (reAuthErr) {
        // Ignore silent upgrade error and fall through
      }

      if (typeof onUnauthorizedCallback === 'function') {
        onUnauthorizedCallback();
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
