import React from 'react';
import {
  View,
  Text,
  Modal,
  StyleSheet,
  TouchableOpacity
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../context/ThemeContext';

export const DiscardChangesModal = ({
  visible,
  onClose,
  onConfirmDiscard,
  partitionLetter = 'A'
}) => {
  const { isDarkMode, colors, shadows } = useTheme();

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onClose}
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
          <View
            style={[
              styles.iconBubble,
              { backgroundColor: isDarkMode ? '#450a0a' : '#fee2e2' }
            ]}
          >
            <Ionicons
              name="close-circle-outline"
              size={32}
              color={isDarkMode ? '#fca5a5' : colors.accentRed}
            />
          </View>

          <Text style={[styles.title, { color: colors.textDark }]}>
            Discard Changes?
          </Text>

          <Text style={[styles.description, { color: colors.textMuted }]}>
            Are you sure you want to leave Partition {partitionLetter}? Any unsaved edits on this form will be discarded.
          </Text>

          <View style={styles.actionContainer}>
            <TouchableOpacity
              style={[styles.primaryBtn, { backgroundColor: colors.accentRed }]}
              onPress={onConfirmDiscard}
            >
              <Ionicons name="trash-outline" size={18} color="#ffffff" />
              <Text style={styles.primaryBtnText}>Discard & Go Back</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.cancelBtn,
                { backgroundColor: isDarkMode ? '#334155' : '#f1f5f9' }
              ]}
              onPress={onClose}
            >
              <Text style={[styles.cancelBtnText, { color: isDarkMode ? '#f8fafc' : colors.textDark }]}>
                Keep Editing
              </Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20
  },
  modalCard: {
    width: '100%',
    maxWidth: 400,
    borderRadius: 20,
    padding: 24,
    alignItems: 'center',
    borderWidth: 1
  },
  iconBubble: {
    width: 60,
    height: 60,
    borderRadius: 30,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 14
  },
  title: {
    fontSize: 19,
    fontWeight: '800',
    textAlign: 'center',
    marginBottom: 8
  },
  description: {
    fontSize: 13,
    textAlign: 'center',
    lineHeight: 20,
    marginBottom: 20
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
  primaryBtnText: {
    color: '#ffffff',
    fontSize: 14,
    fontWeight: '800'
  },
  cancelBtn: {
    paddingVertical: 13,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 12,
    width: '100%'
  },
  cancelBtnText: {
    fontSize: 14,
    fontWeight: '700'
  }
});

