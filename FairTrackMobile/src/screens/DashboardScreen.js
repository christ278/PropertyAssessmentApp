import React, { useState, useEffect, useContext, useMemo } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { AuthContext } from '../context/AuthContext';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { MobileApi } from '../api/mobileApi';
import { HeaderBar } from '../components/HeaderBar';
import { LogoutModal } from '../components/LogoutModal';
import { COLORS, SHADOWS } from '../constants/theme';

export const DashboardScreen = ({ navigation }) => {
  const { user, logout } = useContext(AuthContext);
  const {
    pendingCount,
    isSyncing,
    syncAllToServer,
    lastSyncTime,
    downloadedCount,
    downloadedForms,
    cachedTransactions
  } = useContext(SyncContext);
  const { colors, isDarkMode, shadows } = useTheme();

  const [dashboardData, setDashboardData] = useState(null);
  const [isOffline, setIsOffline] = useState(false);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [logoutModalVisible, setLogoutModalVisible] = useState(false);

  // Compute counts from currently downloaded offline transactions
  const offlineMetrics = useMemo(() => {
    const entries = Object.values(downloadedForms || {});
    let inspectionRequestCount = 0;
    let propertyAssessmentCount = 0;

    entries.forEach((entry) => {
      const item = entry?.transItem || entry || {};
      const tracking = (item.TrackingNo || item.trackingNo || entry?.trackingNo || '').toString().toUpperCase();
      const reqType = (item.RequestType || item.requestType || '').toString().toLowerCase();

      const isInspection = tracking.startsWith('RFI-') || reqType.includes('inspection');
      if (isInspection) {
        inspectionRequestCount += 1;
      } else {
        propertyAssessmentCount += 1;
      }
    });

    return {
      totalDownloaded: entries.length,
      propertyAssessmentCount,
      inspectionRequestCount
    };
  }, [downloadedForms, cachedTransactions]);

  useEffect(() => {
    fetchDashboard();
  }, []);

  const fetchDashboard = async () => {
    try {
      setLoading(true);
      const res = await MobileApi.getDashboard();
      if (res && res.success) {
        setDashboardData(res.data);
        setIsOffline(false);
      }
    } catch (err) {
      // Offline mode: automatically falls back to offlineMetrics from downloaded storage
      setIsOffline(true);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleSyncPress = async () => {
    await syncAllToServer();
    fetchDashboard();
  };

  // Use online counts when connected, or downloaded offline counts when offline
  const propertyAssessmentDisplayCount =
    !isOffline && dashboardData?.pendingAssessmentsCount !== undefined
      ? dashboardData.pendingAssessmentsCount
      : offlineMetrics.propertyAssessmentCount;

  const inspectionRequestDisplayCount =
    !isOffline && dashboardData?.pendingInspectionsCount !== undefined
      ? dashboardData.pendingInspectionsCount
      : offlineMetrics.inspectionRequestCount;

  return (
    <View style={[styles.container, { backgroundColor: colors.bgLight }]}>
      <HeaderBar
        title="FAIRTRACK MOBILE"
        subtitle={`Division ${user?.divisionId || 1} • ${user?.name || 'Staff'}`}
        navigation={navigation}
        onRefresh={() => {
          setRefreshing(true);
          fetchDashboard();
        }}
        isRefreshing={refreshing || loading}
      />

      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={() => {
              setRefreshing(true);
              fetchDashboard();
            }}
            colors={[colors.primary]}
          />
        }
      >
        {/* Sync Alert Banner if edits are waiting */}
        {pendingCount > 0 ? (
          <View
            style={[
              styles.syncBanner,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
          >
            <View style={styles.syncBannerInfo}>
              <View style={styles.syncIconBubble}>
                <Ionicons name="cloud-upload" size={24} color={colors.accentGold} />
              </View>
              <View style={{ flex: 1 }}>
                <Text style={[styles.syncBannerTitle, { color: colors.textDark }]}>
                  {pendingCount} Offline Edits Ready
                </Text>
                <Text style={[styles.syncBannerSubtitle, { color: colors.textMuted }]}>
                  Changes made in field pending office sync
                </Text>
              </View>
            </View>

            <TouchableOpacity
              style={styles.syncBannerBtn}
              onPress={handleSyncPress}
              disabled={isSyncing}
            >
              {isSyncing ? (
                <ActivityIndicator color="#ffffff" size="small" />
              ) : (
                <Text style={styles.syncBannerBtnText}>Sync Now</Text>
              )}
            </TouchableOpacity>
          </View>
        ) : null}

        {/* Welcome Card */}
        <TouchableOpacity
          style={[
            styles.welcomeCard,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
          activeOpacity={0.8}
          onPress={() => navigation.navigate('Main', { screen: 'ProfileTab' })}
        >
          <View style={styles.welcomeInfo}>
            <Text style={[styles.greetingText, { color: colors.textMuted }]}>Welcome,</Text>
            <Text style={[styles.userNameText, { color: colors.textDark }]}>{user?.name || 'Field Inspector'}</Text>
            <Text style={[styles.userRoleText, { color: colors.primary }]}>{user?.divisionName || 'City Assessor Staff'}</Text>
          </View>
          <View
            style={[
              styles.avatarBubble,
              { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
            ]}
          >
            <Ionicons name="person" size={28} color={colors.primary} />
            <View
              style={[
                styles.avatarOnlineDot,
                { backgroundColor: isOffline ? colors.accentGold : COLORS.accentGreen }
              ]}
            />
          </View>
        </TouchableOpacity>

        {/* Overview & Metrics (4 Proper Mobile Field Metrics) */}
        <View style={styles.sectionHeaderRow}>
          <Text style={[styles.sectionTitle, { color: colors.textDark, marginBottom: 0 }]}>
            Overview &amp; Metrics
          </Text>
          <View
            style={[
              styles.modeBadge,
              {
                backgroundColor: isOffline
                  ? isDarkMode ? '#78350f' : '#fef3c7'
                  : isDarkMode ? '#064e3b' : '#dcfce7'
              }
            ]}
          >
            <Ionicons
              name={isOffline ? 'cloud-offline-outline' : 'wifi-outline'}
              size={12}
              color={isOffline ? colors.accentGold : colors.accentGreen}
            />
            <Text
              style={[
                styles.modeBadgeText,
                { color: isOffline ? colors.accentGold : colors.accentGreen }
              ]}
            >
              {isOffline ? 'Offline Mode (Local Data)' : 'Online Mode'}
            </Text>
          </View>
        </View>

        <View style={styles.metricsGrid}>
          {/* 1. Property Assessment (RPA) */}
          <TouchableOpacity
            style={[
              styles.metricCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            activeOpacity={0.8}
            onPress={() => navigation.navigate('Main', { screen: 'Feed' })}
          >
            <View
              style={[
                styles.metricIconBox,
                { backgroundColor: isDarkMode ? '#78350f' : '#fef3c7' }
              ]}
            >
              <Ionicons name="home-outline" size={20} color={colors.accentGold} />
            </View>
            <Text style={[styles.metricNumber, { color: colors.textDark }]}>
              {propertyAssessmentDisplayCount}
            </Text>
            <Text style={[styles.metricLabel, { color: colors.textMuted }]}>Property Assessment</Text>
            {!isOffline && offlineMetrics.propertyAssessmentCount > 0 ? (
              <Text style={[styles.metricSubLabel, { color: colors.primary }]}>
                {offlineMetrics.propertyAssessmentCount} downloaded
              </Text>
            ) : null}
          </TouchableOpacity>

          {/* 2. Inspection Request (RFI) */}
          <TouchableOpacity
            style={[
              styles.metricCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            activeOpacity={0.8}
            onPress={() => navigation.navigate('Main', { screen: 'Feed' })}
          >
            <View
              style={[
                styles.metricIconBox,
                { backgroundColor: isDarkMode ? '#1e3a8a' : '#e0f2fe' }
              ]}
            >
              <Ionicons name="clipboard-outline" size={20} color={colors.accentBlue} />
            </View>
            <Text style={[styles.metricNumber, { color: colors.textDark }]}>
              {inspectionRequestDisplayCount}
            </Text>
            <Text style={[styles.metricLabel, { color: colors.textMuted }]}>Inspection Request</Text>
            {!isOffline && offlineMetrics.inspectionRequestCount > 0 ? (
              <Text style={[styles.metricSubLabel, { color: colors.primary }]}>
                {offlineMetrics.inspectionRequestCount} downloaded
              </Text>
            ) : null}
          </TouchableOpacity>

          {/* 3. Downloaded Offline */}
          <TouchableOpacity
            style={[
              styles.metricCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            activeOpacity={0.8}
            onPress={() => navigation.navigate('Main', { screen: 'Feed' })}
          >
            <View
              style={[
                styles.metricIconBox,
                { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
              ]}
            >
              <Ionicons name="cloud-download-outline" size={20} color={colors.primary} />
            </View>
            <Text style={[styles.metricNumber, { color: colors.textDark }]}>
              {offlineMetrics.totalDownloaded}
            </Text>
            <Text style={[styles.metricLabel, { color: colors.textMuted }]}>Downloaded Offline</Text>
          </TouchableOpacity>

          {/* 4. Pending Sync */}
          <TouchableOpacity
            style={[
              styles.metricCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            activeOpacity={0.8}
            onPress={() => navigation.navigate('Main', { screen: 'SyncCenter' })}
          >
            <View
              style={[
                styles.metricIconBox,
                { backgroundColor: isDarkMode ? '#064e3b' : '#dcfce7' }
              ]}
            >
              <Ionicons name="cloud-upload-outline" size={20} color={colors.accentGreen} />
            </View>
            <Text style={[styles.metricNumber, { color: colors.textDark }]}>
              {pendingCount || 0}
            </Text>
            <Text style={[styles.metricLabel, { color: colors.textMuted }]}>Pending Sync</Text>
          </TouchableOpacity>
        </View>

        {/* Quick Access Grid */}
        <Text style={[styles.sectionTitle, { color: colors.textDark }]}>Field Assessment Modules</Text>
        <View style={styles.actionList}>
          <TouchableOpacity
            style={[
              styles.actionCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            onPress={() => navigation.navigate('Main', { screen: 'Feed' })}
          >
            <View
              style={[
                styles.actionIcon,
                { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
              ]}
            >
              <Ionicons name="list" size={24} color={colors.primary} />
            </View>
            <View style={styles.actionDetails}>
              <Text style={[styles.actionTitle, { color: colors.textDark }]}>Transaction Feed</Text>
              <Text style={[styles.actionSubtitle, { color: colors.textMuted }]}>
                {offlineMetrics.totalDownloaded > 0
                  ? `${offlineMetrics.totalDownloaded} transaction(s) downloaded for offline use`
                  : 'View assigned inspection queue & workflow'}
              </Text>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textMuted} />
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.actionCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            onPress={() => navigation.navigate('BuildingForm')}
          >
            <View
              style={[
                styles.actionIcon,
                { backgroundColor: isDarkMode ? '#78350f' : '#fef3c7' }
              ]}
            >
              <Ionicons name="business-outline" size={24} color={colors.accentGold} />
            </View>
            <View style={styles.actionDetails}>
              <Text style={[styles.actionTitle, { color: colors.textDark }]}>Building Inspection Form</Text>
              <Text style={[styles.actionSubtitle, { color: colors.textMuted }]}>Inspect storey levels, floor area, and structure</Text>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textMuted} />
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.actionCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            onPress={() => navigation.navigate('LandForm')}
          >
            <View
              style={[
                styles.actionIcon,
                { backgroundColor: isDarkMode ? '#1e3a8a' : '#e0f2fe' }
              ]}
            >
              <Ionicons name="map-outline" size={24} color={colors.accentBlue} />
            </View>
            <View style={styles.actionDetails}>
              <Text style={[styles.actionTitle, { color: colors.textDark }]}>Land Appraisal Form</Text>
              <Text style={[styles.actionSubtitle, { color: colors.textMuted }]}>Field appraisal, area, unit value, and market value</Text>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textMuted} />
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.actionCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            onPress={() => navigation.navigate('MachineryForm')}
          >
            <View
              style={[
                styles.actionIcon,
                { backgroundColor: isDarkMode ? '#334155' : '#f1f5f9' }
              ]}
            >
              <Ionicons name="construct-outline" size={24} color={isDarkMode ? '#94a3b8' : '#475569'} />
            </View>
            <View style={styles.actionDetails}>
              <Text style={[styles.actionTitle, { color: colors.textDark }]}>Machinery Assessment</Text>
              <Text style={[styles.actionSubtitle, { color: colors.textMuted }]}>Acquisition cost, depreciation, and appraisal</Text>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textMuted} />
          </TouchableOpacity>

          <TouchableOpacity
            style={[
              styles.actionCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.card
            ]}
            onPress={() => navigation.navigate('Main', { screen: 'SyncCenter' })}
          >
            <View
              style={[
                styles.actionIcon,
                { backgroundColor: isDarkMode ? '#064e3b' : '#dcfce7' }
              ]}
            >
              <Ionicons name="cloud-done-outline" size={24} color={colors.accentGreen} />
            </View>
            <View style={styles.actionDetails}>
              <Text style={[styles.actionTitle, { color: colors.textDark }]}>Office Sync Center</Text>
              <Text style={[styles.actionSubtitle, { color: colors.textMuted }]}>
                {pendingCount > 0 ? `${pendingCount} items waiting to sync` : 'All changes up to date'}
              </Text>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textMuted} />
          </TouchableOpacity>
        </View>

        {/* Safe Logout Button */}
        <TouchableOpacity
          style={[
            styles.logoutBtn,
            isDarkMode && {
              backgroundColor: '#450a0a',
              borderColor: '#7f1d1d'
            }
          ]}
          onPress={() => setLogoutModalVisible(true)}
        >
          <Ionicons
            name="log-out-outline"
            size={18}
            color={isDarkMode ? '#fca5a5' : COLORS.accentRed}
          />
          <Text
            style={[
              styles.logoutBtnText,
              isDarkMode && { color: '#fca5a5' }
            ]}
          >
            Sign Out
          </Text>
        </TouchableOpacity>
      </ScrollView>

      {/* Confirmation Logout Modal */}
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
  syncBanner: {
    backgroundColor: '#fffbeb',
    borderRadius: 16,
    padding: 16,
    borderWidth: 1.5,
    borderColor: '#fde68a',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 16
  },
  syncBannerInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    flex: 1
  },
  syncIconBubble: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: '#fef3c7',
    alignItems: 'center',
    justifyContent: 'center'
  },
  syncBannerTitle: {
    fontSize: 15,
    fontWeight: '800',
    color: '#92400e'
  },
  syncBannerSubtitle: {
    fontSize: 11,
    color: '#b45309',
    marginTop: 2
  },
  syncBannerBtn: {
    backgroundColor: COLORS.accentGold,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 10
  },
  syncBannerBtnText: {
    color: '#ffffff',
    fontWeight: '800',
    fontSize: 13
  },
  welcomeCard: {
    backgroundColor: '#ffffff',
    borderRadius: 18,
    padding: 18,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 20,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  welcomeInfo: {
    flex: 1
  },
  greetingText: {
    fontSize: 12,
    color: COLORS.textMuted,
    fontWeight: '600'
  },
  userNameText: {
    fontSize: 20,
    fontWeight: '800',
    color: COLORS.primaryDark,
    marginTop: 1
  },
  userRoleText: {
    fontSize: 12,
    color: COLORS.primary,
    fontWeight: '700',
    marginTop: 4
  },
  avatarBubble: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: COLORS.primaryLight,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative'
  },
  avatarOnlineDot: {
    position: 'absolute',
    bottom: 2,
    right: 2,
    width: 14,
    height: 14,
    borderRadius: 7,
    backgroundColor: COLORS.accentGreen,
    borderWidth: 2,
    borderColor: '#ffffff'
  },
  sectionHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 12
  },
  modeBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12
  },
  modeBadgeText: {
    fontSize: 11,
    fontWeight: '800'
  },
  sectionTitle: {
    fontSize: 14,
    fontWeight: '800',
    color: COLORS.textDark,
    textTransform: 'uppercase',
    letterSpacing: 0.6,
    marginBottom: 12,
    marginLeft: 4
  },
  metricsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 10,
    marginBottom: 24
  },
  metricCard: {
    flexBasis: '47%',
    flexGrow: 1,
    backgroundColor: '#ffffff',
    borderRadius: 14,
    padding: 14,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: COLORS.border
  },
  metricIconBox: {
    width: 38,
    height: 38,
    borderRadius: 19,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 8
  },
  metricNumber: {
    fontSize: 22,
    fontWeight: '900',
    color: COLORS.textDark
  },
  metricLabel: {
    fontSize: 11,
    color: COLORS.textMuted,
    textAlign: 'center',
    fontWeight: '700',
    marginTop: 2
  },
  metricSubLabel: {
    fontSize: 10,
    fontWeight: '700',
    marginTop: 3
  },
  actionList: {
    gap: 10,
    marginBottom: 24
  },
  actionCard: {
    backgroundColor: '#ffffff',
    borderRadius: 14,
    padding: 14,
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: COLORS.border
  },
  actionIcon: {
    width: 44,
    height: 44,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 14
  },
  actionDetails: {
    flex: 1
  },
  actionTitle: {
    fontSize: 15,
    fontWeight: '800',
    color: COLORS.textDark
  },
  actionSubtitle: {
    fontSize: 11,
    color: COLORS.textMuted,
    marginTop: 2
  },
  logoutBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 14,
    backgroundColor: '#ffffff',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#fecaca',
    marginTop: 8
  },
  logoutBtnText: {
    fontSize: 14,
    fontWeight: '700',
    color: COLORS.accentRed
  }
});
