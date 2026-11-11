import React, { useState, useEffect, useMemo } from 'react';
import {
  Modal,
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  useWindowDimensions,
  Platform
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useTheme } from '../context/ThemeContext';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

const MONTH_NAMES_SHORT = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
];

const WEEKDAY_NAMES = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

export const DatePickerModal = ({
  visible,
  onClose,
  value,
  onSelectDate,
  title = 'Select Date'
}) => {
  const { colors, isDarkMode } = useTheme();
  const { width: windowWidth, height: windowHeight } = useWindowDimensions();

  // Dynamic responsive sizing
  const cardWidth = Math.min(windowWidth - 32, 370);
  const cardPadding = cardWidth < 340 ? 12 : 16;
  const availableGridWidth = cardWidth - (cardPadding * 2);
  const dayCellWidth = availableGridWidth / 7;
  const dayCellCircleSize = Math.max(30, Math.min(38, Math.floor(dayCellWidth * 0.88)));

  const styles = useMemo(
    () => getStyles(colors, isDarkMode, cardWidth, cardPadding, dayCellWidth, dayCellCircleSize),
    [colors, isDarkMode, cardWidth, cardPadding, dayCellWidth, dayCellCircleSize]
  );

  const parseInitialDate = () => {
    if (value && typeof value === 'string' && value.includes('-')) {
      const parts = value.split('-');
      if (parts.length >= 3) {
        const y = parseInt(parts[0], 10);
        const m = parseInt(parts[1], 10) - 1;
        const d = parseInt(parts[2], 10);
        if (!isNaN(y) && !isNaN(m) && !isNaN(d)) {
          return { year: y, month: m, day: d, valid: true };
        }
      }
    }
    const now = new Date();
    return { year: now.getFullYear(), month: now.getMonth(), day: now.getDate(), valid: false };
  };

  const today = useMemo(() => {
    const now = new Date();
    return { year: now.getFullYear(), month: now.getMonth(), day: now.getDate() };
  }, []);

  // Currently viewed month/year in calendar
  const [viewYear, setViewYear] = useState(today.year);
  const [viewMonth, setViewMonth] = useState(today.month);

  // Selected date
  const [selectedYear, setSelectedYear] = useState(today.year);
  const [selectedMonth, setSelectedMonth] = useState(today.month);
  const [selectedDay, setSelectedDay] = useState(today.day);
  const [hasSelection, setHasSelection] = useState(false);

  // Mode: 'calendar' | 'yearMonth'
  const [pickerMode, setPickerMode] = useState('calendar');

  // Decade offset for fast year jump
  const [decadeStartYear, setDecadeStartYear] = useState(Math.floor(today.year / 12) * 12);

  useEffect(() => {
    if (visible) {
      const parsed = parseInitialDate();
      setSelectedYear(parsed.year);
      setSelectedMonth(parsed.month);
      setSelectedDay(parsed.day);
      setViewYear(parsed.year);
      setViewMonth(parsed.month);
      setHasSelection(parsed.valid);
      setPickerMode('calendar');
      setDecadeStartYear(Math.floor(parsed.year / 12) * 12);
    }
  }, [visible, value]);

  // Helpers
  const pad2 = (n) => String(n).padStart(2, '0');
  const formatIsoDate = (y, m, d) => `${y}-${pad2(m + 1)}-${pad2(d)}`;

  const formatFriendlyDate = (y, m, d) => {
    try {
      const dateObj = new Date(y, m, d);
      return dateObj.toLocaleDateString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });
    } catch {
      return `${MONTH_NAMES_SHORT[m]} ${d}, ${y}`;
    }
  };

  // Month navigation
  const handlePrevMonth = () => {
    if (viewMonth === 0) {
      setViewMonth(11);
      setViewYear((prev) => prev - 1);
    } else {
      setViewMonth((prev) => prev - 1);
    }
  };

  const handleNextMonth = () => {
    if (viewMonth === 11) {
      setViewMonth(0);
      setViewYear((prev) => prev + 1);
    } else {
      setViewMonth((prev) => prev + 1);
    }
  };

  // Build the responsive calendar month grid
  const calendarGrid = useMemo(() => {
    const daysInCurrentMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
    const firstDayIndex = new Date(viewYear, viewMonth, 1).getDay(); // 0 = Sun
    const daysInPrevMonth = new Date(viewYear, viewMonth, 0).getDate();

    const cells = [];

    // 1. Previous month trailing days
    for (let i = firstDayIndex - 1; i >= 0; i--) {
      const prevD = daysInPrevMonth - i;
      const prevM = viewMonth === 0 ? 11 : viewMonth - 1;
      const prevY = viewMonth === 0 ? viewYear - 1 : viewYear;
      cells.push({
        day: prevD,
        month: prevM,
        year: prevY,
        isCurrentMonth: false,
        key: `prev-${prevY}-${prevM}-${prevD}`
      });
    }

    // 2. Current month days
    for (let d = 1; d <= daysInCurrentMonth; d++) {
      cells.push({
        day: d,
        month: viewMonth,
        year: viewYear,
        isCurrentMonth: true,
        key: `curr-${viewYear}-${viewMonth}-${d}`
      });
    }

    // 3. Next month leading days to complete the grid (35 or 42 cells)
    const targetLength = cells.length > 35 ? 42 : 35;
    const remaining = targetLength - cells.length;
    for (let d = 1; d <= remaining; d++) {
      const nextM = viewMonth === 11 ? 0 : viewMonth + 1;
      const nextY = viewMonth === 11 ? viewYear + 1 : viewYear;
      cells.push({
        day: d,
        month: nextM,
        year: nextY,
        isCurrentMonth: false,
        key: `next-${nextY}-${nextM}-${d}`
      });
    }

    return cells;
  }, [viewYear, viewMonth]);

  // Handle day click
  const handleSelectDay = (cell) => {
    setSelectedYear(cell.year);
    setSelectedMonth(cell.month);
    setSelectedDay(cell.day);
    setHasSelection(true);

    if (!cell.isCurrentMonth) {
      setViewYear(cell.year);
      setViewMonth(cell.month);
    }
  };

  // Fast Year/Month Picker selections
  const handleSelectYearFromGrid = (yr) => {
    setViewYear(yr);
    setSelectedYear(yr);
  };

  const handleSelectMonthFromGrid = (mIdx) => {
    setViewMonth(mIdx);
    setSelectedMonth(mIdx);
    setPickerMode('calendar');
  };

  // Confirm selection
  const handleConfirm = () => {
    if (hasSelection) {
      const formatted = formatIsoDate(selectedYear, selectedMonth, selectedDay);
      if (onSelectDate) onSelectDate(formatted);
    }
    if (onClose) onClose();
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onClose}
    >
      <View style={styles.modalOverlay}>
        <View style={styles.modalCard}>
          {/* Header Bar */}
          <View style={styles.modalHeader}>
            <View style={styles.headerTitleRow}>
              <View style={styles.headerIconWrap}>
                <Ionicons name="calendar" size={17} color="#ffffff" />
              </View>
              <View style={{ flex: 1 }}>
                <Text style={styles.modalTitle} numberOfLines={1}>{title}</Text>
                <Text style={styles.selectedDateBadge} numberOfLines={1}>
                  {hasSelection
                    ? formatFriendlyDate(selectedYear, selectedMonth, selectedDay)
                    : 'Select a date'}
                </Text>
              </View>
            </View>
            <TouchableOpacity onPress={onClose} style={styles.closeBtn} hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}>
              <Ionicons name="close" size={18} color={colors.textMuted} />
            </TouchableOpacity>
          </View>

          {/* Month / Year Navigator Bar */}
          <View style={styles.navBar}>
            <TouchableOpacity
              style={styles.navArrowBtn}
              onPress={handlePrevMonth}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            >
              <Ionicons name="chevron-back" size={18} color={colors.textDark} />
            </TouchableOpacity>

            <TouchableOpacity
              style={styles.monthYearBtn}
              activeOpacity={0.7}
              onPress={() => setPickerMode((prev) => (prev === 'calendar' ? 'yearMonth' : 'calendar'))}
            >
              <Text style={styles.monthYearTitle}>
                {MONTH_NAMES[viewMonth]} {viewYear}
              </Text>
              <Ionicons
                name={pickerMode === 'yearMonth' ? 'chevron-up' : 'chevron-down'}
                size={15}
                color={colors.primary}
                style={{ marginLeft: 4 }}
              />
            </TouchableOpacity>

            <TouchableOpacity
              style={styles.navArrowBtn}
              onPress={handleNextMonth}
              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
            >
              <Ionicons name="chevron-forward" size={18} color={colors.textDark} />
            </TouchableOpacity>
          </View>

          {pickerMode === 'calendar' ? (
            <View style={styles.calendarContainer}>
              {/* Day of Week Labels (SUN - SAT) */}
              <View style={styles.weekdayRow}>
                {WEEKDAY_NAMES.map((w, index) => (
                  <View key={w} style={styles.weekdayCell}>
                    <Text
                      style={[
                        styles.weekdayText,
                        (index === 0 || index === 6) && styles.weekendText
                      ]}
                    >
                      {w}
                    </Text>
                  </View>
                ))}
              </View>

              {/* 7-Column Responsive Days Grid */}
              <View style={styles.daysGrid}>
                {calendarGrid.map((cell) => {
                  const isSelected =
                    hasSelection &&
                    selectedYear === cell.year &&
                    selectedMonth === cell.month &&
                    selectedDay === cell.day;

                  const isToday =
                    today.year === cell.year &&
                    today.month === cell.month &&
                    today.day === cell.day;

                  return (
                    <TouchableOpacity
                      key={cell.key}
                      style={styles.dayCellContainer}
                      activeOpacity={0.7}
                      onPress={() => handleSelectDay(cell)}
                    >
                      <View
                        style={[
                          styles.dayCell,
                          isToday && !isSelected && styles.dayCellToday,
                          isSelected && styles.dayCellSelected
                        ]}
                      >
                        <Text
                          style={[
                            styles.dayText,
                            !cell.isCurrentMonth && styles.dayTextOtherMonth,
                            isToday && !isSelected && styles.dayTextToday,
                            isSelected && styles.dayTextSelected
                          ]}
                        >
                          {cell.day}
                        </Text>
                        {isToday && !isSelected && (
                          <View style={styles.todayDot} />
                        )}
                      </View>
                    </TouchableOpacity>
                  );
                })}
              </View>
            </View>
          ) : (
            /* Fast Year & Month Picker Mode */
            <ScrollView style={styles.yearMonthContainer} showsVerticalScrollIndicator={false}>
              {/* Decade Navigator for Years */}
              <View style={styles.decadeHeader}>
                <TouchableOpacity
                  style={styles.smallNavBtn}
                  onPress={() => setDecadeStartYear((prev) => prev - 12)}
                >
                  <Ionicons name="chevron-back" size={15} color={colors.textDark} />
                </TouchableOpacity>
                <Text style={styles.decadeTitle}>
                  {decadeStartYear} – {decadeStartYear + 11}
                </Text>
                <TouchableOpacity
                  style={styles.smallNavBtn}
                  onPress={() => setDecadeStartYear((prev) => prev + 12)}
                >
                  <Ionicons name="chevron-forward" size={15} color={colors.textDark} />
                </TouchableOpacity>
              </View>

              {/* Years Grid (12 items) */}
              <View style={styles.jumpGrid}>
                {Array.from({ length: 12 }, (_, i) => decadeStartYear + i).map((yr) => {
                  const isCurYear = viewYear === yr;
                  return (
                    <TouchableOpacity
                      key={yr}
                      style={[styles.jumpChip, isCurYear && styles.jumpChipActive]}
                      onPress={() => handleSelectYearFromGrid(yr)}
                    >
                      <Text style={[styles.jumpChipText, isCurYear && styles.jumpChipTextActive]}>
                        {yr}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </View>

              {/* Months Grid */}
              <Text style={styles.subSectionTitle}>Select Month</Text>
              <View style={styles.jumpGrid}>
                {MONTH_NAMES_SHORT.map((mName, mIdx) => {
                  const isCurMonth = viewMonth === mIdx;
                  return (
                    <TouchableOpacity
                      key={mName}
                      style={[styles.jumpChip, isCurMonth && styles.jumpChipActive]}
                      onPress={() => handleSelectMonthFromGrid(mIdx)}
                    >
                      <Text style={[styles.jumpChipText, isCurMonth && styles.jumpChipTextActive]}>
                        {mName}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </View>
            </ScrollView>
          )}

          {/* Footer Actions (Cancel and Select Date only) */}
          <View style={styles.footerContainer}>
            <TouchableOpacity style={styles.cancelButton} onPress={onClose} activeOpacity={0.7}>
              <Text style={styles.cancelButtonText}>Cancel</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.confirmButton} onPress={handleConfirm} activeOpacity={0.8}>
              <Text style={styles.confirmButtonText}>Select Date</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </Modal>
  );
};

const getStyles = (colors, isDarkMode, cardWidth, cardPadding, dayCellWidth, dayCellCircleSize) =>
  StyleSheet.create({
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.65)',
      justifyContent: 'center',
      alignItems: 'center',
      padding: 16
    },
    modalCard: {
      width: cardWidth,
      backgroundColor: colors.cardBg,
      borderRadius: 22,
      padding: cardPadding,
      borderWidth: 1,
      borderColor: colors.border,
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 8 },
      shadowOpacity: 0.25,
      shadowRadius: 16,
      elevation: 10
    },
    modalHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingBottom: 12,
      borderBottomWidth: 1,
      borderBottomColor: colors.border
    },
    headerTitleRow: {
      flexDirection: 'row',
      alignItems: 'center',
      flex: 1,
      marginRight: 8
    },
    headerIconWrap: {
      width: 34,
      height: 34,
      borderRadius: 9,
      backgroundColor: colors.primary,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: 10
    },
    modalTitle: {
      fontSize: 14,
      fontWeight: '700',
      color: colors.textDark,
      letterSpacing: 0.2
    },
    selectedDateBadge: {
      fontSize: 12,
      fontWeight: '600',
      color: colors.primary,
      marginTop: 2
    },
    closeBtn: {
      padding: 6,
      borderRadius: 8,
      backgroundColor: colors.surfaceSecondary || (isDarkMode ? '#334155' : '#f1f5f9')
    },

    // Month/Year Navigator
    navBar: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingVertical: 10,
      paddingHorizontal: 2
    },
    navArrowBtn: {
      width: 34,
      height: 34,
      borderRadius: 17,
      backgroundColor: colors.surfaceSecondary || (isDarkMode ? '#334155' : '#f1f5f9'),
      alignItems: 'center',
      justifyContent: 'center'
    },
    monthYearBtn: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 12,
      backgroundColor: colors.surfaceSecondary || (isDarkMode ? '#334155' : '#f1f5f9')
    },
    monthYearTitle: {
      fontSize: 14,
      fontWeight: '700',
      color: colors.textDark
    },

    // Calendar
    calendarContainer: {
      marginTop: 2
    },
    weekdayRow: {
      flexDirection: 'row',
      marginBottom: 4
    },
    weekdayCell: {
      width: dayCellWidth,
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: 4
    },
    weekdayText: {
      fontSize: 11,
      fontWeight: '700',
      color: colors.textMuted,
      textTransform: 'uppercase'
    },
    weekendText: {
      color: colors.primary
    },
    daysGrid: {
      flexDirection: 'row',
      flexWrap: 'wrap'
    },
    dayCellContainer: {
      width: dayCellWidth,
      height: dayCellWidth,
      alignItems: 'center',
      justifyContent: 'center'
    },
    dayCell: {
      width: dayCellCircleSize,
      height: dayCellCircleSize,
      borderRadius: Math.floor(dayCellCircleSize / 2),
      alignItems: 'center',
      justifyContent: 'center',
      position: 'relative'
    },
    dayCellToday: {
      borderWidth: 1.5,
      borderColor: colors.primary
    },
    dayCellSelected: {
      backgroundColor: colors.primary,
      shadowColor: colors.primary,
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.35,
      shadowRadius: 4,
      elevation: 4
    },
    dayText: {
      fontSize: 13,
      fontWeight: '600',
      color: colors.textDark
    },
    dayTextOtherMonth: {
      color: isDarkMode ? '#475569' : '#cbd5e1'
    },
    dayTextToday: {
      color: colors.primary,
      fontWeight: '800'
    },
    dayTextSelected: {
      color: '#ffffff',
      fontWeight: '800'
    },
    todayDot: {
      position: 'absolute',
      bottom: 2,
      width: 4,
      height: 4,
      borderRadius: 2,
      backgroundColor: colors.primary
    },

    // Fast Year / Month Jump
    yearMonthContainer: {
      maxHeight: 260,
      paddingVertical: 4
    },
    decadeHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingVertical: 6,
      marginBottom: 6
    },
    smallNavBtn: {
      padding: 6,
      borderRadius: 8,
      backgroundColor: colors.surfaceSecondary || (isDarkMode ? '#334155' : '#f1f5f9')
    },
    decadeTitle: {
      fontSize: 13,
      fontWeight: '700',
      color: colors.textDark
    },
    subSectionTitle: {
      fontSize: 11,
      fontWeight: '700',
      color: colors.textMuted,
      marginTop: 10,
      marginBottom: 6,
      textTransform: 'uppercase',
      letterSpacing: 0.4
    },
    jumpGrid: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: 6
    },
    jumpChip: {
      width: '23%',
      paddingVertical: 7,
      borderRadius: 9,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: colors.surfaceSecondary || (isDarkMode ? '#1e293b' : '#f8fafc'),
      borderWidth: 1,
      borderColor: colors.border
    },
    jumpChipActive: {
      backgroundColor: colors.primary,
      borderColor: colors.primary
    },
    jumpChipText: {
      fontSize: 12,
      fontWeight: '600',
      color: colors.textDark
    },
    jumpChipTextActive: {
      color: '#ffffff',
      fontWeight: '800'
    },

    // Footer
    footerContainer: {
      marginTop: 12,
      paddingTop: 10,
      borderTopWidth: 1,
      borderTopColor: colors.border,
      flexDirection: 'row',
      justifyContent: 'flex-end',
      alignItems: 'center',
      gap: 10
    },
    cancelButton: {
      paddingHorizontal: 16,
      paddingVertical: 9,
      borderRadius: 11,
      backgroundColor: colors.surfaceSecondary || (isDarkMode ? '#334155' : '#f1f5f9')
    },
    cancelButtonText: {
      fontSize: 13,
      fontWeight: '600',
      color: colors.textMuted
    },
    confirmButton: {
      paddingHorizontal: 20,
      paddingVertical: 9,
      borderRadius: 11,
      backgroundColor: colors.primary,
      shadowColor: colors.primary,
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.25,
      shadowRadius: 4,
      elevation: 3
    },
    confirmButtonText: {
      fontSize: 13,
      fontWeight: '700',
      color: '#ffffff'
    }
  });
