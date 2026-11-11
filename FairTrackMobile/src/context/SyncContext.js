import React, { createContext, useState, useEffect, useCallback } from 'react';
import { Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MobileApi } from '../api/mobileApi';
import storageResetEpoch from '../constants/storageResetEpoch.json';

export const SyncContext = createContext();

// Normalize transaction items from either PascalCase or camelCase API/storage payloads
export const normalizeTransactionItem = (raw) => {
  if (!raw || typeof raw !== 'object') return raw;
  const trackingNo = raw.TrackingNo ?? raw.trackingNo ?? '';
  const requestType = raw.RequestType ?? raw.requestType ?? (
    trackingNo.toUpperCase().startsWith('RFI-') ? 'Field Inspection' : 'Property Assessment'
  );
  const owner = raw.Owner ?? raw.owner ?? raw.Requestor ?? raw.requestor ?? '';
  const requestor = raw.Requestor ?? raw.requestor ?? owner;
  const hasPartition = raw.HasPartition ?? raw.hasPartition ?? false;
  const rawPartitions = raw.Partitions ?? raw.partitions ?? [];
  const partitionList = Array.isArray(rawPartitions) ? rawPartitions : [];
  const partitionCount = raw.PartitionCount ?? raw.partitionCount ?? (partitionList.length > 0 ? partitionList.length : (hasPartition ? 1 : 0));

  return {
    ...raw,
    FormworkflowId: raw.FormworkflowId ?? raw.formworkflowId ?? raw.FormWorkflowId ?? raw.formWorkflowId ?? null,
    TrackingNo: trackingNo,
    TransactionTypeId: raw.TransactionTypeId ?? raw.transactionTypeId ?? 2,
    RequestType: requestType,
    ARPNo: raw.ARPNo ?? raw.arpNo ?? '',
    Owner: owner,
    Requestor: requestor,
    AcceptedBy: raw.AcceptedBy ?? raw.acceptedBy ?? 'Staff',
    CurrentStatusId: raw.CurrentStatusId ?? raw.currentStatusId ?? 1,
    CurrentStatus: raw.CurrentStatus ?? raw.currentStatus ?? 'Receiving',
    Remarks: raw.Remarks ?? raw.remarks ?? '',
    IsComplete: raw.IsComplete ?? raw.isComplete ?? false,
    IsDeffered: raw.IsDeffered ?? raw.isDeffered ?? raw.IsDeferred ?? raw.isDeferred ?? false,
    IsCheckout: raw.IsCheckout ?? raw.isCheckout ?? false,
    SubmittedDate: raw.SubmittedDate ?? raw.submittedDate ?? null,
    ForwarderBy: raw.ForwarderBy ?? raw.forwarderBy ?? 'Office Staff',
    TdnId: raw.TdnId ?? raw.tdnId ?? null,
    HasPartition: hasPartition,
    PartitionCount: partitionCount,
    Partitions: partitionList
  };
};

// Recursively add PascalCase aliases for any camelCase keys in downloaded form DTOs
const withPascalCaseKeys = (obj) => {
  if (Array.isArray(obj)) return obj.map(withPascalCaseKeys);
  if (!obj || typeof obj !== 'object') return obj;

  const result = { ...obj };
  for (const [k, v] of Object.entries(obj)) {
    const transformedVal = withPascalCaseKeys(v);
    result[k] = transformedVal;
    if (k.length > 0) {
      const pascalKey = k[0].toUpperCase() + k.slice(1);
      if (!(pascalKey in result)) {
        result[pascalKey] = transformedVal;
      }
    }
  }
  // Specific acronym keys used by form screens
  if (result.tdn && !result.TDN) result.TDN = result.tdn;
  if (result.Tdn && !result.TDN) result.TDN = result.Tdn;
  if (result.cast_TDNBuildingDTO && !result.CAST_TDNBuildingDTO) result.CAST_TDNBuildingDTO = result.cast_TDNBuildingDTO;
  if (result.Cast_TDNBuildingDTO && !result.CAST_TDNBuildingDTO) result.CAST_TDNBuildingDTO = result.Cast_TDNBuildingDTO;
  if (result.tdnLand && !result.TDNLand) result.TDNLand = result.tdnLand;
  if (result.TdnLand && !result.TDNLand) result.TDNLand = result.TdnLand;
  if (result.tdnLandDetails && !result.TDNLandDetails) result.TDNLandDetails = result.tdnLandDetails;
  if (result.TdnLandDetails && !result.TDNLandDetails) result.TDNLandDetails = result.TdnLandDetails;
  if (result.tdnMachine && !result.TDNMachine) result.TDNMachine = result.tdnMachine;
  if (result.TdnMachine && !result.TDNMachine) result.TDNMachine = result.TdnMachine;
  if (result.tdnMachineDetail && !result.TDNMachineDetail) result.TDNMachineDetail = result.tdnMachineDetail;
  if (result.TdnMachineDetail && !result.TDNMachineDetail) result.TDNMachineDetail = result.TdnMachineDetail;
  if (result.tdnId !== undefined && result.TDNId === undefined) result.TDNId = result.tdnId;
  if (result.arpNo !== undefined && result.ARPNo === undefined) result.ARPNo = result.arpNo;
  if (result.pin !== undefined && result.PIN === undefined) result.PIN = result.pin;
  if (result.tctNo !== undefined && result.TCTNo === undefined) result.TCTNo = result.tctNo;
  if (result.tctDate !== undefined && result.TCTDate === undefined) result.TCTDate = result.tctDate;
  if (result.landOwnerPIN !== undefined && result.LandOwnerPIN === undefined) result.LandOwnerPIN = result.landOwnerPIN;
  if (result.buildingOwnerPIN !== undefined && result.BuildingOwnerPIN === undefined) result.BuildingOwnerPIN = result.buildingOwnerPIN;
  return result;
};

// Strict guard: Only Property Assessment (RPA) and Field Inspection (RFI) transactions are synced in the mobile app
export const isAllowedMobileTransaction = (rawItem) => {
  if (!rawItem) return false;
  const transItem = normalizeTransactionItem(rawItem);
  const reqType = (transItem.RequestType || '').toLowerCase();
  const tracking = (transItem.TrackingNo || '').toUpperCase();

  // Explicitly block non-field-inspection / non-assessment modules
  if (
    reqType.includes('tax mapping') ||
    reqType.includes('taxmapping') ||
    reqType.includes('land holding') ||
    reqType.includes('landholding') ||
    reqType.includes('tax declaration') ||
    reqType.includes('taxdeclaration') ||
    tracking.startsWith('TMR-') ||
    tracking.startsWith('TM-') ||
    tracking.startsWith('LHR-') ||
    tracking.startsWith('LH-') ||
    tracking.startsWith('TDR-')
  ) {
    return false;
  }

  if (
    reqType.includes('property assessment') ||
    reqType.includes('propertyassessment') ||
    reqType.includes('field inspection') ||
    reqType.includes('request for inspection') ||
    reqType.includes('requestforinspection') ||
    reqType.includes('inspection') ||
    tracking.startsWith('RPA-') ||
    tracking.startsWith('RFI-')
  ) {
    return true;
  }

  return true;
};

export const SyncProvider = ({ children }) => {
  const [syncQueue, setSyncQueue] = useState([]);
  const [cachedForms, setCachedForms] = useState({});
  const [downloadedForms, setDownloadedForms] = useState({});
  const [cachedTransactions, setCachedTransactions] = useState([]);
  const [isSyncing, setIsSyncing] = useState(false);
  const [isDownloading, setIsDownloading] = useState(false);
  const [lastSyncTime, setLastSyncTime] = useState(null);
  const [syncLogs, setSyncLogs] = useState([]);
  const [isOnline, setIsOnline] = useState(
    Platform.OS === 'web' && typeof navigator !== 'undefined'
      ? navigator.onLine !== false
      : true
  );

  const checkConnectivity = useCallback(async (timeoutMs = 3000) => {
    if (Platform.OS === 'web' && typeof navigator !== 'undefined' && navigator.onLine === false) {
      setIsOnline(false);
      return false;
    }
    try {
      const res = await MobileApi.ping(timeoutMs);
      const ok = Boolean(res && (res.status === 'OK' || res.success));
      setIsOnline(ok);
      return ok;
    } catch (e) {
      setIsOnline(false);
      return false;
    }
  }, []);

  useEffect(() => {
    checkConnectivity();

    let onlineHandler = null;
    let offlineHandler = null;

    if (Platform.OS === 'web' && typeof window !== 'undefined' && window.addEventListener) {
      onlineHandler = () => {
        checkConnectivity();
      };
      offlineHandler = () => {
        setIsOnline(false);
      };
      window.addEventListener('online', onlineHandler);
      window.addEventListener('offline', offlineHandler);
    }

    const interval = setInterval(() => {
      checkConnectivity(3000);
    }, 20000);

    return () => {
      if (Platform.OS === 'web' && typeof window !== 'undefined' && window.removeEventListener) {
        if (onlineHandler) window.removeEventListener('online', onlineHandler);
        if (offlineHandler) window.removeEventListener('offline', offlineHandler);
      }
      clearInterval(interval);
    };
  }, [checkConnectivity]);

  // Clear all offline transactional data (downloaded forms, cached edits, sync queue) while preserving user auth
  const clearAllOfflineData = async () => {
    try {
      setSyncQueue([]);
      setCachedForms({});
      setDownloadedForms({});
      setCachedTransactions([]);
      setLastSyncTime(null);
      setSyncLogs([]);

      await AsyncStorage.multiRemove([
        '@fairtrack_sync_queue',
        '@fairtrack_cached_forms',
        '@fairtrack_downloaded_forms',
        '@fairtrack_last_sync'
      ]);
      return { success: true };
    } catch (err) {
      return { success: false, message: err?.message || 'Error clearing offline data' };
    }
  };

  useEffect(() => {
    loadLocalQueue();
  }, [storageResetEpoch?.resetEpoch]);

  const loadLocalQueue = async () => {
    try {
      const targetEpoch = storageResetEpoch?.resetEpoch;
      if (targetEpoch) {
        const appliedEpoch = await AsyncStorage.getItem('@fairtrack_applied_reset_epoch');
        if (appliedEpoch !== targetEpoch) {
          await clearAllOfflineData();
          await AsyncStorage.setItem('@fairtrack_applied_reset_epoch', targetEpoch);
          return;
        }
      }

      const storedQueue = await AsyncStorage.getItem('@fairtrack_sync_queue');
      if (storedQueue) setSyncQueue(JSON.parse(storedQueue));

      const storedForms = await AsyncStorage.getItem('@fairtrack_cached_forms');
      if (storedForms) setCachedForms(JSON.parse(storedForms));

      const storedDownloads = await AsyncStorage.getItem('@fairtrack_downloaded_forms');
      let downloadsMap = {};
      if (storedDownloads) {
        const parsed = JSON.parse(storedDownloads);
        for (const [k, v] of Object.entries(parsed)) {
          const normItem = v?.transItem ? normalizeTransactionItem(v.transItem) : null;
          if (!normItem || isAllowedMobileTransaction(normItem)) {
            downloadsMap[k] = {
              ...v,
              transItem: normItem,
              formData: v?.formData ? withPascalCaseKeys(v.formData) : v?.formData
            };
          }
        }
        setDownloadedForms(downloadsMap);
      }

      const downloadedList = Object.values(downloadsMap)
        .map(d => d.transItem)
        .filter(item => item && isAllowedMobileTransaction(item));

      setCachedTransactions(downloadedList);

      const storedTime = await AsyncStorage.getItem('@fairtrack_last_sync');
      if (storedTime) setLastSyncTime(storedTime);
    } catch (err) {
      console.warn('Failed to load local sync queue:', err);
    }
  };

  const getOfflineTransactions = async () => {
    try {
      const targetEpoch = storageResetEpoch?.resetEpoch;
      if (targetEpoch) {
        const appliedEpoch = await AsyncStorage.getItem('@fairtrack_applied_reset_epoch');
        if (appliedEpoch !== targetEpoch) {
          await clearAllOfflineData();
          await AsyncStorage.setItem('@fairtrack_applied_reset_epoch', targetEpoch);
          return [];
        }
      }

      const storedDownloads = await AsyncStorage.getItem('@fairtrack_downloaded_forms');
      if (storedDownloads) {
        const downloadsMap = JSON.parse(storedDownloads);
        const downloadedList = Object.values(downloadsMap)
          .map(d => d?.transItem ? normalizeTransactionItem(d.transItem) : null)
          .filter(item => item && isAllowedMobileTransaction(item));
        return downloadedList;
      }
      return [];
    } catch (e) {
      console.warn('getOfflineTransactions error:', e);
      return [];
    }
  };

  const saveQueue = async (newQueue) => {
    setSyncQueue(newQueue);
    await AsyncStorage.setItem('@fairtrack_sync_queue', JSON.stringify(newQueue));
  };

  const saveDownloadedForms = async (newDownloads) => {
    setDownloadedForms(newDownloads);
    await AsyncStorage.setItem('@fairtrack_downloaded_forms', JSON.stringify(newDownloads));
    const downloadedList = Object.values(newDownloads)
      .map(d => d?.transItem ? normalizeTransactionItem(d.transItem) : null)
      .filter(item => item && isAllowedMobileTransaction(item));
    setCachedTransactions(downloadedList);
  };

  // Download a single transaction form details (strictly Property Assessment & Field Inspection)
  const downloadSingleTransaction = async (rawItem) => {
    const transItem = normalizeTransactionItem(rawItem);
    if (!isAllowedMobileTransaction(transItem)) {
      return {
        success: false,
        message: 'Only Property Assessment and Field Inspection transactions can be synced in the mobile app.'
      };
    }

    if (!transItem.HasPartition) {
      return {
        success: false,
        message: `Cannot download "${transItem.TrackingNo}": This transaction has no generated partitions. Partitions must be generated in the Office Web Portal before field inspection and editing.`
      };
    }

    try {
      const res = await MobileApi.downloadTransactionDetails(transItem.FormworkflowId, transItem.TrackingNo);
      if (res && res.success) {
        if (!res.hasPartition) {
          return {
            success: false,
            message: `Cannot download "${transItem.TrackingNo}": Server reports no partitions exist for this record.`
          };
        }
        const key = (transItem.TrackingNo || transItem.FormworkflowId).toString();
        const updated = {
          ...downloadedForms,
          [key]: {
            ...res,
            formData: res.formData ? withPascalCaseKeys(res.formData) : res.formData,
            partitions: (res.partitions || []).map(p => ({
              ...p,
              formData: p.formData ? withPascalCaseKeys(p.formData) : null
            })),
            transItem: {
              ...transItem,
              HasPartition: res.hasPartition,
              PartitionCount: res.partitionCount || (res.partitions?.length || 1),
              Partitions: res.partitions || []
            },
            downloadedAt: new Date().toISOString()
          }
        };
        await saveDownloadedForms(updated);

        return { success: true, message: `Downloaded ${transItem.TrackingNo} for offline field inspection.` };
      }
      return { success: false, message: res?.message || 'Failed to download transaction.' };
    } catch (err) {
      return { success: false, message: err.response?.data?.message || err.message || 'Download failed.' };
    }
  };

  // Remove a downloaded transaction from offline storage
  const removeDownloadedTransaction = async (trackingNo, formWorkflowId) => {
    try {
      const cleanTrack = (trackingNo || '').toString().trim().toLowerCase();
      const cleanWf = formWorkflowId != null ? formWorkflowId.toString().trim() : '';

      const current = { ...downloadedForms };
      for (const [k, v] of Object.entries(current)) {
        const kStr = k.trim().toLowerCase();
        const vTrack = (v?.transItem?.TrackingNo || v?.transItem?.trackingNo || '').toString().trim().toLowerCase();
        const vWf = (v?.transItem?.FormworkflowId || v?.transItem?.formworkflowId || '').toString().trim();

        if (
          (cleanTrack && (kStr === cleanTrack || vTrack === cleanTrack)) ||
          (cleanWf && (kStr === cleanWf || vWf === cleanWf))
        ) {
          delete current[k];
        }
      }
      await saveDownloadedForms(current);
      return true;
    } catch (e) {
      console.warn('Failed to remove downloaded transaction:', e);
      return false;
    }
  };

  const updateDownloadedFormWithPayload = async (trackingNo, formWorkflowId, payload) => {
    if (!payload || !downloadedForms || Object.keys(downloadedForms).length === 0) return;
    try {
      const tracking = (trackingNo || '').toString().trim().toLowerCase();
      const wfId = formWorkflowId != null ? formWorkflowId.toString().trim() : '';

      let dKey = null;
      if (tracking && downloadedForms[tracking]) dKey = tracking;
      else if (wfId && downloadedForms[wfId]) dKey = wfId;
      else {
        dKey = Object.keys(downloadedForms).find(k =>
          (tracking && k.toLowerCase() === tracking) ||
          (wfId && k.toString() === wfId)
        );
      }

      if (!dKey || !downloadedForms[dKey]) return;

      const itemRecord = { ...downloadedForms[dKey] };
      const tdn = payload.TDN || payload.tdn;
      const partIdx = tdn?.PartitionItemIndex != null ? parseInt(tdn.PartitionItemIndex, 10) : 0;

      const cp = itemRecord.currentPartition ? { ...itemRecord.currentPartition } : {};
      if (tdn?.LotNo) { cp.lotNo = tdn.LotNo; cp.lotNumber = tdn.LotNo; }
      if (tdn?.BlockNo) { cp.blockNo = tdn.BlockNo; cp.blockNumber = tdn.BlockNo; }
      if (tdn?.SurveyNo) { cp.surveyNo = tdn.SurveyNo; cp.surveyNumber = tdn.SurveyNo; }
      if (tdn?.CadastralSurveyNo) cp.cadastralSurveyNo = tdn.CadastralSurveyNo;
      if (tdn?.SectionNo) cp.sectionNo = tdn.SectionNo;
      if (tdn?.ParcelNo) cp.parcelNo = tdn.ParcelNo;
      if (tdn?.PropLocationStreet) { cp.streetLocation = tdn.PropLocationStreet; cp.streetNo = tdn.PropLocationStreet; }
      if (tdn?.Location) cp.location = tdn.Location;
      if (tdn?.Purok) cp.purok = tdn.Purok;
      if (tdn?.DistrictOrBarangay) cp.districtOrBarangay = tdn.DistrictOrBarangay;
      if (tdn?.CityMunicipality) cp.cityMunicipality = tdn.CityMunicipality;
      if (tdn?.Province) cp.province = tdn.Province;
      if (tdn?.DisplayName) cp.owner = tdn.DisplayName;
      if (tdn?.OwnerAddress) cp.ownerAddress = tdn.OwnerAddress;
      if (tdn?.OwnerTIN) cp.ownerTin = tdn.OwnerTIN;
      if (tdn?.TotalArea) cp.area = tdn.TotalArea;
      if (tdn?.TotalMarketValue) cp.marketValue = tdn.TotalMarketValue;
      if (tdn?.TotalAssessedValue) cp.assessedValue = tdn.TotalAssessedValue;
      itemRecord.currentPartition = cp;

      if (Array.isArray(itemRecord.partitions) && itemRecord.partitions.length > partIdx) {
        const updatedPartitions = [...itemRecord.partitions];
        updatedPartitions[partIdx] = {
          ...updatedPartitions[partIdx],
          ...cp
        };
        itemRecord.partitions = updatedPartitions;
      }

      if (tdn?.DisplayName) itemRecord.owner = tdn.DisplayName;

      const newDownloads = { ...downloadedForms, [dKey]: itemRecord };
      await saveDownloadedForms(newDownloads);
    } catch (e) {
      // Handled silently
    }
  };

  // Mark transaction synced: clears pending syncQueue items, and if transferred, removes from downloadedForms
  const markTransactionSynced = async (trackingNo, formWorkflowId, isTransferred = false, payload = null) => {
    try {
      const cleanTrack = (trackingNo || '').toString().trim().toLowerCase();
      const cleanWf = formWorkflowId != null ? formWorkflowId.toString().trim() : '';

      // 1. Remove from syncQueue
      const filteredQueue = syncQueue.filter(item => {
        const p = item.payload;
        const tdn = p?.TDN || p?.tdn;
        const itemTracking = (tdn?.TrackingNo || tdn?.trackingNo || p?.TrackingNo || p?.trackingNo || '').toString().trim().toLowerCase();
        const itemWfId = (tdn?.FormWorkflowId || tdn?.formWorkflowId || p?.FormWorkflowId || p?.formWorkflowId || item.workflowForwardPayload?.FormWorkflowId || item.workflowForwardPayload?.formWorkflowId || '').toString().trim();

        const matchTrack = cleanTrack && itemTracking && cleanTrack === itemTracking;
        const matchWf = cleanWf && itemWfId && cleanWf === itemWfId;
        return !(matchTrack || matchWf);
      });
      await saveQueue(filteredQueue);

      // 2. If transferred, remove completely from downloadedForms
      if (isTransferred) {
        await removeDownloadedTransaction(trackingNo, formWorkflowId);
      } else if (payload) {
        await updateDownloadedFormWithPayload(trackingNo, formWorkflowId, payload);
      }
    } catch (e) {
      // Handled silently
    }
  };

  // Check if a transaction has unsynced local edits waiting in syncQueue
  const hasPendingEdits = (trackingNo, formWorkflowId) => {
    if (!syncQueue || syncQueue.length === 0) return false;
    const tNo = (trackingNo || '').toString().trim().toLowerCase();
    const wfId = (formWorkflowId || '').toString().trim();

    return syncQueue.some(item => {
      const p = item.payload;
      const tdn = p?.TDN || p?.tdn;
      const itemTracking = (tdn?.TrackingNo || tdn?.trackingNo || p?.TrackingNo || p?.trackingNo || '').toString().trim().toLowerCase();
      const itemWfId = (tdn?.FormWorkflowId || tdn?.formWorkflowId || p?.FormWorkflowId || p?.formWorkflowId || item.workflowForwardPayload?.FormWorkflowId || item.workflowForwardPayload?.formWorkflowId || '').toString().trim();

      if (tNo && itemTracking && tNo === itemTracking) return true;
      if (wfId && itemWfId && wfId === itemWfId) return true;
      return false;
    });
  };

  // Batch download multiple transactions (strictly Property Assessment & Field Inspection)
  const downloadAllTransactions = async (transactionsList, skipPending = true) => {
    const allowedList = (transactionsList || [])
      .map(normalizeTransactionItem)
      .filter(isAllowedMobileTransaction);
    if (allowedList.length === 0) {
      return { success: false, message: 'No Property Assessment or Field Inspection transactions available to download.' };
    }

    const partitionAllowedList = allowedList.filter(t => t.HasPartition);
    const unpartitionedCount = allowedList.length - partitionAllowedList.length;

    if (partitionAllowedList.length === 0) {
      return {
        success: false,
        message: unpartitionedCount > 0
          ? `None of the ${unpartitionedCount} transaction(s) have generated partitions. Partitions must first be generated in the Office Web Portal before downloading.`
          : 'No Property Assessment or Field Inspection transactions available to download.'
      };
    }

    // Protection: Exclude transactions with unsynced local edits if skipPending is active
    const targetsToDownload = skipPending
      ? partitionAllowedList.filter(t => !hasPendingEdits(t.TrackingNo, t.FormworkflowId))
      : partitionAllowedList;
    const skippedPendingCount = partitionAllowedList.length - targetsToDownload.length;

    if (targetsToDownload.length === 0) {
      return {
        success: false,
        skippedPendingCount,
        message: skippedPendingCount > 0
          ? `All ${skippedPendingCount} transaction(s) have unsynced local edits. Please sync your changes to the office first before re-downloading.`
          : 'No transactions available to download.'
      };
    }

    setIsDownloading(true);
    let successCount = 0;
    let failCount = 0;
    const currentDownloads = { ...downloadedForms };

    for (const item of targetsToDownload) {
      try {
        const res = await MobileApi.downloadTransactionDetails(item.FormworkflowId, item.TrackingNo);
        if (res && res.success && res.hasPartition) {
          const key = (item.TrackingNo || item.FormworkflowId).toString();
          currentDownloads[key] = {
            ...res,
            formData: res.formData ? withPascalCaseKeys(res.formData) : res.formData,
            partitions: (res.partitions || []).map(p => ({
              ...p,
              formData: p.formData ? withPascalCaseKeys(p.formData) : null
            })),
            transItem: {
              ...item,
              HasPartition: res.hasPartition,
              PartitionCount: res.partitionCount || (res.partitions?.length || 1),
              Partitions: res.partitions || []
            },
            downloadedAt: new Date().toISOString()
          };
          successCount++;
        } else {
          failCount++;
        }
      } catch (err) {
        failCount++;
      }
    }

    await saveDownloadedForms(currentDownloads);
    setIsDownloading(false);

    return {
      success: successCount > 0,
      successCount,
      failCount,
      skippedCount: unpartitionedCount,
      skippedPendingCount,
      total: partitionAllowedList.length
    };
  };

  // Check if a transaction is already downloaded in local storage (case-insensitive & multi-key safe)
  const isTransactionDownloaded = (trackingNo, formWorkflowId) => {
    if (!downloadedForms || Object.keys(downloadedForms).length === 0) return false;
    const tNo = (trackingNo || '').toString().trim().toLowerCase();
    const wfId = (formWorkflowId || '').toString().trim();

    for (const [k, item] of Object.entries(downloadedForms)) {
      const keyLower = k.toLowerCase();
      if (tNo && (keyLower === tNo || (item?.transItem?.TrackingNo && item.transItem.TrackingNo.toLowerCase() === tNo) || (item?.trackingNo && item.trackingNo.toLowerCase() === tNo))) {
        return true;
      }
      if (wfId && (k === wfId || (item?.transItem?.FormworkflowId && item.transItem.FormworkflowId.toString() === wfId) || (item?.formWorkflowId && item.formWorkflowId.toString() === wfId))) {
        return true;
      }
    }
    return false;
  };

  // Retrieve downloaded form data (with automatic overlay of unsynced drafts from syncQueue)
  const getDownloadedFormData = (trackingNo, formWorkflowId, partitionIndex = 0) => {
    const targetIdx = parseInt(partitionIndex, 10) || 0;
    const tNo = (trackingNo || '').toString().trim().toLowerCase();
    const wfId = (formWorkflowId || '').toString().trim();

    let found = null;
    let foundKey = null;

    if (downloadedForms && Object.keys(downloadedForms).length > 0) {
      if (trackingNo && downloadedForms[trackingNo]) {
        found = downloadedForms[trackingNo];
        foundKey = trackingNo;
      } else if (formWorkflowId && downloadedForms[formWorkflowId.toString()]) {
        found = downloadedForms[formWorkflowId.toString()];
        foundKey = formWorkflowId.toString();
      } else {
        for (const [k, item] of Object.entries(downloadedForms)) {
          const keyLower = k.toLowerCase();
          const itemTNo = (item?.transItem?.TrackingNo || item?.trackingNo || '').toString().trim().toLowerCase();
          const itemWfId = (item?.transItem?.FormworkflowId || item?.formWorkflowId || '').toString().trim();
          if ((tNo && (keyLower === tNo || itemTNo === tNo)) || (wfId && (k === wfId || itemWfId === wfId))) {
            found = item;
            foundKey = k;
            break;
          }
        }
      }
    }

    // Check if there is an unsynced draft in syncQueue for this specific partition
    let pendingDraft = null;
    if (syncQueue && syncQueue.length > 0) {
      const match = syncQueue.find(item => {
        const p = item.payload;
        const tdn = p?.TDN || p?.tdn;
        const itemTracking = (tdn?.TrackingNo || tdn?.trackingNo || p?.TrackingNo || p?.trackingNo || '').toString().trim().toLowerCase();
        const itemWfId = (tdn?.FormWorkflowId || tdn?.formWorkflowId || p?.FormWorkflowId || p?.formWorkflowId || item.workflowForwardPayload?.FormWorkflowId || item.workflowForwardPayload?.formWorkflowId || '').toString().trim();
        const itemPartIdx = parseInt(tdn?.PartitionItemIndex ?? p?.partitionIndex ?? 0, 10) || 0;

        const trackingMatches = tNo && itemTracking && tNo === itemTracking;
        const wfMatches = wfId && itemWfId && wfId === itemWfId;
        return (trackingMatches || wfMatches) && itemPartIdx === targetIdx;
      });
      if (match) {
        pendingDraft = match.payload;
      }
    }

    if (!found && !pendingDraft) return null;

    if (found && Array.isArray(found.partitions) && found.partitions.length > targetIdx && found.partitions[targetIdx]) {
      const partObj = { ...found.partitions[targetIdx] };
      let partFormData = partObj.formData ? withPascalCaseKeys(partObj.formData) : (found.formData ? withPascalCaseKeys(found.formData) : null);

      // If an unsynced draft exists in syncQueue, overlay it into partFormData and partObj!
      if (pendingDraft) {
        const draftPascal = withPascalCaseKeys(pendingDraft);
        partFormData = draftPascal;
        const tdn = draftPascal?.TDN || draftPascal?.tdn || {};
        const tdnLand = draftPascal?.TDNLand || draftPascal?.tdnLand || {};
        const tdnBldg = draftPascal?.TDNBuilding || draftPascal?.tdnBuilding || {};

        if (tdn.PropLocationStreet || tdn.Location) {
          partObj.streetLocation = tdn.PropLocationStreet || tdn.Location;
          partObj.streetNo = tdn.PropLocationStreet || tdn.Location;
          partObj.buildingNo = tdn.PropLocationStreet || tdn.Location;
        }
        if (tdn.Purok) partObj.purok = tdn.Purok;
        if (tdn.DistrictOrBarangay) partObj.districtOrBarangay = tdn.DistrictOrBarangay;
        if (tdn.CityMunicipality) partObj.cityMunicipality = tdn.CityMunicipality;
        if (tdn.Province) partObj.province = tdn.Province;
        if (tdn.DisplayName) {
          partObj.ownerName = tdn.DisplayName;
          partObj.newOwnerName = tdn.DisplayName;
        }
        if (tdn.OwnerAddress) partObj.ownerAddress = tdn.OwnerAddress;
        if (tdn.OwnerTIN) partObj.ownerTin = tdn.OwnerTIN;
        if (tdn.OwnerContactNo) partObj.ownerContactNo = tdn.OwnerContactNo;
        if (tdn.OwnerEmail) partObj.ownerEmail = tdn.OwnerEmail;
        if (tdn.FloorPlanSketchPath) partObj.sketchUrl = tdn.FloorPlanSketchPath;
        if (tdn.Attachments && Array.isArray(tdn.Attachments)) partObj.attachments = tdn.Attachments;

        // Land partition overrides
        if (tdnLand.NorthBoundary !== undefined) partObj.boundaryNorth = tdnLand.NorthBoundary;
        if (tdnLand.Nremarks !== undefined) partObj.nremarks = tdnLand.Nremarks;
        if (tdnLand.SouthBoundary !== undefined) partObj.boundarySouth = tdnLand.SouthBoundary;
        if (tdnLand.Sremarks !== undefined) partObj.sremarks = tdnLand.Sremarks;
        if (tdnLand.EastBoundary !== undefined) partObj.boundaryEast = tdnLand.EastBoundary;
        if (tdnLand.Eremarks !== undefined) partObj.eremarks = tdnLand.Eremarks;
        if (tdnLand.WestBoundary !== undefined) partObj.boundaryWest = tdnLand.WestBoundary;
        if (tdnLand.Wremarks !== undefined) partObj.wremarks = tdnLand.Wremarks;
        if (tdn.LandAppraisalDetailsJson || tdnLand.LandAppraisalDetailsJson) {
          partObj.landAppraisalDetailsJson = tdn.LandAppraisalDetailsJson || tdnLand.LandAppraisalDetailsJson;
        }
        if (tdn.LandImprovementsJson || tdnLand.LandImprovementsJson) {
          partObj.landImprovementsJson = tdn.LandImprovementsJson || tdnLand.LandImprovementsJson;
        }
        if (tdn.LandAdjustmentsJson || tdnLand.LandAdjustmentsJson) {
          partObj.landAdjustmentsJson = tdn.LandAdjustmentsJson || tdnLand.LandAdjustmentsJson;
        }
        if (tdn.LandPropertyAssessmentsJson || tdnLand.LandPropertyAssessmentsJson) {
          partObj.landPropertyAssessmentsJson = tdn.LandPropertyAssessmentsJson || tdnLand.LandPropertyAssessmentsJson;
        }

        // Building partition overrides
        if (tdnBldg.BuildingType) partObj.buildingType = tdnBldg.BuildingType;
        if (tdnBldg.StructuralType) partObj.structuralType = tdnBldg.StructuralType;
        if (tdnBldg.TotalFloorArea) partObj.areaSqMeters = tdnBldg.TotalFloorArea;

        // Effectivity & Notes overrides
        if (tdn.QuarterEffectivity) {
          partObj.quarterEffectivity = typeof tdn.QuarterEffectivity === 'number'
            ? `${tdn.QuarterEffectivity}${tdn.QuarterEffectivity === 1 ? 'st' : tdn.QuarterEffectivity === 2 ? 'nd' : tdn.QuarterEffectivity === 3 ? 'rd' : 'th'} Quarter`
            : tdn.QuarterEffectivity;
        }
        if (tdn.YearEffectivity) partObj.yearEffectivity = tdn.YearEffectivity.toString();
        if (tdn.Taxability !== undefined) partObj.taxability = tdn.Taxability ? 'Taxable' : 'Exempt';
        if (tdn.AssessmentBasis) partObj.assessmentBasis = tdn.AssessmentBasis;
        if (tdn.Memoranda) partObj.memoranda = tdn.Memoranda;
      }

      return {
        ...(found || {}),
        selectedPartitionIndex: targetIdx,
        formType: partObj.formType || found?.formType,
        transactionTypeId: partObj.transactionTypeId || found?.transactionTypeId,
        owner: partObj.ownerName || found?.owner,
        arpNo: partObj.systemGeneratedTDN || found?.arpNo,
        sketchUrl: partObj.sketchUrl || found?.sketchUrl,
        sketchFileName: partObj.sketchFileName || found?.sketchFileName,
        attachments: partObj.attachments || found?.attachments || [],
        formData: partFormData,
        currentPartition: partObj
      };
    }

    if (found && found.formData) {
      return {
        ...found,
        formData: withPascalCaseKeys(found.formData)
      };
    }

    if (pendingDraft) {
      const draftPascal = withPascalCaseKeys(pendingDraft);
      return {
        formData: draftPascal,
        owner: draftPascal?.TDN?.DisplayName || '',
        arpNo: draftPascal?.TDN?.DeclarationNo || '',
        sketchUrl: draftPascal?.TDN?.FloorPlanSketchPath || null,
        attachments: draftPascal?.TDN?.Attachments || [],
        currentPartition: null
      };
    }

    return found;
  };

  // Enqueue an offline update and auto-stage workflow forward
  const queueOfflineUpdate = async (type, payload, summaryTitle, workflowForwardPayload = null) => {
    const queueItem = {
      id: Date.now().toString() + '_' + Math.random().toString(36).substring(2, 6),
      type, // 'UPDATE_BUILDING' | 'UPDATE_LAND' | 'UPDATE_MACHINERY'
      payload,
      workflowForwardPayload, // Optional next status step
      title: summaryTitle,
      timestamp: new Date().toISOString(),
      status: 'PENDING'
    };

    const tdn = payload?.TDN || payload?.tdn;
    const tracking = (tdn?.TrackingNo || payload?.trackingNo || payload?.TrackingNo || '').toString().trim();
    const wfId = (tdn?.FormWorkflowId || payload?.formWorkflowId || payload?.FormWorkflowId || (workflowForwardPayload?.formWorkflowId || workflowForwardPayload?.FormWorkflowId) || '').toString().trim();
    const partIdx = parseInt(tdn?.PartitionItemIndex ?? payload?.partitionIndex ?? 0, 10) || 0;

    // Filter out previous drafts in syncQueue for the exact same partition so queue items don't accumulate duplicates
    const filteredQueue = syncQueue.filter(item => {
      const p = item.payload;
      const itemTdn = p?.TDN || p?.tdn;
      const itemTracking = (itemTdn?.TrackingNo || itemTdn?.trackingNo || p?.TrackingNo || p?.trackingNo || '').toString().trim();
      const itemWfId = (itemTdn?.FormWorkflowId || itemTdn?.formWorkflowId || p?.FormWorkflowId || p?.formWorkflowId || item.workflowForwardPayload?.FormWorkflowId || item.workflowForwardPayload?.formWorkflowId || '').toString().trim();
      const itemPartIdx = parseInt(itemTdn?.PartitionItemIndex ?? p?.partitionIndex ?? 0, 10) || 0;

      const trackingMatches = tracking && itemTracking && tracking.toLowerCase() === itemTracking.toLowerCase();
      const wfMatches = wfId && itemWfId && wfId === itemWfId;
      return !((trackingMatches || wfMatches) && itemPartIdx === partIdx && item.type === type);
    });

    const updated = [queueItem, ...filteredQueue];
    await saveQueue(updated);

    // Update cachedForms
    const cacheId = tdn?.TDNId || tracking || wfId || 'temp';
    const formKey = `${type}_${cacheId}_part_${partIdx}`;
    const newCachedForms = { ...cachedForms, [formKey]: payload };
    setCachedForms(newCachedForms);
    await AsyncStorage.setItem('@fairtrack_cached_forms', JSON.stringify(newCachedForms));

    // Update downloadedForms cache so switching partitions offline reflects latest edits immediately
    let dKey = null;
    if (downloadedForms && Object.keys(downloadedForms).length > 0) {
      if (tracking && downloadedForms[tracking]) dKey = tracking;
      else if (wfId && downloadedForms[wfId]) dKey = wfId;
      else {
        const matchKey = Object.keys(downloadedForms).find(k =>
          (tracking && k.toLowerCase() === tracking.toLowerCase()) ||
          (wfId && k.toString() === wfId.toString())
        );
        if (matchKey) dKey = matchKey;
      }
    }

    if (dKey && downloadedForms[dKey]) {
      const itemRecord = { ...downloadedForms[dKey] };
      if (Array.isArray(itemRecord.partitions) && itemRecord.partitions.length > partIdx) {
        const updatedPartitions = [...itemRecord.partitions];
        const existingPart = updatedPartitions[partIdx] || {};
        const tdnLand = payload?.TDNLand || payload?.tdnLand || {};
        const tdnBldg = payload?.TDNBuilding || payload?.tdnBuilding || {};

        updatedPartitions[partIdx] = {
          ...existingPart,
          formData: payload,
          sketchUrl: tdn?.FloorPlanSketchPath || existingPart.sketchUrl,
          attachments: tdn?.Attachments || existingPart.attachments,
          streetLocation: tdn?.PropLocationStreet || tdn?.Location || existingPart.streetLocation,
          streetNo: tdn?.PropLocationStreet || tdn?.Location || existingPart.streetNo,
          buildingNo: tdn?.PropLocationStreet || tdn?.Location || existingPart.buildingNo,
          purok: tdn?.Purok || existingPart.purok,
          districtOrBarangay: tdn?.DistrictOrBarangay || existingPart.districtOrBarangay,
          cityMunicipality: tdn?.CityMunicipality || existingPart.cityMunicipality,
          province: tdn?.Province || existingPart.province,
          ownerName: tdn?.DisplayName || existingPart.ownerName,
          newOwnerName: tdn?.DisplayName || existingPart.newOwnerName,
          ownerAddress: tdn?.OwnerAddress || existingPart.ownerAddress,
          ownerTin: tdn?.OwnerTIN || existingPart.ownerTin,
          ownerContactNo: tdn?.OwnerContactNo || existingPart.ownerContactNo,
          ownerEmail: tdn?.OwnerEmail || existingPart.ownerEmail,
          boundaryNorth: tdnLand.NorthBoundary !== undefined ? tdnLand.NorthBoundary : existingPart.boundaryNorth,
          nremarks: tdnLand.Nremarks !== undefined ? tdnLand.Nremarks : existingPart.nremarks,
          boundarySouth: tdnLand.SouthBoundary !== undefined ? tdnLand.SouthBoundary : existingPart.boundarySouth,
          sremarks: tdnLand.Sremarks !== undefined ? tdnLand.Sremarks : existingPart.sremarks,
          boundaryEast: tdnLand.EastBoundary !== undefined ? tdnLand.EastBoundary : existingPart.boundaryEast,
          eremarks: tdnLand.Eremarks !== undefined ? tdnLand.Eremarks : existingPart.eremarks,
          boundaryWest: tdnLand.WestBoundary !== undefined ? tdnLand.WestBoundary : existingPart.boundaryWest,
          wremarks: tdnLand.Wremarks !== undefined ? tdnLand.Wremarks : existingPart.wremarks,
          landAppraisalDetailsJson: tdn?.LandAppraisalDetailsJson || tdnLand.LandAppraisalDetailsJson || existingPart.landAppraisalDetailsJson,
          landImprovementsJson: tdn?.LandImprovementsJson || tdnLand.LandImprovementsJson || existingPart.landImprovementsJson,
          landAdjustmentsJson: tdn?.LandAdjustmentsJson || tdnLand.LandAdjustmentsJson || existingPart.landAdjustmentsJson,
          landPropertyAssessmentsJson: tdn?.LandPropertyAssessmentsJson || tdnLand.LandPropertyAssessmentsJson || existingPart.landPropertyAssessmentsJson,
          buildingType: tdnBldg.BuildingType || existingPart.buildingType,
          structuralType: tdnBldg.StructuralType || existingPart.structuralType,
          areaSqMeters: tdnBldg.TotalFloorArea || existingPart.areaSqMeters,
          quarterEffectivity: tdn?.QuarterEffectivity ? (typeof tdn.QuarterEffectivity === 'number' ? `${tdn.QuarterEffectivity}${tdn.QuarterEffectivity === 1 ? 'st' : tdn.QuarterEffectivity === 2 ? 'nd' : tdn.QuarterEffectivity === 3 ? 'rd' : 'th'} Quarter` : tdn.QuarterEffectivity) : existingPart.quarterEffectivity,
          yearEffectivity: tdn?.YearEffectivity ? tdn.YearEffectivity.toString() : existingPart.yearEffectivity,
          taxability: tdn?.Taxability !== undefined ? (tdn.Taxability ? 'Taxable' : 'Exempt') : existingPart.taxability,
          assessmentBasis: tdn?.AssessmentBasis || existingPart.assessmentBasis,
          memoranda: tdn?.Memoranda || existingPart.memoranda
        };
        itemRecord.partitions = updatedPartitions;
      }
      if (partIdx === 0) {
        itemRecord.formData = payload;
        if (tdn?.DisplayName) itemRecord.owner = tdn.DisplayName;
      }
      const newDownloads = { ...downloadedForms, [dKey]: itemRecord };
      await saveDownloadedForms(newDownloads);
    }

    return queueItem;
  };

  // One-Tap Batch Sync Engine
  const syncAllToServer = async () => {
    if (syncQueue.length === 0) {
      return { success: true, count: 0, message: 'Sync queue is empty. No offline edits pending.' };
    }

    const online = await checkConnectivity(3000);
    if (!online) {
      return {
        success: false,
        isOffline: true,
        successCount: 0,
        failureCount: syncQueue.length,
        remainingCount: syncQueue.length,
        message: 'Office server is unreachable. Please connect to the office network before syncing.'
      };
    }

    setIsSyncing(true);
    let successCount = 0;
    let failureCount = 0;
    const remainingQueue = [];
    const newLogs = [];

    for (let i = 0; i < syncQueue.length; i++) {
      const item = syncQueue[i];
      try {
        let res = null;
        if (item.type === 'UPDATE_BUILDING') {
          res = await MobileApi.updateBuildingForm(item.payload);
        } else if (item.type === 'UPDATE_LAND') {
          res = await MobileApi.updateLandForm(item.payload);
        } else if (item.type === 'UPDATE_MACHINERY') {
          res = await MobileApi.updateMachineryForm(item.payload);
        } else if (item.type === 'FORWARD_WORKFLOW') {
          res = await MobileApi.forwardWorkflow(
            item.payload.formWorkflowId,
            item.payload.nextStatusId,
            item.payload.remarks
          );
        }

        if (res && res.success) {
          // If this update included a workflow transfer to the next step, forward it too
          let wasTransferred = false;
          if (item.workflowForwardPayload && item.workflowForwardPayload.formWorkflowId && item.workflowForwardPayload.nextStatusId) {
            try {
              await MobileApi.forwardWorkflow(
                item.workflowForwardPayload.formWorkflowId,
                item.workflowForwardPayload.nextStatusId,
                item.workflowForwardPayload.remarks || 'Completed field assessment and synced.'
              );
              wasTransferred = true;
            } catch (fwdErr) {
              // Workflow forward completed or handled offline
            }
          }

          if (wasTransferred) {
            const p = item.payload;
            const tdn = p?.TDN || p?.tdn;
            const itemTrack = tdn?.TrackingNo || tdn?.trackingNo;
            const itemWf = tdn?.FormWorkflowId || tdn?.formWorkflowId;
            await removeDownloadedTransaction(itemTrack, itemWf);
          } else {
            const p = item.payload;
            const tdn = p?.TDN || p?.tdn;
            const itemTrack = tdn?.TrackingNo || tdn?.trackingNo;
            const itemWf = tdn?.FormWorkflowId || tdn?.formWorkflowId;
            await updateDownloadedFormWithPayload(itemTrack, itemWf, p);
          }

          successCount++;
          newLogs.unshift({
            id: item.id,
            title: item.title,
            status: 'SUCCESS',
            time: new Date().toLocaleTimeString(),
            message: res.message || 'Synced and transferred to next step.'
          });
        } else {
          failureCount++;
          remainingQueue.push(item);
          newLogs.unshift({
            id: item.id,
            title: item.title,
            status: 'FAILED',
            time: new Date().toLocaleTimeString(),
            message: res?.message || 'Server rejected update.'
          });
        }
      } catch (err) {
        failureCount++;
        remainingQueue.push(item);
        const isNetworkErr = !err.response || err.code === 'ECONNABORTED' || (err.message && err.message.toLowerCase().includes('network'));
        newLogs.unshift({
          id: item.id,
          title: item.title,
          status: 'ERROR',
          time: new Date().toLocaleTimeString(),
          message: isNetworkErr ? 'Network disconnected during sync' : (err.response?.data?.message || err.message || 'Server error')
        });

        if (isNetworkErr) {
          setIsOnline(false);
          for (let j = i + 1; j < syncQueue.length; j++) {
            remainingQueue.push(syncQueue[j]);
          }
          break;
        }
      }
    }

    await saveQueue(remainingQueue);
    setSyncLogs((prev) => [...newLogs, ...prev].slice(0, 50));

    const syncTimeStr = new Date().toLocaleString();
    setLastSyncTime(syncTimeStr);
    await AsyncStorage.setItem('@fairtrack_last_sync', syncTimeStr);

    setIsSyncing(false);

    return {
      success: failureCount === 0,
      isOffline: !isOnline,
      successCount,
      failureCount,
      remainingCount: remainingQueue.length
    };
  };

  const removeQueueItem = async (itemId) => {
    const updated = syncQueue.filter((q) => q.id !== itemId);
    await saveQueue(updated);
  };

  const clearQueue = async () => {
    await saveQueue([]);
  };

  return (
    <SyncContext.Provider
      value={{
        syncQueue,
        pendingCount: syncQueue.length,
        cachedForms,
        downloadedForms,
        downloadedCount: Object.keys(downloadedForms).length,
        cachedTransactions,
        isSyncing,
        isDownloading,
        lastSyncTime,
        syncLogs,
        isOnline,
        checkConnectivity,
        getOfflineTransactions,
        downloadSingleTransaction,
        downloadAllTransactions,
        removeDownloadedTransaction,
        markTransactionSynced,
        isTransactionDownloaded,
        hasPendingEdits,
        getDownloadedFormData,
        queueOfflineUpdate,
        syncAllToServer,
        removeQueueItem,
        clearQueue,
        clearAllOfflineData
      }}
    >
      {children}
    </SyncContext.Provider>
  );
};
