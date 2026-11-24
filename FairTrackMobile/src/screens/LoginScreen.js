import React, { useState, useContext } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Modal,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Image
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { AuthContext } from '../context/AuthContext';
import { COLORS, SHADOWS } from '../constants/theme';
import { useTheme } from '../context/ThemeContext';
import { ActionModal } from '../components/ActionModal';
import { API_CONFIG, DEFAULT_BASE_URL } from '../constants/config';

export const LoginScreen = () => {
  const { isDarkMode, colors, shadows } = useTheme();
  const insets = useSafeAreaInsets();
  const topInset = insets.top > 0 ? insets.top : (Platform.OS === 'ios' ? 47 : 20);
  const { login, serverUrl, updateServerUrl } = useContext(AuthContext);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Server Settings Modal
  const [settingsVisible, setSettingsVisible] = useState(false);
  const [tempUrl, setTempUrl] = useState(serverUrl);
  const [modalConfig, setModalConfig] = useState({ visible: false });

  const showInfoModal = (title, message, icon = 'information-circle', iconType = 'primary') => {
    setModalConfig({
      visible: true,
      title,
      message,
      icon,
      iconType,
      buttons: [
        {
          text: 'OK',
          style: 'primary',
          onPress: () => setModalConfig({ visible: false })
        }
      ],
      onClose: () => setModalConfig({ visible: false }),
      onConfirm: () => setModalConfig({ visible: false }),
      onCancel: () => setModalConfig({ visible: false })
    });
  };

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      showInfoModal('Required Fields', 'Please enter your username and password.', 'alert-circle', 'warning');
      return;
    }

    setIsLoggingIn(true);
    const res = await login(username.trim(), password.trim());
    setIsLoggingIn(false);

    if (!res.success) {
      showInfoModal(
        'Login Failed',
        res.message || 'Please check your credentials and server connection.',
        'close-circle',
        'danger'
      );
    }
  };

  const handleSaveSettings = async () => {
    await updateServerUrl(tempUrl);
    setSettingsVisible(false);
    showInfoModal('Settings Saved', `Server URL updated to: ${tempUrl}`, 'checkmark-circle', 'success');
  };

  return (
    <KeyboardAvoidingView
      style={[styles.container, { backgroundColor: colors.bgLight }]}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Top Server Settings Button */}
        <TouchableOpacity
          style={[
            styles.settingsBtn,
            { top: topInset + 8, backgroundColor: colors.cardBg, borderColor: colors.border }
          ]}
          onPress={() => {
            setTempUrl(serverUrl);
            setSettingsVisible(true);
          }}
        >
          <Ionicons name="server-outline" size={18} color={colors.textMuted} />
          <Text style={[styles.settingsBtnText, { color: colors.textMuted }]}>Server Config</Text>
        </TouchableOpacity>

        {/* Brand Section */}
        <View style={[styles.brandContainer, { marginTop: topInset + 16 }]}>
          <View style={[styles.logoCard, shadows.sm, { backgroundColor: isDarkMode ? '#1e293b' : '#ffffff' }]}>
            <Image
              source={require('../../assets/logo.png')}
              style={styles.appLogo}
              resizeMode="contain"
            />
          </View>
          <View style={[styles.cityBadge, { backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5', borderColor: isDarkMode ? '#059669' : '#a7f3d0' }]}>
            <Text style={[styles.cityBadgeText, { color: isDarkMode ? '#34d399' : '#047857' }]}>
              CITY OF KORONADAL
            </Text>
          </View>
          <Text style={[styles.brandTitle, { color: isDarkMode ? '#f8fafc' : colors.primaryDark }]}>FAIRTRACK</Text>
          <Text style={[styles.brandSubtitle, { color: isDarkMode ? '#34d399' : '#047857' }]}>
            Office of the City Assessor
          </Text>
          <Text style={[styles.brandDescription, { color: colors.textMuted }]}>
            Field Appraisal, Information and Records Tracking System
          </Text>
        </View>

        {/* Login Card */}
        <View style={[styles.card, shadows.card, { backgroundColor: colors.cardBg, borderColor: colors.border }]}>
          <Text style={[styles.cardTitle, { color: colors.textDark }]}>Staff Sign In</Text>
          <Text style={[styles.cardSubtitle, { color: colors.textMuted }]}>
            Enter your assessor credentials to start (works online &amp; offline)
          </Text>

          <View style={styles.inputGroup}>
            <Text style={[styles.inputLabel, { color: colors.textDark }]}>Username</Text>
            <View style={[styles.inputWrapper, { backgroundColor: colors.inputBg, borderColor: colors.border }]}>
              <Ionicons name="person-outline" size={20} color={colors.textMuted} style={styles.inputIcon} />
              <TextInput
                style={[styles.input, { color: colors.textDark }]}
                placeholder="Enter username"
                placeholderTextColor={colors.placeholder}
                value={username}
                onChangeText={setUsername}
                autoCapitalize="none"
                autoCorrect={false}
              />
            </View>
          </View>

          <View style={styles.inputGroup}>
            <Text style={[styles.inputLabel, { color: colors.textDark }]}>Password</Text>
            <View style={[styles.inputWrapper, { backgroundColor: colors.inputBg, borderColor: colors.border }]}>
              <Ionicons name="lock-closed-outline" size={20} color={colors.textMuted} style={styles.inputIcon} />
              <TextInput
                style={[styles.input, { color: colors.textDark }]}
                placeholder="Enter password"
                placeholderTextColor={colors.placeholder}
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!showPassword}
              />
              <TouchableOpacity
                onPress={() => setShowPassword((prev) => !prev)}
                hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
                style={styles.eyeBtn}
                accessibilityRole="button"
                accessibilityLabel={showPassword ? 'Hide password' : 'Show password'}
              >
                <Ionicons
                  name={showPassword ? 'eye-off-outline' : 'eye-outline'}
                  size={20}
                  color={colors.textMuted}
                />
              </TouchableOpacity>
            </View>
          </View>

          <TouchableOpacity
            style={[styles.loginBtn, { backgroundColor: colors.primary }, isLoggingIn && styles.loginBtnDisabled]}
            onPress={handleLogin}
            disabled={isLoggingIn}
          >
            {isLoggingIn ? (
              <ActivityIndicator color="#ffffff" size="small" />
            ) : (
              <>
                <Text style={styles.loginBtnText}>Sign In</Text>
                <Ionicons name="arrow-forward" size={18} color="#ffffff" />
              </>
            )}
          </TouchableOpacity>
        </View>

        <Text style={[styles.footerText, { color: colors.textMuted }]}>
          &copy; {new Date().getFullYear()} City Assessor's Office &bull; Offline-Ready
        </Text>
      </ScrollView>

      {/* Server URL Config Modal */}
      <Modal visible={settingsVisible} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={[styles.modalCard, { backgroundColor: colors.cardBg }]}>
            <Text style={[styles.modalTitle, { color: colors.textDark }]}>Server Connection Settings</Text>
            <Text style={[styles.modalSubtitle, { color: colors.textMuted }]}>
              Configure the backend API server IP/domain for Android connection:
            </Text>

            <TextInput
              style={[
                styles.serverInput,
                { borderColor: colors.primary, color: colors.textDark, backgroundColor: colors.inputBg }
              ]}
              value={tempUrl}
              onChangeText={setTempUrl}
              placeholder={DEFAULT_BASE_URL}
              placeholderTextColor={colors.placeholder}
              autoCapitalize="none"
              autoCorrect={false}
            />

            <View style={styles.quickUrls}>
              {API_CONFIG.PRESET_URLS.map((preset, index) => (
                <TouchableOpacity
                  key={preset.id}
                  style={[
                    styles.quickTag,
                    { backgroundColor: colors.primaryLight },
                    index > 0 && { marginTop: 6 }
                  ]}
                  onPress={() => setTempUrl(preset.url)}
                >
                  <Text style={[styles.quickTagText, { color: colors.primary }]}>
                    {preset.label}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>

            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={styles.modalCancelBtn}
                onPress={() => setSettingsVisible(false)}
              >
                <Text style={[styles.modalCancelText, { color: colors.textMuted }]}>Cancel</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.modalSaveBtn, { backgroundColor: colors.primary }]}
                onPress={handleSaveSettings}
              >
                <Text style={styles.modalSaveText}>Save URL</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>

      <ActionModal
        {...modalConfig}
        onClose={() => setModalConfig(prev => ({ ...prev, visible: false }))}
      />
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.bgLight
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 24
  },
  settingsBtn: {
    position: 'absolute',
    top: 48,
    right: 24,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#ffffff',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  settingsBtnText: {
    fontSize: 12,
    fontWeight: '600',
    color: COLORS.textMuted
  },
  brandContainer: {
    alignItems: 'center',
    marginBottom: 24,
    marginTop: 20
  },
  logoCard: {
    width: 96,
    height: 96,
    borderRadius: 24,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 12,
    borderWidth: 1,
    borderColor: 'rgba(5, 150, 105, 0.15)'
  },
  appLogo: {
    width: 80,
    height: 80
  },
  cityBadge: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    borderRadius: 12,
    borderWidth: 1,
    marginBottom: 6
  },
  cityBadgeText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 1
  },
  brandTitle: {
    fontSize: 28,
    fontWeight: '900',
    letterSpacing: 1.5
  },
  brandSubtitle: {
    fontSize: 14,
    fontWeight: '700',
    marginTop: 2
  },
  brandDescription: {
    fontSize: 11,
    textAlign: 'center',
    marginTop: 4,
    maxWidth: 280,
    lineHeight: 16
  },
  card: {
    backgroundColor: '#ffffff',
    borderRadius: 20,
    padding: 24,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: COLORS.textDark
  },
  cardSubtitle: {
    fontSize: 12,
    color: COLORS.textMuted,
    marginBottom: 20
  },
  inputGroup: {
    marginBottom: 16
  },
  inputLabel: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.textDark,
    marginBottom: 6,
    textTransform: 'uppercase',
    letterSpacing: 0.5
  },
  inputWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1.5,
    borderColor: COLORS.border,
    borderRadius: 12,
    backgroundColor: '#f8fafc',
    paddingHorizontal: 12
  },
  inputIcon: {
    marginRight: 8
  },
  input: {
    flex: 1,
    paddingVertical: 12,
    fontSize: 15,
    color: COLORS.textDark,
    fontWeight: '600'
  },
  eyeBtn: {
    padding: 6,
    marginLeft: 4,
    alignItems: 'center',
    justifyContent: 'center'
  },
  loginBtn: {
    backgroundColor: COLORS.primary,
    borderRadius: 12,
    paddingVertical: 14,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    marginTop: 10,
    shadowColor: COLORS.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.25,
    shadowRadius: 8,
    elevation: 3
  },
  loginBtnDisabled: {
    opacity: 0.7
  },
  loginBtnText: {
    color: '#ffffff',
    fontSize: 15,
    fontWeight: '700'
  },
  footerText: {
    textAlign: 'center',
    fontSize: 11,
    color: COLORS.textMuted,
    marginTop: 24
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    padding: 20
  },
  modalCard: {
    backgroundColor: '#ffffff',
    borderRadius: 20,
    padding: 22
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '800',
    color: COLORS.textDark,
    marginBottom: 4
  },
  modalSubtitle: {
    fontSize: 12,
    color: COLORS.textMuted,
    marginBottom: 14
  },
  serverInput: {
    borderWidth: 1.5,
    borderColor: COLORS.primary,
    borderRadius: 10,
    padding: 12,
    fontSize: 14,
    fontWeight: '600',
    color: COLORS.textDark,
    backgroundColor: '#f8fafc'
  },
  quickUrls: {
    marginTop: 10,
    marginBottom: 18
  },
  quickTag: {
    backgroundColor: COLORS.primaryLight,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    alignSelf: 'flex-start'
  },
  quickTagText: {
    fontSize: 11,
    fontWeight: '700',
    color: COLORS.primary
  },
  modalButtons: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: 10
  },
  modalCancelBtn: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 8
  },
  modalCancelText: {
    fontWeight: '700',
    color: COLORS.textMuted
  },
  modalSaveBtn: {
    backgroundColor: COLORS.primary,
    paddingHorizontal: 18,
    paddingVertical: 10,
    borderRadius: 8
  },
  modalSaveText: {
    fontWeight: '700',
    color: '#ffffff'
  }
});
