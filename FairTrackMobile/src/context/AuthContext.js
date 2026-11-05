import React, { createContext, useState, useEffect } from 'react';
import { Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MobileApi } from '../api/mobileApi';
import { DEFAULT_BASE_URL, setOnUnauthorizedCallback } from '../api/apiClient';

export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [serverUrl, setServerUrl] = useState(DEFAULT_BASE_URL);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadStoredAuth();
    setOnUnauthorizedCallback(() => {
      logout();
    });
  }, []);

  const loadStoredAuth = async () => {
    try {
      const savedUrl = await AsyncStorage.getItem('@fairtrack_server_url');
      if (savedUrl) {
        const isInvalidMobileUrl =
          Platform.OS !== 'web' &&
          (savedUrl.includes('localhost') || savedUrl.includes('10.0.2.2') || savedUrl.includes(':56018'));
        if (isInvalidMobileUrl) {
          setServerUrl(DEFAULT_BASE_URL);
          await AsyncStorage.setItem('@fairtrack_server_url', DEFAULT_BASE_URL);
        } else {
          setServerUrl(savedUrl);
        }
      } else {
        setServerUrl(DEFAULT_BASE_URL);
      }

      const savedToken = await AsyncStorage.getItem('@fairtrack_jwt_token');
      const savedUser = await AsyncStorage.getItem('@fairtrack_user');

      if (savedToken && savedUser) {
        setToken(savedToken);
        setUser(JSON.parse(savedUser));
      }
    } catch (err) {
      console.warn('Failed to restore auth session:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (username, password) => {
    try {
      const res = await MobileApi.login(username, password);
      if (res && res.success && res.accessToken) {
        setToken(res.accessToken);
        setUser(res.user);

        await AsyncStorage.setItem('@fairtrack_jwt_token', res.accessToken);
        await AsyncStorage.setItem('@fairtrack_user', JSON.stringify(res.user));
        return { success: true };
      } else {
        return { success: false, message: res.message || 'Login failed.' };
      }
    } catch (err) {
      const message = err.response?.data?.message || err.message || 'Unable to connect to server.';
      return { success: false, message };
    }
  };

  const logout = async () => {
    try {
      await MobileApi.logout();
    } catch (err) {
      // Ignore network errors when logging out offline
    }
    setUser(null);
    setToken(null);
    await AsyncStorage.removeItem('@fairtrack_jwt_token');
    await AsyncStorage.removeItem('@fairtrack_user');
  };

  const updateServerUrl = async (newUrl) => {
    const cleanUrl = (newUrl || '').trim();
    if (cleanUrl) {
      setServerUrl(cleanUrl);
      await AsyncStorage.setItem('@fairtrack_server_url', cleanUrl);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        serverUrl,
        isAuthenticated: !!token,
        isLoading,
        login,
        logout,
        updateServerUrl
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
