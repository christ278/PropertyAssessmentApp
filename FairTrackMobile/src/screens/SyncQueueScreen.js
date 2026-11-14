import React, { useState, useContext, useMemo } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Modal,
  TextInput,
  KeyboardAvoidingView,
  Platform
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { HeaderBar } from '../components/HeaderBar';
import { ActionModal } from '../components/ActionModal';
import { COLORS } from '../constants/theme';

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
    isOnline,
    checkConnectivity,
    syncAllToServer,
    syncTransactionToServer,
    downloadedForms,
    removeQueueItem,
    clearQueue
  } = useContext(SyncContext);

  // Per-transaction menu & action states
  const [selectedTransactionForMenu, setSelectedTransactionForMenu] = useState(null);
  const [threeDotsModalVisible, setThreeDotsModalVisible] = useState(false);
  const [forwardConfirmModalVisible, setForwardConfirmModalVisible] = useState(false);
  const [transferRemarks, setTransferRemarks] = useState('');
  const [actionLoading, setActionLoading] = useState(false);
  const [syncingKey, setSyncingKey] = useState(null);

  // Group queued items by Transaction (matches TransactionsScreen layout, not per partition)
  const groupedTransactions = useMemo(() => {
    if (!Array.isArray(syncQueue) || syncQueue.length === 0) return [];

    const map = new Map();

    for (const q of syncQueue) {
      const p = q.payload || {};
      const tdn = p.TDN || p.tdn || {};

      const trackingNo = (tdn.TrackingNo || tdn.trackingNo || p.TrackingNo || p.trackingNo || '').toString().trim();
      const formWorkflowId = (tdn.FormWorkflowId || tdn.formWorkflowId || p.FormWorkflowId || p.formWorkflowId || q.workflowForwardPayload?.FormWorkflowId || q.workflowForwardPayload?.formWorkflowId || '').toString().trim();

      const key = (trackingNo || formWorkflowId || q.id).toLowerCase();

      // Look up cached downloaded transItem for richer metadata
      let transItem = null;
      if (downloadedForms) {
        if (trackingNo && downloadedForms[trackingNo]?.transItem) transItem = downloadedForms[trackingNo].transItem;
        else if (formWorkflowId && downloadedForms[formWorkflowId]?.transItem) transItem = downloadedForms[formWorkflowId].transItem;
        else {
          const matchKey = Object.keys(downloadedForms).find(k =>
            (trackingNo && k.toLowerCase() === trackingNo.toLowerCase()) ||
            (formWorkflowId && k.toString() === formWorkflowId.toString())
          );
          if (matchKey) transItem = downloadedForms[matchKey].transItem;
        }
      }

      const partIdx = parseInt(tdn.PartitionItemIndex ?? p.partitionIndex ?? 0, 10) || 0;
      const partLetter = p.partitionLetter || tdn.PartitionIndex || String.fromCharCode(65 + partIdx);

      const kindLabel = q.type === 'UPDATE_LAND' ? 'Land Appraisal' :
                        q.type === 'UPDATE_BUILDING' ? 'Building FAAS' :
                        q.type === 'UPDATE_MACHINERY' ? 'Machinery FAAS' :
                        (transItem?.PropertyType || transItem?.Kind || 'Property Assessment');

      const moduleLabel = (trackingNo && trackingNo.toUpperCase().startsWith('RFI-')) ||
                          (transItem?.RequestType && transItem.RequestType.toLowerCase().includes('inspection'))
                          ? 'Field Inspection'
                          : 'Property Assessment';

      const owner = tdn.DisplayName || tdn.OwnerName || p.ownerName || transItem?.Owner || transItem?.OwnerName || 'Property Owner';
      const street = tdn.PropLocationStreet || tdn.Location || p.streetLocation || '';
      const brgy = tdn.DistrictOrBarangay || p.districtOrBarangay || transItem?.Barangay || '';
      const city = tdn.CityMunicipality || p.cityMunicipality || transItem?.CityMunicipality || '';
      const locTokens = [street, brgy, city].filter(Boolean);
      const location = locTokens.length > 0 ? locTokens.join(', ') : (transItem?.Barangay ? `${transItem.Barangay}, General Santos City` : 'Field Location');

      if (!map.has(key)) {
        map.set(key, {
          key,
          trackingNo: trackingNo || transItem?.TrackingNo || 'Unassigned Tracking',
          formWorkflowId: formWorkflowId || transItem?.FormworkflowId,
          type: q.type,
          kind: kindLabel,
          moduleLabel,
          owner,
          location,
          timestamp: q.timestamp,
          status: q.status || 'PENDING',
          transItem,
          partitions: [],
          queueItemIds: []
        });
      }

      const grp = map.get(key);
      grp.queueItemIds.push(q.id);
      if (!grp.partitions.some(pt => pt.index === partIdx || pt.letter === partLetter)) {
        grp.partitions.push({
          index: partIdx,
          letter: partLetter,
          queueItemId: q.id,
          title: q.title
        });
      }
      if (new Date(q.timestamp) > new Date(grp.timestamp)) {
        grp.timestamp = q.timestamp;
      }
    }

    for (const grp of map.values()) {
      grp.partitions.sort((a, b) => a.index - b.index);
    }

    return Array.from(map.values());
  }, [syncQueue, downloadedForms]);

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

  const handleOpenMenu = (trans) => {
    setSelectedTransactionForMenu(trans);
    setThreeDotsModalVisible(true);
  };

  const handleSyncTransaction = async (trans) => {
    setSyncingKey(trans.key);
    setActionLoading(true);
    try {
      const res = await syncTransactionToServer(trans.trackingNo, trans.formWorkflowId, false);
      setActionLoading(false);
      setSyncingKey(null);
      setThreeDotsModalVisible(false);
      if (res.success) {
        showModal({
          title: 'Office Sync Complete',
          message: res.message || `Successfully synced changes for ${trans.trackingNo}.`,
          icon: 'cloud-done-outline',
          iconType: 'success'
        });
      } else {
        showModal({
          title: 'Sync Failed',
          message: res.message || 'Failed to sync transaction to office portal.',
          icon: 'alert-circle-outline',
          iconType: 'danger'
        });
      }
    } catch (err) {
      setActionLoading(false);
      setSyncingKey(null);
      setThreeDotsModalVisible(false);
      showModal({
        title: 'Sync Error',
        message: err?.message || 'An unexpected error occurred during sync.',
        icon: 'alert-circle-outline',
        iconType: 'danger'
      });
    }
  };

  const handlePromptForwardSupervisor = (trans) => {
    setSelectedTransactionForMenu(trans);
    setThreeDotsModalVisible(false);
    setTransferRemarks('Field inspection completed and transferred to supervisor.');
    setForwardConfirmModalVisible(true);
  };

  const handleExecuteForwardToSupervisor = async () => {
    if (!selectedTransactionForMenu) return;
    const trans = selectedTransactionForMenu;
    setSyncingKey(trans.key);
    setActionLoading(true);
    try {
      const res = await syncTransactionToServer(
        trans.trackingNo,
        trans.formWorkflowId,
        true,
        transferRemarks
      );
      setActionLoading(false);
      setSyncingKey(null);
      setForwardConfirmModalVisible(false);
      if (res.success) {
        showModal({
          title: 'Forwarded to Supervisor',
          message: res.message || `Successfully transferred ${trans.trackingNo} to Supervisor.`,
          icon: 'checkmark-circle-outline',
          iconType: 'success'
        });
      } else {
        showModal({
          title: 'Transfer Failed',
          message: res.message || 'Failed to forward transaction to supervisor.',
          icon: 'alert-circle-outline',
          iconType: 'danger'
        });
      }
    } catch (err) {
      setActionLoading(false);
      setSyncingKey(null);
      setForwardConfirmModalVisible(false);
      showModal({
        title: 'Transfer Error',
        message: err?.message || 'An unexpected error occurred during transfer.',
        icon: 'alert-circle-outline',
        iconType: 'danger'
      });
    }
  };

  const handleDiscardTransaction = (trans) => {
    showModal({
      title: 'Discard Offline Edits',
      message: `Are you sure you want to discard all offline modifications for transaction "${trans.trackingNo}" (${trans.partitions.length} partition${trans.partitions.length > 1 ? 's' : ''})?\n\nThis action cannot be undone.`,
      icon: 'trash-outline',
      iconType: 'danger',
      buttons: [
        {
          text: 'Discard All',
          style: 'danger',
          icon: 'trash-outline',
          onPress: async () => {
            for (const id of trans.queueItemIds) {
              await removeQueueItem(id);
            }
            closeModal();
          }
        },
        { text: 'Cancel', style: 'cancel' }
      ]
    });
  };

  const handleClearAll = () => {
    showModal({
      title: 'Clear Entire Sync Queue',
      message: `Are you sure you want to discard all ${pendingCount} offline modifications across all transactions?`,
      icon: 'trash-outline',
      iconType: 'danger',
      buttons: [
        { text: 'Clear All', style: 'danger', icon: 'trash-outline', onPress: clearQueue },
        { text: 'Cancel', style: 'cancel' }
      ]
    });
  };

  const renderTransactionCard = ({ item }) => {
    const isThisSyncing = syncingKey === item.key;
    const typeColor = item.type === 'UPDATE_BUILDING' ? COLORS.accentGold :
                      item.type === 'UPDATE_LAND' ? COLORS.accentBlue :
                      item.type === 'UPDATE_MACHINERY' ? '#475569' : COLORS.primary;

    return (
      <View
        style={[
          styles.transCard,
          {
            backgroundColor: colors.cardBg,
            borderColor: colors.border
          },
          shadows.card
        ]}
      >
        {/* Card Header: Tracking, Badges, 3-Dots */}
        <View style={styles.cardHeader}>
          <View style={{ flex: 1, paddingRight: 6 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
              <Text style={[styles.trackingNoText, { color: colors.textDark }]}>
                {item.trackingNo}
              </Text>
              <View style={styles.pendingEditsBadge}>
                <Ionicons name="cloud-upload" size={10} color="#b45309" />
                <Text style={styles.pendingEditsBadgeText}>UNSYNCED EDITS</Text>
              </View>
              {item.partitions.length > 1 ? (
                <View style={styles.multiPartitionBadge}>
                  <Ionicons name="layers-outline" size={10} color="#0369a1" />
                  <Text style={styles.multiPartitionBadgeText}>{item.partitions.length} PARTITIONS</Text>
                </View>
              ) : null}
            </View>
            <Text style={styles.typeBadgeText}>
              {item.moduleLabel} • {item.kind}
            </Text>
          </View>

          <TouchableOpacity
            style={[
              styles.threeDotsBtn,
              {
                backgroundColor: isDarkMode ? '#1e293b' : '#f8fafc',
                borderColor: colors.border
              }
            ]}
            onPress={() => handleOpenMenu(item)}
            hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
            disabled={isThisSyncing}
          >
            <Ionicons name="ellipsis-vertical" size={18} color={colors.textDark} />
          </TouchableOpacity>
        </View>

        {/* Card Body: Owner, Location, Partitions Chips, Time */}
        <View style={styles.cardBody}>
          <View style={styles.infoRow}>
            <Ionicons name="person-circle-outline" size={15} color={colors.textDark} />
            <Text style={[styles.ownerText, { color: colors.textDark }]} numberOfLines={1}>
              Owner / Applicant: {item.owner}
            </Text>
          </View>

          <View style={styles.infoRow}>
            <Ionicons name="location-outline" size={14} color={colors.textMuted} />
            <Text style={[styles.infoText, { color: colors.textMuted }]} numberOfLines={1}>
              {item.location}
            </Text>
          </View>

          {/* Partitions with offline edits */}
          <View style={styles.partitionChipRow}>
            <Text style={[styles.partitionChipLabel, { color: colors.textMuted }]}>
              Partitions with edits:
            </Text>
            {item.partitions.map((pt) => (
              <View
                key={`p_${pt.letter}_${pt.index}`}
                style={[
                  styles.partitionChip,
                  { backgroundColor: isDarkMode ? '#0369a122' : '#e0f2fe', borderColor: isDarkMode ? '#0284c7' : '#bae6fd' }
                ]}
              >
                <Text style={[styles.partitionChipText, { color: isDarkMode ? '#38bdf8' : '#0284c7' }]}>
                  Part {pt.letter}
                </Text>
              </View>
            ))}
          </View>

          <View style={styles.infoRow}>
            <Ionicons name="time-outline" size={13} color={colors.textMuted} />
            <Text style={[styles.timeText, { color: colors.textMuted }]}>
              Modified: {new Date(item.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
            </Text>
          </View>
        </View>

        {/* Card Footer: Sync Draft, Sync & Forward, Discard */}
        <View style={styles.cardFooter}>
          <TouchableOpacity
            style={[styles.actionBtnDraft, isThisSyncing && { opacity: 0.7 }]}
            onPress={() => handleSyncTransaction(item)}
            disabled={isThisSyncing}
          >
            {isThisSyncing ? (
              <>
                <ActivityIndicator size="small" color="#0284c7" />
                <Text style={styles.actionBtnDraftText}>Syncing...</Text>
              </>
            ) : (
              <>
                <Ionicons name="cloud-upload-outline" size={14} color="#0284c7" />
                <Text style={styles.actionBtnDraftText}>Sync to Web</Text>
              </>
            )}
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.actionBtnForward, isThisSyncing && { opacity: 0.7 }]}
            onPress={() => handlePromptForwardSupervisor(item)}
            disabled={isThisSyncing}
          >
            <Ionicons name="send" size={13} color="#059669" />
            <Text style={styles.actionBtnForwardText}>Sync & Forward</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={styles.actionBtnDiscard}
            onPress={() => handleDiscardTransaction(item)}
            disabled={isThisSyncing}
            hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
          >
            <Ionicons name="trash-outline" size={15} color={COLORS.accentRed} />
            <Text style={styles.actionBtnDiscardText}>Discard</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.bgLight }]}>
      <HeaderBar navigation={navigation} />

      <View style={styles.content}>
        {/* Screen Title */}
        <View style={styles.screenHeader}>
          <View style={styles.screenHeaderRow}>
            <View style={{ flex: 1 }}>
              <Text style={[styles.screenTitle, { color: colors.textDark }]}>Office Sync Center</Text>
              <Text style={[styles.screenSubtitle, { color: colors.textMuted }]}>
                Offline-to-Online Transaction Manager
              </Text>
            </View>
            <View
              style={[
                styles.countBadge,
                {
                  backgroundColor:
                    groupedTransactions.length > 0
                      ? (isDarkMode ? '#78350f' : '#fef3c7')
                      : (isDarkMode ? '#064e3b' : '#dcfce7')
                }
              ]}
            >
              <Text
                style={[
                  styles.countBadgeText,
                  { color: groupedTransactions.length > 0 ? colors.accentGold : colors.accentGreen }
                ]}
              >
                {groupedTransactions.length} {groupedTransactions.length === 1 ? 'Transaction' : 'Transactions'}
              </Text>
            </View>
          </View>
        </View>

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
            <Text style={[styles.heroTitle, { color: colors.textDark }]}>
              {groupedTransactions.length} {groupedTransactions.length === 1 ? 'Transaction' : 'Transactions'} ({pendingCount} partition edits)
            </Text>
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
                  {!isOnline ? "Sync All to Server (Offline)" : "Sync All Transactions"}
                </Text>
              </>
            )}
          </TouchableOpacity>
        </View>

        {/* Section Header */}
        <View style={styles.sectionHeader}>
          <Text style={[styles.sectionTitle, { color: colors.textDark }]}>
            Transactions with Offline Edits
          </Text>
          {pendingCount > 0 ? (
            <TouchableOpacity onPress={handleClearAll}>
              <Text style={styles.clearAllText}>Clear All</Text>
            </TouchableOpacity>
          ) : null}
        </View>

        {/* Transaction Grouped List */}
        <FlatList
          data={groupedTransactions}
          keyExtractor={(item) => item.key}
          renderItem={renderTransactionCard}
          contentContainerStyle={styles.listContainer}
          showsVerticalScrollIndicator={false}
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
              <Text style={[styles.emptyTitle, { color: colors.textDark }]}>All Transactions Synced</Text>
              <Text style={[styles.emptySub, { color: colors.textMuted }]}>
                There are no pending offline changes. All partition edits are synchronized to the office web portal.
              </Text>
            </View>
          }
        />
      </View>

      {/* 3-Dots Action Menu Modal */}
      <Modal
        visible={threeDotsModalVisible}
        transparent={true}
        animationType="fade"
        onRequestClose={() => {
          if (!actionLoading) setThreeDotsModalVisible(false);
        }}
      >
        <TouchableOpacity
          style={[styles.modalOverlay, { backgroundColor: colors.modalOverlay }]}
          activeOpacity={1}
          onPress={() => {
            if (!actionLoading) setThreeDotsModalVisible(false);
          }}
        >
          <TouchableOpacity
            activeOpacity={1}
            style={[
              styles.actionMenuCard,
              {
                backgroundColor: colors.cardBg,
                borderColor: colors.border
              },
              shadows.hover
            ]}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.actionMenuHeader}>
              <View style={{ flex: 1 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                  <Text style={[styles.actionMenuTracking, { color: colors.textDark }]}>
                    {selectedTransactionForMenu?.trackingNo || 'Transaction'}
                  </Text>
                  <View style={styles.menuPendingBadge}>
                    <Text style={styles.menuPendingBadgeText}>
                      {selectedTransactionForMenu?.partitions?.length || 1} PARTITIONS
                    </Text>
                  </View>
                </View>
                <Text style={[styles.actionMenuOwner, { color: colors.textMuted }]} numberOfLines={1}>
                  {selectedTransactionForMenu?.owner || 'Property Owner'}
                </Text>
              </View>
              {!actionLoading && (
                <TouchableOpacity
                  onPress={() => setThreeDotsModalVisible(false)}
                  style={styles.actionMenuCloseBtn}
                  hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                >
                  <Ionicons name="close" size={20} color={colors.textDark} />
                </TouchableOpacity>
              )}
            </View>

            {actionLoading ? (
              <View style={styles.menuLoadingContainer}>
                <ActivityIndicator size="large" color={colors.primary} />
                <Text style={[styles.menuLoadingText, { color: colors.textDark }]}>
                  Syncing to Web Portal...
                </Text>
                <Text style={[styles.menuLoadingSubtext, { color: colors.textMuted }]}>
                  Uploading offline partition edits, sketches, and attachments for {selectedTransactionForMenu?.trackingNo || 'transaction'}. Please do not close.
                </Text>
              </View>
            ) : (
              <View style={styles.actionMenuList}>
                {/* Option 1: Sync to Web (Draft) */}
                <TouchableOpacity
                  style={[
                    styles.menuActionItem,
                    {
                      backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                      borderColor: colors.border
                    }
                  ]}
                  onPress={() => selectedTransactionForMenu && handleSyncTransaction(selectedTransactionForMenu)}
                >
                  <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#0369a133' : '#e0f2fe' }]}>
                    <Ionicons name="cloud-upload-outline" size={20} color="#0284c7" />
                  </View>
                  <View style={styles.menuActionTextCol}>
                    <Text style={[styles.menuActionTitle, { color: colors.textDark }]}>
                      Sync to Web (Draft)
                    </Text>
                    <Text style={[styles.menuActionSubtitle, { color: colors.textMuted }]}>
                      Upload all offline partition edits without advancing workflow
                    </Text>
                  </View>
                  <Ionicons name="chevron-forward" size={16} color={colors.textMuted} />
                </TouchableOpacity>

                {/* Option 2: Sync & Forward to Supervisor */}
                <TouchableOpacity
                  style={[
                    styles.menuActionItem,
                    {
                      backgroundColor: isDarkMode ? '#064e3b22' : '#f0fdf4',
                      borderColor: isDarkMode ? '#065f46' : '#bbf7d0'
                    }
                  ]}
                  onPress={() => selectedTransactionForMenu && handlePromptForwardSupervisor(selectedTransactionForMenu)}
                >
                  <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#065f46' : '#dcfce7' }]}>
                    <Ionicons name="send" size={18} color="#059669" />
                  </View>
                  <View style={styles.menuActionTextCol}>
                    <Text style={[styles.menuActionTitle, { color: isDarkMode ? '#34d399' : '#047857' }]}>
                      Sync & Forward to Supervisor
                    </Text>
                    <Text style={[styles.menuActionSubtitle, { color: isDarkMode ? '#a7f3d0' : '#15803d' }]}>
                      Sync all partition changes and transfer to Supervisor
                    </Text>
                  </View>
                  <Ionicons name="chevron-forward" size={16} color="#059669" />
                </TouchableOpacity>

                {/* Option 3: Discard Offline Edits */}
                <TouchableOpacity
                  style={[
                    styles.menuActionItem,
                    {
                      backgroundColor: isDarkMode ? '#450a0a22' : '#fef2f2',
                      borderColor: isDarkMode ? '#7f1d1d' : '#fecaca'
                    }
                  ]}
                  onPress={() => {
                    setThreeDotsModalVisible(false);
                    if (selectedTransactionForMenu) {
                      handleDiscardTransaction(selectedTransactionForMenu);
                    }
                  }}
                >
                  <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#7f1d1d' : '#fee2e2' }]}>
                    <Ionicons name="trash-outline" size={18} color={COLORS.accentRed} />
                  </View>
                  <View style={styles.menuActionTextCol}>
                    <Text style={[styles.menuActionTitle, { color: COLORS.accentRed }]}>
                      Discard All Offline Edits
                    </Text>
                    <Text style={[styles.menuActionSubtitle, { color: colors.textMuted }]}>
                      Revert all offline changes for this transaction
                    </Text>
                  </View>
                  <Ionicons name="chevron-forward" size={16} color={COLORS.accentRed} />
                </TouchableOpacity>
              </View>
            )}

            {!actionLoading && (
              <TouchableOpacity
                style={[styles.modalCancelBtn, { borderColor: colors.border, marginTop: 6 }]}
                onPress={() => setThreeDotsModalVisible(false)}
              >
                <Text style={[styles.modalCancelBtnText, { color: colors.textMuted }]}>Close</Text>
              </TouchableOpacity>
            )}
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Forward to Supervisor Confirmation Modal */}
      <Modal
        visible={forwardConfirmModalVisible}
        transparent={true}
        animationType="fade"
        onRequestClose={() => {
          if (!actionLoading) setForwardConfirmModalVisible(false);
        }}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={[styles.modalOverlay, { backgroundColor: colors.modalOverlay }]}
            activeOpacity={1}
            onPress={() => {
              if (!actionLoading) setForwardConfirmModalVisible(false);
            }}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[
                styles.actionMenuCard,
                {
                  backgroundColor: colors.cardBg,
                  borderColor: colors.border
                },
                shadows.hover
              ]}
              onPress={(e) => e.stopPropagation()}
            >
              {actionLoading ? (
                <View style={styles.menuLoadingContainer}>
                  <ActivityIndicator size="large" color="#059669" />
                  <Text style={[styles.menuLoadingText, { color: colors.textDark }]}>
                    Forwarding to Supervisor...
                  </Text>
                  <Text style={[styles.menuLoadingSubtext, { color: colors.textMuted }]}>
                    Synchronizing offline partition changes and advancing workflow for {selectedTransactionForMenu?.trackingNo || 'transaction'} to Supervisor Review. Please do not close.
                  </Text>
                </View>
              ) : (
                <>
                  <View style={[styles.iconBubble, { backgroundColor: isDarkMode ? '#065f46' : '#dcfce7', alignSelf: 'center', marginBottom: 12 }]}>
                    <Ionicons name="send" size={26} color="#059669" />
                  </View>

                  <Text style={[styles.forwardModalTitle, { color: colors.textDark }]}>
                    Forward to Supervisor?
                  </Text>

                  <Text style={[styles.forwardModalDescription, { color: colors.textMuted }]}>
                    This will synchronize all offline partition changes for {selectedTransactionForMenu?.trackingNo} to the Web Portal and advance the workflow to Recommend Approval (Supervisor Review).
                  </Text>

                  <Text style={[styles.inputLabel, { color: colors.textDark, alignSelf: 'flex-start', marginBottom: 4 }]}>
                    Appraiser Notes / Remarks (Optional)
                  </Text>
                  <TextInput
                    style={[
                      styles.remarksInput,
                      {
                        backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                        borderColor: colors.border,
                        color: colors.textDark
                      }
                    ]}
                    placeholder="Enter remarks for supervisor..."
                    placeholderTextColor={colors.placeholder || '#94a3b8'}
                    value={transferRemarks}
                    onChangeText={setTransferRemarks}
                    multiline={true}
                  />

                  <View style={styles.forwardActionBtnRow}>
                    <TouchableOpacity
                      style={[styles.modalCancelBtn, { flex: 1, borderColor: colors.border }]}
                      onPress={() => setForwardConfirmModalVisible(false)}
                      disabled={actionLoading}
                    >
                      <Text style={[styles.modalCancelBtnText, { color: colors.textMuted }]}>Cancel</Text>
                    </TouchableOpacity>

                    <TouchableOpacity
                      style={[styles.confirmForwardBtn, { flex: 2 }]}
                      onPress={handleExecuteForwardToSupervisor}
                      disabled={actionLoading}
                    >
                      <Ionicons name="send" size={16} color="#ffffff" />
                      <Text style={styles.confirmForwardBtnText}>Sync & Forward</Text>
                    </TouchableOpacity>
                  </View>
                </>
              )}
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

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
  screenHeader: {
    marginBottom: 12
  },
  screenHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between'
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
  countBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12
  },
  countBadgeText: {
    fontSize: 11,
    fontWeight: '800'
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
  heroCard: {
    backgroundColor: '#ffffff',
    borderRadius: 18,
    padding: 18,
    marginBottom: 16,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  heroInfo: {
    marginBottom: 14
  },
  heroTitle: {
    fontSize: 20,
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
    gap: 12,
    paddingBottom: 30
  },
  transCard: {
    backgroundColor: '#ffffff',
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    borderBottomWidth: 1,
    borderBottomColor: '#f1f5f9',
    paddingBottom: 10,
    marginBottom: 10
  },
  trackingNoText: {
    fontSize: 16,
    fontWeight: '800',
    color: COLORS.primaryDark
  },
  pendingEditsBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fef3c7',
    borderWidth: 1,
    borderColor: '#fde68a',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
    gap: 3
  },
  pendingEditsBadgeText: {
    fontSize: 9,
    fontWeight: '800',
    color: '#b45309',
    letterSpacing: 0.3
  },
  multiPartitionBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#e0f2fe',
    borderWidth: 1,
    borderColor: '#bae6fd',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
    gap: 3
  },
  multiPartitionBadgeText: {
    fontSize: 9,
    fontWeight: '800',
    color: '#0369a1',
    letterSpacing: 0.3
  },
  typeBadgeText: {
    fontSize: 11,
    fontWeight: '600',
    color: COLORS.textMuted,
    marginTop: 2
  },
  threeDotsBtn: {
    width: 34,
    height: 34,
    borderRadius: 8,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center'
  },
  cardBody: {
    gap: 6
  },
  ownerText: {
    fontSize: 13,
    fontWeight: '800',
    color: COLORS.textDark
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  infoText: {
    fontSize: 12,
    color: COLORS.textMuted,
    fontWeight: '600'
  },
  timeText: {
    fontSize: 11,
    color: COLORS.textMuted,
    fontWeight: '600'
  },
  partitionChipRow: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: 6,
    marginVertical: 4
  },
  partitionChipLabel: {
    fontSize: 11,
    fontWeight: '700'
  },
  partitionChip: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6,
    borderWidth: 1
  },
  partitionChipText: {
    fontSize: 11,
    fontWeight: '800'
  },
  cardFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 12,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: '#f8fafc'
  },
  actionBtnDraft: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#f0f9ff',
    borderWidth: 1,
    borderColor: '#bae6fd',
    paddingVertical: 8,
    paddingHorizontal: 10,
    borderRadius: 8,
    gap: 5
  },
  actionBtnDraftText: {
    fontSize: 12,
    fontWeight: '800',
    color: '#0284c7'
  },
  actionBtnForward: {
    flex: 1.2,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#f0fdf4',
    borderWidth: 1,
    borderColor: '#bbf7d0',
    paddingVertical: 8,
    paddingHorizontal: 10,
    borderRadius: 8,
    gap: 5
  },
  actionBtnForwardText: {
    fontSize: 12,
    fontWeight: '800',
    color: '#059669'
  },
  actionBtnDiscard: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
    paddingHorizontal: 10,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#fee2e2',
    backgroundColor: '#fff5f5',
    gap: 4
  },
  actionBtnDiscardText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.accentRed
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
  },
  modalOverlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20
  },
  actionMenuCard: {
    width: '100%',
    maxWidth: 420,
    borderRadius: 20,
    borderWidth: 1,
    padding: 20
  },
  actionMenuHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    borderBottomWidth: 1,
    borderBottomColor: '#f1f5f9',
    paddingBottom: 12,
    marginBottom: 14
  },
  actionMenuTracking: {
    fontSize: 17,
    fontWeight: '800',
    letterSpacing: -0.3
  },
  actionMenuOwner: {
    fontSize: 13,
    marginTop: 2,
    fontWeight: '500'
  },
  actionMenuCloseBtn: {
    padding: 4
  },
  menuPendingBadge: {
    backgroundColor: '#fef3c7',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6
  },
  menuPendingBadgeText: {
    fontSize: 9,
    fontWeight: '800',
    color: '#b45309'
  },
  menuLoadingContainer: {
    paddingVertical: 36,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 12
  },
  menuLoadingText: {
    fontSize: 16,
    fontWeight: '700'
  },
  menuLoadingSubtext: {
    fontSize: 12,
    textAlign: 'center',
    paddingHorizontal: 16,
    lineHeight: 18
  },
  actionMenuList: {
    gap: 8,
    marginBottom: 10
  },
  menuActionItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 12,
    borderRadius: 12,
    borderWidth: 1,
    gap: 12
  },
  menuActionIconBox: {
    width: 38,
    height: 38,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center'
  },
  menuActionTextCol: {
    flex: 1
  },
  menuActionTitle: {
    fontSize: 14,
    fontWeight: '700'
  },
  menuActionSubtitle: {
    fontSize: 11,
    marginTop: 2
  },
  modalCancelBtn: {
    paddingVertical: 12,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 10,
    borderWidth: 1
  },
  modalCancelBtnText: {
    fontSize: 14,
    fontWeight: '700'
  },
  iconBubble: {
    width: 52,
    height: 52,
    borderRadius: 26,
    alignItems: 'center',
    justifyContent: 'center'
  },
  forwardModalTitle: {
    fontSize: 18,
    fontWeight: '800',
    textAlign: 'center',
    marginBottom: 6
  },
  forwardModalDescription: {
    fontSize: 13,
    lineHeight: 19,
    textAlign: 'center',
    marginBottom: 16,
    paddingHorizontal: 8
  },
  inputLabel: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.2
  },
  remarksInput: {
    width: '100%',
    minHeight: 70,
    borderRadius: 10,
    borderWidth: 1,
    padding: 10,
    fontSize: 13,
    textAlignVertical: 'top',
    marginBottom: 16
  },
  forwardActionBtnRow: {
    flexDirection: 'row',
    gap: 10,
    width: '100%'
  },
  confirmForwardBtn: {
    backgroundColor: '#059669',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 10,
    gap: 6
  },
  confirmForwardBtnText: {
    color: '#ffffff',
    fontSize: 13,
    fontWeight: '800'
  }
});
