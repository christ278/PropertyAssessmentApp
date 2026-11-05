import React, { createContext, useState, useEffect } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MobileApi } from '../api/mobileApi';

export const SyncContext = createContext();

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
        downloadsMap = JSON.parse(storedDownloads);
        setDownloadedForms(downloadsMap);
      }

      // Offline transactions are strictly derived from explicitly downloaded files
      const downloadedList = Object.values(downloadsMap)
        .map(d => d.transItem)
        .filter(Boolean);

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
          .map(d => d.transItem)
          .filter(Boolean);
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
      .map(d => d.transItem)
      .filter(Boolean);
    setCachedTransactions(downloadedList);
  };

  // Download a single transaction form details
  const downloadSingleTransaction = async (transItem) => {
    try {
      const res = await MobileApi.downloadTransactionDetails(transItem.FormworkflowId, transItem.TrackingNo);
      if (res && res.success) {
        const key = (transItem.TrackingNo || transItem.FormworkflowId).toString();
        const updated = {
          ...downloadedForms,
          [key]: {
            ...res,
            transItem,
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

  // Batch download multiple transactions
  const downloadAllTransactions = async (transactionsList) => {
    if (!transactionsList || transactionsList.length === 0) {
      return { success: false, message: 'No transactions available to download.' };
    }

    setIsDownloading(true);
    let successCount = 0;
    let failCount = 0;
    const currentDownloads = { ...downloadedForms };

    for (const item of transactionsList) {
      try {
        const res = await MobileApi.downloadTransactionDetails(item.FormworkflowId, item.TrackingNo);
        if (res && res.success) {
          const key = (item.TrackingNo || item.FormworkflowId).toString();
          currentDownloads[key] = {
            ...res,
            transItem: item,
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
      total: transactionsList.length
    };
  };

  // Check if a transaction is already downloaded
  const isTransactionDownloaded = (trackingNo, formWorkflowId) => {
    if (trackingNo && downloadedForms[trackingNo]) return true;
    if (formWorkflowId && downloadedForms[formWorkflowId.toString()]) return true;
    return false;
  };

  // Retrieve downloaded form data
  const getDownloadedFormData = (trackingNo, formWorkflowId) => {
    if (trackingNo && downloadedForms[trackingNo]) {
      return downloadedForms[trackingNo];
    }
    if (formWorkflowId && downloadedForms[formWorkflowId.toString()]) {
      return downloadedForms[formWorkflowId.toString()];
    }
    return null;
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
      const formKey = `${type}_${payload.TDN.TDNId}`;
      const newCachedForms = { ...cachedForms, [formKey]: payload };
      setCachedForms(newCachedForms);
      await AsyncStorage.setItem('@fairtrack_cached_forms', JSON.stringify(newCachedForms));
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
