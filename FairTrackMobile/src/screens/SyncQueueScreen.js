import React, { useState, useContext } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  Alert,
  ActivityIndicator
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { HeaderBar } from '../components/HeaderBar';
import { ActionModal } from '../components/ActionModal';
import { COLORS, SHADOWS } from '../constants/theme';

export const SyncQueueScreen = ({ navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const [actionModal, setActionModal] = useState({ visible: false, title: '', message: '', icon: 'information-circle-outline', iconType: 'primary', buttons: [] });
  const showModal = (config) => setActionModal({ visible: true, icon: 'information-circle-outline', iconType: 'primary', buttons: [], ...config });
  const closeModal = () => setActionModal(prev => ({ ...prev, visible: false }));
  const {
    syncQueue,
    pendingCount,
    isSyncing,
    lastSyncTime,
    syncLogs,
    isOnline,
    checkConnectivity,
    syncAllToServer,
    removeQueueItem,
    clearQueue
  } = useContext(SyncContext);


  const handleSyncAll = async () => {
    if (pendingCount === 0) {
      showModal({ title: 'Queue Empty', message: 'There are no pending offline changes to sync.', icon: 'checkmark-circle-outline', iconType: 'primary' });
      return;
    }

    const online = await checkConnectivity(2500);
    if (!online) {
      showModal({
        title: 'Office Server Unreachable',
        message: 'Your device is offline or cannot connect to the office server. Please verify your connection to the office Wi-Fi or LAN before syncing.',
        icon: 'cloud-offline-outline',
        iconType: 'warning'
      });
      return;
    }

    const result = await syncAllToServer();
    if (result.success) {
      showModal({ title: 'Office Sync Complete', message: `Successfully synchronized ${result.successCount} updates to the live database!`, icon: 'checkmark-circle-outline', iconType: 'success' });
    } else {
      showModal({
        title: 'Sync Completed with Warnings',
        message: `Synced: ${result.successCount}, Failed: ${result.failureCount}. Unsynced items remain in your offline queue for retry.`,
        icon: 'warning-outline',
        iconType: 'warning'
      });
    }
  };

  const handleRemove = (item) => {
    showModal({
      title: 'Remove Queued Item',
      message: `Are you sure you want to discard offline edit for "${item.title}"?`,
      icon: 'trash-outline',
      iconType: 'danger',
      buttons: [
        { text: 'Discard', style: 'danger', icon: 'trash-outline', onPress: () => removeQueueItem(item.id) },
        { text: 'Cancel', style: 'cancel' }
      ]
    });
  };

  const renderQueueItem = ({ item }) => {
    const typeLabel = item.type.replace('UPDATE_', '') + ' FAAS';
    const typeColor = item.type === 'UPDATE_BUILDING' ? COLORS.accentGold :
                      item.type === 'UPDATE_LAND' ? COLORS.accentBlue :
                      item.type === 'UPDATE_MACHINERY' ? '#475569' : COLORS.primary;

    return (
      <View
        style={[
          styles.queueCard,
          {
            backgroundColor: colors.cardBg,
            borderColor: colors.border
          },
          shadows.card
        ]}
      >
        <View style={styles.queueCardHeader}>
          <View style={[styles.typeBadge, { backgroundColor: typeColor + '20' }]}>
            <Text style={[styles.typeBadgeText, { color: typeColor }]}>{typeLabel}</Text>
          </View>
          <Text style={[styles.timeText, { color: colors.textMuted }]}>
            {new Date(item.timestamp).toLocaleTimeString()}
          </Text>
        </View>

        <Text style={[styles.itemTitle, { color: colors.textDark }]}>{item.title}</Text>

        <View style={styles.queueCardFooter}>
          <View style={styles.statusIndicator}>
            <Ionicons name="time-outline" size={14} color={colors.accentGold} />
            <Text style={[styles.statusText, { color: colors.accentGold }]}>Pending Office Sync</Text>
          </View>

          <TouchableOpacity onPress={() => handleRemove(item)} style={styles.deleteBtn}>
            <Ionicons name="trash-outline" size={16} color={COLORS.accentRed} />
            <Text style={styles.deleteText}>Discard</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.bgLight }]}>
      <HeaderBar title="OFFICE SYNC CENTER" subtitle="Offline-to-Online Batch Manager" navigation={navigation} />

      <View style={styles.content}>
        {!isOnline && (
          <View style={styles.offlineNoticeBanner}>
            <Ionicons name="cloud-offline-outline" size={18} color="#b45309" />
            <Text style={styles.offlineNoticeText}>
              Device is offline. Office sync will be available when connected to the local office network.
            </Text>
          </View>
        )}

        {/* Sync Summary Hero Card */}
        <View
          style={[
            styles.heroCard,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.card
          ]}
        >
          <View style={styles.heroInfo}>
            <Text style={[styles.heroTitle, { color: colors.textDark }]}>{pendingCount} Offline Edits</Text>
            <Text style={[styles.heroSub, { color: colors.textMuted }]}>
              {lastSyncTime ? `Last Synced: ${lastSyncTime}` : 'No sync performed yet this session'}
            </Text>
          </View>

          <TouchableOpacity
            style={[
              styles.syncActionBtn,
              { backgroundColor: colors.primary },
              (pendingCount === 0 || isSyncing) && styles.syncBtnDisabled
            ]}
            onPress={handleSyncAll}
            disabled={pendingCount === 0 || isSyncing}
          >
            {isSyncing ? (
              <ActivityIndicator color="#ffffff" size="small" />
            ) : (
              <>
                <Ionicons name={!isOnline ? "cloud-offline-outline" : "cloud-upload"} size={20} color="#ffffff" />
                <Text style={styles.syncActionText}>
                  {!isOnline ? "Sync All to Server (Offline)" : "Sync All to Server"}
                </Text>
              </>
            )}
          </TouchableOpacity>
        </View>

        {/* Section Header */}
        <View style={styles.sectionHeader}>
          <Text style={[styles.sectionTitle, { color: colors.textDark }]}>Queued Offline Modifications</Text>
          {pendingCount > 0 ? (
            <TouchableOpacity onPress={clearQueue}>
              <Text style={styles.clearAllText}>Clear All</Text>
            </TouchableOpacity>
          ) : null}
        </View>

        {/* Queue List */}
        <FlatList
          data={syncQueue}
          keyExtractor={(item) => item.id}
          renderItem={renderQueueItem}
          contentContainerStyle={styles.listContainer}
          ListEmptyComponent={
            <View style={styles.emptyState}>
              <View
                style={[
                  styles.emptyIconCircle,
                  { backgroundColor: isDarkMode ? '#064e3b' : '#dcfce7' }
                ]}
              >
                <Ionicons name="checkmark-done" size={36} color={colors.accentGreen} />
              </View>
              <Text style={[styles.emptyTitle, { color: colors.textDark }]}>All Changes Synced</Text>
              <Text style={[styles.emptySub, { color: colors.textMuted }]}>
                There are no pending offline changes. You're ready for field inspections.
              </Text>
            </View>
          }
        />
      </View>
      <ActionModal
        visible={actionModal.visible}
        title={actionModal.title}
        message={actionModal.message}
        icon={actionModal.icon}
        iconType={actionModal.iconType}
        buttons={actionModal.buttons}
        onClose={closeModal}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.bgLight
  },
  content: {
    flex: 1,
    padding: 16
  },
  heroCard: {
    backgroundColor: '#ffffff',
    borderRadius: 18,
    padding: 18,
    marginBottom: 18,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  heroInfo: {
    marginBottom: 14
  },
  heroTitle: {
    fontSize: 22,
    fontWeight: '900',
    color: COLORS.primaryDark
  },
  heroSub: {
    fontSize: 12,
    color: COLORS.textMuted,
    marginTop: 2
  },
  syncActionBtn: {
    backgroundColor: COLORS.primary,
    borderRadius: 12,
    paddingVertical: 14,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    shadowColor: COLORS.primary,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.25,
    shadowRadius: 8,
    elevation: 3
  },
  syncBtnDisabled: {
    opacity: 0.6
  },
  syncActionText: {
    color: '#ffffff',
    fontSize: 15,
    fontWeight: '800'
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 10,
    paddingHorizontal: 4
  },
  sectionTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: COLORS.textDark,
    textTransform: 'uppercase',
    letterSpacing: 0.5
  },
  clearAllText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.accentRed
  },
  listContainer: {
    gap: 10,
    paddingBottom: 30
  },
  queueCard: {
    backgroundColor: '#ffffff',
    borderRadius: 14,
    padding: 14,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  queueCardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 6
  },
  typeBadge: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6
  },
  typeBadgeText: {
    fontSize: 11,
    fontWeight: '800'
  },
  timeText: {
    fontSize: 11,
    color: COLORS.textMuted,
    fontWeight: '600'
  },
  itemTitle: {
    fontSize: 14,
    fontWeight: '700',
    color: COLORS.textDark,
    marginBottom: 10
  },
  queueCardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: '#f8fafc',
    paddingTop: 8
  },
  statusIndicator: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  statusText: {
    fontSize: 11,
    fontWeight: '700',
    color: COLORS.accentGold
  },
  deleteBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 3
  },
  deleteText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.accentRed
  },
  offlineNoticeBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fef3c7',
    borderWidth: 1,
    borderColor: '#fde68a',
    borderRadius: 12,
    paddingHorizontal: 14,
    paddingVertical: 10,
    marginBottom: 14,
    gap: 8
  },
  offlineNoticeText: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
    color: '#92400e'
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 50,
    backgroundColor: '#ffffff',
    borderRadius: 16,
    borderWidth: 1,
    borderColor: COLORS.border,
    paddingHorizontal: 20
  },
  emptyIconCircle: {
    width: 64,
    height: 64,
    borderRadius: 32,
    backgroundColor: '#dcfce7',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 12
  },
  emptyTitle: {
    fontSize: 17,
    fontWeight: '800',
    color: COLORS.textDark
  },
  emptySub: {
    fontSize: 12,
    color: COLORS.textMuted,
    textAlign: 'center',
    marginTop: 4
  }
});
