import { Platform } from 'react-native';
import * as FileSystem from 'expo-file-system';
import AsyncStorage from '@react-native-async-storage/async-storage';

// =============================================================================
// PERSISTENT OFFLINE STORAGE FOR FAIRTRACK MOBILE (DEVICE FOLDER & INDEXEDDB)
// =============================================================================

// IndexedDB helper for Web browser persistence (bypasses 5MB localStorage limit)
const openWebDb = () => {
  return new Promise((resolve) => {
    if (typeof window === 'undefined' || !window.indexedDB) {
      return resolve(null);
    }
    try {
      const request = window.indexedDB.open('FairTrackOfflineStore', 1);
      request.onupgradeneeded = (e) => {
        const db = e.target.result;
        if (!db.objectStoreNames.contains('files')) {
          db.createObjectStore('files', { keyPath: 'id' });
        }
      };
      request.onsuccess = (e) => resolve(e.target.result);
      request.onerror = () => resolve(null);
    } catch (e) {
      resolve(null);
    }
  });
};

/**
 * Save an inspection photo or sketch physically to the mobile device folder.
 * On native mobile: saves to FileSystem.documentDirectory (InspectionAttachments/ or InspectionSketches/)
 * On web: saves to browser IndexedDB so it survives page refresh without exceeding localStorage quota.
 */
export const saveFileLocally = async ({ trackingNo, fileName, base64, uri, type = 'attachment' }) => {
  const cleanTracking = (trackingNo || 'general').replace(/[^a-zA-Z0-9_\-]/g, '_');
  const cleanName = (fileName || `photo_${Date.now()}.jpg`).replace(/[^a-zA-Z0-9_\-.]/g, '_');
  const fileId = `${cleanTracking}_${Date.now()}_${cleanName}`;

  if (Platform.OS === 'web') {
    const dataUrl = base64 ? `data:image/jpeg;base64,${base64}` : (uri || '');
    try {
      const db = await openWebDb();
      if (db) {
        await new Promise((resolve) => {
          const tx = db.transaction('files', 'readwrite');
          const store = tx.objectStore('files');
          store.put({
            id: fileId,
            data: dataUrl,
            fileName: cleanName,
            trackingNo: cleanTracking,
            base64: base64 || null,
            savedAt: new Date().toISOString()
          });
          tx.oncomplete = () => resolve(true);
          tx.onerror = () => resolve(false);
        });
      }
    } catch (e) {
      console.warn('Web IndexedDB save warning:', e);
    }

    return {
      localUri: dataUrl,
      fileId: fileId,
      base64: base64
    };
  }

  // Native Mobile (Android / iOS): Physical directory on device storage
  try {
    const subFolder = type === 'sketch' ? 'InspectionSketches' : 'InspectionAttachments';
    const folderPath = `${FileSystem.documentDirectory}${subFolder}/${cleanTracking}/`;

    const dirInfo = await FileSystem.getInfoAsync(folderPath);
    if (!dirInfo.exists) {
      await FileSystem.makeDirectoryAsync(folderPath, { intermediates: true });
    }

    const localFilePath = `${folderPath}${cleanName}`;

    if (base64) {
      let cleanB64 = base64;
      if (cleanB64.includes(',')) cleanB64 = cleanB64.split(',')[1];
      await FileSystem.writeAsStringAsync(localFilePath, cleanB64, {
        encoding: FileSystem.EncodingType.Base64
      });
    } else if (uri && uri !== localFilePath) {
      await FileSystem.copyAsync({ from: uri, to: localFilePath });
    }

    return {
      localUri: localFilePath,
      fileId: fileId,
      base64: base64
    };
  } catch (err) {
    console.warn('Native mobile physical file save warning:', err);
    return {
      localUri: uri,
      fileId: fileId,
      base64: base64
    };
  }
};

/**
 * Downloads a server file to the physical mobile device storage when downloading from web.
 */
export const downloadServerFileToMobile = async (serverUrl, trackingNo, fileName, type = 'attachment') => {
  if (!serverUrl || serverUrl.startsWith('data:') || serverUrl.startsWith('file:')) {
    return serverUrl;
  }

  if (Platform.OS === 'web') {
    return serverUrl; // Browser can load server URLs over HTTP directly
  }

  try {
    const cleanTracking = (trackingNo || 'general').replace(/[^a-zA-Z0-9_\-]/g, '_');
    const cleanName = (fileName || serverUrl.split('/').pop() || `file_${Date.now()}.jpg`).replace(/[^a-zA-Z0-9_\-.]/g, '_');
    const subFolder = type === 'sketch' ? 'InspectionSketches' : 'InspectionAttachments';
    const folderPath = `${FileSystem.documentDirectory}${subFolder}/${cleanTracking}/`;

    const dirInfo = await FileSystem.getInfoAsync(folderPath);
    if (!dirInfo.exists) {
      await FileSystem.makeDirectoryAsync(folderPath, { intermediates: true });
    }

    const localFilePath = `${folderPath}${cleanName}`;
    const fileInfo = await FileSystem.getInfoAsync(localFilePath);
    if (fileInfo.exists) {
      return localFilePath;
    }

    const res = await FileSystem.downloadAsync(serverUrl, localFilePath);
    return res.uri;
  } catch (e) {
    console.warn('downloadServerFileToMobile warning:', e);
    return serverUrl;
  }
};

/**
 * Save partition offline attachments & sketch so they survive page reloads unconditionally.
 */
export const savePartitionAttachmentsOffline = async (trackingNo, partitionIndex, attachments, sketchUrl) => {
  const cleanTracking = (trackingNo || 'general').replace(/[^a-zA-Z0-9_\-]/g, '_');
  const partIdx = parseInt(partitionIndex, 10) || 0;
  const storeKey = `draft_atts_${cleanTracking}_${partIdx}`;

  if (Platform.OS === 'web') {
    try {
      const db = await openWebDb();
      if (db) {
        await new Promise((resolve) => {
          const tx = db.transaction('files', 'readwrite');
          const store = tx.objectStore('files');
          store.put({
            id: storeKey,
            attachments: attachments || [],
            sketchUrl: sketchUrl || '',
            savedAt: new Date().toISOString()
          });
          tx.oncomplete = () => resolve(true);
          tx.onerror = () => resolve(false);
        });
      }
    } catch (e) {
      console.warn('savePartitionAttachmentsOffline IndexedDB warning:', e);
    }
  }

  // Also save in AsyncStorage (with lightweight items if needed)
  try {
    const lightweightAtts = (attachments || []).map(a => {
      return {
        ...a,
        base64Data: null
      };
    });
    await AsyncStorage.setItem(`@fairtrack_part_draft_${cleanTracking}_${partIdx}`, JSON.stringify({
      attachments: Platform.OS === 'web' ? attachments : lightweightAtts,
      sketchUrl: sketchUrl || '',
      savedAt: new Date().toISOString()
    }));
  } catch (e) {
    try {
      const bareAtts = (attachments || []).map(a => ({
        id: a.id,
        fileName: a.fileName,
        fileType: a.fileType,
        fileSize: a.fileSize,
        caption: a.caption
      }));
      await AsyncStorage.setItem(`@fairtrack_part_draft_${cleanTracking}_${partIdx}`, JSON.stringify({
        attachments: bareAtts,
        sketchUrl: (sketchUrl && sketchUrl.length < 500) ? sketchUrl : '',
        savedAt: new Date().toISOString()
      }));
    } catch (e2) {}
  }
};

/**
 * Load partition offline attachments & sketch on reload.
 */
export const loadPartitionAttachmentsOffline = async (trackingNo, partitionIndex) => {
  const cleanTracking = (trackingNo || 'general').replace(/[^a-zA-Z0-9_\-]/g, '_');
  const partIdx = parseInt(partitionIndex, 10) || 0;
  const storeKey = `draft_atts_${cleanTracking}_${partIdx}`;

  // 1. On Web, first try IndexedDB (holds full images without 5MB quota limits)
  if (Platform.OS === 'web') {
    try {
      const db = await openWebDb();
      if (db) {
        const record = await new Promise((resolve) => {
          const tx = db.transaction('files', 'readonly');
          const store = tx.objectStore('files');
          const req = store.get(storeKey);
          req.onsuccess = () => resolve(req.result);
          req.onerror = () => resolve(null);
        });
        if (record && (record.attachments?.length > 0 || record.sketchUrl)) {
          return {
            attachments: record.attachments || [],
            sketchUrl: record.sketchUrl || ''
          };
        }
      }
    } catch (e) {
      console.warn('loadPartitionAttachmentsOffline IndexedDB warning:', e);
    }
  }

  // 2. Try AsyncStorage
  try {
    const raw = await AsyncStorage.getItem(`@fairtrack_part_draft_${cleanTracking}_${partIdx}`);
    if (raw) {
      const parsed = JSON.parse(raw);
      if (parsed && (parsed.attachments?.length > 0 || parsed.sketchUrl)) {
        return {
          attachments: parsed.attachments || [],
          sketchUrl: parsed.sketchUrl || ''
        };
      }
    }
  } catch (e) {
    console.warn('loadPartitionAttachmentsOffline AsyncStorage warning:', e);
  }

  return { attachments: [], sketchUrl: '' };
};
