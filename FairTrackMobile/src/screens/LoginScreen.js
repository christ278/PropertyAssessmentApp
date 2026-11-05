import React, { useState, useContext } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ActivityIndicator,
  Modal,
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Image
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { AuthContext } from '../context/AuthContext';
import { COLORS, SHADOWS } from '../constants/theme';

export const LoginScreen = () => {
  const insets = useSafeAreaInsets();
  const topInset = insets.top > 0 ? insets.top : (Platform.OS === 'ios' ? 47 : 20);
  const { login, serverUrl, updateServerUrl } = useContext(AuthContext);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  // Server Settings Modal
  const [settingsVisible, setSettingsVisible] = useState(false);
  const [tempUrl, setTempUrl] = useState(serverUrl);

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      Alert.alert('Required Fields', 'Please enter your username and password.');
      return;
    }

    setIsLoggingIn(true);
    const res = await login(username.trim(), password.trim());
    setIsLoggingIn(false);

    if (!res.success) {
      Alert.alert('Login Failed', res.message || 'Please check your credentials and server connection.');
    }
  };

  const handleSaveSettings = async () => {
    await updateServerUrl(tempUrl);
    setSettingsVisible(false);
    Alert.alert('Settings Saved', `Server URL updated to: ${tempUrl}`);
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Top Server Settings Button */}
        <TouchableOpacity
          style={[styles.settingsBtn, { top: topInset + 8 }]}
          onPress={() => {
            setTempUrl(serverUrl);
            setSettingsVisible(true);
          }}
        >
          <Ionicons name="server-outline" size={18} color={COLORS.textMuted} />
          <Text style={styles.settingsBtnText}>Server Config</Text>
        </TouchableOpacity>

        {/* Brand Section */}
        <View style={[styles.brandContainer, { marginTop: topInset + 24 }]}>
          <Image
            source={require('../../assets/logo.png')}
            style={styles.appLogo}
            resizeMode="cover"
          />
          <Text style={styles.brandTitle}>FAIRTRACK</Text>
          <Text style={styles.brandSubtitle}>City Assessor's Mobile Field Portal</Text>
          <View style={styles.tagBadge}>
            <Text style={styles.tagBadgeText}>Koronadal City &bull; JWT Secured</Text>
          </View>
        </View>

        {/* Login Card */}
        <View style={[styles.card, SHADOWS.card]}>
          <Text style={styles.cardTitle}>Staff Sign In</Text>
          <Text style={styles.cardSubtitle}>Enter your assessor credentials to start</Text>

          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Username</Text>
            <View style={styles.inputWrapper}>
              <Ionicons name="person-outline" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Enter username"
                placeholderTextColor="#94a3b8"
                value={username}
                onChangeText={setUsername}
                autoCapitalize="none"
                autoCorrect={false}
              />
            </View>
          </View>

          <View style={styles.inputGroup}>
            <Text style={styles.inputLabel}>Password</Text>
            <View style={styles.inputWrapper}>
              <Ionicons name="lock-closed-outline" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Enter password"
                placeholderTextColor="#94a3b8"
                value={password}
                onChangeText={setPassword}
                secureTextEntry
              />
            </View>
          </View>

          <TouchableOpacity
            style={[styles.loginBtn, isLoggingIn && styles.loginBtnDisabled]}
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

        <Text style={styles.footerText}>
          &copy; {new Date().getFullYear()} City Assessor's Office &bull; Offline-Ready
        </Text>
      </ScrollView>

      {/* Server URL Config Modal */}
      <Modal visible={settingsVisible} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.modalCard}>
            <Text style={styles.modalTitle}>Server Connection Settings</Text>
            <Text style={styles.modalSubtitle}>
              Configure the backend API server IP/domain for Android connection:
            </Text>

            <TextInput
              style={styles.serverInput}
              value={tempUrl}
              onChangeText={setTempUrl}
              placeholder="http://192.168.50.9:5156"
              autoCapitalize="none"
              autoCorrect={false}
            />

            <View style={styles.quickUrls}>
              <TouchableOpacity
                style={styles.quickTag}
                onPress={() => setTempUrl('http://192.168.50.9:5156')}
              >
                <Text style={styles.quickTagText}>LAN Wi-Fi (192.168.50.9:5156)</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.quickTag, { marginTop: 6 }]}
                onPress={() => setTempUrl('http://10.0.2.2:5156')}
              >
                <Text style={styles.quickTagText}>Android Emulator (10.0.2.2:5156)</Text>
              </TouchableOpacity>
            </View>

            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={styles.modalCancelBtn}
                onPress={() => setSettingsVisible(false)}
              >
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.modalSaveBtn}
                onPress={handleSaveSettings}
              >
                <Text style={styles.modalSaveText}>Save URL</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
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
