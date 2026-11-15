import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../context/ThemeContext';
import { SIZES } from '../constants/theme';

/**
 * FormStepperHeader
 * Displays:
 * - STEP X OF Y badge & percentage complete
 * - Progress bar track with active fill
 * - Horizontal interactive step nodes with connector lines (tap to jump)
 * - Current section title banner with icon and optional badge
 */
export const FormStepperHeader = ({
  tabs = [],
  activeTab = 0,
  onSelectTab,
  attachmentCount = 0,
  attachmentTabIndex = -1,
  style
}) => {
  const { colors, isDarkMode, shadows } = useTheme();
  const totalTabs = tabs.length;
  const currentTab = tabs[activeTab] || tabs[0] || {};
  const progressPercent = totalTabs > 0 ? Math.round(((activeTab + 1) / totalTabs) * 100) : 0;

  return (
    <View
      style={[
        styles.headerCard,
        {
          backgroundColor: isDarkMode ? colors.cardBg : '#ffffff',
          borderColor: isDarkMode ? colors.border : '#e2e8f0'
        },
        shadows.card,
        style
      ]}
    >
      {/* Top Meta: Step X of Y & Percent */}
      <View style={styles.metaRow}>
        <View
          style={[
            styles.stepBadge,
            { backgroundColor: isDarkMode ? 'rgba(20, 184, 166, 0.18)' : colors.primaryLight }
          ]}
        >
          <Text style={[styles.stepBadgeText, { color: colors.primary }]}>
            STEP {activeTab + 1} OF {totalTabs}
          </Text>
        </View>

        <Text style={[styles.percentText, { color: isDarkMode ? colors.textMuted : '#64748b' }]}>
          {progressPercent}% Complete
        </Text>
      </View>

      {/* Progress Bar Track */}
      <View
        style={[
          styles.progressTrack,
          { backgroundColor: isDarkMode ? '#334155' : '#e2e8f0' }
        ]}
      >
        <View
          style={[
            styles.progressFill,
            {
              width: `${progressPercent}%`,
              backgroundColor: colors.primary
            }
          ]}
        />
      </View>

      {/* Interactive Step Circles with Connectors */}
      <View style={styles.nodesContainer}>
        {tabs.map((tab, idx) => {
          const isCompleted = idx < activeTab;
          const isActive = idx === activeTab;
          const isFuture = idx > activeTab;

          return (
            <React.Fragment key={tab.label || idx}>
              {/* Connector line between steps */}
              {idx > 0 && (
                <View
                  style={[
                    styles.connectorLine,
                    {
                      backgroundColor:
                        idx <= activeTab
                          ? colors.primary
                          : isDarkMode
                          ? '#334155'
                          : '#cbd5e1'
                    }
                  ]}
                />
              )}

              {/* Step Circle Node */}
              <TouchableOpacity
                activeOpacity={0.7}
                onPress={() => onSelectTab && onSelectTab(idx)}
                style={[
                  styles.nodeCircle,
                  isCompleted && {
                    backgroundColor: isDarkMode ? 'rgba(20, 184, 166, 0.2)' : colors.primaryLight,
                    borderColor: colors.primary,
                    borderWidth: 1.5
                  },
                  isActive && {
                    backgroundColor: colors.primary,
                    borderColor: colors.primary,
                    borderWidth: 2,
                    transform: [{ scale: 1.06 }]
                  },
                  isFuture && {
                    backgroundColor: isDarkMode ? '#1e293b' : '#f8fafc',
                    borderColor: isDarkMode ? '#334155' : '#cbd5e1',
                    borderWidth: 1.5
                  }
                ]}
                hitSlop={{ top: 8, bottom: 8, left: 6, right: 6 }}
                accessibilityLabel={`Go to step ${idx + 1}: ${tab.label}`}
              >
                {isCompleted ? (
                  <Ionicons name="checkmark" size={14} color={colors.primary} />
                ) : (
                  <Text
                    style={[
                      styles.nodeText,
                      isActive && { color: '#ffffff', fontWeight: '800' },
                      isFuture && { color: isDarkMode ? '#64748b' : '#94a3b8' }
                    ]}
                  >
                    {idx + 1}
                  </Text>
                )}
              </TouchableOpacity>
            </React.Fragment>
          );
        })}
      </View>

      {/* Active Section Banner */}
      <View
        style={[
          styles.activeBanner,
          {
            backgroundColor: isDarkMode ? 'rgba(255, 255, 255, 0.04)' : '#f8fafc',
            borderColor: isDarkMode ? 'rgba(255, 255, 255, 0.08)' : '#e2e8f0'
          }
        ]}
      >
        <Ionicons
          name={currentTab.icon || 'document-text-outline'}
          size={16}
          color={colors.primary}
        />
        <Text
          numberOfLines={1}
          style={[
            styles.activeBannerText,
            { color: isDarkMode ? colors.textDark : '#0f172a' }
          ]}
        >
          {currentTab.label}
          {activeTab === attachmentTabIndex && attachmentCount > 0 ? ` (${attachmentCount})` : ''}
        </Text>
      </View>
    </View>
  );
};

/**
 * FormStepperFooter
 * Thumb-friendly previous / next step navigation bar directly above action buttons
 */
export const FormStepperFooter = ({
  activeTab = 0,
  totalTabs = 1,
  onPrev,
  onNext,
  prevLabel = 'Previous Step',
  nextLabel = 'Next Step',
  style
}) => {
  const { colors, isDarkMode } = useTheme();
  const isFirst = activeTab === 0;
  const isLast = activeTab >= totalTabs - 1;

  return (
    <View style={[styles.footerContainer, style]}>
      {/* Previous Step Button */}
      <TouchableOpacity
        activeOpacity={0.7}
        onPress={onPrev}
        disabled={isFirst}
        style={[
          styles.footerBtn,
          styles.prevBtn,
          {
            backgroundColor: isDarkMode ? colors.cardBg : '#ffffff',
            borderColor: isDarkMode ? colors.border : '#cbd5e1',
            opacity: isFirst ? 0.45 : 1
          }
        ]}
      >
        <Ionicons
          name="arrow-back-outline"
          size={16}
          color={
            isFirst
              ? isDarkMode
                ? '#64748b'
                : '#94a3b8'
              : isDarkMode
              ? colors.textDark
              : '#334155'
          }
        />
        <Text
          style={[
            styles.footerBtnText,
            {
              color: isFirst
                ? isDarkMode
                  ? '#64748b'
                  : '#94a3b8'
                : isDarkMode
                ? colors.textDark
                : '#334155'
            }
          ]}
        >
          {prevLabel}
        </Text>
      </TouchableOpacity>

      {/* Next Step Button (or Last Step indicator) */}
      {isLast ? (
        <View
          style={[
            styles.footerBtn,
            styles.lastStepBadge,
            {
              backgroundColor: isDarkMode ? 'rgba(16, 185, 129, 0.15)' : '#ecfdf5',
              borderColor: isDarkMode ? 'rgba(16, 185, 129, 0.4)' : '#a7f3d0'
            }
          ]}
        >
          <Ionicons name="checkmark-circle-outline" size={16} color="#10b981" />
          <Text style={[styles.footerBtnText, { color: '#10b981', fontWeight: '700' }]}>
            All Steps Reviewed
          </Text>
        </View>
      ) : (
        <TouchableOpacity
          activeOpacity={0.7}
          onPress={onNext}
          style={[
            styles.footerBtn,
            styles.nextBtn,
            { backgroundColor: colors.primary }
          ]}
        >
          <Text style={[styles.footerBtnText, { color: '#ffffff', fontWeight: '700' }]}>
            {nextLabel}
          </Text>
          <Ionicons name="arrow-forward-outline" size={16} color="#ffffff" />
        </TouchableOpacity>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  headerCard: {
    marginHorizontal: 12,
    marginTop: 8,
    marginBottom: 6,
    paddingHorizontal: 14,
    paddingTop: 10,
    paddingBottom: 10,
    borderRadius: 14,
    borderWidth: 1
  },
  metaRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  stepBadge: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6
  },
  stepBadgeText: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6
  },
  percentText: {
    fontSize: 12,
    fontWeight: '600'
  },
  progressTrack: {
    height: 4,
    borderRadius: 2,
    marginVertical: 8,
    overflow: 'hidden'
  },
  progressFill: {
    height: '100%',
    borderRadius: 2
  },
  nodesContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginVertical: 4
  },
  connectorLine: {
    flex: 1,
    height: 2
  },
  nodeCircle: {
    width: 28,
    height: 28,
    borderRadius: 14,
    justifyContent: 'center',
    alignItems: 'center'
  },
  nodeText: {
    fontSize: 12,
    fontWeight: '700'
  },
  activeBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingVertical: 6,
    paddingHorizontal: 10,
    borderRadius: 8,
    borderWidth: 1,
    marginTop: 8
  },
  activeBannerText: {
    fontSize: 13,
    fontWeight: '700',
    flex: 1
  },
  footerContainer: {
    flexDirection: 'row',
    gap: 10,
    marginHorizontal: 16,
    marginTop: 14,
    marginBottom: 4
  },
  footerBtn: {
    flex: 1,
    minHeight: SIZES.inputMinHeight,
    borderRadius: SIZES.inputRadius,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingHorizontal: 12
  },
  prevBtn: {
    borderWidth: 1
  },
  nextBtn: {},
  lastStepBadge: {
    borderWidth: 1
  },
  footerBtnText: {
    fontSize: 13,
    fontWeight: '600'
  }
});
