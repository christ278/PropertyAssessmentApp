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

  const handleOpenFormFromMenu = (trans) => {
    setThreeDotsModalVisible(false);
    if (!trans) return;
    const targetItem = trans.transItem || {
      TrackingNo: trans.trackingNo,
      FormworkflowId: trans.formWorkflowId,
      TransactionTypeId: trans.transactionTypeId,
      Kind: trans.kind
    };
    const pKind = trans.kind || targetItem.Kind || 'Building';
    const pTypeId = trans.transactionTypeId || (pKind === 'Land' ? 1 : pKind === 'Machinery' ? 3 : 2);
    const firstPart = trans.partitions && trans.partitions[0];
    const navParams = {
      trackingNo: trans.trackingNo,
      formWorkflowId: trans.formWorkflowId,
      partitionIndex: firstPart ? firstPart.index : 0,
      partitionLetter: firstPart ? firstPart.letter : 'A',
      transactionTypeId: pTypeId,
      partitionKind: pKind
    };
    if (pTypeId === 1 || pKind === 'Land') {
      navigation.navigate('LandForm', navParams);
    } else if (pTypeId === 3 || pKind === 'Machinery') {
      navigation.navigate('MachineryForm', navParams);
    } else {
      navigation.navigate('BuildingForm', navParams);
    }
  };

  const renderTransactionCard = ({ item }) => {
    const isThisSyncing = syncingKey === item.key;
    const isLand = item.type === 'UPDATE_LAND' || (item.kind || '').toLowerCase().includes('land');
    const isMach = item.type === 'UPDATE_MACHINERY' || (item.kind || '').toLowerCase().includes('machinery');
    const kindLabel = isLand ? 'Land' : isMach ? 'Machinery' : 'Building';
    const kindColor = isLand ? (isDarkMode ? '#2dd4bf' : '#0f766e') :
                      isMach ? (isDarkMode ? '#818cf8' : '#4f46e5') :
                      (isDarkMode ? '#fbbf24' : '#d97706');
    const kindBg = isLand ? (isDarkMode ? '#134e4a44' : '#ccfbf1') :
                   isMach ? (isDarkMode ? '#312e8144' : '#e0e7ff') :
                   (isDarkMode ? '#78350f44' : '#fef3c7');
    const kindIcon = isLand ? 'leaf-outline' : isMach ? 'construct-outline' : 'business-outline';

    const isInspection = (item.trackingNo || '').toUpperCase().startsWith('RFI-') ||
                         (item.moduleLabel && item.moduleLabel.toLowerCase().includes('inspection'));
    const moduleLabel = isInspection ? 'Inspection Request' : 'Property Assessment';
    const moduleIcon = isInspection ? 'search-circle-outline' : 'document-text-outline';
    const moduleColor = isInspection ? (isDarkMode ? '#60a5fa' : '#2563eb') : (isDarkMode ? '#a78bfa' : '#7c3aed');
    const moduleBg = isInspection ? (isDarkMode ? '#1e3a8a33' : '#eff6ff') : (isDarkMode ? '#4c1d9533' : '#f5f3ff');

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
        {/* Card Header: Tracking No + Queue Status & 3-Dots */}
        <View style={styles.cardHeader}>
          <Text style={[styles.trackingNoText, { color: colors.textDark }]} numberOfLines={1}>
            {item.trackingNo}
          </Text>

          <View style={styles.cardHeaderRight}>
            <View style={styles.pendingEditsBadge}>
              <Ionicons name="cloud-upload" size={10} color="#b45309" />
              <Text style={styles.pendingEditsBadgeText}>
                {item.partitions.length} {item.partitions.length === 1 ? 'EDIT' : 'EDITS'}
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
              onPress={(e) => {
                e.stopPropagation();
                handleOpenMenu(item);
              }}
              hitSlop={{ top: 12, bottom: 12, left: 12, right: 12 }}
              disabled={isThisSyncing}
            >
              <Ionicons name="ellipsis-vertical" size={17} color={colors.textDark} />
            </TouchableOpacity>
          </View>
        </View>

        {/* Workflow & Property Hierarchy Indicators: [Inspection Request / Property Assessment] > [Land / Building / Machinery] */}
        <View style={styles.categoryBadgeRow}>
          <View style={[styles.modulePill, { backgroundColor: moduleBg }]}>
            <Ionicons name={moduleIcon} size={12} color={moduleColor} />
            <Text style={[styles.modulePillText, { color: moduleColor }]}>{moduleLabel}</Text>
          </View>

          <Ionicons name="chevron-forward" size={11} color={colors.textMuted} />

          <View style={[styles.kindPill, { backgroundColor: kindBg }]}>
            <Ionicons name={kindIcon} size={11} color={kindColor} />
            <Text style={[styles.kindPillText, { color: kindColor }]}>{kindLabel}</Text>
          </View>
        </View>

        {/* Card Body: Owner, Location & Partition Chips */}
        <View style={styles.cardBody}>
          <Text style={[styles.ownerText, { color: colors.textDark }]} numberOfLines={1}>
            {item.owner}
          </Text>

          {item.location ? (
            <View style={styles.metaRow}>
              <Ionicons name="location-outline" size={12} color={colors.textMuted} />
              <Text style={[styles.metaText, { color: colors.textMuted }]} numberOfLines={1}>
                {item.location}
              </Text>
            </View>
          ) : null}

          {/* Partitions with offline edits as clean visual chips */}
          <View style={styles.partitionChipRow}>
            <Text style={[styles.partitionChipLabel, { color: colors.textMuted }]}>
              Partitions:
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
        </View>

        {/* Card Footer: Timestamp & Syncing State */}
        <View style={[styles.cardFooter, { borderTopColor: isDarkMode ? '#1e293b' : '#f1f5f9' }]}>
          <View style={styles.timeRow}>
            <Ionicons name="time-outline" size={12} color={colors.textMuted} />
            <Text style={[styles.timeText, { color: colors.textMuted }]}>
              Modified {new Date(item.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
            </Text>
          </View>

          {isThisSyncing ? (
            <View style={styles.syncingRow}>
              <ActivityIndicator size="small" color="#0284c7" />
              <Text style={styles.syncingText}>
                Syncing...
              </Text>
            </View>
          ) : (
            <Text style={[styles.timeText, { color: colors.textMuted }]}>
              Tap ⋮ for sync & actions
            </Text>
          )}
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
                {/* Option 0: Open & Inspect Partitions */}
                <TouchableOpacity
                  style={[
                    styles.menuActionItem,
                    {
                      backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                      borderColor: colors.border
                    }
                  ]}
                  onPress={() => selectedTransactionForMenu && handleOpenFormFromMenu(selectedTransactionForMenu)}
                >
                  <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#334155' : '#f1f5f9' }]}>
                    <Ionicons name="folder-open-outline" size={20} color={colors.primary} />
                  </View>
                  <View style={styles.menuActionTextCol}>
                    <Text style={[styles.menuActionTitle, { color: colors.textDark }]}>
                      Open & Inspect Partitions
                    </Text>
                    <Text style={[styles.menuActionSubtitle, { color: colors.textMuted }]}>
                      Review field appraisal edits before syncing
                    </Text>
                  </View>
                  <Ionicons name="chevron-forward" size={16} color={colors.textMuted} />
                </TouchableOpacity>

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
    padding: 14,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: '#f1f5f9',
    paddingBottom: 8,
    marginBottom: 8
  },
  cardHeaderLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flex: 1,
    marginRight: 6
  },
  cardHeaderRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  categoryBadgeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginBottom: 8,
    flexWrap: 'wrap'
  },
  modulePill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 7,
    paddingVertical: 2.5,
    borderRadius: 6
  },
  modulePillText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.2
  },
  kindPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 7,
    paddingVertical: 2.5,
    borderRadius: 6
  },
  kindPillText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.2
  },
  trackingNoText: {
    fontSize: 14,
    fontWeight: '800'
  },
  pendingEditsBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fef3c7',
    borderWidth: 1,
    borderColor: '#fde68a',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 5,
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
    width: 32,
    height: 32,
    borderRadius: 8,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center'
  },
  cardBody: {
    gap: 4
  },
  ownerText: {
    fontSize: 15,
    fontWeight: '800',
    marginBottom: 2
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5
  },
  metaText: {
    fontSize: 12,
    fontWeight: '500',
    flex: 1
  },
  timeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  timeText: {
    fontSize: 11,
    fontWeight: '500'
  },
  partitionChipRow: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: 6,
    marginTop: 4
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
    justifyContent: 'space-between',
    gap: 8,
    marginTop: 10,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#f1f5f9'
  },
  syncingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  syncingText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#0284c7'
  },
  quickSyncBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: 6,
    gap: 4
  },
  quickSyncBtnText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#ffffff'
  },
  pendingEditsFooterBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fef3c7',
    borderWidth: 1,
    borderColor: '#fde68a',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    gap: 4
  },
  pendingEditsFooterBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#b45309'
  },
  openMenuFooterBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: 6,
    borderWidth: 1,
    gap: 4
  },
  openMenuFooterText: {
    fontSize: 11,
    fontWeight: '700'
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
