import React, { createContext, useState, useEffect, useContext } from 'react';
import { useColorScheme } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { LIGHT_COLORS, DARK_COLORS, getShadows } from '../constants/theme';

export const ThemeContext = createContext();

const THEME_STORAGE_KEY = '@fairtrack_theme_mode';

export const ThemeProvider = ({ children }) => {
  const systemColorScheme = useColorScheme();
  const [themeMode, setThemeModeState] = useState('light');
  const [isLoaded, setIsLoaded] = useState(false);

  useEffect(() => {
    loadSavedTheme();
  }, []);

  const loadSavedTheme = async () => {
    try {
      const savedTheme = await AsyncStorage.getItem(THEME_STORAGE_KEY);
      if (savedTheme === 'dark' || savedTheme === 'light') {
        setThemeModeState(savedTheme);
      } else if (systemColorScheme === 'dark') {
        setThemeModeState('dark');
      }
    } catch (err) {
      console.warn('Failed to load theme preference:', err);
    } finally {
      setIsLoaded(true);
    }
  };

  const setThemeMode = async (mode) => {
    try {
      setThemeModeState(mode);
      await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
    } catch (err) {
      console.warn('Failed to persist theme preference:', err);
    }
  };

  const toggleTheme = () => {
    const nextMode = themeMode === 'dark' ? 'light' : 'dark';
    setThemeMode(nextMode);
  };

  const isDarkMode = themeMode === 'dark';
  const colors = isDarkMode ? DARK_COLORS : LIGHT_COLORS;
  const shadows = getShadows(isDarkMode);

  return (
    <ThemeContext.Provider
      value={{
        isDarkMode,
        themeMode,
        colors,
        shadows,
        toggleTheme,
        setThemeMode,
        isLoaded
      }}
    >
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    return {
      isDarkMode: false,
      themeMode: 'light',
      colors: LIGHT_COLORS,
      shadows: getShadows(false),
      toggleTheme: () => {},
      setThemeMode: () => {},
      isLoaded: true
    };
  }
  return context;
};
