import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { COLORS } from '../constants/theme';

export const StatusBadge = ({ statusId, statusText, isComplete, isDeferred, customLabel }) => {
  const cleanCustom = customLabel || statusText;
  let label = cleanCustom ? String(cleanCustom).toUpperCase() : (statusId != null ? `STEP ${statusId}` : 'PENDING');
  let bgColor = COLORS.primaryLight;
  let textColor = COLORS.primary;

  if (isComplete) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'COMPLETED';
    bgColor = '#dcfce7';
    textColor = COLORS.accentGreen;
  } else if (isDeferred) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'DEFERRED';
    bgColor = '#fee2e2';
    textColor = COLORS.accentRed;
  } else if (statusId === 1) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'RECEIVING';
    bgColor = '#e0f2fe';
    textColor = COLORS.accentBlue;
  } else if (statusId === 3) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'INSPECTION';
    bgColor = '#fef3c7';
    textColor = COLORS.accentGold;
  } else if (statusId === 2) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'APPRAISAL REVIEW';
    bgColor = '#ede9fe';
    textColor = '#7c3aed';
  } else if (statusId === 4) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'CHECK & REVIEW';
    bgColor = '#f1f5f9';
    textColor = '#475569';
  } else if (statusId === 8) {
    label = cleanCustom ? String(cleanCustom).toUpperCase() : 'APPROVAL';
    bgColor = '#fef3c7';
    textColor = '#b45309';
  }

  return (
    <View style={[styles.badge, { backgroundColor: bgColor }]}>
      <Text style={[styles.text, { color: textColor }]}>{label}</Text>
    </View>
  );
};

const styles = StyleSheet.create({
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6,
    alignSelf: 'flex-start'
  },
  text: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.3
  }
});
