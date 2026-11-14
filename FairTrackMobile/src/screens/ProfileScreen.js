import React, { useState, useContext } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Alert
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { AuthContext } from '../context/AuthContext';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { MobileApi } from '../api/mobileApi';
import { HeaderBar } from '../components/HeaderBar';
import { LogoutModal } from '../components/LogoutModal';
import { COLORS, SHADOWS } from '../constants/theme';

export const ProfileScreen = ({ navigation }) => {
  const { user, logout, serverUrl } = useContext(AuthContext);
  const { isDarkMode, colors, shadows, setThemeMode } = useTheme();
  const {

    pendingCount,
    downloadedCount,
    lastSyncTime,
    isSyncing,
    syncAllToServer,
    clearQueue
  } = useContext(SyncContext);

  const [logoutModalVisible, setLogoutModalVisible] = useState(false);
  const [testingConnection, setTestingConnection] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState(null); // { online: boolean, message: string }

  const handleTestConnection = async () => {
    try {
      setTestingConnection(true);
      setConnectionStatus(null);
      const res = await MobileApi.ping();
      setTestingConnection(false);

      if (res && res.status === 'OK') {
        setConnectionStatus({
          online: true,
          message: `Connected: ${res.database?.connected ? 'Database Online' : 'Server Online'}`
        });
      } else {
        setConnectionStatus({
          online: false,
          message: 'Server responded with unexpected status.'
        });
      }
    } catch (err) {
      setTestingConnection(false);
      setConnectionStatus({
        online: false,
        message: err.message || 'Cannot reach server. Field Offline Mode active.'
      });
    }
  };

  const handleSyncPress = async () => {
    if (pendingCount === 0) {
      Alert.alert('Queue Empty', 'All offline changes have already been synchronized.');
      return;
    }
    const res = await syncAllToServer();
    if (res.success) {
      Alert.alert('Office Sync Complete', `Successfully synced ${res.successCount} updates!`);
    } else {
      Alert.alert(
        'Sync Notice',
        `Synced: ${res.successCount}, Failed: ${res.failureCount}. Unsynced items remain in offline queue.`
      );
    }
  };

  const getInitials = (name) => {
    if (!name) return 'FA';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.bgLight }]}>
      <HeaderBar
        navigation={navigation}
        onRefresh={handleTestConnection}
        isRefreshing={testingConnection}
      />

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Screen Title */}
        <View style={styles.screenHeader}>
          <Text style={[styles.screenTitle, { color: colors.textDark }]}>Staff Profile</Text>
          <Text style={[styles.screenSubtitle, { color: colors.textMuted }]}>
            Account Settings & Device Diagnostics
          </Text>
        </View>
        {/* Profile Identity Card */}
        <View
          style={[
            styles.profileCard,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
        >
          <View
            style={[
              styles.avatarBubble,
              {
                backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight,
                borderColor: colors.primary
              }
            ]}
          >
            <Text style={[styles.avatarText, { color: colors.primary }]}>{getInitials(user?.name)}</Text>
          </View>
          <Text style={[styles.profileName, { color: colors.textDark }]}>{user?.name || 'Field Assessor'}</Text>
          <Text style={[styles.profileUsername, { color: colors.textMuted }]}>@{user?.username || 'assessor'}</Text>

          <View style={styles.badgeRow}>
            <View
              style={[
                styles.divisionBadge,
                { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
              ]}
            >
              <Ionicons name="business-outline" size={14} color={colors.primary} />
              <Text style={[styles.divisionBadgeText, { color: isDarkMode ? '#f8fafc' : colors.primaryDark }]}>
                {user?.divisionName || `Division ${user?.divisionId || 1}`}
              </Text>
            </View>

            {user?.isAdmin ? (
              <View
                style={[
                  styles.adminBadge,
                  isDarkMode && { backgroundColor: '#78350f' }
                ]}
              >
                <Ionicons name="shield-checkmark" size={13} color={isDarkMode ? '#fde68a' : '#b45309'} />
                <Text style={[styles.adminBadgeText, isDarkMode && { color: '#fef3c7' }]}>Assessor Admin</Text>
              </View>
            ) : (
              <View
                style={[
                  styles.roleBadge,
                  isDarkMode && { backgroundColor: '#1e3a8a' }
                ]}
              >
                <Ionicons name="person" size={13} color={isDarkMode ? '#93c5fd' : '#2563eb'} />
                <Text style={[styles.roleBadgeText, isDarkMode && { color: '#bfdbfe' }]}>Field Inspector</Text>
              </View>
            )}
          </View>
        </View>

        {/* Appearance & Theme Section */}
        <Text style={[styles.sectionHeading, { color: colors.textDark }]}>Appearance & Theme</Text>
        <View
          style={[
            styles.card,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
        >
          <View style={styles.infoRow}>
            <View
              style={[
                styles.infoIconBox,
                { backgroundColor: isDarkMode ? 'rgba(20, 184, 166, 0.18)' : '#fef3c7' }
              ]}
            >
              <Ionicons
                name={isDarkMode ? 'moon' : 'sunny'}
                size={18}
                color={isDarkMode ? '#14b8a6' : '#d97706'}
              />
            </View>
            <View style={styles.infoContent}>
              <Text style={[styles.infoTitle, { color: colors.textDark }]}>Theme Preference</Text>
              <Text style={[styles.infoValue, { color: colors.textMuted }]}>
                {isDarkMode ? 'Dark theme active' : 'Light theme active'}
              </Text>
            </View>
          </View>

          <View style={[styles.separator, { backgroundColor: colors.border }]} />

          {/* Segmented Pill Selector */}
          <View
            style={[
              styles.themePillBar,
              { backgroundColor: isDarkMode ? '#0b1120' : '#f1f5f9' }
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
                size={15}
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
                Light Theme
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
                Dark Theme
              </Text>
            </TouchableOpacity>
          </View>
        </View>

        {/* Offline Storage & Sync Summary */}
        <Text style={[styles.sectionHeading, { color: colors.textDark }]}>Offline Storage & Sync Status</Text>
        <View
          style={[
            styles.card,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
        >
          <View style={styles.infoRow}>
            <View
              style={[
                styles.infoIconBox,
                { backgroundColor: isDarkMode ? '#78350f' : '#fef3c7' }
              ]}
            >
              <Ionicons name="cloud-upload-outline" size={18} color={colors.accentGold} />
            </View>
            <View style={styles.infoContent}>
              <Text style={[styles.infoTitle, { color: colors.textDark }]}>Pending Offline Edits</Text>
              <Text style={[styles.infoValue, { color: colors.textMuted }]}>
                {pendingCount} record{pendingCount === 1 ? '' : 's'} waiting to sync
              </Text>
            </View>
            {pendingCount > 0 ? (
              <View style={styles.badgePending}>
                <Text style={styles.badgePendingText}>{pendingCount}</Text>
              </View>
            ) : (
              <Ionicons name="checkmark-circle" size={20} color={colors.accentGreen} />
            )}
          </View>

          <View style={[styles.separator, { backgroundColor: colors.border }]} />

          <View style={styles.infoRow}>
            <View
              style={[
                styles.infoIconBox,
                { backgroundColor: isDarkMode ? '#1e3a8a' : '#e0f2fe' }
              ]}
            >
              <Ionicons name="download-outline" size={18} color={colors.accentBlue} />
            </View>
            <View style={styles.infoContent}>
              <Text style={[styles.infoTitle, { color: colors.textDark }]}>Downloaded Offline Properties</Text>
              <Text style={[styles.infoValue, { color: colors.textMuted }]}>
                {downloadedCount} transaction{downloadedCount === 1 ? '' : 's'} stored on device
              </Text>
            </View>
          </View>

          <View style={[styles.separator, { backgroundColor: colors.border }]} />

          <View style={styles.infoRow}>
            <View
              style={[
                styles.infoIconBox,
                { backgroundColor: isDarkMode ? '#334155' : '#f1f5f9' }
              ]}
            >
              <Ionicons name="time-outline" size={18} color={colors.textMuted} />
            </View>
            <View style={styles.infoContent}>
              <Text style={[styles.infoTitle, { color: colors.textDark }]}>Last Office Sync</Text>
              <Text style={[styles.infoValue, { color: colors.textMuted }]}>{lastSyncTime || 'No recent sync'}</Text>
            </View>
          </View>

          {/* Sync Queue Actions */}
          <View style={styles.cardActionsRow}>
            <TouchableOpacity
              style={[styles.syncActionBtn, pendingCount === 0 && styles.syncActionBtnDisabled]}
              onPress={handleSyncPress}
              disabled={isSyncing || pendingCount === 0}
            >
              {isSyncing ? (
                <ActivityIndicator size="small" color="#ffffff" />
              ) : (
                <>
                  <Ionicons name="cloud-upload" size={16} color="#ffffff" />
                  <Text style={styles.syncActionBtnText}>Sync Offline Edits</Text>
                </>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.viewQueueBtn,
                isDarkMode && { backgroundColor: '#134e4a' }
              ]}
              onPress={() => navigation.navigate('SyncCenter')}
            >
              <Ionicons name="list-outline" size={16} color={colors.primary} />
              <Text style={[styles.viewQueueBtnText, { color: colors.primary }]}>Sync Center</Text>
            </TouchableOpacity>
          </View>
        </View>

        {/* Server & Connectivity Settings */}
        <Text style={[styles.sectionHeading, { color: colors.textDark }]}>Server & Network Connectivity</Text>
        <View
          style={[
            styles.card,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
        >
          <View style={styles.infoRow}>
            <View
              style={[
                styles.infoIconBox,
                { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
              ]}
            >
              <Ionicons name="server-outline" size={18} color={colors.primary} />
            </View>
            <View style={styles.infoContent}>
              <Text style={[styles.infoTitle, { color: colors.textDark }]}>Configured Server URL</Text>
              <Text style={[styles.infoValue, { color: colors.textMuted }]} numberOfLines={1}>{serverUrl || 'Default'}</Text>
            </View>
          </View>

          {connectionStatus ? (
            <View
              style={[
                styles.statusBanner,
                {
                  backgroundColor: connectionStatus.online
                    ? (isDarkMode ? '#064e3b' : '#ecfdf5')
                    : (isDarkMode ? '#450a0a' : '#fef2f2'),
                  borderColor: connectionStatus.online
                    ? (isDarkMode ? '#059669' : '#a7f3d0')
                    : (isDarkMode ? '#991b1b' : '#fecaca')
                }
              ]}
            >
              <Ionicons
                name={connectionStatus.online ? 'checkmark-circle' : 'alert-circle'}
                size={16}
                color={connectionStatus.online ? colors.accentGreen : COLORS.accentRed}
              />
              <Text
                style={[
                  styles.statusBannerText,
                  { color: connectionStatus.online ? (isDarkMode ? '#a7f3d0' : '#065f46') : (isDarkMode ? '#fca5a5' : '#991b1b') }
                ]}
              >
                {connectionStatus.message}
              </Text>
            </View>
          ) : null}

          <TouchableOpacity
            style={[
              styles.testBtn,
              isDarkMode && {
                backgroundColor: '#1e293b',
                borderColor: '#334155'
              }
            ]}
            onPress={handleTestConnection}
            disabled={testingConnection}
          >
            {testingConnection ? (
              <ActivityIndicator size="small" color={colors.primary} />
            ) : (
              <>
                <Ionicons name="pulse-outline" size={16} color={colors.primary} />
                <Text style={[styles.testBtnText, { color: colors.primary }]}>Test Server Connection</Text>
              </>
            )}
          </TouchableOpacity>
        </View>

        {/* Security & Sign Out Section */}
        <Text style={[styles.sectionHeading, { color: colors.textDark }]}>Session & Security</Text>
        <View
          style={[
            styles.card,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
        >
          <View style={styles.infoRow}>
            <View
              style={[
                styles.infoIconBox,
                { backgroundColor: isDarkMode ? '#334155' : '#f1f5f9' }
              ]}
            >
              <Ionicons name="key-outline" size={18} color={colors.textMuted} />
            </View>
            <View style={styles.infoContent}>
              <Text style={[styles.infoTitle, { color: colors.textDark }]}>Session Token</Text>
              <Text style={[styles.infoValue, { color: colors.textMuted }]}>JWT Authenticated • 7-Day Rolling Validity</Text>
            </View>
          </View>

          <View style={[styles.separator, { backgroundColor: colors.border }]} />

          {/* Primary Logout Button */}
          <TouchableOpacity
            style={styles.logoutButton}
            onPress={() => setLogoutModalVisible(true)}
          >
            <Ionicons name="log-out-outline" size={20} color="#ffffff" />
            <Text style={styles.logoutButtonText}>Sign Out of FairTrack</Text>
          </TouchableOpacity>
        </View>

        {/* Footer */}
        <View style={styles.footer}>
          <Text style={[styles.footerText, { color: colors.textMuted }]}>FairTrack Mobile Field Assessment Portal</Text>
          <Text style={[styles.footerSubtext, { color: colors.textMuted }]}>Version 1.0.0 • City Assessor's Office</Text>
        </View>
      </ScrollView>

      {/* Confirmation Modal */}
      <LogoutModal
        visible={logoutModalVisible}
        onClose={() => setLogoutModalVisible(false)}
        onConfirmLogout={async () => {
          setLogoutModalVisible(false);
          await logout();
        }}
        pendingCount={pendingCount}
        onSyncAll={syncAllToServer}
        user={user}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.bgLight
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 40
  },
  profileCard: {
    backgroundColor: '#ffffff',
    borderRadius: 20,
    padding: 24,
    alignItems: 'center',
    marginBottom: 20
  },
  avatarBubble: {
    width: 76,
    height: 76,
    borderRadius: 38,
    backgroundColor: COLORS.primaryLight,
    borderWidth: 3,
    borderColor: COLORS.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 12
  },
  avatarText: {
    fontSize: 26,
    fontWeight: '900',
    color: COLORS.primary
  },
  profileName: {
    fontSize: 20,
    fontWeight: '800',
    color: COLORS.textDark,
    textAlign: 'center'
  },
  profileUsername: {
    fontSize: 13,
    color: COLORS.textMuted,
    marginTop: 2,
    marginBottom: 12
  },
  badgeRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    justifyContent: 'center'
  },
  divisionBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    backgroundColor: COLORS.primaryLight,
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16
  },
  divisionBadgeText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.primaryDark
  },
  roleBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    backgroundColor: '#eff6ff',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 16
  },
  roleBadgeText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#1e40af'
  },
  adminBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    backgroundColor: '#fffbeb',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 16
  },
  adminBadgeText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#92400e'
  },
  sectionHeading: {
    fontSize: 14,
    fontWeight: '800',
    color: COLORS.textDark,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 10,
    marginLeft: 4
  },
  card: {
    backgroundColor: '#ffffff',
    borderRadius: 16,
    padding: 16,
    marginBottom: 20
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12
  },
  infoIconBox: {
    width: 38,
    height: 38,
    borderRadius: 19,
    backgroundColor: '#f1f5f9',
    alignItems: 'center',
    justifyContent: 'center'
  },
  infoContent: {
    flex: 1
  },
  infoTitle: {
    fontSize: 13,
    fontWeight: '700',
    color: COLORS.textDark
  },
  infoValue: {
    fontSize: 12,
    color: COLORS.textMuted,
    marginTop: 2
  },
  badgePending: {
    backgroundColor: COLORS.accentGold,
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 12
  },
  badgePendingText: {
    color: '#ffffff',
    fontSize: 11,
    fontWeight: '800'
  },
  separator: {
    height: 1,
    backgroundColor: COLORS.border,
    marginVertical: 14
  },
  cardActionsRow: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 14
  },
  syncActionBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    backgroundColor: COLORS.primary,
    paddingVertical: 11,
    borderRadius: 10
  },
  syncActionBtnDisabled: {
    backgroundColor: '#94a3b8',
    opacity: 0.6
  },
  syncActionBtnText: {
    color: '#ffffff',
    fontSize: 13,
    fontWeight: '700'
  },
  viewQueueBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 5,
    backgroundColor: COLORS.primaryLight,
    paddingHorizontal: 14,
    paddingVertical: 11,
    borderRadius: 10
  },
  viewQueueBtnText: {
    color: COLORS.primary,
    fontSize: 13,
    fontWeight: '700'
  },
  statusBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    padding: 10,
    borderRadius: 10,
    borderWidth: 1,
    marginTop: 12,
    marginBottom: 6
  },
  statusBannerText: {
    fontSize: 12,
    fontWeight: '600',
    flex: 1
  },
  testBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    borderWidth: 1,
    borderColor: COLORS.borderDark,
    borderRadius: 10,
    paddingVertical: 10,
    marginTop: 12,
    backgroundColor: '#ffffff'
  },
  testBtnText: {
    color: COLORS.primary,
    fontSize: 13,
    fontWeight: '700'
  },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: COLORS.accentRed,
    paddingVertical: 14,
    borderRadius: 12,
    marginTop: 6
  },
  logoutButtonText: {
    color: '#ffffff',
    fontSize: 15,
    fontWeight: '800'
  },
  footer: {
    alignItems: 'center',
    marginTop: 10,
    marginBottom: 20
  },
  footerText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.textMuted
  },
  footerSubtext: {
    fontSize: 11,
    color: '#94a3b8',
    marginTop: 3
  },
  themePillBar: {
    flexDirection: 'row',
    borderRadius: 12,
    padding: 4,
    gap: 6
  },
  themePill: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 10,
    borderRadius: 10
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
    backgroundColor: '#1e293b',
    shadowColor: '#000000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.3,
    shadowRadius: 2,
    elevation: 2,
    borderWidth: 1,
    borderColor: '#334155'
  },
  themePillLabel: {
    fontSize: 13
  },
  screenHeader: {
    marginBottom: 16
  },
  screenTitle: {
    fontSize: 20,
    fontWeight: '800',
    letterSpacing: -0.3
  },
  screenSubtitle: {
    fontSize: 12,
    marginTop: 2,
    fontWeight: '500'
  },
});

