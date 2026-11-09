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

export const LoginScreen = () => {
  const { colors, shadows } = useTheme();
  const insets = useSafeAreaInsets();
  const topInset = insets.top > 0 ? insets.top : (Platform.OS === 'ios' ? 47 : 20);
  const { login, serverUrl, updateServerUrl } = useContext(AuthContext);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Server Settings Modal
  const [settingsVisible, setSettingsVisible] = useState(false);
  const [tempUrl, setTempUrl] = useState(serverUrl);
  const [modalConfig, setModalConfig] = useState({ visible: false });

  const showInfoModal = (title, message, icon = 'information-circle', iconColor = colors.primary) => {
    setModalConfig({
      visible: true,
      title,
      message,
      icon,
      iconColor,
      singleButton: true,
      confirmText: 'OK',
      confirmColor: colors.primary,
      onConfirm: () => setModalConfig({ visible: false }),
      onCancel: () => setModalConfig({ visible: false })
    });
  };

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      showInfoModal('Required Fields', 'Please enter your username and password.', 'alert-circle', colors.warning);
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
        colors.danger
      );
    }
  };

  const handleSaveSettings = async () => {
    await updateServerUrl(tempUrl);
    setSettingsVisible(false);
    showInfoModal('Settings Saved', `Server URL updated to: ${tempUrl}`, 'checkmark-circle', colors.success);
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
        <View style={[styles.brandContainer, { marginTop: topInset + 24 }]}>
          <Image
            source={require('../../assets/logo.png')}
            style={styles.appLogo}
            resizeMode="cover"
          />
          <Text style={[styles.brandTitle, { color: colors.primaryDark }]}>FAIRTRACK</Text>
          <Text style={[styles.brandSubtitle, { color: colors.textMuted }]}>
            City Assessor's Mobile Field Portal
          </Text>
          <View style={[styles.tagBadge, { backgroundColor: colors.primaryLight }]}>
            <Text style={[styles.tagBadgeText, { color: colors.primary }]}>
              Koronadal City &bull; Offline &amp; JWT Secured
            </Text>
          </View>
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
                secureTextEntry
              />
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
              placeholder="http://192.168.50.9:5156"
              placeholderTextColor={colors.placeholder}
              autoCapitalize="none"
              autoCorrect={false}
            />

            <View style={styles.quickUrls}>
              <TouchableOpacity
                style={[styles.quickTag, { backgroundColor: colors.primaryLight }]}
                onPress={() => setTempUrl('http://192.168.50.9:5156')}
              >
                <Text style={[styles.quickTagText, { color: colors.primary }]}>
                  LAN Wi-Fi (192.168.50.9:5156)
                </Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.quickTag, { marginTop: 6, backgroundColor: colors.primaryLight }]}
                onPress={() => setTempUrl('http://10.0.2.2:5156')}
              >
                <Text style={[styles.quickTagText, { color: colors.primary }]}>
                  Android Emulator (10.0.2.2:5156)
                </Text>
              </TouchableOpacity>
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

      <ActionModal {...modalConfig} />
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
    marginBottom: 28,
    marginTop: 40
  },
  appLogo: {
    width: 90,
    height: 90,
    borderRadius: 22,
    marginBottom: 14,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.25,
    shadowRadius: 10,
    elevation: 6
  },
  brandTitle: {
    fontSize: 28,
    fontWeight: '900',
    color: COLORS.primaryDark,
    letterSpacing: -0.5
  },
  brandSubtitle: {
    fontSize: 13,
    color: COLORS.textMuted,
    marginTop: 2
  },
  tagBadge: {
    backgroundColor: COLORS.primaryLight,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    marginTop: 8
  },
  tagBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: COLORS.primary
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
