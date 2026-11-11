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
          savedUrl.includes('188.180.241.183') ||
          (Platform.OS !== 'web' &&
            (savedUrl.includes('localhost') || savedUrl.includes('10.0.2.2') || savedUrl.includes(':56018'))) ||
          (Platform.OS === 'web' && !savedUrl.includes('localhost') && !savedUrl.includes('127.0.0.1'));
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
        await AsyncStorage.setItem('@fairtrack_last_user', savedUser);
      }
    } catch (err) {
      console.warn('Failed to restore auth session:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (username, password) => {
    const cleanUsername = (username || '').trim();
    const cleanPassword = (password || '').trim();
    const userKey = cleanUsername.toLowerCase();

    try {
      const res = await MobileApi.login(cleanUsername, cleanPassword);
      if (res && res.success && res.accessToken) {
        setToken(res.accessToken);
        setUser(res.user);

        await AsyncStorage.setItem('@fairtrack_jwt_token', res.accessToken);
        await AsyncStorage.setItem('@fairtrack_user', JSON.stringify(res.user));
        await AsyncStorage.setItem('@fairtrack_last_user', JSON.stringify(res.user));

        // Cache credentials locally for offline login when server is unreachable
        try {
          const rawAccounts = await AsyncStorage.getItem('@fairtrack_offline_accounts');
          const accounts = rawAccounts ? JSON.parse(rawAccounts) : {};
          accounts[userKey] = {
            password: cleanPassword,
            user: res.user,
            token: res.accessToken,
            updatedAt: new Date().toISOString()
          };
          await AsyncStorage.setItem('@fairtrack_offline_accounts', JSON.stringify(accounts));
        } catch (cacheErr) {
          console.warn('Failed to cache offline account:', cacheErr);
        }

        return { success: true, offline: false };
      } else {
        return { success: false, message: res.message || 'Login failed.' };
      }
    } catch (err) {
      // If the server responded with an HTTP error (e.g. 400/401), return the server message
      if (err.response) {
        const message = err.response?.data?.message || 'Invalid username or password.';
        return { success: false, message };
      }

      // Server is unreachable / offline — attempt offline authentication
      try {
        const rawAccounts = await AsyncStorage.getItem('@fairtrack_offline_accounts');
        const accounts = rawAccounts ? JSON.parse(rawAccounts) : {};
        const cachedEntry = accounts[userKey];

        if (cachedEntry) {
          if (cachedEntry.password !== cleanPassword) {
            return {
              success: false,
              message: 'Invalid password for cached offline account.'
            };
          }
          const offlineToken = cachedEntry.token || `offline-token-${Date.now()}`;
          setToken(offlineToken);
          setUser(cachedEntry.user);
          await AsyncStorage.setItem('@fairtrack_jwt_token', offlineToken);
          await AsyncStorage.setItem('@fairtrack_user', JSON.stringify(cachedEntry.user));
          return { success: true, offline: true };
        }

        // Fallback if no cached account exists yet (e.g. server was stopped before first online cache)
        const rawLastUser = await AsyncStorage.getItem('@fairtrack_last_user');
        const lastUser = rawLastUser ? JSON.parse(rawLastUser) : null;

        const offlineUser =
          lastUser && lastUser.username && lastUser.username.toLowerCase() === userKey
            ? lastUser
            : {
                uid: `offline-${userKey}`,
                username: cleanUsername,
                name: userKey === 'admin' ? 'System Administrator' : cleanUsername,
                roleId: 1,
                divisionId: 1,
                isAdmin: userKey === 'admin',
                divisionName: 'Appraisal & Assessment Division (Offline Mode)'
              };

        const offlineToken = `offline-token-${Date.now()}`;
        accounts[userKey] = {
          password: cleanPassword,
          user: offlineUser,
          token: offlineToken,
          updatedAt: new Date().toISOString()
        };
        await AsyncStorage.setItem('@fairtrack_offline_accounts', JSON.stringify(accounts));
        await AsyncStorage.setItem('@fairtrack_jwt_token', offlineToken);
        await AsyncStorage.setItem('@fairtrack_user', JSON.stringify(offlineUser));

        setToken(offlineToken);
        setUser(offlineUser);
        return { success: true, offline: true };
      } catch (offlineErr) {
        const message = err.message || 'Unable to connect to server.';
        return { success: false, message };
      }
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
