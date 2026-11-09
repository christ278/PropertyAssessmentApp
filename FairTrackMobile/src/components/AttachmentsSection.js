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
import { MobileApi } from '../api/mobileApi';
import { resolveServerUrl } from '../api/apiClient';
import { useTheme } from '../context/ThemeContext';
import { ActionModal } from './ActionModal';

export const AttachmentsSection = ({
  trackingNo,
  sketchUrl,
  attachments = [],
  onAttachmentsChange,
  propertyKind = 'Building' // 'Building' | 'Land' | 'Machinery'
}) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const styles = React.useMemo(() => getStyles(colors, isDarkMode), [colors, isDarkMode]);

  const [resolvedSketchUri, setResolvedSketchUri] = useState('');
  const [fullscreenImage, setFullscreenImage] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [photoToDelete, setPhotoToDelete] = useState(null);

  // Resolve web sketch URL to full address
  const [resolvedAttachmentUris, setResolvedAttachmentUris] = useState({});

  useEffect(() => {
    let isMounted = true;
    const resolveAll = async () => {
      const uris = {};
      for (const att of (attachments || [])) {
        if (att && att.fileUrl) {
          uris[att.id || att.fileUrl] = await resolveServerUrl(att.fileUrl);
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

  // Helper to upload or add photo to attachments
  const processAndAddPhoto = async ({ uri, fileName, fileSize, base64 }) => {
    setUploading(true);
    let newAttachment = {
      id: `photo_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
      fileName: fileName,
      fileUrl: uri,
      fileType: 'image',
      fileSize: fileSize,
      base64Data: base64,
      caption: '',
      uploadedAt: new Date().toISOString()
    };

    // Try online upload if server is reachable
    try {
      const formData = new FormData();
      formData.append('trackingNo', trackingNo || 'general');
      formData.append('caption', '');
      formData.append('fileType', 'image');

      if (Platform.OS === 'web') {
        const resBlob = await fetch(uri);
        const blob = await resBlob.blob();
        formData.append('file', blob, fileName);
      } else {
        formData.append('file', {
          uri: uri,
          type: 'image/jpeg',
          name: fileName
        });
      }

      const uploadRes = await MobileApi.uploadAttachment(formData);
      if (uploadRes && uploadRes.success && uploadRes.data) {
        const serverAtt = uploadRes.data;
        newAttachment = {
          ...newAttachment,
          id: serverAtt.id || newAttachment.id,
          fileName: serverAtt.fileName || newAttachment.fileName,
          fileUrl: serverAtt.fileUrl,
          fileType: 'image',
          base64Data: null // Saved on server
        };
      }
    } catch (netErr) {
      // Offline fallback: keep local URI and base64Data for office batch sync
      console.log('Saved inspection photo offline for office sync');
    } finally {
      setUploading(false);
    }

    const updated = [...(attachments || []), newAttachment];
    if (onAttachmentsChange) onAttachmentsChange(updated);
  };

  // Delete photo attachment
  const handleDeletePhoto = (id) => {
    setPhotoToDelete(id);
  };

  // Update caption
  const handleUpdateCaption = (id, text) => {
    const updated = (attachments || []).map(a => {
      if (a.id === id) return { ...a, caption: text };
      return a;
    });
    if (onAttachmentsChange) onAttachmentsChange(updated);
  };

  return (
    <View style={styles.container}>
      {/* =========================================================================
          SECTION 1: WEB-UPLOADED SITE SKETCH / FLOOR PLAN BLUEPRINT
          ========================================================================= */}
      <View style={[styles.card, shadows.card]}>
        <View style={styles.headerRow}>
          <View style={styles.headerIconBox}>
            <Ionicons name="map-outline" size={20} color={colors.primary} />
          </View>
          <View style={{ flex: 1 }}>
            <Text style={styles.cardTitle}>Site Sketch & Floor Plan Blueprint</Text>
            <Text style={styles.cardSubtitle}>Uploaded from Assessor Office Web Portal</Text>
          </View>
          <View style={styles.badgeWeb}>
            <Text style={styles.badgeWebText}>Office Web Sketch</Text>
          </View>
        </View>

        {resolvedSketchUri ? (
          <View style={styles.sketchContainer}>
            <TouchableOpacity
              activeOpacity={0.85}
              style={styles.sketchImageWrap}
              onPress={() => setFullscreenImage(resolvedSketchUri)}
            >
              <Image
                source={{ uri: resolvedSketchUri }}
                style={styles.sketchImage}
                resizeMode="contain"
              />
              <View style={styles.zoomOverlay}>
                <Ionicons name="scan-outline" size={16} color="#ffffff" />
                <Text style={styles.zoomOverlayText}>Tap to Enlarge</Text>
              </View>
            </TouchableOpacity>

            <View style={styles.sketchFooter}>
              <View style={{ flex: 1 }}>
                <Text style={styles.sketchFileName} numberOfLines={1}>
                  {sketchUrl.split('/').pop()}
                </Text>
                <Text style={styles.sketchHint}>Official blueprint for {propertyKind} inspection</Text>
              </View>
              <TouchableOpacity
                style={styles.viewFullBtn}
                onPress={() => setFullscreenImage(resolvedSketchUri)}
              >
                <Ionicons name="expand-outline" size={16} color={colors.primary} />
                <Text style={styles.viewFullBtnText}>Full View</Text>
              </TouchableOpacity>
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

      {/* =========================================================================
          SECTION 2: MULTIPLE FIELD INSPECTION PHOTOS (CAMERA & GALLERY ONLY)
          ========================================================================= */}
      <View style={[styles.card, shadows.card, { marginTop: 14 }]}>
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
              const itemUri = resolvedAttachmentUris[item.id || item.fileUrl] || item.fileUrl;
              return (
                <View key={item.id || idx} style={styles.attachmentCard}>
                  <View style={styles.attTopRow}>
                    {/* Photo Thumbnail */}
                    <TouchableOpacity
                      activeOpacity={0.8}
                      onPress={() => setFullscreenImage(itemUri)}
                      style={styles.attThumbnailWrap}
                    >
                      <Image
                        source={{ uri: itemUri }}
                        style={styles.attThumbnail}
                        resizeMode="cover"
                      />
                      <View style={styles.thumbZoomPill}>
                        <Ionicons name="search" size={10} color="#ffffff" />
                      </View>
                    </TouchableOpacity>

                    {/* Photo Meta */}
                    <View style={styles.attMetaWrap}>
                      <Text style={styles.attFileName} numberOfLines={1}>
                        {item.fileName || `Photo ${idx + 1}`}
                      </Text>
                      <View style={styles.attSubMeta}>
                        <Text style={styles.attSizeText}>{item.fileSize || 'Image'}</Text>
                        <Text style={styles.attDot}>•</Text>
                        <Text style={styles.attTimeText}>
                          {item.uploadedAt ? new Date(item.uploadedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Local'}
                        </Text>
                      </View>
                    </View>

                    {/* Delete Photo Button */}
                    <TouchableOpacity
                      style={styles.deleteAttBtn}
                      onPress={() => handleDeletePhoto(item.id)}
                    >
                      <Ionicons name="trash-outline" size={18} color={colors.accentRed} />
                    </TouchableOpacity>
                  </View>

                  {/* Caption input */}
                  <View style={styles.captionRow}>
                    <Ionicons name="pencil-outline" size={14} color={colors.textMuted} style={{ marginRight: 6 }} />
                    <TextInput
                      style={styles.captionInput}
                      value={item.caption || ''}
                      onChangeText={(txt) => handleUpdateCaption(item.id, txt)}
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
            <Image
              source={{ uri: fullscreenImage }}
              style={styles.modalFullscreenImage}
              resizeMode="contain"
            />
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
              const updated = (attachments || []).filter(a => a.id !== photoToDelete);
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
    backgroundColor: '#0f172a',
    position: 'relative',
    alignItems: 'center',
    justifyContent: 'center'
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
    width: 52,
    height: 52,
    borderRadius: 6,
    overflow: 'hidden',
    backgroundColor: '#000000',
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
  }
});
