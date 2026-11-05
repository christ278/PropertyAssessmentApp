export const LIGHT_COLORS = {
  primary: '#006766',
  primaryDark: '#004d4d',
  primaryLight: '#e6f4f1',
  accentGold: '#f59e0b',
  accentGreen: '#10b981',
  accentBlue: '#2563eb',
  accentRed: '#ef4444',
  bgLight: '#f8fafc',
  cardBg: '#ffffff',
  surface: '#ffffff',
  surfaceSecondary: '#f1f5f9',
  textDark: '#0f172a',
  textMuted: '#64748b',
  border: '#e2e8f0',
  borderDark: '#cbd5e1',
  cardBorder: '#e2e8f0',
  inputBg: '#ffffff',
  inputText: '#0f172a',
  inputBorder: '#cbd5e1',
  modalOverlay: 'rgba(15, 23, 42, 0.45)'
};

export const DARK_COLORS = {
  primary: '#14b8a6', // bright teal accent
  primaryDark: '#0d9488',
  primaryLight: '#134e4a', // dark teal container
  accentGold: '#fbbf24',
  accentGreen: '#34d399',
  accentBlue: '#60a5fa',
  accentRed: '#f87171',
  bgLight: '#0f172a', // slate-900 background
  cardBg: '#1e293b', // slate-800 card surface
  surface: '#1e293b',
  surfaceSecondary: '#0b1120',
  textDark: '#f8fafc', // crisp white/light text
  textMuted: '#94a3b8', // slate-400 muted text
  border: '#334155', // slate-700
  borderDark: '#475569',
  cardBorder: '#334155',
  inputBg: '#0b1120',
  inputText: '#f8fafc',
  inputBorder: '#334155',
  modalOverlay: 'rgba(0, 0, 0, 0.75)'
};

// Default backwards compatibility
export const COLORS = LIGHT_COLORS;

export const FONTS = {
  regular: 'System',
  medium: 'System',
  bold: 'System'
};

export const SHADOWS = {
  card: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 8,
    elevation: 2
  },
  hover: {
    shadowColor: '#006766',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.15,
    shadowRadius: 12,
    elevation: 4
  }
};

export const getShadows = (isDarkMode) => ({
  card: {
    shadowColor: isDarkMode ? '#000000' : '#000000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: isDarkMode ? 0.35 : 0.05,
    shadowRadius: 8,
    elevation: isDarkMode ? 3 : 2
  },
  hover: {
    shadowColor: isDarkMode ? '#14b8a6' : '#006766',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: isDarkMode ? 0.35 : 0.15,
    shadowRadius: 12,
    elevation: isDarkMode ? 6 : 4
  }
});

