import React, { createContext, useState, useEffect, useCallback } from 'react';
import { Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MobileApi } from '../api/mobileApi';
import storageResetEpoch from '../constants/storageResetEpoch.json';
import { downloadServerFileToMobile } from '../utils/fileStorage';

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
  const [historyRecords, setHistoryRecords] = useState([]);
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

      const storedHistory = await AsyncStorage.getItem('@fairtrack_history_records');
      if (storedHistory) {
        try {
          const parsed = JSON.parse(storedHistory);
          if (Array.isArray(parsed)) {
            setHistoryRecords(parsed);
          }
        } catch (e) {
          console.warn('Failed parsing @fairtrack_history_records:', e);
        }
      }
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
    try {
      await AsyncStorage.setItem('@fairtrack_sync_queue', JSON.stringify(newQueue));
    } catch (err) {
      console.warn('saveQueue storage warning:', err);
    }
  };

  const saveDownloadedForms = async (newDownloads) => {
    setDownloadedForms(newDownloads);
    try {
      await AsyncStorage.setItem('@fairtrack_downloaded_forms', JSON.stringify(newDownloads));
    } catch (err) {
      console.warn('saveDownloadedForms storage warning:', err);
    }
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
        const tracking = transItem.TrackingNo || '';
        const partitionsWithLocalFiles = await Promise.all((res.partitions || []).map(async (p) => {
          let updatedAtts = p.attachments || [];
          if (Platform.OS !== 'web' && Array.isArray(p.attachments)) {
            updatedAtts = await Promise.all(p.attachments.map(async (a) => {
              if (a.fileUrl && !a.fileUrl.startsWith('file:') && !a.fileUrl.startsWith('data:')) {
                const local = await downloadServerFileToMobile(a.fileUrl, tracking, a.fileName, 'attachment');
                return { ...a, fileUrl: local };
              }
              return a;
            }));
          }
          let updatedSketch = p.sketchUrl;
          if (Platform.OS !== 'web' && p.sketchUrl && !p.sketchUrl.startsWith('file:') && !p.sketchUrl.startsWith('data:')) {
            updatedSketch = await downloadServerFileToMobile(p.sketchUrl, tracking, p.sketchFileName || 'sketch.jpg', 'sketch');
          }
          return {
            ...p,
            attachments: updatedAtts,
            sketchUrl: updatedSketch,
            formData: p.formData ? withPascalCaseKeys(p.formData) : null
          };
        }));

        const updated = {
          ...downloadedForms,
          [key]: {
            ...res,
            formData: res.formData ? withPascalCaseKeys(res.formData) : res.formData,
            partitions: partitionsWithLocalFiles,
            transItem: {
              ...transItem,
              HasPartition: res.hasPartition,
              PartitionCount: res.partitionCount || (res.partitions?.length || 1),
              Partitions: partitionsWithLocalFiles
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

  const updatePartitionAttachmentsLocally = async (trackingNo, formWorkflowId, partitionIndex, newAttachments, newSketchUrl) => {
    if (!downloadedForms || Object.keys(downloadedForms).length === 0) return;
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

      if (!dKey) {
        dKey = (trackingNo || formWorkflowId || `record_${Date.now()}`).toString();
      }

      const itemRecord = (downloadedForms && downloadedForms[dKey])
        ? { ...downloadedForms[dKey] }
        : {
            trackingNo: trackingNo || '',
            formWorkflowId: formWorkflowId || null,
            partitions: []
          };

      const partIdx = typeof partitionIndex === 'number' ? partitionIndex : (parseInt(partitionIndex, 10) || 0);
      const updatedPartitions = Array.isArray(itemRecord.partitions) ? [...itemRecord.partitions] : [];
      while (updatedPartitions.length <= partIdx) {
        updatedPartitions.push({});
      }
      const existingPart = { ...(updatedPartitions[partIdx] || {}) };

        if (Array.isArray(newAttachments)) {
          existingPart.attachments = newAttachments;
          if (existingPart.formData?.TDN) {
            existingPart.formData.TDN.Attachments = newAttachments;
          }
        }
        if (newSketchUrl !== undefined) {
          existingPart.sketchUrl = newSketchUrl;
          if (existingPart.formData?.TDN) {
            existingPart.formData.TDN.FloorPlanSketchPath = newSketchUrl;
          }
        }

        updatedPartitions[partIdx] = existingPart;
        itemRecord.partitions = updatedPartitions;

        if (partIdx === 0) {
          itemRecord.currentPartition = existingPart;
          if (Array.isArray(newAttachments)) itemRecord.attachments = newAttachments;
          if (newSketchUrl !== undefined) itemRecord.sketchUrl = newSketchUrl;
        }

        const nextForms = { ...downloadedForms, [dKey]: itemRecord };
        setDownloadedForms(nextForms);
        try {
          await AsyncStorage.setItem('@fairtrack_downloaded_forms', JSON.stringify(nextForms));
        } catch (storageErr) {
          console.warn('AsyncStorage setItem quota reached, saving lightweight metadata copy:', storageErr);
          const lightweightForms = JSON.parse(JSON.stringify(nextForms));
          for (const key of Object.keys(lightweightForms)) {
            const form = lightweightForms[key];
            if (form?.partitions) {
              for (const part of form.partitions) {
                if (part.attachments) {
                  part.attachments = part.attachments.map(a => ({ ...a, base64Data: null }));
                }
              }
            }
          }
          try {
            await AsyncStorage.setItem('@fairtrack_downloaded_forms', JSON.stringify(lightweightForms));
          } catch (e2) {}
        }
    } catch (e) {
      console.warn('updatePartitionAttachmentsLocally error:', e);
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
      const tdnBldg = payload.CAST_TDNBuildingDTO || payload.cast_TDNBuildingDTO || payload.TDNBuilding || payload.tdnBuilding || {};
      const tdnLand = payload.TDNLand || payload.tdnLand || {};

      const pascalPayload = withPascalCaseKeys(payload);

      if (Array.isArray(itemRecord.partitions) && itemRecord.partitions.length > partIdx) {
        const updatedPartitions = [...itemRecord.partitions];
        const existingPart = { ...(updatedPartitions[partIdx] || {}) };

        if (tdn?.LotNo) { existingPart.lotNo = tdn.LotNo; existingPart.lotNumber = tdn.LotNo; }
        if (tdn?.BlockNo) { existingPart.blockNo = tdn.BlockNo; existingPart.blockNumber = tdn.BlockNo; }
        if (tdn?.SurveyNo) { existingPart.surveyNo = tdn.SurveyNo; existingPart.surveyNumber = tdn.SurveyNo; }
        if (tdn?.CadastralSurveyNo) existingPart.cadastralSurveyNo = tdn.CadastralSurveyNo;
        if (tdn?.SectionNo) existingPart.sectionNo = tdn.SectionNo;
        if (tdn?.ParcelNo) existingPart.parcelNo = tdn.ParcelNo;
        if (tdn?.PropLocationStreet) { existingPart.streetLocation = tdn.PropLocationStreet; existingPart.streetNo = tdn.PropLocationStreet; existingPart.buildingNo = tdn.PropLocationStreet; }
        if (tdn?.Location) existingPart.location = tdn.Location;
        if (tdn?.Purok) existingPart.purok = tdn.Purok;
        if (tdn?.DistrictOrBarangay) existingPart.districtOrBarangay = tdn.DistrictOrBarangay;
        if (tdn?.CityMunicipality) existingPart.cityMunicipality = tdn.CityMunicipality;
        if (tdn?.Province) existingPart.province = tdn.Province;
        if (tdn?.DisplayName) { existingPart.owner = tdn.DisplayName; existingPart.ownerName = tdn.DisplayName; existingPart.newOwnerName = tdn.DisplayName; }
        if (tdn?.OwnerAddress) existingPart.ownerAddress = tdn.OwnerAddress;
        if (tdn?.OwnerTIN) existingPart.ownerTin = tdn.OwnerTIN;
        if (tdn?.OwnerContactNo) existingPart.ownerContactNo = tdn.OwnerContactNo;
        if (tdn?.OwnerEmail) existingPart.ownerEmail = tdn.OwnerEmail;
        if (tdn?.OwnerGender) existingPart.ownerGender = tdn.OwnerGender;
        if (tdn?.OwnerCivilStatus) existingPart.ownerCivilStatus = tdn.OwnerCivilStatus;
        if (tdn?.TotalArea) existingPart.area = tdn.TotalArea;
        if (tdn?.TotalMarketValue) existingPart.marketValue = tdn.TotalMarketValue;
        if (tdn?.TotalAssessedValue) existingPart.assessedValue = tdn.TotalAssessedValue;
        if (tdn?.FloorPlanSketchPath) existingPart.sketchUrl = tdn.FloorPlanSketchPath;
        if (tdn?.Attachments && Array.isArray(tdn.Attachments)) existingPart.attachments = tdn.Attachments;

        // Land boundaries & schedules
        if (tdnLand.NorthBoundary !== undefined) existingPart.boundaryNorth = tdnLand.NorthBoundary;
        if (tdnLand.Nremarks !== undefined) existingPart.nremarks = tdnLand.Nremarks;
        if (tdnLand.SouthBoundary !== undefined) existingPart.boundarySouth = tdnLand.SouthBoundary;
        if (tdnLand.Sremarks !== undefined) existingPart.sremarks = tdnLand.Sremarks;
        if (tdnLand.EastBoundary !== undefined) existingPart.boundaryEast = tdnLand.EastBoundary;
        if (tdnLand.Eremarks !== undefined) existingPart.eremarks = tdnLand.Eremarks;
        if (tdnLand.WestBoundary !== undefined) existingPart.boundaryWest = tdnLand.WestBoundary;
        if (tdnLand.Wremarks !== undefined) existingPart.wremarks = tdnLand.Wremarks;
        if (tdn?.LandAppraisalDetailsJson || tdnLand.LandAppraisalDetailsJson) {
          existingPart.landAppraisalDetailsJson = tdn?.LandAppraisalDetailsJson || tdnLand.LandAppraisalDetailsJson;
        }
        if (tdn?.LandImprovementsJson || tdnLand.LandImprovementsJson) {
          existingPart.landImprovementsJson = tdn?.LandImprovementsJson || tdnLand.LandImprovementsJson;
        }
        if (tdn?.LandAdjustmentsJson || tdnLand.LandAdjustmentsJson) {
          existingPart.landAdjustmentsJson = tdn?.LandAdjustmentsJson || tdnLand.LandAdjustmentsJson;
        }
        if (tdn?.LandPropertyAssessmentsJson || tdnLand.LandPropertyAssessmentsJson) {
          existingPart.landPropertyAssessmentsJson = tdn?.LandPropertyAssessmentsJson || tdnLand.LandPropertyAssessmentsJson;
        }

        // Building specifics
        if (tdnBldg.StucturalType || tdnBldg.StructuralType) existingPart.structuralType = tdnBldg.StucturalType || tdnBldg.StructuralType;
        if (tdnBldg.BuildingClassification) existingPart.buildingClassification = tdnBldg.BuildingClassification;
        if (tdnBldg.TotalbldgArea || tdnBldg.TotalFloorArea) {
          existingPart.areaSqMeters = tdnBldg.TotalbldgArea || tdnBldg.TotalFloorArea;
          existingPart.area = tdnBldg.TotalbldgArea || tdnBldg.TotalFloorArea;
        }
        if (tdnBldg.TotalMarketVal) existingPart.marketValue = tdnBldg.TotalMarketVal;
        if (tdnBldg.TotalAssessedVal) existingPart.assessedValue = tdnBldg.TotalAssessedVal;

        existingPart.formData = pascalPayload;
        updatedPartitions[partIdx] = existingPart;
        itemRecord.partitions = updatedPartitions;

        if (partIdx === 0) {
          itemRecord.currentPartition = existingPart;
          itemRecord.formData = pascalPayload;
        }
      }
      if (tdn?.DisplayName) itemRecord.owner = tdn.DisplayName;

      const newDownloads = { ...downloadedForms, [dKey]: itemRecord };
      await saveDownloadedForms(newDownloads);
    } catch (e) {
      // Handled silently
    }
  };

  // =========================================================================
  // PERMANENT LOCAL HISTORY STORE (@fairtrack_history_records)
  // Preserves offline drafts, live syncs, and completed/transferred appraisals
  // =========================================================================
  const saveToHistory = async (payload, options = {}) => {
    try {
      if (!payload && !options.transItem) return;
      const tdn = payload?.TDN || payload?.tdn || {};
      const tdnLand = payload?.TDNLand || payload?.tdnLand || {};
      const tdnBldg = payload?.TDNBuilding || payload?.tdnBuilding || {};
      const tdnMach = payload?.TDNMachinery || payload?.tdnMachinery || {};

      const trackingNo = (
        options.trackingNo ||
        tdn.TrackingNo ||
        tdn.trackingNo ||
        payload?.TrackingNo ||
        payload?.trackingNo ||
        options.transItem?.TrackingNo ||
        ''
      ).toString().trim();

      const formWorkflowId =
        options.formWorkflowId != null
          ? options.formWorkflowId
          : (tdn.FormWorkflowId ?? tdn.formWorkflowId ?? payload?.FormWorkflowId ?? payload?.formWorkflowId ?? options.transItem?.FormworkflowId);

      const partitionIndex = parseInt(
        options.partitionIndex ?? tdn.PartitionItemIndex ?? payload?.partitionIndex ?? 0,
        10
      ) || 0;

      const partitionLetter =
        options.partitionLetter ||
        tdn.PartitionItemLetter ||
        tdn.PartitionIndex ||
        payload?.partitionLetter ||
        payload?.partitionIndexLetter ||
        String.fromCharCode(65 + partitionIndex);

      let propertyKind = options.propertyKind;
      if (!propertyKind) {
        if (tdnLand && (tdnLand.CadastralSurveyNo || tdnLand.NorthBoundary || tdnLand.LandAppraisalDetailsJson || tdnLand.TDNLandId)) propertyKind = 'Land';
        else if (tdnBldg && (tdnBldg.BuildingClassification || tdnBldg.StructuralType || tdnBldg.TotalFloorArea || tdnBldg.BuildingFloorsJson || tdnBldg.TDNBuildingId)) propertyKind = 'Building';
        else if (tdnMach && (tdnMach.AppraisalDetailsJson || tdnMach.PropertyAssessmentsJson || tdnMach.TDNMachineId)) propertyKind = 'Machinery';
        else if (options.transItem?.PropertyType) propertyKind = options.transItem.PropertyType;
        else if (options.type) {
          if (options.type.includes('LAND')) propertyKind = 'Land';
          else if (options.type.includes('BUILDING')) propertyKind = 'Building';
          else if (options.type.includes('MACHINERY')) propertyKind = 'Machinery';
        }
        if (!propertyKind) propertyKind = 'Property';
      }

      let marketValue = 0;
      let assessedValue = 0;
      if (tdn.TotalMarketValue != null) marketValue = parseFloat(tdn.TotalMarketValue) || 0;
      else if (tdnLand.TotalMarketValue != null) marketValue = parseFloat(tdnLand.TotalMarketValue) || 0;
      else if (tdnBldg.TotalMarketValue != null) marketValue = parseFloat(tdnBldg.TotalMarketValue) || 0;
      else if (tdnMach.TotalMarketValue != null) marketValue = parseFloat(tdnMach.TotalMarketValue) || 0;

      if (tdn.TotalAssessedValue != null) assessedValue = parseFloat(tdn.TotalAssessedValue) || 0;
      else if (tdnLand.TotalAssessedValue != null) assessedValue = parseFloat(tdnLand.TotalAssessedValue) || 0;
      else if (tdnBldg.TotalAssessedValue != null) assessedValue = parseFloat(tdnBldg.TotalAssessedValue) || 0;
      else if (tdnMach.TotalAssessedValue != null) assessedValue = parseFloat(tdnMach.TotalAssessedValue) || 0;

      const ownerName =
        tdn.DisplayName ||
        tdn.OwnerName ||
        payload?.ownerName ||
        options.transItem?.OwnerName ||
        options.ownerName ||
        'Unknown Owner';

      const arpNo = tdn.ARPNo || tdn.ArpNo || payload?.arpNo || options.transItem?.ArpNo || 'N/A';
      const pin = tdn.PIN || tdn.Pin || payload?.pin || options.transItem?.PIN || 'N/A';
      const declarationNo = tdn.DeclarationNo || tdn.SystemGeneratedTDN || tdn.declarationNo || 'N/A';
      const barangay = tdn.DistrictOrBarangay || tdn.Barangay || options.transItem?.Barangay || '';
      const cityMunicipality = tdn.CityMunicipality || options.transItem?.CityMunicipality || 'Koronadal City';
      const province = tdn.Province || options.transItem?.Province || 'South Cotabato';
      const streetLocation = tdn.PropLocationStreet || tdn.Location || '';
      const attachments = Array.isArray(tdn.Attachments) ? tdn.Attachments : (payload?.attachments || []);
      const sketchUrl = tdn.FloorPlanSketchPath || payload?.sketchUrl || null;

      const recordKey = `${(trackingNo || formWorkflowId || 'TX').toLowerCase()}_part_${partitionIndex}`;
      const nowIso = new Date().toISOString();

      const newRecord = {
        id: `${trackingNo || 'TX'}_${partitionLetter}_${Date.now()}`,
        recordKey,
        trackingNo,
        formWorkflowId,
        propertyKind,
        partitionIndex,
        partitionLetter,
        declarationNo,
        ownerName,
        arpNo,
        pin,
        barangay,
        cityMunicipality,
        province,
        streetLocation,
        marketValue,
        assessedValue,
        actionType: options.actionType || 'SYNCED', // 'TRANSFERRED' | 'SYNCED' | 'DRAFT' | 'DRAFT_TRANSFER'
        statusLabel: options.statusLabel || (options.actionType === 'TRANSFERRED' ? 'Transferred to Assessor Review' : 'Synced to Office Web Portal'),
        savedAt: nowIso,
        attachmentsCount: attachments.length,
        sketchUrl,
        payload,
        transItem: options.transItem || {
          TrackingNo: trackingNo,
          FormworkflowId: formWorkflowId,
          OwnerName: ownerName,
          Barangay: barangay,
          PropertyType: propertyKind,
          Status: options.statusLabel || 'Completed Field Assessment',
          HasPartition: true,
          PartitionCount: 1
        }
      };

      setHistoryRecords(prev => {
        const filtered = prev.filter(r => r.recordKey !== recordKey);
        const updated = [newRecord, ...filtered];
        AsyncStorage.setItem('@fairtrack_history_records', JSON.stringify(updated)).catch(e => {
          console.warn('Failed to persist history records', e);
        });
        return updated;
      });

      return newRecord;
    } catch (err) {
      console.warn('saveToHistory error:', err);
    }
  };

  const removeHistoryRecord = async (recordIdOrKey) => {
    try {
      const updated = historyRecords.filter(r => r.id !== recordIdOrKey && r.recordKey !== recordIdOrKey);
      setHistoryRecords(updated);
      await AsyncStorage.setItem('@fairtrack_history_records', JSON.stringify(updated));
      return { success: true };
    } catch (e) {
      return { success: false, message: e?.message };
    }
  };

  const clearHistoryRecords = async () => {
    try {
      setHistoryRecords([]);
      await AsyncStorage.removeItem('@fairtrack_history_records');
      return { success: true };
    } catch (e) {
      return { success: false, message: e?.message };
    }
  };

  // Mark transaction synced: clears pending syncQueue items, saves to History, and if transferred, removes from downloadedForms
  const markTransactionSynced = async (trackingNo, formWorkflowId, isTransferred = false, payload = null) => {
    try {
      const cleanTrack = (trackingNo || '').toString().trim().toLowerCase();
      const cleanWf = formWorkflowId != null ? formWorkflowId.toString().trim() : '';

      // Find existing downloaded record before removing if payload was not provided
      let effectivePayload = payload;
      let existingDownloaded = null;
      if (downloadedForms) {
        if (trackingNo && downloadedForms[trackingNo]) existingDownloaded = downloadedForms[trackingNo];
        else if (formWorkflowId && downloadedForms[formWorkflowId.toString()]) existingDownloaded = downloadedForms[formWorkflowId.toString()];
        else {
          for (const [k, item] of Object.entries(downloadedForms)) {
            const keyLower = k.toLowerCase();
            const itemTNo = (item?.transItem?.TrackingNo || item?.trackingNo || '').toString().trim().toLowerCase();
            const itemWfId = (item?.transItem?.FormworkflowId || item?.formWorkflowId || '').toString().trim();
            if ((cleanTrack && (keyLower === cleanTrack || itemTNo === cleanTrack)) || (cleanWf && (k === cleanWf || itemWfId === cleanWf))) {
              existingDownloaded = item;
              break;
            }
          }
        }
      }

      if (!effectivePayload && existingDownloaded) {
        effectivePayload = existingDownloaded.formData || existingDownloaded;
      }

      // Save permanently to History before purging
      if (effectivePayload || existingDownloaded) {
        await saveToHistory(effectivePayload, {
          trackingNo,
          formWorkflowId,
          actionType: isTransferred ? 'TRANSFERRED' : 'SYNCED',
          statusLabel: isTransferred ? 'Transferred to Assessor Review' : 'Synced to Office Web Portal',
          transItem: existingDownloaded?.transItem || null
        });
      }

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
      console.warn('markTransactionSynced error:', e);
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

    if (!found && !pendingDraft) {
      if (historyRecords && historyRecords.length > 0) {
        const histMatch = historyRecords.find(h => {
          const hTrack = (h.trackingNo || '').toString().trim().toLowerCase();
          const hWf = (h.formWorkflowId || '').toString().trim();
          const hIdx = parseInt(h.partitionIndex ?? 0, 10) || 0;
          const trackMatches = tNo && hTrack && tNo === hTrack;
          const wfMatches = wfId && hWf && wfId === hWf;
          return (trackMatches || wfMatches) && hIdx === targetIdx;
        }) || historyRecords.find(h => {
          const hTrack = (h.trackingNo || '').toString().trim().toLowerCase();
          const hWf = (h.formWorkflowId || '').toString().trim();
          const trackMatches = tNo && hTrack && tNo === hTrack;
          const wfMatches = wfId && hWf && wfId === hWf;
          return trackMatches || wfMatches;
        });

        if (histMatch) {
          const histPayload = histMatch.payload ? withPascalCaseKeys(histMatch.payload) : {};
          const tdn = histPayload?.TDN || histPayload?.tdn || {};
          const tdnLand = histPayload?.TDNLand || histPayload?.tdnLand || {};
          const tdnBldg = histPayload?.TDNBuilding || histPayload?.tdnBuilding || {};
          const tdnMach = histPayload?.TDNMachinery || histPayload?.tdnMachinery || {};

          return {
            trackingNo: histMatch.trackingNo,
            formWorkflowId: histMatch.formWorkflowId,
            owner: histMatch.ownerName,
            arpNo: histMatch.arpNo,
            pin: histMatch.pin,
            tdnId: tdn?.TDNId || tdn?.tdnId,
            sketchUrl: histMatch.sketchUrl || tdn?.FloorPlanSketchPath,
            attachments: tdn?.Attachments || histPayload?.attachments || [],
            isHistorySnapshot: true,
            historyActionType: histMatch.actionType,
            historySavedAt: histMatch.savedAt,
            transItem: histMatch.transItem || {
              TrackingNo: histMatch.trackingNo,
              FormworkflowId: histMatch.formWorkflowId,
              OwnerName: histMatch.ownerName,
              Barangay: histMatch.barangay,
              PropertyType: histMatch.propertyKind,
              Status: histMatch.statusLabel,
              HasPartition: true,
              PartitionCount: 1
            },
            formData: histPayload,
            currentPartition: {
              partitionLetter: histMatch.partitionLetter || 'A',
              systemGeneratedTDN: tdn?.SystemGeneratedTDN || tdn?.DeclarationNo || histMatch.declarationNo || '',
              arpNo: histMatch.arpNo,
              pin: histMatch.pin,
              ownerName: histMatch.ownerName,
              ownerAddress: tdn?.OwnerAddress,
              ownerTin: tdn?.OwnerTIN || tdn?.OwnerTin,
              ownerContactNo: tdn?.OwnerContactNo,
              ownerEmail: tdn?.OwnerEmail,
              ownerGender: tdn?.OwnerGender || 'Male',
              districtOrBarangay: histMatch.barangay,
              cityMunicipality: histMatch.cityMunicipality,
              province: histMatch.province,
              streetLocation: histMatch.streetLocation,
              sketchUrl: histMatch.sketchUrl || tdn?.FloorPlanSketchPath,
              attachments: tdn?.Attachments || histPayload?.attachments || [],
              boundaryNorth: tdnLand?.NorthBoundary,
              nremarks: tdnLand?.Nremarks,
              boundarySouth: tdnLand?.SouthBoundary,
              sremarks: tdnLand?.Sremarks,
              boundaryEast: tdnLand?.EastBoundary,
              eremarks: tdnLand?.Eremarks,
              boundaryWest: tdnLand?.WestBoundary,
              wremarks: tdnLand?.Wremarks,
              landAppraisalDetailsJson: tdn?.LandAppraisalDetailsJson || tdnLand?.LandAppraisalDetailsJson,
              landImprovementsJson: tdn?.LandImprovementsJson || tdnLand?.LandImprovementsJson,
              landAdjustmentsJson: tdn?.LandAdjustmentsJson || tdnLand?.LandAdjustmentsJson,
              landPropertyAssessmentsJson: tdn?.LandPropertyAssessmentsJson || tdnLand?.LandPropertyAssessmentsJson,
              buildingType: tdnBldg?.BuildingClassification || tdnBldg?.BuildingType,
              structuralType: tdnBldg?.StucturalType || tdnBldg?.StructuralType,
              areaSqMeters: tdnBldg?.TotalbldgArea || tdnBldg?.TotalFloorArea,
              buildingFloorsJson: tdnBldg?.BuildingFloorsJson,
              additionalItemsJson: tdnBldg?.AdditionalItemsJson,
              machineryAppraisalDetailsJson: tdn?.MachineryAppraisalDetailsJson || tdnMach?.MachineryAppraisalDetailsJson || tdn?.AppraisalDetailsJson || tdnMach?.AppraisalDetailsJson,
              machineryPropertyAssessmentsJson: tdn?.MachineryPropertyAssessmentsJson || tdnMach?.MachineryPropertyAssessmentsJson || tdn?.PropertyAssessmentsJson || tdnMach?.PropertyAssessmentsJson,
              formData: histPayload
            },
            partitions: [
              {
                partitionLetter: histMatch.partitionLetter || 'A',
                formData: histPayload
              }
            ]
          };
        }
      }
      return null;
    }

    if (found && Array.isArray(found.partitions) && found.partitions.length > targetIdx && found.partitions[targetIdx]) {
      const partObj = { ...found.partitions[targetIdx] };
      // Strict isolation: partition must only use its own formData, never cross-pollinate from another partition
      let partFormData = partObj.formData ? withPascalCaseKeys(partObj.formData) : null;

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

    if (found && found.formData && targetIdx === 0) {
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

    // Save to permanent device history as offline draft
    await saveToHistory(payload, {
      type,
      actionType: workflowForwardPayload ? 'DRAFT_TRANSFER' : 'DRAFT',
      statusLabel: workflowForwardPayload ? 'Queued to Transfer (Offline)' : 'Saved Offline Draft'
    });

    // Update cachedForms
    const cacheId = tdn?.TDNId || tracking || wfId || 'temp';
    const formKey = `${type}_${cacheId}_part_${partIdx}`;
    const newCachedForms = { ...cachedForms, [formKey]: payload };
    setCachedForms(newCachedForms);
    try {
      await AsyncStorage.setItem('@fairtrack_cached_forms', JSON.stringify(newCachedForms));
    } catch (err) {
      console.warn('Saving cached forms storage warning:', err);
    }

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
          buildingType: tdnBldg.BuildingClassification || tdnBldg.BuildingType || existingPart.buildingType,
          buildingClassification: tdnBldg.BuildingClassification || existingPart.buildingClassification,
          structuralType: tdnBldg.StucturalType || tdnBldg.StructuralType || existingPart.structuralType,
          areaSqMeters: tdnBldg.TotalbldgArea || tdnBldg.TotalFloorArea || existingPart.areaSqMeters,
          quarterEffectivity: tdn?.QuarterEffectivity ? (typeof tdn.QuarterEffectivity === 'number' ? `${tdn.QuarterEffectivity}${tdn.QuarterEffectivity === 1 ? 'st' : tdn.QuarterEffectivity === 2 ? 'nd' : tdn.QuarterEffectivity === 3 ? 'rd' : 'th'} Quarter` : tdn.QuarterEffectivity) : existingPart.quarterEffectivity,
          yearEffectivity: tdn?.YearEffectivity ? tdn.YearEffectivity.toString() : existingPart.yearEffectivity,
          taxability: tdn?.Taxability !== undefined ? (tdn.Taxability ? 'Taxable' : 'Exempt') : existingPart.taxability,
          assessmentBasis: tdn?.AssessmentBasis || existingPart.assessmentBasis,
          memoranda: tdn?.Memoranda || existingPart.memoranda,
          formData: withPascalCaseKeys(payload)
        };
        itemRecord.partitions = updatedPartitions;
      }
      if (partIdx === 0 && !itemRecord.formData) {
        itemRecord.formData = withPascalCaseKeys(payload);
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
            item.payload.remarks,
            item.payload.trackingNo
          );
        }

        if (res && res.success) {
          // If this update included a workflow transfer to the next step, forward it too
          let wasTransferred = false;
          const p = item.payload;
          const tdn = p?.TDN || p?.tdn;
          const itemTrack = tdn?.TrackingNo || tdn?.trackingNo;
          const fwdTrack = item.workflowForwardPayload?.trackingNo || itemTrack;

          if (item.workflowForwardPayload && (item.workflowForwardPayload.formWorkflowId || fwdTrack) && item.workflowForwardPayload.nextStatusId) {
            try {
              await MobileApi.forwardWorkflow(
                item.workflowForwardPayload.formWorkflowId,
                item.workflowForwardPayload.nextStatusId,
                item.workflowForwardPayload.remarks || 'Completed field assessment and synced.',
                fwdTrack
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
            await saveToHistory(p, {
              type: item.type,
              trackingNo: itemTrack,
              formWorkflowId: itemWf,
              actionType: 'TRANSFERRED',
              statusLabel: 'Transferred to Assessor Review'
            });
            await removeDownloadedTransaction(itemTrack, itemWf);
          } else {
            const p = item.payload;
            const tdn = p?.TDN || p?.tdn;
            const itemTrack = tdn?.TrackingNo || tdn?.trackingNo;
            const itemWf = tdn?.FormWorkflowId || tdn?.formWorkflowId;
            await saveToHistory(p, {
              type: item.type,
              trackingNo: itemTrack,
              formWorkflowId: itemWf,
              actionType: 'SYNCED',
              statusLabel: 'Synced to Office Web Portal'
            });
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

  // Transaction-Level Sync Engine: Sync all partitions of a transaction to web, and optionally forward workflow to supervisor
  const syncTransactionToServer = async (trackingNo, formWorkflowId, forwardToSupervisor = false, remarks = '') => {
    const online = await checkConnectivity(3000);
    if (!online) {
      return {
        success: false,
        isOffline: true,
        message: 'Office server is unreachable. Please connect to the office network or Wi-Fi before syncing.'
      };
    }

    const cleanTrack = (trackingNo || '').toString().trim().toLowerCase();
    const cleanWf = formWorkflowId != null ? formWorkflowId.toString().trim() : '';

    // Match all items in syncQueue for this transaction
    const targetQueueItems = syncQueue.filter(item => {
      const p = item.payload;
      const tdn = p?.TDN || p?.tdn;
      const itemTracking = (tdn?.TrackingNo || tdn?.trackingNo || p?.TrackingNo || p?.trackingNo || '').toString().trim().toLowerCase();
      const itemWfId = (tdn?.FormWorkflowId || tdn?.formWorkflowId || p?.FormWorkflowId || p?.formWorkflowId || item.workflowForwardPayload?.FormWorkflowId || item.workflowForwardPayload?.formWorkflowId || '').toString().trim();

      const trackMatches = cleanTrack && itemTracking && cleanTrack === itemTracking;
      const wfMatches = cleanWf && itemWfId && cleanWf === itemWfId;
      return trackMatches || wfMatches;
    });

    setIsSyncing(true);
    let successCount = 0;
    let failCount = 0;
    const processedIds = [];
    const newLogs = [];

    for (let i = 0; i < targetQueueItems.length; i++) {
      const item = targetQueueItems[i];
      try {
        let res = null;
        if (item.type === 'UPDATE_BUILDING') {
          res = await MobileApi.updateBuildingForm(item.payload);
        } else if (item.type === 'UPDATE_LAND') {
          res = await MobileApi.updateLandForm(item.payload);
        } else if (item.type === 'UPDATE_MACHINERY') {
          res = await MobileApi.updateMachineryForm(item.payload);
        }

        if (res && res.success) {
          successCount++;
          processedIds.push(item.id);
          const p = item.payload;
          const tdn = p?.TDN || p?.tdn;
          const itemTrack = tdn?.TrackingNo || tdn?.trackingNo || trackingNo;
          const itemWf = tdn?.FormWorkflowId || tdn?.formWorkflowId || formWorkflowId;

          await saveToHistory(p, {
            type: item.type,
            trackingNo: itemTrack,
            formWorkflowId: itemWf,
            actionType: forwardToSupervisor ? 'TRANSFERRED' : 'SYNCED',
            statusLabel: forwardToSupervisor ? 'Transferred to Assessor Review' : 'Synced to Office Web Portal'
          });
          await updateDownloadedFormWithPayload(itemTrack, itemWf, p);

          newLogs.unshift({
            id: item.id,
            title: item.title,
            status: 'SUCCESS',
            time: new Date().toLocaleTimeString(),
            message: res.message || 'Synced partition to office portal.'
          });
        } else {
          failCount++;
          newLogs.unshift({
            id: item.id,
            title: item.title,
            status: 'FAILED',
            time: new Date().toLocaleTimeString(),
            message: res?.message || 'Server rejected partition update.'
          });
        }
      } catch (err) {
        failCount++;
        newLogs.unshift({
          id: item.id,
          title: item.title,
          status: 'ERROR',
          time: new Date().toLocaleTimeString(),
          message: err.response?.data?.message || err.message || 'Server error syncing partition'
        });
      }
    }

    // Remove successfully processed items from syncQueue
    if (processedIds.length > 0) {
      const remainingQueue = syncQueue.filter(q => !processedIds.includes(q.id));
      await saveQueue(remainingQueue);
    }
    if (newLogs.length > 0) {
      setSyncLogs(prev => [...newLogs, ...prev].slice(0, 50));
    }

    // Forward to supervisor if requested
    let forwarded = false;
    if (forwardToSupervisor) {
      if (failCount > 0) {
        setIsSyncing(false);
        return {
          success: false,
          syncedCount: successCount,
          failCount,
          message: `Cannot forward to supervisor because ${failCount} partition edit(s) failed to sync.`
        };
      }

      const wfNumericId = parseInt(cleanWf, 10);
      if (!wfNumericId || isNaN(wfNumericId)) {
        setIsSyncing(false);
        return {
          success: false,
          syncedCount: successCount,
          message: 'Invalid Form Workflow ID for forwarding to supervisor.'
        };
      }

      try {
        const fwdRes = await MobileApi.forwardWorkflow(
          wfNumericId,
          4, // 4 = Checked / Recommend Approval (Supervisor)
          remarks || 'Field inspection completed and transferred to supervisor.'
        );

        if (fwdRes && fwdRes.success) {
          forwarded = true;
          // Mark transaction as transferred -> removes from downloaded forms and updates history
          await markTransactionSynced(trackingNo, formWorkflowId, true);
        } else {
          setIsSyncing(false);
          return {
            success: false,
            syncedCount: successCount,
            message: fwdRes?.message || 'Partitions synced, but failed to forward workflow to supervisor.'
          };
        }
      } catch (fwdErr) {
        setIsSyncing(false);
        return {
          success: false,
          syncedCount: successCount,
          message: fwdErr.response?.data?.message || fwdErr.message || 'Error forwarding workflow to supervisor.'
        };
      }
    }

    setIsSyncing(false);

    const syncTimeStr = new Date().toLocaleString();
    setLastSyncTime(syncTimeStr);
    await AsyncStorage.setItem('@fairtrack_last_sync', syncTimeStr);

    let displayMessage = '';
    if (failCount > 0) {
      const firstErr = newLogs.find(l => l.status !== 'SUCCESS');
      displayMessage = `${failCount} partition(s) failed to sync for ${trackingNo}` +
        (successCount > 0 ? ` (${successCount} succeeded)` : '') +
        `. ${firstErr?.message || 'Server error.'}`;
    } else if (forwardToSupervisor) {
      displayMessage = `Transaction ${trackingNo} successfully synchronized and transferred to Supervisor!`;
    } else if (successCount > 0) {
      displayMessage = `Successfully synchronized ${successCount} partition(s) for ${trackingNo} to the web portal.`;
    } else {
      displayMessage = `Transaction ${trackingNo} is already up to date with the web portal.`;
    }

    return {
      success: failCount === 0,
      syncedCount: successCount,
      failCount,
      forwarded,
      message: displayMessage
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
        updatePartitionAttachmentsLocally,
        updateDownloadedFormWithPayload,
        queueOfflineUpdate,
        syncAllToServer,
        syncTransactionToServer,
        removeQueueItem,
        clearQueue,
        clearAllOfflineData,
        historyRecords,
        historyCount: historyRecords.length,
        saveToHistory,
        removeHistoryRecord,
        clearHistoryRecords
      }}
    >
      {children}
    </SyncContext.Provider>
  );
};
