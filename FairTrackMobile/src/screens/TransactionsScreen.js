import React, { useState, useEffect, useContext } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  TextInput,
  RefreshControl,
  ActivityIndicator,
  Alert
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { MobileApi } from '../api/mobileApi';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { HeaderBar } from '../components/HeaderBar';
import { StatusBadge } from '../components/StatusBadge';
import { COLORS, SHADOWS } from '../constants/theme';

export const TransactionsScreen = ({ navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const {

    downloadSingleTransaction,
    downloadAllTransactions,
    isTransactionDownloaded,
    getOfflineTransactions,
    downloadedForms,
    downloadedCount,
    isDownloading
  } = useContext(SyncContext);

  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState(0); // 0 = All, 4 = Downloaded Only, 1 = Land, 2 = Building, 3 = Machinery
  const [downloadingId, setDownloadingId] = useState(null);
  const [isOffline, setIsOffline] = useState(false);

  useEffect(() => {
    fetchTransactions();
  }, []);

  const loadOfflineData = async () => {
    if (typeof getOfflineTransactions === 'function') {
      const offlineList = await getOfflineTransactions();
      setTransactions(offlineList || []);
    }
  };

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const res = await MobileApi.getTransactions(
        (selectedType > 0 && selectedType !== 4) ? selectedType : null,
        searchQuery.trim(),
        1,
        50
      );
      if (res && res.success) {
        setIsOffline(false);
        const list = res.data || [];
        setTransactions(list);
      }
    } catch (err) {
      console.warn('Network unavailable, falling back to downloaded offline transactions:', err);
      setIsOffline(true);
      await loadOfflineData();
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  // Instant offline-compatible filtering and search
  const displayTransactions = React.useMemo(() => {
    let list = transactions;
    if (selectedType === 4) {
      // Downloaded only
      list = list.filter(t => isTransactionDownloaded(t.TrackingNo, t.FormworkflowId));
    } else if (selectedType > 0) {
      list = list.filter(t => t.TransactionTypeId === selectedType);
    }
    if (searchQuery.trim()) {
      const q = searchQuery.trim().toLowerCase();
      list = list.filter(t =>
        (t.TrackingNo || '').toLowerCase().includes(q) ||
        (t.Owner || '').toLowerCase().includes(q) ||
        (t.ARPNo || '').toLowerCase().includes(q) ||
        (t.Requestor || '').toLowerCase().includes(q)
      );
    }
    return list;
  }, [transactions, selectedType, searchQuery, downloadedForms]);

  const handleDownloadSingle = async (item) => {
    setDownloadingId(item.FormworkflowId);
    const res = await downloadSingleTransaction(item);
    setDownloadingId(null);

    if (res.success) {
      Alert.alert('Downloaded', res.message);
    } else {
      Alert.alert('Download Failed', res.message);
    }
  };

  const handleDownloadAll = async () => {
    if (transactions.length === 0) {
      Alert.alert('No Transactions', 'There are no assigned transactions in your list to download.');
      return;
    }

    Alert.alert(
      'Download All for Offline Field Work',
      `Download ${transactions.length} assigned property transactions to your phone storage for offline inspection?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Download All',
          onPress: async () => {
            const res = await downloadAllTransactions(transactions);
            if (res.success) {
              Alert.alert(
                'Download Complete',
                `Successfully downloaded ${res.successCount} of ${res.total} property forms to your phone. Ready for offline inspection!`
              );
            } else {
              Alert.alert('Download Incomplete', res.message || 'Some records failed to download.');
            }
          }
        }
      ]
    );
  };

  const handleTransactionPress = (item) => {
    const downloaded = isTransactionDownloaded(item.TrackingNo, item.FormworkflowId);

    // If offline and NOT downloaded, prevent opening and inform user
    if (isOffline && !downloaded) {
      Alert.alert(
        'File Not Downloaded',
        `Transaction "${item.TrackingNo}" has not been downloaded to your device.\n\nPlease connect to the internet and tap Download to make this form available offline.`
      );
      return;
    }

    // Route to appropriate form based on transaction type
    if (item.TransactionTypeId === 1) {
      navigation.navigate('LandForm', {
        trackingNo: item.TrackingNo,
        formWorkflowId: item.FormworkflowId,
        tdnId: item.TdnId
      });
    } else if (item.TransactionTypeId === 3) {
      navigation.navigate('MachineryForm', {
        trackingNo: item.TrackingNo,
        formWorkflowId: item.FormworkflowId,
        tdnId: item.TdnId
      });
    } else {
      navigation.navigate('BuildingForm', {
        trackingNo: item.TrackingNo,
        formWorkflowId: item.FormworkflowId,
        tdnId: item.TdnId
      });
    }
  };

  const renderItem = ({ item }) => {
    const isDownloaded = isTransactionDownloaded(item.TrackingNo, item.FormworkflowId);
    const isThisDownloading = downloadingId === item.FormworkflowId;

    const typeLabel = item.TransactionTypeId === 2 ? 'Building FAAS' :
                      item.TransactionTypeId === 1 ? 'Land Appraisal' :
                      item.TransactionTypeId === 3 ? 'Machinery FAAS' : 'Property Assessment';

    return (
      <TouchableOpacity
        style={[
          styles.transCard,
          {
            backgroundColor: colors.cardBg,
            borderColor: colors.border
          },
          shadows.card
        ]}
        onPress={() => handleTransactionPress(item)}
      >
        <View style={styles.cardHeader}>
          <View style={{ flex: 1 }}>
            <Text style={[styles.trackingNoText, { color: colors.textDark }]}>{item.TrackingNo || 'N/A'}</Text>
            <Text style={styles.typeBadgeText}>{typeLabel}</Text>
          </View>
          <StatusBadge
            statusId={item.CurrentStatusId}
            isComplete={item.IsComplete}
            isDeferred={item.IsDeffered}
          />
        </View>

        <View style={styles.cardBody}>
          {item.Owner ? (
            <View style={styles.infoRow}>
              <Ionicons name="person-circle-outline" size={15} color={colors.textDark} />
              <Text style={[styles.ownerText, { color: colors.textDark }]}>{item.Owner}</Text>
            </View>
          ) : null}

          {item.ARPNo ? (
            <View style={styles.infoRow}>
              <Ionicons name="pricetag-outline" size={14} color={colors.textMuted} />
              <Text style={[styles.infoText, { color: colors.textMuted }]}>ARP No: {item.ARPNo}</Text>
            </View>
          ) : null}

          <View style={styles.infoRow}>
            <Ionicons name="calendar-outline" size={14} color={colors.textMuted} />
            <Text style={[styles.infoText, { color: colors.textMuted }]}>
              Submitted: {new Date(item.SubmittedDate).toLocaleDateString()}
            </Text>
          </View>

          {item.Remarks ? (
            <View
              style={[
                styles.remarksBox,
                isDarkMode && { backgroundColor: '#0f172a', borderColor: '#334155' }
              ]}
            >
              <Text style={[styles.remarksText, { color: colors.textMuted }]} numberOfLines={2}>
                • {item.Remarks}
              </Text>
            </View>
          ) : null}
        </View>

        {/* Download Indicator & Actions */}
        <View style={styles.cardFooter}>
          {isDownloaded ? (
            <View style={styles.downloadedBadge}>
              <Ionicons name="checkmark-circle" size={14} color="#16a34a" />
              <Text style={styles.downloadedBadgeText}>Downloaded (Offline Ready)</Text>
            </View>
          ) : (
            <TouchableOpacity
              style={[
                styles.downloadBtn,
                isDarkMode && { backgroundColor: '#134e4a', borderColor: '#0d9488' }
              ]}
              onPress={() => handleDownloadSingle(item)}
              disabled={isThisDownloading}
            >
              {isThisDownloading ? (
                <ActivityIndicator size="small" color={colors.primary} />
              ) : (
                <>
                  <Ionicons name="cloud-download-outline" size={14} color={colors.primary} />
                  <Text style={[styles.downloadBtnText, { color: colors.primary }]}>Download to Device</Text>
                </>
              )}
            </TouchableOpacity>
          )}

          <View style={styles.openActionWrapper}>
            <Text style={[styles.tapActionText, { color: colors.primary }]}>
              {isDownloaded ? 'Open Offline' : 'Inspect Form'}
            </Text>
            <Ionicons name="arrow-forward-circle" size={18} color={colors.primary} />
          </View>
        </View>
      </TouchableOpacity>
    );
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.bgLight }]}>
      <HeaderBar title="MY TRANSACTIONS" subtitle="Assigned Field Inspections" navigation={navigation} />

      {/* Offline Status Alert */}
      {isOffline ? (
        <View style={styles.offlineNoticeBar}>
          <Ionicons name="cloud-offline" size={16} color="#b45309" />
          <Text style={styles.offlineNoticeText}>
            Offline Mode • Showing {transactions.length} downloaded form(s) stored on device
          </Text>
        </View>
      ) : null}

      {/* Batch Download Bar */}
      <View
        style={[
          styles.batchDownloadBar,
          {
            backgroundColor: colors.cardBg,
            borderColor: colors.border
          },
          shadows.card
        ]}
      >
        <View style={{ flex: 1 }}>
          <Text style={[styles.batchTitle, { color: colors.textDark }]}>Field Work Preparation</Text>
          <Text style={[styles.batchSubtitle, { color: colors.textMuted }]}>
            {downloadedCount || 0} of {transactions.length} downloaded to phone storage
          </Text>
        </View>
        <TouchableOpacity
          style={[styles.batchDownloadBtn, (isDownloading || isOffline) && { opacity: 0.6 }]}
          onPress={handleDownloadAll}
          disabled={isDownloading || isOffline}
        >
          {isDownloading ? (
            <ActivityIndicator size="small" color="#ffffff" />
          ) : (
            <>
              <Ionicons name="download-outline" size={16} color="#ffffff" />
              <Text style={styles.batchDownloadBtnText}>Download All</Text>
            </>
          )}
        </TouchableOpacity>
      </View>

      {/* Search Input */}
      <View style={styles.searchContainer}>
        <View
          style={[
            styles.searchInputWrapper,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            }
          ]}
        >
          <Ionicons name="search" size={18} color={colors.textMuted} />
          <TextInput
            style={[styles.searchInput, { color: colors.textDark }]}
            placeholder="Search by Tracking No, ARP, Owner..."
            placeholderTextColor={colors.textMuted}
            value={searchQuery}
            onChangeText={setSearchQuery}
            onSubmitEditing={fetchTransactions}
            returnKeyType="search"
          />
          {searchQuery ? (
            <TouchableOpacity onPress={() => { setSearchQuery(''); fetchTransactions(); }}>
              <Ionicons name="close-circle" size={18} color={colors.textMuted} />
            </TouchableOpacity>
          ) : null}
        </View>
      </View>

      {/* Filter Tabs */}
      <View style={styles.filterTabs}>
        <TouchableOpacity
          style={[
            styles.tabPill,
            { backgroundColor: colors.cardBg, borderColor: colors.border },
            selectedType === 0 && { backgroundColor: colors.primary, borderColor: colors.primary }
          ]}
          onPress={() => setSelectedType(0)}
        >
          <Text
            style={[
              styles.tabText,
              { color: colors.textMuted },
              selectedType === 0 && styles.tabTextActive
            ]}
          >
            All ({transactions.length})
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[
            styles.tabPill,
            { backgroundColor: colors.cardBg, borderColor: colors.border },
            selectedType === 4 && { backgroundColor: colors.primary, borderColor: colors.primary }
          ]}
          onPress={() => setSelectedType(4)}
        >
          <Text
            style={[
              styles.tabText,
              { color: colors.textMuted },
              selectedType === 4 && styles.tabTextActive
            ]}
          >
            Downloaded ({downloadedCount || 0})
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[
            styles.tabPill,
            { backgroundColor: colors.cardBg, borderColor: colors.border },
            selectedType === 2 && { backgroundColor: colors.primary, borderColor: colors.primary }
          ]}
          onPress={() => setSelectedType(2)}
        >
          <Text
            style={[
              styles.tabText,
              { color: colors.textMuted },
              selectedType === 2 && styles.tabTextActive
            ]}
          >
            Building
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[
            styles.tabPill,
            { backgroundColor: colors.cardBg, borderColor: colors.border },
            selectedType === 1 && { backgroundColor: colors.primary, borderColor: colors.primary }
          ]}
          onPress={() => setSelectedType(1)}
        >
          <Text
            style={[
              styles.tabText,
              { color: colors.textMuted },
              selectedType === 1 && styles.tabTextActive
            ]}
          >
            Land
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[
            styles.tabPill,
            { backgroundColor: colors.cardBg, borderColor: colors.border },
            selectedType === 3 && { backgroundColor: colors.primary, borderColor: colors.primary }
          ]}
          onPress={() => setSelectedType(3)}
        >
          <Text
            style={[
              styles.tabText,
              { color: colors.textMuted },
              selectedType === 3 && styles.tabTextActive
            ]}
          >
            Machinery
          </Text>
        </TouchableOpacity>
      </View>

      {/* Transaction List */}
      {loading ? (
        <View style={styles.loaderCenter}>
          <ActivityIndicator color={colors.primary} size="large" />
          <Text style={[styles.loaderText, { color: colors.textMuted }]}>Loading transactions...</Text>
        </View>
      ) : (
        <FlatList
          data={displayTransactions}
          keyExtractor={(item) => (item.FormworkflowId || item.TrackingNo || Math.random()).toString()}
          renderItem={renderItem}
          contentContainerStyle={styles.listContent}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={() => { setRefreshing(true); fetchTransactions(); }} colors={[COLORS.primary]} />
          }
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Ionicons name="documents-outline" size={48} color={COLORS.textMuted} />
              <Text style={styles.emptyTitle}>
                {isOffline ? 'No Downloaded Forms Stored' : 'No Assigned Transactions'}
              </Text>
              <Text style={styles.emptySubtitle}>
                {isOffline
                  ? 'You have not downloaded any forms to your device yet. Connect to the internet to download transactions for offline use.'
                  : 'You currently have no transactions checked out to your account.'}
              </Text>
            </View>
          }
        />
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.bgLight
  },
  offlineNoticeBar: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fef3c7',
    borderBottomWidth: 1,
    borderBottomColor: '#fde68a',
    paddingHorizontal: 16,
    paddingVertical: 8,
    gap: 8
  },
  offlineNoticeText: {
    fontSize: 12,
    fontWeight: '700',
    color: '#92400e',
    flex: 1
  },
  batchDownloadBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: '#ffffff',
    marginHorizontal: 16,
    marginTop: 12,
    padding: 12,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: '#e2e8f0'
  },
  batchTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: COLORS.textDark
  },
  batchSubtitle: {
    fontSize: 11,
    color: COLORS.textMuted,
    marginTop: 2
  },
  batchDownloadBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: COLORS.primary,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 10,
    gap: 6
  },
  batchDownloadBtnText: {
    color: '#ffffff',
    fontSize: 12,
    fontWeight: '800'
  },
  searchContainer: {
    paddingHorizontal: 16,
    paddingTop: 10,
    paddingBottom: 4
  },
  searchInputWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#ffffff',
    borderWidth: 1,
    borderColor: COLORS.border,
    borderRadius: 12,
    paddingHorizontal: 12,
    paddingVertical: 8,
    gap: 8
  },
  searchInput: {
    flex: 1,
    fontSize: 14,
    color: COLORS.textDark,
    fontWeight: '600'
  },
  filterTabs: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingVertical: 8,
    gap: 8
  },
  tabPill: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
    backgroundColor: '#ffffff',
    borderWidth: 1,
    borderColor: COLORS.border
  },
  tabPillActive: {
    backgroundColor: COLORS.primary,
    borderColor: COLORS.primary
  },
  tabText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.textMuted
  },
  tabTextActive: {
    color: '#ffffff'
  },
  listContent: {
    padding: 16,
    paddingTop: 4,
    gap: 12,
    paddingBottom: 40
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
  typeBadgeText: {
    fontSize: 11,
    fontWeight: '600',
    color: COLORS.textMuted,
    marginTop: 2
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
  remarksBox: {
    backgroundColor: '#f8fafc',
    padding: 8,
    borderRadius: 8,
    marginTop: 4
  },
  remarksText: {
    fontSize: 11,
    color: '#475569',
    fontStyle: 'italic'
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 12,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: '#f8fafc'
  },
  downloadedBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#dcfce7',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6,
    gap: 4
  },
  downloadedBadgeText: {
    fontSize: 11,
    fontWeight: '700',
    color: '#15803d'
  },
  downloadBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#f0fdf4',
    borderWidth: 1,
    borderColor: '#86efac',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 6,
    gap: 4
  },
  downloadBtnText: {
    fontSize: 11,
    fontWeight: '800',
    color: COLORS.primary
  },
  openActionWrapper: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  tapActionText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.primary
  },
  loaderCenter: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center'
  },
  loaderText: {
    marginTop: 10,
    fontSize: 13,
    color: COLORS.textMuted,
    fontWeight: '600'
  },
  emptyContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 60
  },
  emptyTitle: {
    fontSize: 16,
    fontWeight: '800',
    color: COLORS.textDark,
    marginTop: 12
  },
  emptySubtitle: {
    fontSize: 12,
    color: COLORS.textMuted,
    marginTop: 4
  }
});
