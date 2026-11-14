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

    // Kind Badge Theme
    let kindColor = colors.primary;
    let kindIcon = 'business-outline';
    let kindLabel = 'BUILDING FAAS';

    if (pKind === 'Land') {
      kindColor = colors.accentGold;
      kindIcon = 'leaf-outline';
      kindLabel = 'LAND APPRAISAL';
    } else if (pKind === 'Machinery') {
      kindColor = '#6366f1';
      kindIcon = 'construct-outline';
      kindLabel = 'MACHINERY FAAS';
    }

    // Status Badge Theme
    let statusBg = isDarkMode ? '#064e3b' : '#dcfce7';
    let statusColor = isDarkMode ? '#34d399' : '#15803d';
    let statusIcon = 'checkmark-done-circle';
    let statusText = item.statusLabel || 'Transferred to Review';

    if (isDraft) {
      statusBg = isDarkMode ? '#78350f' : '#fef3c7';
      statusColor = isDarkMode ? '#fbbf24' : '#b45309';
      statusIcon = 'cloud-offline';
      statusText = item.statusLabel || 'Saved Offline Draft';
    } else if (!isTransferred) {
      statusBg = isDarkMode ? '#0f766e' : '#ccfbf1';
      statusColor = isDarkMode ? '#2dd4bf' : '#0f766e';
      statusIcon = 'cloud-done';
      statusText = item.statusLabel || 'Synced to Office';
    }

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
        {/* Top Badges Row */}
        <View style={styles.cardHeaderRow}>
          <View style={styles.badgesLeft}>
            <View style={[styles.kindBadge, { backgroundColor: kindColor + '18' }]}>
              <Ionicons name={kindIcon} size={13} color={kindColor} />
              <Text style={[styles.kindBadgeText, { color: kindColor }]}>
                {kindLabel}
              </Text>
            </View>

            <View style={[styles.partitionBadge, { backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9' }]}>
              <Text style={[styles.partitionBadgeText, { color: colors.textDark }]}>
                Part {item.partitionLetter || 'A'}
              </Text>
            </View>
          </View>

          <View style={[styles.statusBadge, { backgroundColor: statusBg }]}>
            <Ionicons name={statusIcon} size={13} color={statusColor} />
            <Text style={[styles.statusBadgeText, { color: statusColor }]}>
              {statusText}
            </Text>
          </View>
        </View>

        {/* Tracking No & Owner Name */}
        <View style={styles.cardMain}>
          <Text style={[styles.trackingNoText, { color: colors.primary }]}>
            {item.trackingNo || 'NO-TRACKING'}
          </Text>
          <Text style={[styles.ownerNameText, { color: colors.textDark }]} numberOfLines={1}>
            {item.ownerName || 'Unknown Owner'}
          </Text>
        </View>

        {/* Location Row */}
        <View style={styles.metaRow}>
          <Ionicons name="location-outline" size={14} color={colors.textMuted} />
          <Text style={[styles.metaText, { color: colors.textMuted }]} numberOfLines={1}>
            {item.streetLocation ? `${item.streetLocation}, ` : ''}
            {item.barangay ? `${item.barangay}, ` : ''}
            {item.cityMunicipality || 'Koronadal City'}
          </Text>
        </View>

        {/* Cadastral / Title Row */}
        <View style={styles.refRow}>
          <View style={styles.refItem}>
            <Text style={[styles.refLabel, { color: colors.textMuted }]}>ARP NO:</Text>
            <Text style={[styles.refValue, { color: colors.textDark }]}>{item.arpNo || 'N/A'}</Text>
          </View>
          <View style={styles.refDivider} />
          <View style={styles.refItem}>
            <Text style={[styles.refLabel, { color: colors.textMuted }]}>PIN:</Text>
            <Text style={[styles.refValue, { color: colors.textDark }]}>{item.pin || 'N/A'}</Text>
          </View>
        </View>

        {/* Valuation Box */}
        <View
          style={[
            styles.valuationContainer,
            { backgroundColor: isDarkMode ? '#0f172a' : '#f8fafc', borderColor: colors.border }
          ]}
        >
          <View style={styles.valCol}>
            <Text style={[styles.valLabel, { color: colors.textMuted }]}>MARKET VALUE</Text>
            <Text style={[styles.valValue, { color: colors.textDark }]}>
              {formatCurrency(item.marketValue)}
            </Text>
          </View>
          <View style={styles.valDivider} />
          <View style={styles.valCol}>
            <Text style={[styles.valLabel, { color: colors.textMuted }]}>ASSESSED VALUE</Text>
            <Text style={[styles.valValue, { color: colors.accentGreen }]}>
              {formatCurrency(item.assessedValue)}
            </Text>
          </View>
        </View>

        {/* Footer: Timestamp, Photos & Actions */}
        <View style={styles.cardFooter}>
          <View style={styles.footerInfo}>
            <View style={styles.timeRow}>
              <Ionicons name="time-outline" size={13} color={colors.textMuted} />
              <Text style={[styles.timeText, { color: colors.textMuted }]}>
                {formatFriendlyDate(item.savedAt)}
              </Text>
            </View>
            {item.attachmentsCount > 0 && (
              <View style={styles.photoCountRow}>
                <Ionicons name="images-outline" size={13} color={colors.accentGold} />
                <Text style={[styles.photoCountText, { color: colors.accentGold }]}>
                  {item.attachmentsCount} photo{item.attachmentsCount > 1 ? 's' : ''}
                </Text>
              </View>
            )}
          </View>

          <View style={styles.actionButtonsRow}>
            <TouchableOpacity
              style={[styles.actionBtn, styles.actionBtnOutline, { borderColor: colors.border }]}
              onPress={() => setSelectedSnapshot(item)}
              activeOpacity={0.7}
            >
              <Ionicons name="eye-outline" size={14} color={colors.textDark} />
              <Text style={[styles.actionBtnText, { color: colors.textDark }]}>Snapshot</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.actionBtn, styles.actionBtnPrimary, { backgroundColor: colors.primary }]}
              onPress={() => handleOpenForm(item)}
              activeOpacity={0.7}
            >
              <Ionicons name="document-text-outline" size={14} color="#ffffff" />
              <Text style={[styles.actionBtnText, { color: '#ffffff' }]}>View Form</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.iconBtn, { backgroundColor: isDarkMode ? '#450a0a' : '#fee2e2' }]}
              onPress={() => handlePromptDelete(item)}
              activeOpacity={0.7}
            >
              <Ionicons name="trash-outline" size={15} color={colors.accentRed} />
            </TouchableOpacity>
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
    borderRadius: 12,
    borderWidth: 1,
    padding: 14,
    marginBottom: 12
  },
  cardHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 10
  },
  badgesLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  kindBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    gap: 4
  },
  kindBadgeText: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.3
  },
  partitionBadge: {
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6
  },
  partitionBadgeText: {
    fontSize: 11,
    fontWeight: '700'
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    gap: 4
  },
  statusBadgeText: {
    fontSize: 11,
    fontWeight: '700'
  },
  cardMain: {
    marginBottom: 6
  },
  trackingNoText: {
    fontSize: 12,
    fontFamily: 'monospace',
    fontWeight: '800',
    marginBottom: 2
  },
  ownerNameText: {
    fontSize: 16,
    fontWeight: '800'
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    marginBottom: 8
  },
  metaText: {
    fontSize: 12,
    fontWeight: '500'
  },
  refRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 10
  },
  refItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  refLabel: {
    fontSize: 10,
    fontWeight: '700'
  },
  refValue: {
    fontSize: 11,
    fontWeight: '600'
  },
  refDivider: {
    width: 1,
    height: 10,
    backgroundColor: '#94a3b8',
    marginHorizontal: 8
  },
  valuationContainer: {
    flexDirection: 'row',
    borderRadius: 8,
    borderWidth: 1,
    paddingVertical: 8,
    paddingHorizontal: 12,
    marginBottom: 12
  },
  valCol: {
    flex: 1
  },
  valLabel: {
    fontSize: 9,
    fontWeight: '800',
    letterSpacing: 0.5,
    marginBottom: 2
  },
  valValue: {
    fontSize: 13,
    fontWeight: '800'
  },
  valDivider: {
    width: 1,
    backgroundColor: '#cbd5e1',
    marginHorizontal: 12
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: '#e2e8f018',
    paddingTop: 10
  },
  footerInfo: {
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
  photoCountRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    marginTop: 2
  },
  photoCountText: {
    fontSize: 11,
    fontWeight: '700'
  },
  actionButtonsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  actionBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 6,
    gap: 4
  },
  actionBtnOutline: {
    borderWidth: 1
  },
  actionBtnPrimary: {},
  actionBtnText: {
    fontSize: 11,
    fontWeight: '700'
  },
  iconBtn: {
    padding: 6,
    borderRadius: 6
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
