import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  Image,
  StyleSheet,
  Alert,
  ActivityIndicator,
  Modal,
  TextInput,
  Platform
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { resolveServerUrl, DEFAULT_BASE_URL } from '../api/apiClient';
import { useTheme } from '../context/ThemeContext';
import { ActionModal } from './ActionModal';
import { saveFileLocally } from '../utils/fileStorage';

export const AttachmentsSection = ({
  trackingNo,
  formWorkflowId = null,
  partitionIndex = 0,
  sketchUrl,
  sketchBase64,
  attachments = [],
  onAttachmentsChange,
  onSketchChange,
  propertyKind = 'Building', // 'Building' | 'Land' | 'Machinery'
  showSketch = true,
  showAttachments = true
}) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const styles = React.useMemo(() => getStyles(colors, isDarkMode), [colors, isDarkMode]);

  const [resolvedSketchUri, setResolvedSketchUri] = useState('');
  const [fullscreenImage, setFullscreenImage] = useState(null);
  const [fullscreenError, setFullscreenError] = useState(false);
  const [sketchError, setSketchError] = useState(false);
  const [failedPhotoIds, setFailedPhotoIds] = useState({});
  const [uploading, setUploading] = useState(false);
  const [photoToDelete, setPhotoToDelete] = useState(null);

  // Resolve web sketch URL to full address
  const [resolvedAttachmentUris, setResolvedAttachmentUris] = useState({});

  useEffect(() => {
    setSketchError(false);
  }, [sketchUrl, sketchBase64]);

  const openFullscreen = (uri) => {
    setFullscreenError(false);
    setFullscreenImage(uri);
  };

  useEffect(() => {
    let isMounted = true;
    const resolveAll = async () => {
      const uris = {};
      for (const att of (attachments || [])) {
        if (!att) continue;
        const rawUrl = att.fileUrl || att.FileUrl || att.uri;
        if (rawUrl) {
          const resolved = await resolveServerUrl(rawUrl);
          const id = att.id || att.Id;
          if (id) uris[id] = resolved;
          uris[rawUrl] = resolved;
          if (att.fileUrl) uris[att.fileUrl] = resolved;
          if (att.FileUrl) uris[att.FileUrl] = resolved;
        }
      }
      if (isMounted) setResolvedAttachmentUris(uris);
    };
    resolveAll();
    return () => { isMounted = false; };
  }, [attachments]);

  useEffect(() => {
    let isMounted = true;
    if (sketchUrl) {
      resolveServerUrl(sketchUrl).then(fullUrl => {
        if (isMounted) setResolvedSketchUri(fullUrl);
      });
    } else {
      setResolvedSketchUri('');
    }
    return () => { isMounted = false; };
  }, [sketchUrl]);

  const getFullPhotoUrl = (rawUrl, itemId) => {
    if (!rawUrl) return '';
    if (resolvedAttachmentUris[itemId]) return resolvedAttachmentUris[itemId];
    if (resolvedAttachmentUris[rawUrl]) return resolvedAttachmentUris[rawUrl];
    if (rawUrl.startsWith('http://') || rawUrl.startsWith('https://') || rawUrl.startsWith('data:') || rawUrl.startsWith('file:') || rawUrl.startsWith('content:')) {
      return rawUrl;
    }
    const base = DEFAULT_BASE_URL.replace(/\/+$/, '');
    const cleanPath = rawUrl.startsWith('/') ? rawUrl : '/' + rawUrl;
    return `${base}${cleanPath}`;
  };

  const displaySketchUri = (sketchBase64 ? `data:image/jpeg;base64,${sketchBase64}` : null)
    || resolvedSketchUri
    || (sketchUrl && (/^(https?:|data:|file:|content:)/.test(sketchUrl)
        ? sketchUrl
        : `${DEFAULT_BASE_URL.replace(/\/+$/, '')}/${sketchUrl.replace(/^\/+/, '')}`));

  // Request permissions for camera
  const requestCameraPermission = async () => {
    if (Platform.OS !== 'web') {
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Camera Permission', 'Camera permission is required to capture inspection photos.');
        return false;
      }
    }
    return true;
  };

  // Request permissions for photo library
  const requestLibraryPermission = async () => {
    if (Platform.OS !== 'web') {
      const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Gallery Permission', 'Media gallery permission is required to select photos.');
        return false;
      }
    }
    return true;
  };

  // 1. Take photo via Camera (Photos Only)
  const handleTakePhoto = async () => {
    const hasPermission = await requestCameraPermission();
    if (!hasPermission) return;

    try {
      const result = await ImagePicker.launchCameraAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        allowsEditing: false,
        quality: 0.75,
        base64: true
      });

      if (!result.canceled && result.assets && result.assets.length > 0) {
        for (const asset of result.assets) {
          await processAndAddPhoto({
            uri: asset.uri,
            fileName: asset.fileName || `Photo_${Date.now()}.jpg`,
            fileSize: asset.fileSize ? `${Math.round(asset.fileSize / 1024)} KB` : '500 KB',
            base64: asset.base64
          });
        }
      }
    } catch (err) {
      console.warn('Camera error:', err);
      Alert.alert('Camera Error', 'Could not open camera.');
    }
  };

  // 2. Select multiple photos from Gallery (Photos Only)
  const handleSelectFromGallery = async () => {
    const hasPermission = await requestLibraryPermission();
    if (!hasPermission) return;

    try {
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        allowsMultipleSelection: true,
        selectionLimit: 15,
        quality: 0.75,
        base64: true
      });

      if (!result.canceled && result.assets && result.assets.length > 0) {
        for (const asset of result.assets) {
          await processAndAddPhoto({
            uri: asset.uri,
            fileName: asset.fileName || `Photo_${Date.now()}.jpg`,
            fileSize: asset.fileSize ? `${Math.round(asset.fileSize / 1024)} KB` : '500 KB',
            base64: asset.base64
          });
        }
      }
    } catch (err) {
      console.warn('Gallery error:', err);
      Alert.alert('Gallery Error', 'Could not access photo library.');
    }
  };

  // Helper to store photo completely offline on the device
  const processAndAddPhoto = async ({ uri, fileName, fileSize, base64 }) => {
    setUploading(true);

    const saved = await saveFileLocally({
      trackingNo: trackingNo || 'general',
      fileName: fileName,
      base64: base64,
      uri: uri,
      type: 'attachment'
    });

    let newAttachment = {
      id: `photo_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
      fileName: fileName,
      fileUrl: saved.localUri,
      fileType: 'image',
      fileSize: fileSize,
      base64Data: base64 || null,
      caption: '',
      uploadedAt: new Date().toISOString()
    };

    setUploading(false);

    const updated = [...(attachments || []), newAttachment];
    if (onAttachmentsChange) onAttachmentsChange(updated);
  };

  // Sketch: store completely offline on the device; uploaded only during office sync
  const applySketchAsset = async (asset) => {
    if (!asset || !onSketchChange) return;
    setUploading(true);

    const saved = await saveFileLocally({
      trackingNo: trackingNo || 'general',
      fileName: asset.fileName || `Sketch_${Date.now()}.jpg`,
      base64: asset.base64,
      uri: asset.uri,
      type: 'sketch'
    });

    setUploading(false);
    onSketchChange({
      uri: saved.localUri,
      base64: asset.base64 || null,
      fileName: asset.fileName || `Sketch_${Date.now()}.jpg`
    });
  };

  const handleSketchCamera = async () => {
    if (!(await requestCameraPermission())) return;
    try {
      const result = await ImagePicker.launchCameraAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        allowsEditing: true,
        quality: 0.8,
        base64: true
      });
      if (!result.canceled && result.assets?.length) applySketchAsset(result.assets[0]);
    } catch (err) {
      Alert.alert('Camera Error', 'Could not open camera.');
    }
  };

  const handleSketchGallery = async () => {
    if (!(await requestLibraryPermission())) return;
    try {
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        allowsEditing: true,
        quality: 0.8,
        base64: true
      });
      if (!result.canceled && result.assets?.length) applySketchAsset(result.assets[0]);
    } catch (err) {
      Alert.alert('Gallery Error', 'Could not access photo library.');
    }
  };

  const handleSketchRemove = () => {
    if (onSketchChange) onSketchChange({ uri: '', base64: null, fileName: '' });
  };

  // Delete photo attachment
  const handleDeletePhoto = (id) => {
    setPhotoToDelete(id);
  };

  // Update caption
  const handleUpdateCaption = (id, text) => {
    const updated = (attachments || []).map(a => {
      const aid = a?.id || a?.Id || a?.fileUrl || a?.FileUrl;
      if (aid === id) return { ...a, caption: text, Caption: text };
      return a;
    });
    if (onAttachmentsChange) onAttachmentsChange(updated);
  };

  return (
    <View style={styles.container}>
      {/* =========================================================================
          SECTION 1: WEB-UPLOADED SITE SKETCH / FLOOR PLAN BLUEPRINT
          ========================================================================= */}
      {showSketch && (
        <View style={[styles.card, shadows.card]}>
          <View style={styles.headerRow}>
            <View style={styles.headerIconBox}>
              <Ionicons name="map-outline" size={20} color={colors.primary} />
            </View>
            <View style={{ flex: 1 }}>
              <Text style={styles.cardTitle}>Site Sketch & Floor Plan Blueprint</Text>
              <Text style={styles.cardSubtitle}>Upload or replace on site; synced with your next office sync</Text>
            </View>
            <View style={styles.badgeWeb}>
              <Text style={styles.badgeWebText}>Office Web Sketch</Text>
            </View>
          </View>

          {!!onSketchChange && (
            <View style={styles.actionButtonsRow}>
              <TouchableOpacity style={[styles.actionBtn, styles.actionBtnCamera]} onPress={handleSketchCamera}>
                <Ionicons name="camera" size={18} color="#ffffff" />
                <Text style={styles.actionBtnText}>{sketchUrl ? 'Retake' : 'Take Sketch'}</Text>
              </TouchableOpacity>
              <TouchableOpacity style={[styles.actionBtn, styles.actionBtnGallery]} onPress={handleSketchGallery}>
                <Ionicons name="images" size={18} color="#ffffff" />
                <Text style={styles.actionBtnText}>{sketchUrl ? 'Replace' : 'Upload'}</Text>
              </TouchableOpacity>
              {!!sketchUrl && (
                <TouchableOpacity style={[styles.actionBtn, { backgroundColor: colors.accentRed, flex: 0, paddingHorizontal: 14 }]} onPress={handleSketchRemove}>
                  <Ionicons name="trash-outline" size={18} color="#ffffff" />
                </TouchableOpacity>
              )}
            </View>
          )}

          {displaySketchUri ? (
            <View style={styles.sketchContainer}>
              <TouchableOpacity
                activeOpacity={0.85}
                style={styles.sketchImageWrap}
                onPress={() => !sketchError && openFullscreen(displaySketchUri)}
              >
                {sketchError ? (
                  <View style={styles.sketchErrorWrap}>
                    <Ionicons name="cloud-offline-outline" size={38} color={colors.textMuted} />
                    <Text style={styles.sketchErrorTitle}>Sketch Not Cached Offline</Text>
                    <Text style={styles.sketchErrorText}>
                      This blueprint could not be loaded from offline storage. You can take or replace a new site sketch using the buttons above.
                    </Text>
                  </View>
                ) : (
                  <>
                    <Image
                      source={{ uri: displaySketchUri }}
                      style={styles.sketchImage}
                      resizeMode="contain"
                      onError={() => setSketchError(true)}
                    />
                    <View style={styles.zoomOverlay}>
                      <Ionicons name="scan-outline" size={16} color="#ffffff" />
                      <Text style={styles.zoomOverlayText}>Tap to Enlarge</Text>
                    </View>
                  </>
                )}
              </TouchableOpacity>

              <View style={styles.sketchFooter}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.sketchFileName} numberOfLines={1}>
                    {(sketchUrl || '').split('/').pop() || 'blueprint.jpg'}
                  </Text>
                  <Text style={styles.sketchHint}>Official blueprint for {propertyKind} inspection</Text>
                </View>
                {!sketchError && (
                  <TouchableOpacity
                    style={styles.viewFullBtn}
                    onPress={() => openFullscreen(displaySketchUri)}
                  >
                    <Ionicons name="expand-outline" size={16} color={colors.primary} />
                    <Text style={styles.viewFullBtnText}>Full View</Text>
                  </TouchableOpacity>
                )}
              </View>
            </View>
          ) : (
            <View style={styles.emptySketchBox}>
              <Ionicons name="document-text-outline" size={38} color={colors.textMuted} />
              <Text style={styles.emptySketchTitle}>No Blueprint Sketch Uploaded</Text>
              <Text style={styles.emptySketchText}>
                No Site Sketch or Floor Plan blueprint has been attached from the Office Web Portal for this partition yet.
              </Text>
            </View>
          )}
        </View>
      )}

      {/* =========================================================================
          SECTION 2: MULTIPLE FIELD INSPECTION PHOTOS (CAMERA & GALLERY ONLY)
          ========================================================================= */}
      {showAttachments && (
        <View style={[styles.card, shadows.card, { marginTop: showSketch ? 14 : 0 }]}>
        <View style={styles.headerRow}>
          <View style={[styles.headerIconBox, { backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5' }]}>
            <Ionicons name="camera-outline" size={20} color={isDarkMode ? '#34d399' : '#059669'} />
          </View>
          <View style={{ flex: 1 }}>
            <Text style={styles.cardTitle}>Field Inspection Photos</Text>
            <Text style={styles.cardSubtitle}>
              Take camera photos or select multiple photos from gallery
            </Text>
          </View>
          <View style={styles.countBadge}>
            <Text style={styles.countBadgeText}>{attachments.length} Photos</Text>
          </View>
        </View>

        {/* Upload Buttons Action Bar (Photos Only: Camera & Gallery) */}
        <View style={styles.actionButtonsRow}>
          <TouchableOpacity
            style={[styles.actionBtn, styles.actionBtnCamera]}
            onPress={handleTakePhoto}
            disabled={uploading}
          >
            <Ionicons name="camera" size={18} color="#ffffff" />
            <Text style={styles.actionBtnText}>Take Photo</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.actionBtn, styles.actionBtnGallery]}
            onPress={handleSelectFromGallery}
            disabled={uploading}
          >
            <Ionicons name="images" size={18} color="#ffffff" />
            <Text style={styles.actionBtnText}>Add Photos</Text>
          </TouchableOpacity>
        </View>

        {uploading && (
          <View style={styles.uploadingBar}>
            <ActivityIndicator size="small" color={colors.primary} />
            <Text style={styles.uploadingText}>Saving inspection photo...</Text>
          </View>
        )}

        {/* Inspection Photos List */}
        {attachments.length === 0 ? (
          <View style={styles.emptyFilesBox}>
            <Ionicons name="camera-reverse-outline" size={36} color={colors.textMuted} />
            <Text style={styles.emptyFilesTitle}>No Field Photos Added</Text>
            <Text style={styles.emptyFilesText}>
              Tap Take Photo or Add Photos above to capture site evidence for this {propertyKind} inspection.
            </Text>
          </View>
        ) : (
          <View style={styles.attachmentsList}>
            {attachments.map((item, idx) => {
              const rawUrl = item?.fileUrl || item?.FileUrl || item?.uri || '';
              const itemId = item?.id || item?.Id || rawUrl || ('photo_' + idx);
              const fileName = item?.fileName || item?.FileName || `Photo ${idx + 1}`;
              const fileSize = item?.fileSize || item?.FileSize || 'Image';
              const fileType = (item?.fileType || item?.FileType || 'image').toLowerCase();
              const caption = item?.caption || item?.Caption || '';
              const uploadedAt = item?.uploadedAt || item?.UploadedAt;
              const itemB64 = item?.base64Data || item?.Base64Data;
              const itemUri = (rawUrl.startsWith('blob:') && itemB64)
                ? `data:image/jpeg;base64,${itemB64}`
                : (itemB64 ? `data:image/jpeg;base64,${itemB64}` : getFullPhotoUrl(rawUrl, itemId));
              const isPdf = fileType === 'document' || fileName.toLowerCase().endsWith('.pdf') || rawUrl.toLowerCase().endsWith('.pdf');

              return (
                <View key={itemId} style={styles.attachmentCard}>
                  <View style={styles.attTopRow}>
                    {/* Photo Thumbnail */}
                    <TouchableOpacity
                      activeOpacity={0.8}
                      onPress={() => !isPdf && !failedPhotoIds[itemId] && openFullscreen(itemUri)}
                      style={styles.attThumbnailWrap}
                    >
                      {isPdf ? (
                        <View style={[styles.attThumbnail, { backgroundColor: '#f1f5f9', alignItems: 'center', justifyContent: 'center' }]}>
                          <Ionicons name="document-text" size={26} color="#059669" />
                        </View>
                      ) : failedPhotoIds[itemId] ? (
                        <View style={[styles.attThumbnail, { backgroundColor: isDarkMode ? '#334155' : '#f1f5f9', alignItems: 'center', justifyContent: 'center' }]}>
                          <Ionicons name="cloud-offline-outline" size={22} color={colors.textMuted} />
                        </View>
                      ) : (
                        <Image
                          key={itemUri}
                          source={{ uri: itemUri }}
                          style={styles.attThumbnail}
                          resizeMode="cover"
                          onError={() => setFailedPhotoIds(prev => ({ ...prev, [itemId]: true }))}
                        />
                      )}
                      {!isPdf && !failedPhotoIds[itemId] && (
                        <View style={styles.thumbZoomPill}>
                          <Ionicons name="search" size={10} color="#ffffff" />
                        </View>
                      )}
                    </TouchableOpacity>

                    {/* Photo Meta */}
                    <View style={styles.attMetaWrap}>
                      <Text style={styles.attFileName} numberOfLines={1}>
                        {fileName}
                      </Text>
                      <View style={styles.attSubMeta}>
                        <Text style={styles.attSizeText}>{fileSize}</Text>
                        <Text style={styles.attDot}>•</Text>
                        <Text style={styles.attTimeText}>
                          {uploadedAt ? new Date(uploadedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Local'}
                        </Text>
                      </View>
                    </View>

                    {/* Delete Photo Button */}
                    <TouchableOpacity
                      style={styles.deleteAttBtn}
                      onPress={() => handleDeletePhoto(itemId)}
                    >
                      <Ionicons name="trash-outline" size={18} color={colors.accentRed} />
                    </TouchableOpacity>
                  </View>

                  {/* Caption input */}
                  <View style={styles.captionRow}>
                    <Ionicons name="pencil-outline" size={14} color={colors.textMuted} style={{ marginRight: 6 }} />
                    <TextInput
                      style={styles.captionInput}
                      value={caption}
                      onChangeText={(txt) => handleUpdateCaption(itemId, txt)}
                      placeholder="Add photo notes / caption (e.g. Front View, Defects)..."
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>
              );
            })}
          </View>
        )}
      </View>
      )}

      {/* =========================================================================
          FULL-SCREEN IMAGE PREVIEW MODAL
          ========================================================================= */}
      <Modal
        visible={!!fullscreenImage}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setFullscreenImage(null)}
      >
        <View style={styles.modalBackdrop}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Photo Preview</Text>
            <TouchableOpacity
              style={styles.modalCloseBtn}
              onPress={() => setFullscreenImage(null)}
            >
              <Ionicons name="close" size={24} color="#ffffff" />
            </TouchableOpacity>
          </View>

          {fullscreenImage && (
            fullscreenError ? (
              <View style={styles.modalErrorContainer}>
                <Ionicons name="cloud-offline-outline" size={56} color="#94a3b8" />
                <Text style={styles.modalErrorTitle}>Photo Unavailable Offline</Text>
                <Text style={styles.modalErrorSubtitle}>
                  This photo was not saved to local device storage before going offline.
                </Text>
              </View>
            ) : (
              <Image
                source={{ uri: fullscreenImage }}
                style={styles.modalFullscreenImage}
                resizeMode="contain"
                onError={() => setFullscreenError(true)}
              />
            )
          )}
        </View>
      </Modal>

      <ActionModal
        visible={!!photoToDelete}
        title="Remove Photo"
        message="Are you sure you want to remove this inspection photo?"
        icon="trash-outline"
        iconType="danger"
        onClose={() => setPhotoToDelete(null)}
        buttons={[
          {
            text: 'Remove',
            style: 'danger',
            icon: 'trash-outline',
            onPress: () => {
              const updated = (attachments || []).filter(a => {
                const aid = a?.id || a?.Id || a?.fileUrl || a?.FileUrl;
                return aid !== photoToDelete;
              });
              if (onAttachmentsChange) onAttachmentsChange(updated);
              setPhotoToDelete(null);
            }
          },
          { text: 'Cancel', style: 'cancel', onPress: () => setPhotoToDelete(null) }
        ]}
      />
    </View>
  );
};

const getStyles = (colors, isDarkMode) => StyleSheet.create({
  container: {
    marginBottom: 16
  },
  card: {
    backgroundColor: colors.cardBg,
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: colors.border
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 14
  },
  headerIconBox: {
    width: 38,
    height: 38,
    borderRadius: 10,
    backgroundColor: colors.primaryLight,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12
  },
  cardTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: colors.textDark
  },
  cardSubtitle: {
    fontSize: 12,
    color: colors.textMuted,
    marginTop: 2
  },
  badgeWeb: {
    backgroundColor: isDarkMode ? '#3b0764' : '#ede9fe',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 6
  },
  badgeWebText: {
    fontSize: 11,
    fontWeight: '700',
    color: isDarkMode ? '#d8b4fe' : '#6d28d9'
  },
  countBadge: {
    backgroundColor: colors.surfaceSecondary,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12
  },
  countBadgeText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textDark
  },

  // Sketch container
  sketchContainer: {
    backgroundColor: colors.surfaceSecondary,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: 'hidden'
  },
  sketchImageWrap: {
    height: 220,
    backgroundColor: isDarkMode ? '#1e293b' : '#f8fafc',
    position: 'relative',
    alignItems: 'center',
    justifyContent: 'center'
  },
  sketchErrorWrap: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: 20
  },
  sketchErrorTitle: {
    fontSize: 14,
    fontWeight: '700',
    color: colors.textDark,
    marginTop: 8
  },
  sketchErrorText: {
    fontSize: 12,
    color: colors.textMuted,
    textAlign: 'center',
    marginTop: 4,
    maxWidth: 280
  },
  sketchImage: {
    width: '100%',
    height: '100%'
  },
  zoomOverlay: {
    position: 'absolute',
    bottom: 10,
    right: 10,
    backgroundColor: 'rgba(15, 23, 42, 0.75)',
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: 6,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  zoomOverlayText: {
    color: '#ffffff',
    fontSize: 11,
    fontWeight: '600'
  },
  sketchFooter: {
    padding: 12,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: colors.cardBg
  },
  sketchFileName: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textDark
  },
  sketchHint: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2
  },
  viewFullBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.primaryLight,
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 6,
    gap: 4
  },
  viewFullBtnText: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.primary
  },
  emptySketchBox: {
    padding: 24,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surfaceSecondary,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.border,
    borderStyle: 'dashed'
  },
  emptySketchTitle: {
    fontSize: 14,
    fontWeight: '700',
    color: colors.textDark,
    marginTop: 8
  },
  emptySketchText: {
    fontSize: 12,
    color: colors.textMuted,
    textAlign: 'center',
    marginTop: 4,
    maxWidth: 280
  },

  // Action buttons
  actionButtonsRow: {
    flexDirection: 'row',
    gap: 10,
    marginBottom: 12
  },
  actionBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 12,
    borderRadius: 8,
    gap: 8
  },
  actionBtnCamera: {
    backgroundColor: '#2563eb'
  },
  actionBtnGallery: {
    backgroundColor: '#059669'
  },
  actionBtnText: {
    color: '#ffffff',
    fontSize: 13,
    fontWeight: '700'
  },
  uploadingBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 8,
    backgroundColor: colors.primaryLight,
    borderRadius: 6,
    marginBottom: 12
  },
  uploadingText: {
    fontSize: 12,
    color: colors.primary,
    fontWeight: '500'
  },

  // Empty files
  emptyFilesBox: {
    padding: 24,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surfaceSecondary,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.border,
    borderStyle: 'dashed'
  },
  emptyFilesTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.textDark,
    marginTop: 6
  },
  emptyFilesText: {
    fontSize: 12,
    color: colors.textMuted,
    textAlign: 'center',
    marginTop: 3,
    maxWidth: 260
  },

  // Attachment item card
  attachmentsList: {
    gap: 10
  },
  attachmentCard: {
    backgroundColor: colors.surfaceSecondary,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.border,
    padding: 10
  },
  attTopRow: {
    flexDirection: 'row',
    alignItems: 'center'
  },
  attThumbnailWrap: {
    width: 60,
    height: 60,
    borderRadius: 8,
    overflow: 'hidden',
    backgroundColor: isDarkMode ? '#1e293b' : '#e2e8f0',
    borderWidth: 1,
    borderColor: colors.border,
    position: 'relative'
  },
  attThumbnail: {
    width: '100%',
    height: '100%'
  },
  thumbZoomPill: {
    position: 'absolute',
    bottom: 2,
    right: 2,
    backgroundColor: 'rgba(0,0,0,0.6)',
    padding: 3,
    borderRadius: 4
  },
  attMetaWrap: {
    flex: 1,
    marginLeft: 12
  },
  attFileName: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textDark
  },
  attSubMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 3
  },
  attSizeText: {
    fontSize: 11,
    color: colors.textMuted
  },
  attDot: {
    marginHorizontal: 5,
    fontSize: 11,
    color: colors.textMuted
  },
  attTimeText: {
    fontSize: 11,
    color: colors.textMuted
  },
  deleteAttBtn: {
    padding: 8
  },
  captionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 8,
    backgroundColor: colors.inputBg,
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: 6,
    paddingHorizontal: 8,
    paddingVertical: 4
  },
  captionInput: {
    flex: 1,
    fontSize: 12,
    color: colors.inputText,
    padding: 0
  },

  // Modal
  modalBackdrop: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.95)',
    justifyContent: 'center',
    alignItems: 'center'
  },
  modalHeader: {
    position: 'absolute',
    top: 40,
    left: 20,
    right: 20,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    zIndex: 10
  },
  modalTitle: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700'
  },
  modalCloseBtn: {
    padding: 8,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    borderRadius: 20
  },
  modalFullscreenImage: {
    width: '100%',
    height: '80%'
  },
  modalErrorContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: 30
  },
  modalErrorTitle: {
    color: '#ffffff',
    fontSize: 17,
    fontWeight: '700',
    marginTop: 14
  },
  modalErrorSubtitle: {
    color: '#94a3b8',
    fontSize: 13,
    textAlign: 'center',
    marginTop: 6,
    maxWidth: 280
  }
});
