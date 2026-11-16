import React, { useState, useMemo, useContext } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  Modal,
  ScrollView,
  RefreshControl
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../context/ThemeContext';
import { SyncContext } from '../context/SyncContext';
import { HeaderBar } from '../components/HeaderBar';
import { ActionModal } from '../components/ActionModal';
import { COLORS } from '../constants/theme';

const formatCurrency = (val) => {
  const num = parseFloat(val);
  if (isNaN(num)) return '₱0.00';
  return '₱' + num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

const formatFriendlyDate = (dateString) => {
  if (!dateString) return 'Date not recorded';
  const parsed = new Date(dateString);
  if (isNaN(parsed.getTime())) return String(dateString);
  return parsed.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
};

export const HistoryScreen = ({ navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const {
    historyRecords,
    historyCount,
    removeHistoryRecord,
    clearHistoryRecords
  } = useContext(SyncContext);

  const [searchQuery, setSearchQuery] = useState('');
  const [activeFilter, setActiveFilter] = useState('ALL');
  const [refreshing, setRefreshing] = useState(false);
  const [selectedSnapshot, setSelectedSnapshot] = useState(null);
  const [selectedRecordForMenu, setSelectedRecordForMenu] = useState(null);
  const [threeDotsModalVisible, setThreeDotsModalVisible] = useState(false);
  const [actionModal, setActionModal] = useState({
    visible: false,
    title: '',
    message: '',
    icon: 'information-circle-outline',
    iconType: 'primary',
    buttons: []
  });

  const showModal = (cfg) => {
    setActionModal({
      visible: true,
      title: cfg.title || '',
      message: cfg.message || '',
      icon: cfg.icon || 'information-circle-outline',
      iconType: cfg.iconType || 'primary',
      buttons: cfg.buttons || [{ text: 'OK', onPress: () => setActionModal(prev => ({ ...prev, visible: false })) }]
    });
  };

  const closeModal = () => {
    setActionModal(prev => ({ ...prev, visible: false }));
  };

  // Compute category counts
  const counts = useMemo(() => {
    const list = historyRecords || [];
    let land = 0;
    let bldg = 0;
    let mach = 0;
    let transferred = 0;
    let synced = 0;
    let drafts = 0;

    for (const r of list) {
      const k = (r.propertyKind || '').toLowerCase();
      if (k === 'land') land++;
      else if (k === 'machinery') mach++;
      else bldg++;

      const act = (r.actionType || '').toUpperCase();
      if (act === 'TRANSFERRED') transferred++;
      else if (act === 'SYNCED') synced++;
      else drafts++;
    }

    return {
      all: list.length,
      land,
      building: bldg,
      machinery: mach,
      transferred,
      synced,
      drafts
    };
  }, [historyRecords]);

  // Filter and search
  const filteredRecords = useMemo(() => {
    const list = historyRecords || [];
    const query = searchQuery.trim().toLowerCase();

    return list.filter(item => {
      // 1. Tab filter
      if (activeFilter === 'LAND' && (item.propertyKind || '').toLowerCase() !== 'land') return false;
      if (activeFilter === 'BUILDING' && (item.propertyKind || '').toLowerCase() !== 'building') return false;
      if (activeFilter === 'MACHINERY' && (item.propertyKind || '').toLowerCase() !== 'machinery') return false;
      if (activeFilter === 'TRANSFERRED' && (item.actionType || '').toUpperCase() !== 'TRANSFERRED') return false;
      if (activeFilter === 'SYNCED' && (item.actionType || '').toUpperCase() !== 'SYNCED') return false;
      if (activeFilter === 'DRAFT' && !['DRAFT', 'DRAFT_TRANSFER'].includes((item.actionType || '').toUpperCase())) return false;

      // 2. Search query filter
      if (!query) return true;
      const track = (item.trackingNo || '').toLowerCase();
      const owner = (item.ownerName || '').toLowerCase();
      const arp = (item.arpNo || '').toLowerCase();
      const pin = (item.pin || '').toLowerCase();
      const dec = (item.declarationNo || '').toLowerCase();
      const brgy = (item.barangay || '').toLowerCase();
      const kind = (item.propertyKind || '').toLowerCase();

      return (
        track.includes(query) ||
        owner.includes(query) ||
        arp.includes(query) ||
        pin.includes(query) ||
        dec.includes(query) ||
        brgy.includes(query) ||
        kind.includes(query)
      );
    });
  }, [historyRecords, activeFilter, searchQuery]);

  const handleOpenForm = (item) => {
    const pKind = item.propertyKind || 'Building';
    const navParams = {
      trackingNo: item.trackingNo,
      formWorkflowId: item.formWorkflowId,
      partitionIndex: item.partitionIndex || 0,
      partitionLetter: item.partitionLetter || 'A',
      partitionKind: pKind,
      fromHistory: true
    };

    if (pKind === 'Land') {
      navigation.navigate('LandForm', navParams);
    } else if (pKind === 'Machinery') {
      navigation.navigate('MachineryForm', navParams);
    } else {
      navigation.navigate('BuildingForm', navParams);
    }
  };

  const handlePromptDelete = (item) => {
    showModal({
      title: 'Delete History Record',
      message: `Are you sure you want to delete the local history snapshot for "${item.trackingNo}" (Partition ${item.partitionLetter})?\n\nThis will permanently remove it from your device storage.`,
      icon: 'trash-outline',
      iconType: 'danger',
      buttons: [
        {
          text: 'Delete',
          style: 'danger',
          icon: 'trash-outline',
          onPress: async () => {
            closeModal();
            await removeHistoryRecord(item.id || item.recordKey);
          }
        },
        { text: 'Cancel', style: 'cancel', onPress: closeModal }
      ]
    });
  };

  const handlePromptClearAll = () => {
    if (!historyRecords || historyRecords.length === 0) return;
    showModal({
      title: 'Clear All History',
      message: `Are you sure you want to clear all ${historyRecords.length} appraisal history record(s) saved on this phone?\n\nThis action cannot be undone.`,
      icon: 'warning-outline',
      iconType: 'danger',
      buttons: [
        {
          text: 'Clear All',
          style: 'danger',
          icon: 'trash-outline',
          onPress: async () => {
            closeModal();
            await clearHistoryRecords();
          }
        },
        { text: 'Cancel', style: 'cancel', onPress: closeModal }
      ]
    });
  };

  const renderFilterTab = (key, label, count) => {
    const isActive = activeFilter === key;
    return (
      <TouchableOpacity
        key={key}
        style={[
          styles.filterChip,
          isActive && styles.filterChipActive,
          {
            backgroundColor: isActive ? colors.primary : colors.cardBg,
            borderColor: isActive ? colors.primary : colors.border
          }
        ]}
        onPress={() => setActiveFilter(key)}
        activeOpacity={0.7}
      >
        <Text
          style={[
            styles.filterChipText,
            { color: isActive ? '#ffffff' : colors.textDark }
          ]}
        >
          {label} {count !== undefined ? `(${count})` : ''}
        </Text>
      </TouchableOpacity>
    );
  };

  const renderHistoryCard = ({ item }) => {
    const pKind = item.propertyKind || 'Building';
    const isTransferred = (item.actionType || '').toUpperCase() === 'TRANSFERRED';
    const isDraft = ['DRAFT', 'DRAFT_TRANSFER'].includes((item.actionType || '').toUpperCase());

    const isLand = pKind === 'Land';
    const isMach = pKind === 'Machinery';
    const kindLabel = isLand ? 'Land' : isMach ? 'Machinery' : 'Building';
    const kindColor = isLand ? (isDarkMode ? '#2dd4bf' : '#0f766e') :
                      isMach ? (isDarkMode ? '#818cf8' : '#4f46e5') :
                      (isDarkMode ? '#fbbf24' : '#d97706');
    const kindBg = isLand ? (isDarkMode ? '#134e4a44' : '#ccfbf1') :
                   isMach ? (isDarkMode ? '#312e8144' : '#e0e7ff') :
                   (isDarkMode ? '#78350f44' : '#fef3c7');
    const kindIcon = isLand ? 'leaf-outline' : isMach ? 'construct-outline' : 'business-outline';

    const isInspection = (item.trackingNo || '').toUpperCase().startsWith('RFI-') ||
                         (item.requestType && item.requestType.toLowerCase().includes('inspection'));
    const moduleLabel = isInspection ? 'Inspection Request' : 'Property Assessment';
    const moduleIcon = isInspection ? 'search-circle-outline' : 'document-text-outline';
    const moduleColor = isInspection ? (isDarkMode ? '#60a5fa' : '#2563eb') : (isDarkMode ? '#a78bfa' : '#7c3aed');
    const moduleBg = isInspection ? (isDarkMode ? '#1e3a8a33' : '#eff6ff') : (isDarkMode ? '#4c1d9533' : '#f5f3ff');

    // Status Badge Theme (Only shown for Transferred or Synced)
    let statusBg = null;
    let statusColor = null;
    let statusIcon = null;
    let statusText = null;

    if (isTransferred) {
      statusBg = isDarkMode ? '#064e3b' : '#dcfce7';
      statusColor = isDarkMode ? '#34d399' : '#15803d';
      statusIcon = 'checkmark-done-circle';
      statusText = 'Transferred';
    } else if (!isDraft) {
      statusBg = isDarkMode ? '#0f766e' : '#ccfbf1';
      statusColor = isDarkMode ? '#2dd4bf' : '#0f766e';
      statusIcon = 'cloud-done';
      statusText = 'Synced';
    }

    const fullLocation = [
      item.streetLocation,
      item.barangay,
      item.cityMunicipality || 'Koronadal City'
    ].filter(Boolean).join(', ');

    return (
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
        {/* Card Header: Tracking No + Status & 3-Dots */}
        <View style={styles.cardHeader}>
          <Text style={[styles.trackingNoText, { color: colors.textDark }]} numberOfLines={1}>
            {item.trackingNo || 'Unassigned'}
          </Text>

          <View style={styles.headerRightActions}>
            {statusText ? (
              <View style={[styles.statusBadge, { backgroundColor: statusBg }]}>
                <Ionicons name={statusIcon} size={11} color={statusColor} />
                <Text style={[styles.statusBadgeText, { color: statusColor }]}>
                  {statusText}
                </Text>
              </View>
            ) : null}

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
                setSelectedRecordForMenu(item);
                setThreeDotsModalVisible(true);
              }}
              hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
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

          <View style={[styles.partitionBadge, { backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9' }]}>
            <Text style={[styles.partitionBadgeText, { color: colors.textMuted }]}>
              PART {item.partitionLetter || 'A'}
            </Text>
          </View>
        </View>

        {/* Card Body: Owner & Clean Secondary Metadata */}
        <View style={styles.cardBody}>
          <Text style={[styles.ownerText, { color: colors.textDark }]} numberOfLines={1}>
            {item.ownerName || 'Unknown Owner'}
          </Text>

          {fullLocation ? (
            <View style={styles.metaRow}>
              <Ionicons name="location-outline" size={12} color={colors.textMuted} />
              <Text style={[styles.metaText, { color: colors.textMuted }]} numberOfLines={1}>
                {fullLocation}
              </Text>
            </View>
          ) : null}

          {(item.arpNo || item.pin || item.declarationNo) ? (
            <View style={styles.metaRow}>
              <Ionicons name="pricetag-outline" size={12} color={colors.textMuted} />
              <Text style={[styles.metaText, { color: colors.textMuted }]} numberOfLines={1}>
                {[
                  item.arpNo ? `ARP: ${item.arpNo}` : null,
                  item.pin ? `PIN: ${item.pin}` : null,
                  item.declarationNo && item.declarationNo !== 'N/A' ? `TDN: ${item.declarationNo}` : null
                ].filter(Boolean).join(' • ')}
              </Text>
            </View>
          ) : null}

          {/* Sleek Dual Valuation Micro-Tiles */}
          <View style={styles.metricTileRow}>
            <View
              style={[
                styles.metricTile,
                {
                  backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                  borderColor: colors.border
                }
              ]}
            >
              <Text style={[styles.metricLabel, { color: colors.textMuted }]}>MARKET VALUE</Text>
              <Text style={[styles.metricValue, { color: colors.textDark }]} numberOfLines={1}>
                {formatCurrency(item.marketValue)}
              </Text>
            </View>

            <View
              style={[
                styles.metricTile,
                {
                  backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                  borderColor: colors.border
                }
              ]}
            >
              <Text style={[styles.metricLabel, { color: colors.textMuted }]}>ASSESSED VALUE</Text>
              <Text style={[styles.metricValue, { color: colors.accentGreen }]} numberOfLines={1}>
                {formatCurrency(item.assessedValue)}
              </Text>
            </View>
          </View>
        </View>

        {/* Card Footer: Timestamp & Photo indicator */}
        <View style={[styles.cardFooter, { borderTopColor: isDarkMode ? '#1e293b' : '#f1f5f9' }]}>
          <View style={styles.footerLeft}>
            <View style={styles.timeRow}>
              <Ionicons name="time-outline" size={12} color={colors.textMuted} />
              <Text style={[styles.timeText, { color: colors.textMuted }]} numberOfLines={1}>
                {formatFriendlyDate(item.savedAt)}
              </Text>
            </View>
            {item.attachmentsCount > 0 ? (
              <View style={[styles.photoCountBadge, { backgroundColor: isDarkMode ? '#78350f33' : '#fef3c7' }]}>
                <Ionicons name="images-outline" size={11} color={colors.accentGold} />
                <Text style={[styles.photoCountBadgeText, { color: colors.accentGold }]}>
                  {item.attachmentsCount} photo{item.attachmentsCount > 1 ? 's' : ''}
                </Text>
              </View>
            ) : null}
          </View>
        </View>
      </View>
    );
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.bgLight }]}>
      <HeaderBar
        navigation={navigation}
        onRefresh={() => {
          setRefreshing(true);
          setTimeout(() => setRefreshing(false), 300);
        }}
        isRefreshing={refreshing}
      />

      {/* Screen Title */}
      <View style={styles.screenHeader}>
        <View style={styles.screenHeaderRow}>
          <View style={{ flex: 1 }}>
            <Text style={[styles.screenTitle, { color: colors.textDark }]}>Appraisal History</Text>
            <Text style={[styles.screenSubtitle, { color: colors.textMuted }]}>
              Permanent Local Records Archive
            </Text>
          </View>
          <View style={[styles.countBadge, { backgroundColor: isDarkMode ? '#1e1b4b' : '#ede9fe' }]}>
            <Text style={[styles.countBadgeText, { color: '#8b5cf6' }]}>
              {historyRecords.length} Saved
            </Text>
          </View>
        </View>
      </View>

      {/* Top Search & Clear Bar */}
      <View style={[styles.topBar, { backgroundColor: colors.cardBg, borderBottomColor: colors.border }]}>
        <View
          style={[
            styles.searchBox,
            { backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9', borderColor: colors.border }
          ]}
        >
          <Ionicons name="search" size={17} color={colors.textMuted} />
          <TextInput
            style={[styles.searchInput, { color: colors.textDark }]}
            placeholder="Search tracking, owner, ARP, PIN, brgy..."
            placeholderTextColor={colors.placeholder}
            value={searchQuery}
            onChangeText={setSearchQuery}
          />
          {searchQuery ? (
            <TouchableOpacity onPress={() => setSearchQuery('')} hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}>
              <Ionicons name="close-circle" size={18} color={colors.textMuted} />
            </TouchableOpacity>
          ) : null}
        </View>

        {historyRecords.length > 0 && (
          <TouchableOpacity
            style={[styles.clearAllBtn, { backgroundColor: isDarkMode ? '#450a0a' : '#fee2e2' }]}
            onPress={handlePromptClearAll}
            activeOpacity={0.7}
          >
            <Ionicons name="trash-bin-outline" size={14} color={colors.accentRed} />
            <Text style={[styles.clearAllBtnText, { color: colors.accentRed }]}>Clear</Text>
          </TouchableOpacity>
        )}
      </View>

      {/* Filter Tabs Horizontal Scroll */}
      <View style={[styles.filterBar, { backgroundColor: colors.cardBg, borderBottomColor: colors.border }]}>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.filterScroll}>
          {renderFilterTab('ALL', 'All', counts.all)}
          {renderFilterTab('LAND', 'Land', counts.land)}
          {renderFilterTab('BUILDING', 'Building', counts.building)}
          {renderFilterTab('MACHINERY', 'Machinery', counts.machinery)}
          {renderFilterTab('TRANSFERRED', 'Transferred', counts.transferred)}
          {renderFilterTab('SYNCED', 'Synced', counts.synced)}
          {renderFilterTab('DRAFT', 'Drafts', counts.drafts)}
        </ScrollView>
      </View>

      {/* History Records List */}
      <FlatList
        data={filteredRecords}
        keyExtractor={(item) => item.id || item.recordKey || Math.random().toString()}
        renderItem={renderHistoryCard}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={() => {
              setRefreshing(true);
              setTimeout(() => setRefreshing(false), 300);
            }}
            colors={[colors.primary]}
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <View
              style={[
                styles.emptyIconBox,
                { backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9' }
              ]}
            >
              <Ionicons
                name={searchQuery ? 'search-outline' : 'time-outline'}
                size={44}
                color={colors.textMuted}
              />
            </View>
            <Text style={[styles.emptyTitle, { color: colors.textDark }]}>
              {searchQuery ? 'No Matching Records' : 'No Appraisal History Yet'}
            </Text>
            <Text style={[styles.emptySubtitle, { color: colors.textMuted }]}>
              {searchQuery
                ? `No appraisal records matched "${searchQuery}". Try searching with another keyword.`
                : 'Whenever you save an appraisal draft offline, sync live to office, or transfer to assessor review, a permanent snapshot will be preserved here on your phone.'}
            </Text>
          </View>
        }
      />

      {/* ========================================================================= */}
      {/* SNAPSHOT DETAIL MODAL                                                     */}
      {/* ========================================================================= */}
      <Modal
        visible={!!selectedSnapshot}
        transparent
        animationType="slide"
        onRequestClose={() => setSelectedSnapshot(null)}
      >
        <View style={[styles.modalOverlay, { backgroundColor: colors.modalOverlay }]}>
          <View style={[styles.modalContent, { backgroundColor: colors.cardBg }]}>
            {/* Modal Header */}
            <View style={[styles.modalHeader, { borderBottomColor: colors.border }]}>
              <View style={{ flex: 1 }}>
                <Text style={[styles.modalTitle, { color: colors.textDark }]}>
                  Appraisal Snapshot
                </Text>
                <Text style={[styles.modalSubtitle, { color: colors.textMuted }]}>
                  {selectedSnapshot?.trackingNo} • Partition {selectedSnapshot?.partitionLetter || 'A'}
                </Text>
              </View>
              <TouchableOpacity
                onPress={() => setSelectedSnapshot(null)}
                style={styles.modalCloseBtn}
                hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
              >
                <Ionicons name="close" size={22} color={colors.textDark} />
              </TouchableOpacity>
            </View>

            {/* Modal Body */}
            <ScrollView contentContainerStyle={styles.modalBody}>
              {selectedSnapshot && (
                <>
                  {/* Status & Timestamp Header Banner */}
                  <View
                    style={[
                      styles.snapshotBanner,
                      {
                        backgroundColor: (selectedSnapshot.actionType || '').toUpperCase() === 'TRANSFERRED'
                          ? (isDarkMode ? '#064e3b' : '#dcfce7')
                          : (isDarkMode ? '#0f766e' : '#ccfbf1')
                      }
                    ]}
                  >
                    <Ionicons
                      name={(selectedSnapshot.actionType || '').toUpperCase() === 'TRANSFERRED' ? 'checkmark-circle' : 'cloud-done'}
                      size={18}
                      color={(selectedSnapshot.actionType || '').toUpperCase() === 'TRANSFERRED' ? '#16a34a' : '#0f766e'}
                    />
                    <View style={{ marginLeft: 8, flex: 1 }}>
                      <Text
                        style={[
                          styles.snapshotBannerTitle,
                          {
                            color: (selectedSnapshot.actionType || '').toUpperCase() === 'TRANSFERRED' ? '#16a34a' : '#0f766e'
                          }
                        ]}
                      >
                        {selectedSnapshot.statusLabel || 'Completed Field Assessment'}
                      </Text>
                      <Text style={[styles.snapshotBannerSubtitle, { color: colors.textMuted }]}>
                        Saved on {formatFriendlyDate(selectedSnapshot.savedAt)}
                      </Text>
                    </View>
                  </View>

                  {/* Section 1: Property Identification */}
                  <Text style={[styles.sectionHeading, { color: colors.primary }]}>
                    PROPERTY IDENTIFICATION
                  </Text>
                  <View style={[styles.detailTable, { borderColor: colors.border }]}>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Property Kind</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.propertyKind}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Tracking No</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark, fontFamily: 'monospace', fontWeight: '700' }]}>
                        {selectedSnapshot.trackingNo}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>ARP No</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.arpNo || 'N/A'}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>PIN</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.pin || 'N/A'}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>TDN No</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.declarationNo || 'N/A'}
                      </Text>
                    </View>
                  </View>

                  {/* Section 2: Owner & Location */}
                  <Text style={[styles.sectionHeading, { color: colors.primary }]}>
                    TAXPAYER &amp; LOCATION
                  </Text>
                  <View style={[styles.detailTable, { borderColor: colors.border }]}>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Owner / Declarant</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark, fontWeight: '700' }]}>
                        {selectedSnapshot.ownerName}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Street / Location</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.streetLocation || 'Not specified'}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Barangay</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.barangay || 'BRGY. ZONE I'}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>City / Municipality</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.cityMunicipality || 'Koronadal City'}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Province</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.province || 'South Cotabato'}
                      </Text>
                    </View>
                  </View>

                  {/* Section 3: Valuations */}
                  <Text style={[styles.sectionHeading, { color: colors.primary }]}>
                    FINANCIAL VALUATION
                  </Text>
                  <View style={[styles.detailTable, { borderColor: colors.border }]}>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Total Market Value</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark, fontWeight: '700' }]}>
                        {formatCurrency(selectedSnapshot.marketValue)}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Total Assessed Value</Text>
                      <Text style={[styles.detailTableValue, { color: colors.accentGreen, fontWeight: '700' }]}>
                        {formatCurrency(selectedSnapshot.assessedValue)}
                      </Text>
                    </View>
                  </View>

                  {/* Section 4: Attachments & Sketch */}
                  <Text style={[styles.sectionHeading, { color: colors.primary }]}>
                    ATTACHMENTS &amp; SKETCH
                  </Text>
                  <View style={[styles.detailTable, { borderColor: colors.border }]}>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Floor Plan / Sketch</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.sketchUrl ? 'Included on device' : 'None'}
                      </Text>
                    </View>
                    <View style={styles.detailTableRow}>
                      <Text style={[styles.detailTableLabel, { color: colors.textMuted }]}>Photos Attached</Text>
                      <Text style={[styles.detailTableValue, { color: colors.textDark }]}>
                        {selectedSnapshot.attachmentsCount || 0} photo(s)
                      </Text>
                    </View>
                  </View>
                </>
              )}
            </ScrollView>

            {/* Modal Actions */}
            <View style={[styles.modalActionsRow, { borderTopColor: colors.border }]}>
              <TouchableOpacity
                style={[styles.modalBtn, styles.modalBtnSecondary, { borderColor: colors.border }]}
                onPress={() => setSelectedSnapshot(null)}
              >
                <Text style={[styles.modalBtnText, { color: colors.textDark }]}>Close</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.modalBtn, styles.modalBtnPrimary, { backgroundColor: colors.primary }]}
                onPress={() => {
                  const target = selectedSnapshot;
                  setSelectedSnapshot(null);
                  if (target) handleOpenForm(target);
                }}
              >
                <Ionicons name="document-text-outline" size={16} color="#ffffff" style={{ marginRight: 6 }} />
                <Text style={[styles.modalBtnText, { color: '#ffffff' }]}>View Form Details</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>

      {/* 3-Dots Action Menu Modal */}
      <Modal
        visible={threeDotsModalVisible}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setThreeDotsModalVisible(false)}
      >
        <TouchableOpacity
          style={[styles.menuModalOverlay, { backgroundColor: colors.modalOverlay || 'rgba(0, 0, 0, 0.55)' }]}
          activeOpacity={1}
          onPress={() => setThreeDotsModalVisible(false)}
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
            <View style={[styles.actionMenuHeader, { borderBottomColor: isDarkMode ? '#334155' : '#f1f5f9' }]}>
              <View style={{ flex: 1, marginRight: 8 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                  <Text style={[styles.actionMenuTracking, { color: colors.textDark }]} numberOfLines={1}>
                    {selectedRecordForMenu?.trackingNo || 'History Record'}
                  </Text>
                  <View style={[styles.partitionBadge, { backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9' }]}>
                    <Text style={[styles.partitionBadgeText, { color: colors.textDark }]}>
                      Part {selectedRecordForMenu?.partitionLetter || 'A'}
                    </Text>
                  </View>
                </View>
                <Text style={[styles.actionMenuOwner, { color: colors.textMuted }]} numberOfLines={1}>
                  {selectedRecordForMenu?.ownerName || 'Unknown Owner'} • {selectedRecordForMenu?.propertyKind || 'Property'}
                </Text>
              </View>
              <TouchableOpacity
                onPress={() => setThreeDotsModalVisible(false)}
                style={styles.actionMenuCloseBtn}
                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
              >
                <Ionicons name="close" size={20} color={colors.textDark} />
              </TouchableOpacity>
            </View>

            <View style={styles.actionMenuList}>
              {/* Option 1: View Assessment Form */}
              <TouchableOpacity
                style={[
                  styles.menuActionItem,
                  {
                    backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                    borderColor: colors.border
                  }
                ]}
                onPress={() => {
                  const item = selectedRecordForMenu;
                  setThreeDotsModalVisible(false);
                  if (item) handleOpenForm(item);
                }}
              >
                <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#0369a133' : '#e0f2fe' }]}>
                  <Ionicons name="document-text" size={18} color={isDarkMode ? '#38bdf8' : '#0284c7'} />
                </View>
                <View style={styles.menuActionTextCol}>
                  <Text style={[styles.menuActionTitle, { color: colors.textDark }]}>
                    View Assessment Form
                  </Text>
                  <Text style={[styles.menuActionSubtitle, { color: colors.textMuted }]}>
                    Open full appraisal details & partition data in view mode
                  </Text>
                </View>
                <Ionicons name="chevron-forward" size={16} color={colors.textMuted} />
              </TouchableOpacity>

              {/* Option 2: Quick Snapshot Breakdown */}
              <TouchableOpacity
                style={[
                  styles.menuActionItem,
                  {
                    backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc',
                    borderColor: colors.border
                  }
                ]}
                onPress={() => {
                  const item = selectedRecordForMenu;
                  setThreeDotsModalVisible(false);
                  if (item) setSelectedSnapshot(item);
                }}
              >
                <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#064e3b33' : '#dcfce7' }]}>
                  <Ionicons name="eye" size={18} color={isDarkMode ? '#34d399' : '#059669'} />
                </View>
                <View style={styles.menuActionTextCol}>
                  <Text style={[styles.menuActionTitle, { color: colors.textDark }]}>
                    Quick Snapshot Breakdown
                  </Text>
                  <Text style={[styles.menuActionSubtitle, { color: colors.textMuted }]}>
                    Inspect property valuations, cadastral info, and specs
                  </Text>
                </View>
                <Ionicons name="chevron-forward" size={16} color={colors.textMuted} />
              </TouchableOpacity>

              {/* Option 3: Delete Record */}
              <TouchableOpacity
                style={[
                  styles.menuActionItem,
                  {
                    backgroundColor: isDarkMode ? '#450a0a22' : '#fef2f2',
                    borderColor: isDarkMode ? '#7f1d1d' : '#fecaca'
                  }
                ]}
                onPress={() => {
                  const item = selectedRecordForMenu;
                  setThreeDotsModalVisible(false);
                  if (item) handlePromptDelete(item);
                }}
              >
                <View style={[styles.menuActionIconBox, { backgroundColor: isDarkMode ? '#7f1d1d' : '#fee2e2' }]}>
                  <Ionicons name="trash-outline" size={18} color={colors.accentRed} />
                </View>
                <View style={styles.menuActionTextCol}>
                  <Text style={[styles.menuActionTitle, { color: colors.accentRed }]}>
                    Delete Record
                  </Text>
                  <Text style={[styles.menuActionSubtitle, { color: isDarkMode ? '#fca5a5' : '#b91c1c' }]}>
                    Permanently delete this local snapshot from device
                  </Text>
                </View>
                <Ionicons name="chevron-forward" size={16} color={colors.accentRed} />
              </TouchableOpacity>
            </View>
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Confirmation Modal */}
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
    flex: 1
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderBottomWidth: 1
  },
  searchBox: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 7,
    borderRadius: 8,
    borderWidth: 1
  },
  searchInput: {
    flex: 1,
    fontSize: 13,
    marginLeft: 6,
    padding: 0
  },
  clearAllBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 8,
    marginLeft: 8
  },
  clearAllBtnText: {
    fontSize: 12,
    fontWeight: '700',
    marginLeft: 4
  },
  filterBar: {
    borderBottomWidth: 1,
    paddingVertical: 8
  },
  filterScroll: {
    paddingHorizontal: 12,
    gap: 8
  },
  filterChip: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
    borderWidth: 1
  },
  filterChipActive: {
    borderColor: 'transparent'
  },
  filterChipText: {
    fontSize: 12,
    fontWeight: '700'
  },
  listContent: {
    padding: 12,
    paddingBottom: 40
  },
  card: {
    borderRadius: 14,
    borderWidth: 1,
    padding: 14,
    marginBottom: 12
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: '#f1f5f9',
    paddingBottom: 8,
    marginBottom: 8
  },
  cardHeaderLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    flex: 1,
    marginRight: 6
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
  partitionBadge: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 5
  },
  partitionBadgeText: {
    fontSize: 9,
    fontWeight: '800'
  },
  headerRightActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 3.5,
    borderRadius: 6,
    gap: 4
  },
  statusBadgeText: {
    fontSize: 10,
    fontWeight: '700'
  },
  threeDotsBtn: {
    width: 32,
    height: 32,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1
  },
  cardBody: {
    gap: 4,
    marginBottom: 6
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
  metricTileRow: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 6
  },
  metricTile: {
    flex: 1,
    paddingVertical: 6,
    paddingHorizontal: 10,
    borderRadius: 8,
    borderWidth: 1
  },
  metricLabel: {
    fontSize: 9,
    fontWeight: '800',
    letterSpacing: 0.5
  },
  metricValue: {
    fontSize: 13,
    fontWeight: '800',
    marginTop: 2
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#f1f5f9'
  },
  footerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flex: 1,
    marginRight: 6
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
  photoCountBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 5
  },
  photoCountBadgeText: {
    fontSize: 10,
    fontWeight: '700'
  },
  openFormBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 6,
    gap: 3
  },
  openFormBtnText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#ffffff'
  },
  menuModalOverlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20
  },
  actionMenuCard: {
    width: '100%',
    maxWidth: 420,
    borderRadius: 20,
    padding: 20,
    borderWidth: 1
  },
  actionMenuHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    paddingBottom: 14,
    marginBottom: 14,
    borderBottomWidth: 1
  },
  actionMenuTracking: {
    fontSize: 16,
    fontWeight: '800'
  },
  actionMenuOwner: {
    fontSize: 12,
    marginTop: 2,
    fontWeight: '500'
  },
  actionMenuCloseBtn: {
    padding: 4
  },
  actionMenuList: {
    gap: 8
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
    fontSize: 13,
    fontWeight: '700',
    marginBottom: 2
  },
  menuActionSubtitle: {
    fontSize: 11,
    lineHeight: 15
  },
  emptyContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 60,
    paddingHorizontal: 24
  },
  emptyIconBox: {
    width: 80,
    height: 80,
    borderRadius: 40,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 16
  },
  emptyTitle: {
    fontSize: 17,
    fontWeight: '800',
    marginBottom: 6,
    textAlign: 'center'
  },
  emptySubtitle: {
    fontSize: 13,
    lineHeight: 19,
    textAlign: 'center',
    maxWidth: 320
  },
  modalOverlay: {
    flex: 1,
    justifyContent: 'flex-end'
  },
  modalContent: {
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    maxHeight: '85%',
    paddingBottom: 24
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderBottomWidth: 1
  },
  modalTitle: {
    fontSize: 17,
    fontWeight: '800'
  },
  modalSubtitle: {
    fontSize: 12,
    marginTop: 2
  },
  modalCloseBtn: {
    padding: 4
  },
  modalBody: {
    padding: 16
  },
  snapshotBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 12,
    borderRadius: 8,
    marginBottom: 16
  },
  snapshotBannerTitle: {
    fontSize: 13,
    fontWeight: '800'
  },
  snapshotBannerSubtitle: {
    fontSize: 11,
    marginTop: 2
  },
  sectionHeading: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
    marginTop: 10,
    marginBottom: 6
  },
  detailTable: {
    borderRadius: 8,
    borderWidth: 1,
    overflow: 'hidden',
    marginBottom: 10
  },
  detailTableRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: '#94a3b830'
  },
  detailTableLabel: {
    fontSize: 12,
    fontWeight: '600'
  },
  detailTableValue: {
    fontSize: 12,
    fontWeight: '500',
    textAlign: 'right',
    maxWidth: '60%'
  },
  modalActionsRow: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingTop: 12,
    borderTopWidth: 1,
    gap: 10
  },
  modalBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 12,
    borderRadius: 8
  },
  modalBtnSecondary: {
    borderWidth: 1
  },
  modalBtnPrimary: {},
  modalBtnText: {
    fontSize: 13,
    fontWeight: '700'
  },
  screenHeader: {
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 4
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
});
