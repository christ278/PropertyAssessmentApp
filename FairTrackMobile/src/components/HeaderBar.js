import React, { useState, useContext } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Image,
  Modal,
  Platform
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { COLORS } from '../constants/theme';
import { SyncContext } from '../context/SyncContext';
import { AuthContext } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { LogoutModal } from './LogoutModal';

export const HeaderBar = ({ title, subtitle, onSyncPress, navigation }) => {
  const insets = useSafeAreaInsets();
  const { pendingCount, isSyncing, syncAllToServer } = useContext(SyncContext);
  const { user, logout } = useContext(AuthContext);
  const { isDarkMode, colors, shadows, setThemeMode } = useTheme();

  // Dynamic Island / Status Bar Top Inset
  const topInset = insets.top > 0 ? insets.top : (Platform.OS === 'ios' ? 47 : 12);
  const containerPaddingTop = topInset + 6;

  const [accountMenuVisible, setAccountMenuVisible] = useState(false);
  const [logoutModalVisible, setLogoutModalVisible] = useState(false);

  const getInitials = (name) => {
    if (!name) return 'FA';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  };

  const handleTriggerLogout = () => {
    setAccountMenuVisible(false);
    setLogoutModalVisible(true);
  };

  const handleConfirmLogout = async () => {
    setLogoutModalVisible(false);
    await logout();
  };

  return (
    <View
      style={[
        styles.container,
        {
          backgroundColor: colors.cardBg,
          borderBottomColor: colors.border,
          paddingTop: containerPaddingTop
        }
      ]}
    >
      <View style={styles.leftSection}>
        <Image
          source={require('../../assets/logo.png')}
          style={styles.headerLogo}
          resizeMode="cover"
        />
        <View style={styles.titleContainer}>
          <Text
            style={[
              styles.title,
              { color: isDarkMode ? colors.textDark : colors.primaryDark }
            ]}
          >
            {title || 'FAIRTRACK'}
          </Text>
          {subtitle ? (
            <Text style={[styles.subtitle, { color: colors.textMuted }]}>{subtitle}</Text>
          ) : null}
        </View>
      </View>

      <View style={styles.actionsContainer}>
        {/* Sync Queue Button with Badge */}
        <TouchableOpacity
          style={[
            styles.syncButton,
            { backgroundColor: isDarkMode ? '#1e293b' : colors.primaryLight },
            pendingCount > 0 && styles.syncButtonActive
          ]}
          onPress={() => {
            if (onSyncPress) onSyncPress();
            else if (navigation) navigation.navigate('SyncCenter');
          }}
          disabled={isSyncing}
          accessibilityLabel="Office Sync"
        >
          <Ionicons
            name={isSyncing ? 'sync' : 'cloud-upload-outline'}
            size={20}
            color={pendingCount > 0 ? '#ffffff' : colors.primary}
          />
          {pendingCount > 0 ? (
            <View style={styles.badge}>
              <Text style={styles.badgeText}>{pendingCount}</Text>
            </View>
          ) : null}
        </TouchableOpacity>

        {/* User Account / Logout Avatar Button */}
        <TouchableOpacity
          style={[
            styles.avatarButton,
            { backgroundColor: isDarkMode ? colors.primary : colors.primaryDark }
          ]}
          onPress={() => setAccountMenuVisible(true)}
          accessibilityLabel="Account and Sign Out"
        >
          <Text style={styles.avatarButtonText}>{getInitials(user?.name)}</Text>
        </TouchableOpacity>
      </View>

      {/* Account Quick Menu Modal */}
      <Modal
        visible={accountMenuVisible}
        transparent
        animationType="fade"
        onRequestClose={() => setAccountMenuVisible(false)}
      >
        <TouchableOpacity
          style={[
            styles.menuOverlay,
            {
              backgroundColor: colors.modalOverlay,
              paddingTop: containerPaddingTop + 48
            }
          ]}
          activeOpacity={1}
          onPress={() => setAccountMenuVisible(false)}
        >
          <TouchableOpacity
            activeOpacity={1}
            style={[
              styles.menuCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.hover
            ]}
          >
            {/* Header User Details */}
            <View style={styles.menuHeader}>
              <View
                style={[
                  styles.menuAvatarBubble,
                  { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
                ]}
              >
                <Text style={[styles.menuAvatarText, { color: colors.primary }]}>
                  {getInitials(user?.name)}
                </Text>
              </View>
              <View style={styles.menuHeaderText}>
                <Text style={[styles.menuUserName, { color: colors.textDark }]}>
                  {user?.name || 'Field Assessor'}
                </Text>
                <Text style={[styles.menuUserHandle, { color: colors.textMuted }]}>
                  @{user?.username || 'assessor'}
                </Text>
                <Text style={[styles.menuUserDivision, { color: colors.primary }]}>
                  {user?.divisionName || `Division ${user?.divisionId || 1}`}
                </Text>
              </View>
            </View>

            <View style={[styles.menuDivider, { backgroundColor: colors.border }]} />

            {/* Appearance / Dark & Light Mode Toggle */}
            <View
              style={[
                styles.themeSection,
                {
                  backgroundColor: isDarkMode ? '#0b1120' : '#f8fafc',
                  borderColor: colors.border
                }
              ]}
            >
              <View style={styles.themeRow}>
                <View style={styles.themeLeftCol}>
                  <View
                    style={[
                      styles.themeIconBox,
                      {
                        backgroundColor: isDarkMode ? 'rgba(20, 184, 166, 0.18)' : '#fef3c7'
                      }
                    ]}
                  >
                    <Ionicons
                      name={isDarkMode ? 'moon' : 'sunny'}
                      size={18}
                      color={isDarkMode ? '#14b8a6' : '#d97706'}
                    />
                  </View>
                  <View style={styles.themeTextContainer}>
                    <Text style={[styles.themeTitle, { color: colors.textDark }]}>
                      Theme Mode
                    </Text>
                    <Text style={[styles.themeSubtitle, { color: colors.textMuted }]}>
                      {isDarkMode ? 'Dark theme active' : 'Light theme active'}
                    </Text>
                  </View>
                </View>
              </View>

              {/* Segmented Pill Selector (Light / Dark) */}
              <View
                style={[
                  styles.themePillBar,
                  {
                    backgroundColor: isDarkMode ? '#1e293b' : '#e2e8f0'
                  }
                ]}
              >
                <TouchableOpacity
                  style={[
                    styles.themePill,
                    !isDarkMode && styles.themePillActiveLight
                  ]}
                  onPress={() => setThemeMode('light')}
                  activeOpacity={0.7}
                >
                  <Ionicons
                    name="sunny"
                    size={14}
                    color={!isDarkMode ? '#d97706' : colors.textMuted}
                  />
                  <Text
                    style={[
                      styles.themePillLabel,
                      {
                        color: !isDarkMode ? '#0f172a' : colors.textMuted,
                        fontWeight: !isDarkMode ? '800' : '600'
                      }
                    ]}
                  >
                    Light
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[
                    styles.themePill,
                    isDarkMode && styles.themePillActiveDark
                  ]}
                  onPress={() => setThemeMode('dark')}
                  activeOpacity={0.7}
                >
                  <Ionicons
                    name="moon"
                    size={14}
                    color={isDarkMode ? '#14b8a6' : colors.textMuted}
                  />
                  <Text
                    style={[
                      styles.themePillLabel,
                      {
                        color: isDarkMode ? '#f8fafc' : colors.textMuted,
                        fontWeight: isDarkMode ? '800' : '600'
                      }
                    ]}
                  >
                    Dark
                  </Text>
                </TouchableOpacity>
              </View>
            </View>

            <View style={[styles.menuDivider, { backgroundColor: colors.border }]} />

            {/* Sign Out Button */}
            <TouchableOpacity
              style={[
                styles.menuLogoutBtn,
                isDarkMode && {
                  backgroundColor: '#450a0a',
                  borderColor: '#7f1d1d'
                }
              ]}
              onPress={handleTriggerLogout}
            >
              <Ionicons
                name="log-out-outline"
                size={18}
                color={isDarkMode ? '#fca5a5' : COLORS.accentRed}
              />
              <Text
                style={[
                  styles.menuLogoutBtnText,
                  isDarkMode && { color: '#fca5a5' }
                ]}
              >
                Sign Out
              </Text>
            </TouchableOpacity>
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Reusable Safe Logout Confirmation Modal */}
      <LogoutModal
        visible={logoutModalVisible}
        onClose={() => setLogoutModalVisible(false)}
        onConfirmLogout={handleConfirmLogout}
        pendingCount={pendingCount}
        onSyncAll={syncAllToServer}
        user={user}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingBottom: 14,
    borderBottomWidth: 1
  },
  leftSection: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
    gap: 10
  },
  headerLogo: {
    width: 32,
    height: 32,
    borderRadius: 8
  },
  titleContainer: {
    flex: 1
  },
  title: {
    fontSize: 18,
    fontWeight: '800',
    letterSpacing: -0.3
  },
  subtitle: {
    fontSize: 12,
    marginTop: 2
  },
  actionsContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  syncButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative'
  },
  syncButtonActive: {
    backgroundColor: COLORS.accentGold
  },
  badge: {
    position: 'absolute',
    top: -4,
    right: -4,
    backgroundColor: COLORS.accentRed,
    minWidth: 18,
    height: 18,
    borderRadius: 9,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 4,
    borderWidth: 1.5,
    borderColor: '#ffffff'
  },
  badgeText: {
    color: '#ffffff',
    fontSize: 10,
    fontWeight: '800'
  },
  avatarButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center'
  },
  avatarButtonText: {
    color: '#ffffff',
    fontSize: 13,
    fontWeight: '800'
  },
  menuOverlay: {
    flex: 1,
    justifyContent: 'flex-start',
    alignItems: 'flex-end',
    paddingTop: 65,
    paddingRight: 16
  },
  menuCard: {
    width: 300,
    borderRadius: 18,
    padding: 16,
    borderWidth: 1
  },
  menuHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12
  },
  menuAvatarBubble: {
    width: 44,
    height: 44,
    borderRadius: 22,
    alignItems: 'center',
    justifyContent: 'center'
  },
  menuAvatarText: {
    fontSize: 16,
    fontWeight: '900'
  },
  menuHeaderText: {
    flex: 1
  },
  menuUserName: {
    fontSize: 14,
    fontWeight: '800'
  },
  menuUserHandle: {
    fontSize: 11
  },
  menuUserDivision: {
    fontSize: 11,
    fontWeight: '600',
    marginTop: 2
  },
  menuDivider: {
    height: 1,
    marginVertical: 12
  },
  themeSection: {
    borderRadius: 14,
    padding: 12,
    borderWidth: 1,
    gap: 10
  },
  themeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  themeLeftCol: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    flex: 1
  },
  themeIconBox: {
    width: 34,
    height: 34,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center'
  },
  themeTextContainer: {
    flex: 1
  },
  themeTitle: {
    fontSize: 13,
    fontWeight: '700'
  },
  themeSubtitle: {
    fontSize: 11,
    marginTop: 1
  },
  themePillBar: {
    flexDirection: 'row',
    borderRadius: 10,
    padding: 3,
    gap: 4
  },
  themePill: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 7,
    borderRadius: 8
  },
  themePillActiveLight: {
    backgroundColor: '#ffffff',
    shadowColor: '#000000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2
  },
  themePillActiveDark: {
    backgroundColor: '#0f172a',
    shadowColor: '#000000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.3,
    shadowRadius: 2,
    elevation: 2,
    borderWidth: 1,
    borderColor: '#334155'
  },
  themePillLabel: {
    fontSize: 12
  },
  menuLogoutBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#fef2f2',
    borderWidth: 1,
    borderColor: '#fecaca',
    paddingVertical: 10,
    borderRadius: 10
  },
  menuLogoutBtnText: {
    color: COLORS.accentRed,
    fontSize: 13,
    fontWeight: '700'
  }
});

