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

export const ActionModal = ({
  visible,
  title,
  message,
  icon = 'information-circle-outline',
  iconType = 'primary', // 'primary' | 'success' | 'warning' | 'danger'
  buttons = [], // [{ text, style: 'primary' | 'danger' | 'cancel' | 'outline', icon, onPress }]
  onClose
}) => {
  const { isDarkMode, colors, shadows } = useTheme();

  const getIconTheme = () => {
    if (iconType === 'success') {
      return {
        bg: isDarkMode ? '#064e3b' : '#dcfce7',
        color: isDarkMode ? '#34d399' : '#16a34a'
      };
    }
    if (iconType === 'warning') {
      return {
        bg: isDarkMode ? '#78350f' : '#fef3c7',
        color: isDarkMode ? '#fbbf24' : '#d97706'
      };
    }
    if (iconType === 'danger') {
      return {
        bg: isDarkMode ? '#450a0a' : '#fee2e2',
        color: isDarkMode ? '#fca5a5' : colors.accentRed
      };
    }
    return {
      bg: isDarkMode ? '#134e4a' : colors.primaryLight,
      color: colors.primary
    };
  };

  const iconTheme = getIconTheme();
  const actionButtons = buttons && buttons.length > 0
    ? buttons
    : [{ text: 'OK', style: 'primary', onPress: onClose }];

  return (
    <Modal
      visible={!!visible}
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
          <View style={[styles.iconBubble, { backgroundColor: iconTheme.bg }]}>
            <Ionicons name={icon} size={30} color={iconTheme.color} />
          </View>

          <Text style={[styles.title, { color: colors.textDark }]}>
            {title}
          </Text>

          {message ? (
            <Text style={[styles.description, { color: colors.textMuted }]}>
              {message}
            </Text>
          ) : null}

          <View style={styles.actionContainer}>
            {actionButtons.map((btn, idx) => {
              const isCancel = btn.style === 'cancel';
              const isDanger = btn.style === 'danger' || btn.style === 'destructive';
              const isOutline = btn.style === 'outline';

              let btnBg = colors.primary;
              let btnText = '#ffffff';
              let borderStyle = {};

              if (isCancel) {
                btnBg = isDarkMode ? '#334155' : '#f1f5f9';
                btnText = isDarkMode ? '#f8fafc' : colors.textDark;
              } else if (isDanger) {
                btnBg = colors.accentRed;
                btnText = '#ffffff';
              } else if (isOutline) {
                btnBg = isDarkMode ? '#1e293b' : '#ffffff';
                btnText = colors.primary;
                borderStyle = { borderWidth: 1.5, borderColor: colors.primary };
              }

              return (
                <TouchableOpacity
                  key={idx}
                  style={[
                    styles.btn,
                    { backgroundColor: btnBg },
                    borderStyle
                  ]}
                  onPress={() => {
                    if (onClose) onClose();
                    if (typeof btn.onPress === 'function') {
                      btn.onPress();
                    }
                  }}
                >
                  {btn.icon ? (
                    <Ionicons name={btn.icon} size={18} color={btnText} />
                  ) : null}
                  <Text style={[styles.btnText, { color: btnText }]}>
                    {btn.text}
                  </Text>
                </TouchableOpacity>
              );
            })}
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
    fontSize: 18,
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
  btn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 13,
    borderRadius: 12,
    width: '100%'
  },
  btnText: {
    fontSize: 14,
    fontWeight: '800'
  }
});

