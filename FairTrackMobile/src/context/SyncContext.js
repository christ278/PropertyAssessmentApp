import React, { createContext, useState, useEffect } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MobileApi } from '../api/mobileApi';

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

  useEffect(() => {
    loadLocalQueue();
  }, []);

  const loadLocalQueue = async () => {
    try {
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
  const removeDownloadedTransaction = async (key) => {
    try {
      const current = { ...downloadedForms };
      delete current[key.toString()];
      await saveDownloadedForms(current);
      return true;
    } catch (e) {
      console.warn('Failed to remove downloaded transaction:', e);
      return false;
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
      const itemWfId = (tdn?.FormWorkflowId || tdn?.formWorkflowId || p?.FormWorkflowId || p?.formWorkflowId || item.workflowForwardPayload?.FormWorkflowId || '').toString().trim();

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

  // Check if a transaction is already downloaded
  const isTransactionDownloaded = (trackingNo, formWorkflowId) => {
    if (trackingNo && downloadedForms[trackingNo]) return true;
    if (formWorkflowId && downloadedForms[formWorkflowId.toString()]) return true;
    return false;
  };

  // Retrieve downloaded form data
  const getDownloadedFormData = (trackingNo, formWorkflowId, partitionIndex = 0) => {
    let found = null;
    if (trackingNo && downloadedForms[trackingNo]) {
      found = downloadedForms[trackingNo];
    } else if (formWorkflowId && downloadedForms[formWorkflowId.toString()]) {
      found = downloadedForms[formWorkflowId.toString()];
    }
    if (!found) return null;

    const targetIdx = parseInt(partitionIndex, 10) || 0;
    if (Array.isArray(found.partitions) && found.partitions.length > targetIdx && found.partitions[targetIdx]) {
      const partObj = found.partitions[targetIdx];
      const partFormData = partObj.formData ? withPascalCaseKeys(partObj.formData) : (found.formData ? withPascalCaseKeys(found.formData) : null);
      return {
        ...found,
        selectedPartitionIndex: targetIdx,
        formType: partObj.formType || found.formType,
        transactionTypeId: partObj.transactionTypeId || found.transactionTypeId,
        owner: partObj.ownerName || found.owner,
        arpNo: partObj.systemGeneratedTDN || found.arpNo,
        sketchUrl: partObj.sketchUrl || found.sketchUrl,
        sketchFileName: partObj.sketchFileName || found.sketchFileName,
        attachments: partObj.attachments || found.attachments || [],
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

    const updated = [queueItem, ...syncQueue];
    await saveQueue(updated);

    // Update local cached form representation
    if (payload && payload.TDN && payload.TDN.TDNId) {
      const partIdx = parseInt(payload.TDN.PartitionItemIndex, 10) || 0;
      const formKey = `${type}_${payload.TDN.TDNId}_part_${partIdx}`;
      const newCachedForms = { ...cachedForms, [formKey]: payload };
      setCachedForms(newCachedForms);
      await AsyncStorage.setItem('@fairtrack_cached_forms', JSON.stringify(newCachedForms));

      // Also update downloadedForms cache so switching partitions offline reflects latest edits
      const tracking = payload.TDN.TrackingNo;
      const wfId = payload.TDN.FormWorkflowId;
      const dKey = (tracking || wfId || '').toString();
      if (dKey && downloadedForms[dKey]) {
        const itemRecord = { ...downloadedForms[dKey] };
        if (Array.isArray(itemRecord.partitions) && itemRecord.partitions.length > partIdx) {
          const updatedPartitions = [...itemRecord.partitions];
          updatedPartitions[partIdx] = {
            ...updatedPartitions[partIdx],
            formData: payload,
            sketchUrl: payload.TDN.FloorPlanSketchPath || updatedPartitions[partIdx].sketchUrl,
            attachments: payload.TDN.Attachments || updatedPartitions[partIdx].attachments
          };
          itemRecord.partitions = updatedPartitions;
        }
        if (partIdx === 0) {
          itemRecord.formData = payload;
        }
        const newDownloads = { ...downloadedForms, [dKey]: itemRecord };
        setDownloadedForms(newDownloads);
        await AsyncStorage.setItem('@fairtrack_downloaded_forms', JSON.stringify(newDownloads));
      }
    }

    return queueItem;
  };

  // One-Tap Batch Sync Engine
  const syncAllToServer = async () => {
    if (syncQueue.length === 0) {
      return { success: true, count: 0, message: 'Sync queue is empty. No offline edits pending.' };
    }

    setIsSyncing(true);
    let successCount = 0;
    let failureCount = 0;
    const remainingQueue = [];
    const newLogs = [];

    for (const item of syncQueue) {
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
          if (item.workflowForwardPayload && item.workflowForwardPayload.formWorkflowId && item.workflowForwardPayload.nextStatusId) {
            try {
              await MobileApi.forwardWorkflow(
                item.workflowForwardPayload.formWorkflowId,
                item.workflowForwardPayload.nextStatusId,
                item.workflowForwardPayload.remarks || 'Completed field assessment and synced.'
              );
            } catch (fwdErr) {
              console.warn('Auto workflow forward warning:', fwdErr);
            }
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
        newLogs.unshift({
          id: item.id,
          title: item.title,
          status: 'ERROR',
          time: new Date().toLocaleTimeString(),
          message: err.response?.data?.message || err.message || 'Network / Server error'
        });
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
        getOfflineTransactions,
        downloadSingleTransaction,
        downloadAllTransactions,
        removeDownloadedTransaction,
        isTransactionDownloaded,
        hasPendingEdits,
        getDownloadedFormData,
        queueOfflineUpdate,
        syncAllToServer,
        removeQueueItem,
        clearQueue
      }}
    >
      {children}
    </SyncContext.Provider>
  );
};
