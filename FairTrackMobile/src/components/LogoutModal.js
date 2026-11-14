import React, { useState } from 'react';
import {
  View,
  Text,
  Modal,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
  Platform
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { COLORS, SHADOWS } from '../constants/theme';
import { useTheme } from '../context/ThemeContext';

export const LogoutModal = ({
  visible,
  onClose,
  onConfirmLogout,
  pendingCount = 0,
  onSyncAll,
  user
}) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const [syncingBeforeLogout, setSyncingBeforeLogout] = useState(false);
  const [syncError, setSyncError] = useState(null);


  const handleSyncAndLogout = async () => {
    if (typeof onSyncAll !== 'function') {
      handleSignOutAction();
      return;
    }

    try {
      setSyncingBeforeLogout(true);
      setSyncError(null);
      const res = await onSyncAll();
      setSyncingBeforeLogout(false);

      if (res && res.success) {
        handleSignOutAction();
      } else {
        setSyncError(
          `Sync completed with ${res?.failureCount || 0} issues. You can sign out anyway or stay logged in to check.`
        );
      }
    } catch (err) {
      setSyncingBeforeLogout(false);
      setSyncError(err.message || 'Failed to sync offline changes before logout.');
    }
  };

  const handleSignOutAction = () => {
    setSyncError(null);
    if (typeof onConfirmLogout === 'function') {
      onConfirmLogout();
    }
  };

  const handleClose = () => {
    if (syncingBeforeLogout) return;
    setSyncError(null);
    if (typeof onClose === 'function') {
      onClose();
    }
  };

  const hasPending = pendingCount > 0;

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={handleClose}
    >
      <View style={[styles.overlay, { backgroundColor: colors.modalOverlay }]}>
        <View
          style={[
            styles.modalCard,
            {
              backgroundColor: colors.cardBg,
              borderColor: colors.border
            },
            shadows.hover
          ]}
        >
          {/* Header Icon */}
          <View
            style={[
              styles.iconBubble,
              {
                backgroundColor: hasPending
                  ? (isDarkMode ? '#78350f' : '#fef3c7')
                  : (isDarkMode ? '#450a0a' : '#fee2e2')
              }
            ]}
          >
            <Ionicons
              name={hasPending ? 'cloud-upload-outline' : 'log-out-outline'}
              size={30}
              color={hasPending ? colors.accentGold : (isDarkMode ? '#fca5a5' : COLORS.accentRed)}
            />
          </View>

          {/* Title */}
          <Text style={[styles.title, { color: colors.textDark }]}>
            {hasPending ? 'Unsynced Changes Pending' : 'Sign Out of FairTrack'}
          </Text>

          {/* User Badge */}
          {user ? (
            <View
              style={[
                styles.userBadge,
                { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }
              ]}
            >
              <Ionicons name="person-circle-outline" size={16} color={colors.primary} />
              <Text style={[styles.userBadgeText, { color: isDarkMode ? '#f8fafc' : colors.primaryDark }]}>
                {user.name || user.username || 'Current User'} ({user.divisionName || `Division ${user.divisionId || 1}`})
              </Text>
            </View>
          ) : null}

          {/* Description */}
          {hasPending ? (
            <View
              style={[
                styles.warningBox,
                isDarkMode && {
                  backgroundColor: '#271e05',
                  borderColor: '#78350f'
                }
              ]}
            >
              <Ionicons name="warning-outline" size={18} color="#f59e0b" style={{ marginTop: 2 }} />
              <View style={{ flex: 1 }}>
                <Text style={[styles.warningTitle, isDarkMode && { color: '#fbbf24' }]}>
                  {pendingCount} offline edit{pendingCount > 1 ? 's' : ''} waiting to sync
                </Text>
                <Text style={[styles.warningText, isDarkMode && { color: '#fef3c7' }]}>
                  Your field assessments are safely saved on this device. We recommend syncing to the office database now before signing out.
                </Text>
              </View>
            </View>
          ) : (
            <Text style={[styles.description, { color: colors.textMuted }]}>
              Are you sure you want to end your session? You will return to the staff login screen.
            </Text>
          )}

          {/* Sync Error Notice if any */}
          {syncError ? (
            <View style={styles.errorBox}>
              <Ionicons name="alert-circle-outline" size={16} color={COLORS.accentRed} />
              <Text style={styles.errorText}>{syncError}</Text>
            </View>
          ) : null}

          {/* Actions */}
          <View style={styles.actionContainer}>
            {hasPending ? (
              <>
                <TouchableOpacity
                  style={[styles.primaryBtn, styles.syncBtn]}
                  onPress={handleSyncAndLogout}
                  disabled={syncingBeforeLogout}
                >
                  {syncingBeforeLogout ? (
                    <ActivityIndicator size="small" color="#ffffff" />
                  ) : (
                    <>
                      <Ionicons name="cloud-done-outline" size={18} color="#ffffff" />
                      <Text style={styles.primaryBtnText}>Sync First & Sign Out</Text>
                    </>
                  )}
                </TouchableOpacity>

                <TouchableOpacity
                  style={[
                    styles.dangerOutlineBtn,
                    isDarkMode && {
                      backgroundColor: '#450a0a',
                      borderColor: '#7f1d1d'
                    }
                  ]}
                  onPress={handleSignOutAction}
                  disabled={syncingBeforeLogout}
                >
                  <Ionicons name="log-out-outline" size={17} color={isDarkMode ? '#fca5a5' : COLORS.accentRed} />
                  <Text style={[styles.dangerOutlineBtnText, isDarkMode && { color: '#fca5a5' }]}>
                    Sign Out Anyway
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[
                    styles.cancelBtn,
                    isDarkMode && { backgroundColor: '#334155' }
                  ]}
                  onPress={handleClose}
                  disabled={syncingBeforeLogout}
                >
                  <Text style={[styles.cancelBtnText, isDarkMode && { color: '#f8fafc' }]}>
                    Stay Logged In
                  </Text>
                </TouchableOpacity>
              </>
            ) : (
              <>
                <TouchableOpacity
                  style={[styles.primaryBtn, styles.dangerBtn]}
                  onPress={handleSignOutAction}
                >
                  <Ionicons name="log-out-outline" size={18} color="#ffffff" />
                  <Text style={styles.primaryBtnText}>Yes, Sign Out</Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[
                    styles.cancelBtn,
                    isDarkMode && { backgroundColor: '#334155' }
                  ]}
                  onPress={handleClose}
                >
                  <Text style={[styles.cancelBtnText, isDarkMode && { color: '#f8fafc' }]}>
                    Cancel
                  </Text>
                </TouchableOpacity>
              </>
            )}
          </View>
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    backgroundColor: 'rgba(15, 23, 42, 0.65)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20
  },
  modalCard: {
    width: '100%',
    maxWidth: 420,
    backgroundColor: '#ffffff',
    borderRadius: 20,
    padding: 24,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: COLORS.border
  },
  iconBubble: {
    width: 62,
    height: 62,
    borderRadius: 31,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 16
  },
  title: {
    fontSize: 20,
    fontWeight: '800',
    color: COLORS.textDark,
    textAlign: 'center',
    marginBottom: 8
  },
  userBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    backgroundColor: COLORS.primaryLight,
    paddingHorizontal: 12,
    paddingVertical: 5,
    borderRadius: 20,
    marginBottom: 14
  },
  userBadgeText: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.primaryDark
  },
  description: {
    fontSize: 14,
    color: COLORS.textMuted,
    textAlign: 'center',
    lineHeight: 20,
    marginBottom: 20
  },
  warningBox: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 10,
    backgroundColor: '#fffbeb',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#fde68a',
    padding: 12,
    marginBottom: 18,
    width: '100%'
  },
  warningTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: '#92400e',
    marginBottom: 3
  },
  warningText: {
    fontSize: 12,
    color: '#78350f',
    lineHeight: 17
  },
  errorBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: '#fef2f2',
    borderWidth: 1,
    borderColor: '#fecaca',
    borderRadius: 10,
    padding: 10,
    marginBottom: 16,
    width: '100%'
  },
  errorText: {
    fontSize: 12,
    color: COLORS.accentRed,
    flex: 1,
    fontWeight: '600'
  },
  actionContainer: {
    width: '100%',
    gap: 10
  },
  primaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 14,
    borderRadius: 12,
    width: '100%'
  },
  syncBtn: {
    backgroundColor: COLORS.primary
  },
  dangerBtn: {
    backgroundColor: COLORS.accentRed
  },
  primaryBtnText: {
    color: '#ffffff',
    fontSize: 15,
    fontWeight: '800'
  },
  dangerOutlineBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 13,
    borderRadius: 12,
    borderWidth: 1.5,
    borderColor: '#fecaca',
    backgroundColor: '#fff5f5',
    width: '100%'
  },
  dangerOutlineBtnText: {
    color: COLORS.accentRed,
    fontSize: 14,
    fontWeight: '700'
  },
  cancelBtn: {
    paddingVertical: 13,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 12,
    backgroundColor: '#f1f5f9'
  },
  cancelBtnText: {
    color: COLORS.textMuted,
    fontSize: 14,
    fontWeight: '700'
  }
});
