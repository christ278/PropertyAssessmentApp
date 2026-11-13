import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform, NativeModules } from 'react-native';

// Automatically detect the Metro bundler host IP on physical devices when running in Expo Go
const getLanBaseUrl = () => {
  try {
    const scriptURL = NativeModules?.SourceCode?.scriptURL;
    if (scriptURL) {
      const match = scriptURL.match(/^https?:\/\/([^:/]+):/);
      if (match && match[1] && match[1] !== 'localhost' && match[1] !== '127.0.0.1') {
        return `http://${match[1]}:5156`;
      }
    }
  } catch (e) {
    // Fallback below
  }
  return 'http://192.168.50.9:5156';
};

// Default to localhost for web, or the PC's LAN IP for mobile devices
export const DEFAULT_BASE_URL = Platform.OS === 'web'
  ? 'http://localhost:5156'
  : getLanBaseUrl();

export const resolveServerUrl = async (path) => {
  if (!path) return '';
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('data:') || path.startsWith('file:') || path.startsWith('blob:') || path.startsWith('content:')) {
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
      // Avoid using localhost or 56018 on physical mobile devices if previously cached
      const isInvalidMobileUrl =
        Platform.OS !== 'web' &&
        (cleanBase.includes('localhost') || cleanBase.includes('10.0.2.2') || cleanBase.includes(':56018'));

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
