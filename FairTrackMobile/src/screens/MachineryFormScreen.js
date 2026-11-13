import React, { useState, useEffect, useContext, useMemo, useRef } from 'react';
import {
  View,
  Text,
  ScrollView,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  Platform,
  Switch,
  Modal,
  KeyboardAvoidingView
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { MobileApi } from '../api/mobileApi';
import { HeaderBar } from '../components/HeaderBar';
import { ActionModal } from '../components/ActionModal';
import { DiscardChangesModal } from '../components/DiscardChangesModal';
import { AttachmentsSection } from '../components/AttachmentsSection';
import { savePartitionAttachmentsOffline, loadPartitionAttachmentsOffline } from '../utils/fileStorage';
import { DatePickerModal } from '../components/DatePickerModal';
import { BARANGAY_OPTIONS, normalizeBarangay } from '../constants/locationData';

// CAST-aligned 3 Tabs for Machinery (matching web _MachineryPartitionForm.cshtml)
const TABS = [
  { label: '1. Property Information & Owner (CAST)', icon: 'information-circle-outline' },
  { label: '2. Property Appraisal & Assessment Roll (CAST)', icon: 'calculator-outline' },
  { label: '3. Memoranda, Effectivity & Back Taxes', icon: 'document-text-outline' },
];

const GENDER_OPTIONS = ['Male', 'Female', 'Corporate / Entity'];
const MACHINERY_CLASSIFICATIONS = ['INDUSTRIAL', 'COMMERCIAL', 'RESIDENTIAL', 'AGRICULTURAL', 'SPECIAL'];
const MACHINERY_CONDITIONS = ['NEW', 'SECOND HAND'];
const EFFECTIVITY_QUARTERS = ['1st Quarter', '2nd Quarter', '3rd Quarter', '4th Quarter'];
const TAXABILITY_OPTIONS = ['Taxable', 'Exempt'];

export const MachineryFormScreen = ({ route, navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const styles = useMemo(() => getStyles(colors, isDarkMode), [colors, isDarkMode]);

  const trackingNoParam = route?.params?.trackingNo || '';
  const formWorkflowIdParam = route?.params?.formWorkflowId || null;
  const partitionIndexParam = route?.params?.partitionIndex ?? 0;
  const partitionLetterParam = route?.params?.partitionLetter || String.fromCharCode(65 + (parseInt(partitionIndexParam, 10) || 0));
  const fromHistory = route?.params?.fromHistory === true;

  const {
    queueOfflineUpdate,
    markTransactionSynced,
    getDownloadedFormData,
    updatePartitionAttachmentsLocally,
    isTransactionDownloaded,
    hasPendingEdits,
    downloadSingleTransaction,
    isOnline,
    checkConnectivity
  } = useContext(SyncContext);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [notDownloadedError, setNotDownloadedError] = useState(false);
  const [discardModalVisible, setDiscardModalVisible] = useState(false);
  const [completeModalConfig, setCompleteModalConfig] = useState({
    visible: false,
    title: '',
    message: '',
    iconType: 'success',
    icon: 'checkmark-circle-outline'
  });
  const isDownloaded = isTransactionDownloaded(trackingNoParam, formWorkflowIdParam);

  const showCompleteModalAndGoBack = (title, message, iconType = 'success', icon = 'checkmark-circle-outline') => {
    setCompleteModalConfig({
      visible: true,
      title,
      message,
      iconType,
      icon
    });
  };

  const tabScrollRef = useRef(null);
  const [activeTab, setActiveTab] = useState(0);

  // Picker modal state (generic bottom sheet selector)
  const [pickerModal, setPickerModal] = useState({
    visible: false,
    title: '',
    options: [],
    selectedValue: null,
    onSelect: null
  });

  const openPicker = (title, options, selectedValue, onSelect) => {
    setPickerModal({
      visible: true,
      title,
      options,
      selectedValue,
      onSelect
    });
  };

  // DatePicker modal state
  const [datePickerState, setDatePickerState] = useState({
    visible: false,
    title: '',
    currentDate: new Date().toISOString().substring(0, 10),
    targetField: ''
  });

  const openDatePicker = (title, targetField, currentDate) => {
    setDatePickerState({
      visible: true,
      title,
      targetField,
      currentDate: currentDate || new Date().toISOString().substring(0, 10)
    });
  };

  const handleDateConfirm = (selectedDate) => {
    const field = datePickerState.targetField;
    if (field === 'machineryDateRegistered') setMachineryDateRegistered(selectedDate);
    else if (field === 'importationDateIssued') setImportationDateIssued(selectedDate);
    else if (field === 'orDate') {
      setOrDate(selectedDate);
      if (orNo) setOrNoAndDate(`${orNo} / ${selectedDate}`);
    } else if (field === 'localDateIssued') setLocalDateIssued(selectedDate);
    else if (field === 'mAcquiredDate') setMAcquiredDate(selectedDate);
    setDatePickerState(prev => ({ ...prev, visible: false }));
  };

  // =========================================================================
  // TAB 1: PROPERTY INFORMATION, OWNER & LOCATION
  // =========================================================================
  const [tdnId, setTdnId] = useState(route?.params?.tdnId || null);
  const [tdnMachineId, setTdnMachineId] = useState(null);
  const [trackingNo, setTrackingNo] = useState(trackingNoParam);
  const [declarationNo, setDeclarationNo] = useState('');
  const [arpNo, setArpNo] = useState('');
  const [pin, setPin] = useState('');
  const [sectionNo, setSectionNo] = useState('');
  const [lotNo, setLotNo] = useState('');

  // Owner Demographics
  const [ownerName, setOwnerName] = useState('');
  const [ownerGender, setOwnerGender] = useState('Male');
  const [ownerTin, setOwnerTin] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerContact, setOwnerContact] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');

  // Role Pills (Administrator, Beneficial User, Possessor)
  const [isAdministrator, setIsAdministrator] = useState(false);
  const [isBeneficialUser, setIsBeneficialUser] = useState(false);
  const [isPossessor, setIsPossessor] = useState(false);

  // Administrator / Beneficial User / Possessor Details
  const [adminName, setAdminName] = useState('');
  const [adminGender, setAdminGender] = useState('Male');
  const [adminTin, setAdminTin] = useState('');
  const [adminAddress, setAdminAddress] = useState('');
  const [adminContact, setAdminContact] = useState('');
  const [adminEmail, setAdminEmail] = useState('');

  // Property Registration Details (Imported vs Local)
  const [isLocallyManufactured, setIsLocallyManufactured] = useState(false);
  // Panel A: Imported
  const [machineryRegNo, setMachineryRegNo] = useState('');
  const [machineryDateRegistered, setMachineryDateRegistered] = useState('');
  const [importationPermitNo, setImportationPermitNo] = useState('');
  const [importationDateIssued, setImportationDateIssued] = useState('');
  const [countryOfOrigin, setCountryOfOrigin] = useState('');
  const [supplierSeller, setSupplierSeller] = useState('');
  const [acquisitionCost, setAcquisitionCost] = useState('');

  // Panel B: Local
  const [orNo, setOrNo] = useState('');
  const [orDate, setOrDate] = useState('');
  const [orNoAndDate, setOrNoAndDate] = useState('');
  const [localDateIssued, setLocalDateIssued] = useState('');
  const [supplierAddress, setSupplierAddress] = useState('');

  // Property Location & Reference
  const [isLandOwnerAlsoBuildingOwner, setIsLandOwnerAlsoBuildingOwner] = useState(false);
  const [buildingReferenceName, setBuildingReferenceName] = useState('');
  const [buildingPin, setBuildingPin] = useState('');
  const [buildingTdNo, setBuildingTdNo] = useState('');
  const [landOwnerName, setLandOwnerName] = useState('');
  const [landPin, setLandPin] = useState('');
  const [landTdNo, setLandTdNo] = useState('');
  const [streetLocation, setStreetLocation] = useState('');
  const [districtOrBarangay, setDistrictOrBarangay] = useState('BRGY. ZONE I');
  const [cityMunicipality] = useState('Koronadal City');
  const [province] = useState('South Cotabato');

  const handleToggleLandOwnerAlsoBuildingOwner = () => {
    const next = !isLandOwnerAlsoBuildingOwner;
    setIsLandOwnerAlsoBuildingOwner(next);
    if (next) {
      if (landOwnerName && landOwnerName.trim()) {
        setBuildingReferenceName(landOwnerName.trim());
      } else if (buildingReferenceName && buildingReferenceName.trim()) {
        setLandOwnerName(buildingReferenceName.trim());
      } else if (ownerName && ownerName.trim()) {
        setLandOwnerName(ownerName.trim());
        setBuildingReferenceName(ownerName.trim());
      }

      if (landPin && landPin.trim() && !buildingPin) {
        setBuildingPin(landPin.trim());
      } else if (buildingPin && buildingPin.trim() && !landPin) {
        setLandPin(buildingPin.trim());
      }

      if (landTdNo && landTdNo.trim() && !buildingTdNo) {
        setBuildingTdNo(landTdNo.trim());
      } else if (buildingTdNo && buildingTdNo.trim() && !landTdNo) {
        setLandTdNo(buildingTdNo.trim());
      }
    } else {
      // When unchecked: Land Owner is NOT the Building Owner, clear the mirrored building reference fields
      setBuildingReferenceName('');
      setBuildingPin('');
      setBuildingTdNo('');
    }
  };

  const handleBuildingOwnerChange = (val) => {
    setBuildingReferenceName(val);
    if (isLandOwnerAlsoBuildingOwner) {
      setLandOwnerName(val);
    }
  };

  const handleLandOwnerChange = (val) => {
    setLandOwnerName(val);
    if (isLandOwnerAlsoBuildingOwner) {
      setBuildingReferenceName(val);
    }
  };

  // =========================================================================
  // TAB 2: PROPERTY APPRAISAL & ASSESSMENT ROLL (CAST)
  // =========================================================================
  // Table 1: Machinery & Equipment Schedule (16-Column CAST Standard)
  const [machineryDetailsList, setMachineryDetailsList] = useState([]);

  // Modal State for Add / Edit Machinery Appraisal Detail
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [editingDetailIndex, setEditingDetailIndex] = useState(null);

  // Detail Modal Fields
  const [mClassification, setMClassification] = useState('INDUSTRIAL');
  const [mBrandModel, setMBrandModel] = useState('');
  const [mCapacity, setMCapacity] = useState('');
  const [mCondition, setMCondition] = useState('NEW');
  const [mAcquiredDate, setMAcquiredDate] = useState(new Date().toISOString().substring(0, 10));
  const [mEstLife, setMEstLife] = useState('20');
  const [mYearInstalled, setMYearInstalled] = useState(new Date().getFullYear().toString());
  const [mYearOperation, setMYearOperation] = useState(new Date().getFullYear().toString());
  const [mYearsUsed, setMYearsUsed] = useState('0');
  const [mRemLife, setMRemLife] = useState('20');
  const [mOriginalCost, setMOriginalCost] = useState('0.00');
  const [mInstallationCost, setMInstallationCost] = useState('0.00');
  const [mTotalCost, setMTotalCost] = useState('0.00');
  const [mDepPercentage, setMDepPercentage] = useState('0');
  const [mDepValue, setMDepValue] = useState('0.00');
  const [mDepreciatedValue, setMDepreciatedValue] = useState('0.00');

  // Table 2: Property Assessment Roll (Actual Use & Assessment Level)
  const [machineryPropertyAssessments, setMachineryPropertyAssessments] = useState([]);

  // =========================================================================
  // TAB 3: MEMORANDA, EFFECTIVITY & BACK TAXES
  // =========================================================================
  const [taxability, setTaxability] = useState(true);
  const [quarterEffectivity, setQuarterEffectivity] = useState('1st Quarter');
  const [yearEffectivity, setYearEffectivity] = useState(new Date().getFullYear().toString());
  const [assessmentBasis, setAssessmentBasis] = useState('Sec. 224-225 RA 7160');
  const [backTaxesYearFrom, setBackTaxesYearFrom] = useState('');
  const [backTaxesYearTo, setBackTaxesYearTo] = useState('');
  const [memoranda, setMemoranda] = useState('');
  const [remarks, setRemarks] = useState('');
  const [attachments, setAttachments] = useState([]);

  // Workflow Forwarding
  const [autoForward, setAutoForward] = useState(true);
  const [nextStatusId] = useState(4); // 4 = Checked / Recommend Approval

  // =========================================================================
  // COMPUTED VALUATIONS & SUMMARY KPIS
  // =========================================================================
  const totalsTable1 = useMemo(() => {
    let orig = 0;
    let install = 0;
    let total = 0;
    let dep = 0;
    let depMv = 0;
    machineryDetailsList.forEach(item => {
      orig += parseFloat(item.OriginalCost) || 0;
      install += parseFloat(item.InstallationCost) || 0;
      total += parseFloat(item.TotalCost) || ((parseFloat(item.OriginalCost) || 0) + (parseFloat(item.InstallationCost) || 0));
      dep += parseFloat(item.DepreciationTotalValue) || 0;
      depMv += parseFloat(item.DepreciatedValue) || parseFloat(item.MarketValue) || 0;
    });
    return {
      totalOrigCost: Math.round(orig * 100) / 100,
      totalInstallCost: Math.round(install * 100) / 100,
      totalCost: Math.round(total * 100) / 100,
      totalDepValue: Math.round(dep * 100) / 100,
      totalDepreciatedValue: Math.round(depMv * 100) / 100
    };
  }, [machineryDetailsList]);

  const totalsTable2 = useMemo(() => {
    let mv = 0;
    let av = 0;
    machineryPropertyAssessments.forEach(item => {
      const rowMv = parseFloat(item.MarketValue) || 0;
      const rowAv = parseFloat(item.AssessmentValue) || Math.round(rowMv * ((parseFloat(item.AssessmentLevelPercent) || 80) / 100) * 100) / 100;
      mv += rowMv;
      av += rowAv;
    });
    return {
      totalRollMv: Math.round(mv * 100) / 100,
      totalRollAv: Math.round(av * 100) / 100
    };
  }, [machineryPropertyAssessments]);

  // Overall Market Value and Assessed Value
  const grandMarketValue = totalsTable2.totalRollMv > 0 ? totalsTable2.totalRollMv : totalsTable1.totalDepreciatedValue;
  const grandAssessedValue = totalsTable2.totalRollAv > 0 ? totalsTable2.totalRollAv : Math.round(grandMarketValue * 0.8 * 100) / 100;
  const annualTaxDue = Math.round(grandAssessedValue * 0.03 * 100) / 100;

  // =========================================================================
  // LIFECYCLE & HYDRATION
  // =========================================================================
  useEffect(() => {
    loadMachineryData();
  }, []);

  const loadMachineryData = async () => {
    setNotDownloadedError(false);

    // 0. Load any locally saved partition offline attachments
    const offlineAttData = await loadPartitionAttachmentsOffline(trackingNoParam, partitionIndexParam);
    if (offlineAttData.attachments && offlineAttData.attachments.length > 0) {
      setAttachments(offlineAttData.attachments);
    }

    // 1. Populate from offline downloaded cache if available
    const downloaded = getDownloadedFormData(trackingNoParam, formWorkflowIdParam, partitionIndexParam);
    if (downloaded) {
      if (downloaded.currentPartition) {
        populateFromPartition(downloaded.currentPartition);
      }
      // Top-level formData belongs to Partition 1; never apply it over partitions after the first
      if (downloaded.formData && ((parseInt(partitionIndexParam, 10) || 0) === 0 || !downloaded.currentPartition)) {
        populateFieldsFromDTO(downloaded.formData);
      }
      if (downloaded.attachments && Array.isArray(downloaded.attachments)) {
        setAttachments(downloaded.attachments);
      }
    }

    // 2. If online and no unsynced edits, fetch latest live transaction details
    const hasUnsynced = typeof hasPendingEdits === 'function' && hasPendingEdits(trackingNoParam, formWorkflowIdParam);
    if (!hasUnsynced && (trackingNoParam || formWorkflowIdParam)) {
      try {
        if (!downloaded) setLoading(true);
        const liveRes = await MobileApi.downloadTransactionDetails(formWorkflowIdParam, trackingNoParam, partitionIndexParam);
        if (liveRes && liveRes.success) {
          const targetIdx = parseInt(partitionIndexParam, 10) || 0;
          const livePart = (liveRes.partitions && liveRes.partitions[targetIdx]) || null;
          if (livePart) {
            populateFromPartition(livePart);
          }
          const liveAtts = livePart?.attachments || liveRes.attachments || [];
          if (Array.isArray(liveAtts) && liveAtts.length > 0) {
            setAttachments(liveAtts);
          }
          if (typeof downloadSingleTransaction === 'function') {
            await downloadSingleTransaction({
              TrackingNo: trackingNoParam || liveRes.trackingNo,
              FormworkflowId: formWorkflowIdParam || liveRes.formWorkflowId,
              HasPartition: true
            });
          }
        } else if (!downloaded) {
          setNotDownloadedError(true);
        }
      } catch (e) {
        if (!downloaded) setNotDownloadedError(true);
      } finally {
        setLoading(false);
      }
    } else if (!downloaded) {
      setNotDownloadedError(true);
    }
  };

  const populateFromPartition = (p) => {
    if (!p) return;
    // Partition isolation: for partitions after the first, reset fields unconditionally
    // so stale state from another partition never leaks.
    if ((parseInt(partitionIndexParam, 10) || 0) > 0) {
      setSectionNo(p.sectionNo || '');
      setLotNo(p.lotNo || p.lotNumber || '');
      setPin(p.pin || '');
      setArpNo(p.arpNo || '');
      setOwnerName(p.ownerName || p.newOwnerName || '');
      setOwnerGender(p.ownerGender || '');
      setOwnerTin(p.ownerTin || p.ownerTIN || '');
      setOwnerAddress(p.ownerAddress || '');
      setOwnerContact(p.ownerContactNo || '');
      setOwnerEmail(p.ownerEmail || '');
      setAdminName(p.administratorName || '');
      setMachineryRegNo(p.machineryRegNo || '');
      setImportationPermitNo(p.importationPermitNo || '');
      setCountryOfOrigin(p.countryOfOrigin || '');
      setSupplierSeller(p.supplierSeller || '');
      setAcquisitionCost(p.acquisitionCost != null ? p.acquisitionCost.toString() : '');
      setOrNo(p.orNo || '');
      setStreetLocation(p.streetLocation || p.streetNo || p.propertyLocation || '');
      setDistrictOrBarangay(p.districtOrBarangay ? normalizeBarangay(p.districtOrBarangay) : '');
      setMemoranda(p.memoranda || '');
      setRemarks(p.remarks || '');
    }
    if (p.systemGeneratedTDN) setDeclarationNo(p.systemGeneratedTDN);
    if (p.arpNo) setArpNo(p.arpNo);
    if (p.pin) setPin(p.pin);
    if (p.sectionNo) setSectionNo(p.sectionNo);
    if (p.lotNo || p.lotNumber) setLotNo(p.lotNo || p.lotNumber);

    // Demographics
    if (p.ownerName || p.newOwnerName) setOwnerName(p.ownerName || p.newOwnerName);
    if (p.ownerGender) setOwnerGender(p.ownerGender);
    if (p.ownerTin || p.ownerTIN) setOwnerTin(p.ownerTin || p.ownerTIN);
    if (p.ownerAddress) setOwnerAddress(p.ownerAddress);
    if (p.ownerContactNo) setOwnerContact(p.ownerContactNo);
    if (p.ownerEmail) setOwnerEmail(p.ownerEmail);

    setIsAdministrator(!!p.isAdministrator);
    setIsBeneficialUser(!!p.isBeneficialUser);
    setIsPossessor(!!p.isPossessor);
    if (p.administratorName) setAdminName(p.administratorName);
    if (p.administratorGender) setAdminGender(p.administratorGender);
    if (p.administratorTin) setAdminTin(p.administratorTin);
    if (p.administratorAddress) setAdminAddress(p.administratorAddress);
    if (p.administratorContactNo) setAdminContact(p.administratorContactNo);
    if (p.administratorEmail) setAdminEmail(p.administratorEmail);

    // Registration Details
    if (p.isLocallyManufactured != null) setIsLocallyManufactured(!!p.isLocallyManufactured);
    if (p.machineryRegNo) setMachineryRegNo(p.machineryRegNo);
    if (p.machineryDateRegistered) setMachineryDateRegistered(p.machineryDateRegistered.toString().substring(0, 10));
    if (p.importationPermitNo) setImportationPermitNo(p.importationPermitNo);
    if (p.importationDateIssued) setImportationDateIssued(p.importationDateIssued.toString().substring(0, 10));
    if (p.countryOfOrigin) setCountryOfOrigin(p.countryOfOrigin);
    if (p.supplierSeller) setSupplierSeller(p.supplierSeller);
    if (p.acquisitionCost != null) setAcquisitionCost(p.acquisitionCost.toString());
    if (p.orNo) setOrNo(p.orNo);
    if (p.orDate) setOrDate(p.orDate.toString().substring(0, 10));
    if (p.orNoAndDate) setOrNoAndDate(p.orNoAndDate);
    if (p.localDateIssued) setLocalDateIssued(p.localDateIssued.toString().substring(0, 10));
    if (p.supplierAddress) setSupplierAddress(p.supplierAddress);

    // Location & References
    if (p.isLandOwnerAlsoBuildingOwner != null) setIsLandOwnerAlsoBuildingOwner(!!p.isLandOwnerAlsoBuildingOwner);
    if (p.buildingReferenceName) setBuildingReferenceName(p.buildingReferenceName);
    if (p.buildingPin) setBuildingPin(p.buildingPin);
    if (p.buildingTdNo) setBuildingTdNo(p.buildingTdNo);
    if (p.landOwnerName) setLandOwnerName(p.landOwnerName);
    if (p.landPin) setLandPin(p.landPin);
    if (p.landTdNo) setLandTdNo(p.landTdNo);
    if (p.streetLocation || p.streetNo || p.propertyLocation) setStreetLocation(p.streetLocation || p.streetNo || p.propertyLocation);
    if (p.districtOrBarangay) setDistrictOrBarangay(normalizeBarangay(p.districtOrBarangay));

    // Dynamic Appraisal Schedules
    const detailsJson = p.machineryAppraisalDetailsJson || p.MachineryAppraisalDetailsJson;
    if (detailsJson) {
      try {
        const parsed = typeof detailsJson === 'string' ? JSON.parse(detailsJson) : detailsJson;
        if (Array.isArray(parsed) && parsed.length > 0) setMachineryDetailsList(parsed);
      } catch (e) { }
    }

    const rollJson = p.machineryPropertyAssessmentsJson || p.MachineryPropertyAssessmentsJson;
    if (rollJson) {
      try {
        const parsed = typeof rollJson === 'string' ? JSON.parse(rollJson) : rollJson;
        if (Array.isArray(parsed) && parsed.length > 0) setMachineryPropertyAssessments(parsed);
      } catch (e) { }
    }

    // Effectivity & Memoranda
    if (p.taxability != null) setTaxability(p.taxability === 'Taxable' || p.taxability === true);
    if (p.effectivityQuarter) setQuarterEffectivity(p.effectivityQuarter);
    if (p.effectivityYear != null) setYearEffectivity(p.effectivityYear.toString());
    if (p.assessmentBasis) setAssessmentBasis(p.assessmentBasis);
    if (p.backTaxesYearFrom != null) setBackTaxesYearFrom(p.backTaxesYearFrom.toString());
    if (p.backTaxesYearTo != null) setBackTaxesYearTo(p.backTaxesYearTo.toString());
    if (p.memoranda) setMemoranda(p.memoranda);
    if (p.remarks) setRemarks(p.remarks);
  };

  const populateFieldsFromDTO = (d) => {
    if (!d) return;
    if (d.TDN) {
      const t = d.TDN;
      if (t.TDNId) setTdnId(t.TDNId);
      if (t.DeclarationNo) setDeclarationNo(t.DeclarationNo);
      if (t.ARPNo) setArpNo(t.ARPNo);
      if (t.PIN) setPin(t.PIN);
      if (t.SectionNo) setSectionNo(t.SectionNo);
      if (t.LotNo) setLotNo(t.LotNo);
      if (t.DisplayName) setOwnerName(t.DisplayName);
      if (t.OwnerAddress) setOwnerAddress(t.OwnerAddress);
      if (t.OwnerTIN) setOwnerTin(t.OwnerTIN);
      if (t.OwnerContactNo) setOwnerContact(t.OwnerContactNo);
      if (t.OwnerEmail) setOwnerEmail(t.OwnerEmail);
      if (t.OwnerGender) setOwnerGender(t.OwnerGender);
      setIsAdministrator(!!t.IsAdministrator);
      setIsBeneficialUser(!!t.IsBeneficialUser);
      setIsPossessor(!!t.IsPossessor);
      if (t.AdministratorName) setAdminName(t.AdministratorName);
      if (t.AdministratorTin) setAdminTin(t.AdministratorTin);
      if (t.AdministratorAddress) setAdminAddress(t.AdministratorAddress);
      if (t.AdministratorContactNo) setAdminContact(t.AdministratorContactNo);
      if (t.AdministratorEmail) setAdminEmail(t.AdministratorEmail);
      if (t.AdministratorGender) setAdminGender(t.AdministratorGender);

      if (t.PropLocationStreet) setStreetLocation(t.PropLocationStreet);
      if (t.DistrictOrBarangay) setDistrictOrBarangay(normalizeBarangay(t.DistrictOrBarangay));
      if (t.YearEffectivity != null) setYearEffectivity(t.YearEffectivity.toString());
      if (t.QuarterEffectivity != null) {
        const qVal = t.QuarterEffectivity;
        setQuarterEffectivity(`${qVal}${qVal === 1 ? 'st' : qVal === 2 ? 'nd' : qVal === 3 ? 'rd' : 'th'} Quarter`);
      }
      if (t.Taxability != null) setTaxability(!!t.Taxability);
      if (t.AssessmentBasis) setAssessmentBasis(t.AssessmentBasis);
      if (t.BackTaxesYearFrom != null) setBackTaxesYearFrom(t.BackTaxesYearFrom.toString());
      if (t.BackTaxesYearTo != null) setBackTaxesYearTo(t.BackTaxesYearTo.toString());
      if (t.Memoranda) setMemoranda(t.Memoranda);

      const rawAtts = t.Attachments || t.attachments;
      if (Array.isArray(rawAtts) && rawAtts.length > 0) setAttachments(rawAtts);
    }

    if (d.TDNMachine) {
      const m = d.TDNMachine;
      if (m.TDNMachineId) setTdnMachineId(m.TDNMachineId);
      if (m.BuildingOwner) setBuildingReferenceName(m.BuildingOwner);
      if (m.BuildingOwnerPIN) setBuildingPin(m.BuildingOwnerPIN);
      if (m.BuildingTdNo) setBuildingTdNo(m.BuildingTdNo);
      if (m.LandOwner) setLandOwnerName(m.LandOwner);
      if (m.LandOwnerPIN) setLandPin(m.LandOwnerPIN);
      if (m.LandTdNo) setLandTdNo(m.LandTdNo);
      if (m.IsLandOwnerAlsoBuildingOwner != null) setIsLandOwnerAlsoBuildingOwner(!!m.IsLandOwnerAlsoBuildingOwner);
      if (m.IsLocallyManufactured != null) setIsLocallyManufactured(!!m.IsLocallyManufactured);
      if (m.MachineryRegNo) setMachineryRegNo(m.MachineryRegNo);
      if (m.MachineryDateRegistered) setMachineryDateRegistered(m.MachineryDateRegistered.toString().substring(0, 10));
      if (m.ImportationPermitNo) setImportationPermitNo(m.ImportationPermitNo);
      if (m.ImportationDateIssued) setImportationDateIssued(m.ImportationDateIssued.toString().substring(0, 10));
      if (m.CountryOfOrigin) setCountryOfOrigin(m.CountryOfOrigin);
      if (m.SupplierSeller) setSupplierSeller(m.SupplierSeller);
      if (m.SupplierAddress) setSupplierAddress(m.SupplierAddress);
      if (m.AcquisitionCost != null) setAcquisitionCost(m.AcquisitionCost.toString());
      if (m.OrNo) setOrNo(m.OrNo);
      if (m.OrDate) setOrDate(m.OrDate.toString().substring(0, 10));
      if (m.OrNoAndDate) setOrNoAndDate(m.OrNoAndDate);
      if (m.LocalDateIssued) setLocalDateIssued(m.LocalDateIssued.toString().substring(0, 10));

      if (m.MachineryAppraisalDetailsJson) {
        try {
          const parsed = JSON.parse(m.MachineryAppraisalDetailsJson);
          if (Array.isArray(parsed) && parsed.length > 0) setMachineryDetailsList(parsed);
        } catch (e) { }
      }
      if (m.MachineryPropertyAssessmentsJson) {
        try {
          const parsed = JSON.parse(m.MachineryPropertyAssessmentsJson);
          if (Array.isArray(parsed) && parsed.length > 0) setMachineryPropertyAssessments(parsed);
        } catch (e) { }
      }
    }

    if (d.TDNMachineDetail && Array.isArray(d.TDNMachineDetail) && d.TDNMachineDetail.length > 0) {
      if (machineryDetailsList.length === 0) {
        const mapped = d.TDNMachineDetail.map((md, idx) => ({
          id: 'md_' + idx + '_' + Date.now(),
          Classification: md.KindOfMachinery || 'INDUSTRIAL',
          DescriptionBrand: md.BrandModel || '',
          BrandModel: md.BrandModel || '',
          Capacity: md.Capacity || '',
          AcquiredDate: md.AcquiredDate ? md.AcquiredDate.toString().substring(0, 10) : new Date().toISOString().substring(0, 10),
          ConditionWhenAcquired: md.ConditionWhenAcquired || 'NEW',
          EconomicLifeEstimated: md.EconomicLifeEstimated || 20,
          YearInstalled: md.DateOfInstallation ? parseInt(md.DateOfInstallation.toString().substring(0, 4), 10) : new Date().getFullYear(),
          YearOfOperation: md.DateOfOperation ? parseInt(md.DateOfOperation.toString().substring(0, 4), 10) : new Date().getFullYear(),
          NoOfYearsUsed: 0,
          RemainingEconomicLife: md.EconomicLifeEstimated || 20,
          OriginalCost: md.OriginalCost || 0,
          InstallationCost: 0,
          TotalCost: md.OriginalCost || 0,
          DepreciationTotalPercentage: md.DepreciationTotalPercentage || 0,
          DepreciationTotalValue: 0,
          DepreciatedValue: md.DepreciatedValue || md.OriginalCost || 0,
          MarketValue: md.MarketValue || md.OriginalCost || 0
        }));
        setMachineryDetailsList(mapped);
      }
    }
  };

  // =========================================================================
  // TABLE 1: MACHINERY APPRAISAL MODAL & CRUD
  // =========================================================================
  const openAddDetailModal = () => {
    setEditingDetailIndex(null);
    setMClassification('INDUSTRIAL');
    setMBrandModel('');
    setMCapacity('');
    setMCondition('NEW');
    setMAcquiredDate(new Date().toISOString().substring(0, 10));
    setMEstLife('20');
    setMYearInstalled(new Date().getFullYear().toString());
    setMYearOperation(new Date().getFullYear().toString());
    setMYearsUsed('0');
    setMRemLife('20');
    setMOriginalCost('0.00');
    setMInstallationCost('0.00');
    setMTotalCost('0.00');
    setMDepPercentage('0');
    setMDepValue('0.00');
    setMDepreciatedValue('0.00');
    setDetailModalVisible(true);
  };

  const openEditDetailModal = (index) => {
    const item = machineryDetailsList[index];
    if (!item) return;
    setEditingDetailIndex(index);
    setMClassification(item.Classification || 'INDUSTRIAL');
    setMBrandModel(item.BrandModel || item.DescriptionBrand || '');
    setMCapacity(item.Capacity || '');
    setMCondition(item.ConditionWhenAcquired || 'NEW');
    setMAcquiredDate(item.AcquiredDate ? item.AcquiredDate.substring(0, 10) : new Date().toISOString().substring(0, 10));
    setMEstLife((item.EconomicLifeEstimated || 20).toString());
    setMYearInstalled((item.YearInstalled || new Date().getFullYear()).toString());
    setMYearOperation((item.YearOfOperation || item.YearOperation || new Date().getFullYear()).toString());
    setMYearsUsed((item.NoOfYearsUsed || 0).toString());
    setMRemLife((item.RemainingEconomicLife || 20).toString());
    setMOriginalCost((item.OriginalCost || 0).toString());
    setMInstallationCost((item.InstallationCost || 0).toString());
    setMTotalCost((item.TotalCost || item.OriginalCost || 0).toString());
    setMDepPercentage((item.DepreciationTotalPercentage || 0).toString());
    setMDepValue((item.DepreciationTotalValue || 0).toString());
    setMDepreciatedValue((item.DepreciatedValue || item.MarketValue || 0).toString());
    setDetailModalVisible(true);
  };

  const recomputeModalValues = (orig, install, depRate, ecoLife, yrsUsed) => {
    const origNum = parseFloat(orig) || 0;
    const installNum = parseFloat(install) || 0;
    const total = Math.round((origNum + installNum) * 100) / 100;
    const lifeNum = parseFloat(ecoLife) || 20;
    const usedNum = parseFloat(yrsUsed) || 0;
    const rem = Math.max(0, lifeNum - usedNum);

    let depPct = parseFloat(depRate);
    if (isNaN(depPct) || depPct === 0) {
      depPct = lifeNum > 0 ? Math.min(80, Math.round((usedNum / lifeNum) * 100 * 100) / 100) : 0;
    }
    const depVal = Math.round(total * (depPct / 100) * 100) / 100;
    const depMv = Math.max(0, Math.round((total - depVal) * 100) / 100);

    setMTotalCost(total.toFixed(2));
    setMRemLife(rem.toString());
    setMDepPercentage(depPct.toString());
    setMDepValue(depVal.toFixed(2));
    setMDepreciatedValue(depMv.toFixed(2));
  };

  const saveDetailModal = () => {
    if (!mBrandModel.trim()) {
      Alert.alert('Required', 'Please enter a description or brand for the machinery.');
      return;
    }
    const orig = parseFloat(mOriginalCost) || 0;
    const install = parseFloat(mInstallationCost) || 0;
    const total = Math.round((orig + install) * 100) / 100;
    const depPct = parseFloat(mDepPercentage) || 0;
    const depVal = Math.round(total * (depPct / 100) * 100) / 100;
    const depMv = Math.max(0, Math.round((total - depVal) * 100) / 100);
    const ecoLife = parseInt(mEstLife, 10) || 20;
    const yrsUsed = parseInt(mYearsUsed, 10) || 0;
    const remLife = Math.max(0, ecoLife - yrsUsed);

    const newItem = {
      id: editingDetailIndex !== null && machineryDetailsList[editingDetailIndex]?.id ? machineryDetailsList[editingDetailIndex].id : 'mach_' + Date.now(),
      Classification: mClassification,
      DescriptionBrand: mBrandModel,
      BrandModel: mBrandModel,
      Capacity: mCapacity,
      ConditionWhenAcquired: mCondition,
      AcquiredDate: mAcquiredDate,
      EconomicLifeEstimated: ecoLife,
      YearInstalled: parseInt(mYearInstalled, 10) || new Date().getFullYear(),
      YearOfOperation: parseInt(mYearOperation, 10) || new Date().getFullYear(),
      NoOfYearsUsed: yrsUsed,
      RemainingEconomicLife: remLife,
      OriginalCost: orig,
      InstallationCost: install,
      TotalCost: total,
      DepreciationTotalPercentage: depPct,
      DepreciationTotalValue: depVal,
      DepreciatedValue: depMv,
      MarketValue: depMv
    };

    let updated;
    if (editingDetailIndex !== null) {
      updated = [...machineryDetailsList];
      updated[editingDetailIndex] = newItem;
    } else {
      updated = [...machineryDetailsList, newItem];
    }
    setMachineryDetailsList(updated);
    setDetailModalVisible(false);
  };

  const removeDetailItem = (index) => {
    Alert.alert('Confirm Delete', 'Remove this machinery item from the appraisal schedule?', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Delete',
        style: 'destructive',
        onPress: () => {
          setMachineryDetailsList(prev => prev.filter((_, idx) => idx !== index));
        }
      }
    ]);
  };

  // =========================================================================
  // TABLE 2: PROPERTY ASSESSMENT ROLL ACTIONS
  // =========================================================================
  const syncMachineryAssessmentRollFromAppraisal = () => {
    const totalDepMv = totalsTable1.totalDepreciatedValue;
    const primaryClass = machineryDetailsList[0]?.Classification || 'INDUSTRIAL';
    const defaultLevel = primaryClass === 'RESIDENTIAL' ? 50 : 80;
    const av = Math.round(totalDepMv * (defaultLevel / 100.0) * 100) / 100;

    setMachineryPropertyAssessments([
      {
        id: 'roll_' + Date.now(),
        ActualUseName: primaryClass,
        MarketValue: totalDepMv,
        AssessmentLevelPercent: defaultLevel,
        AssessmentValue: av
      }
    ]);
  };

  const addMachineryAssessmentRollRow = () => {
    const primaryClass = 'INDUSTRIAL';
    const defaultLevel = 80;
    setMachineryPropertyAssessments(prev => [
      ...prev,
      {
        id: 'roll_' + Date.now(),
        ActualUseName: primaryClass,
        MarketValue: 0,
        AssessmentLevelPercent: defaultLevel,
        AssessmentValue: 0
      }
    ]);
  };

  const updateRollRow = (index, field, val) => {
    setMachineryPropertyAssessments(prev => {
      const copy = [...prev];
      const row = { ...copy[index] };
      if (field === 'ActualUseName') {
        row.ActualUseName = val;
        if (val === 'RESIDENTIAL') row.AssessmentLevelPercent = 50;
        else if (val === 'AGRICULTURAL') row.AssessmentLevelPercent = 40;
        else row.AssessmentLevelPercent = 80;
      } else if (field === 'MarketValue') {
        row.MarketValue = parseFloat(val) || 0;
      } else if (field === 'AssessmentLevelPercent') {
        row.AssessmentLevelPercent = parseFloat(val) || 0;
      }
      row.AssessmentValue = Math.round((row.MarketValue * (row.AssessmentLevelPercent / 100)) * 100) / 100;
      copy[index] = row;
      return copy;
    });
  };

  const removeRollRow = (index) => {
    setMachineryPropertyAssessments(prev => prev.filter((_, idx) => idx !== index));
  };

  // =========================================================================
  // SAVE & SYNC
  // =========================================================================
  const navigateBack = () => {
    setDiscardModalVisible(false);
    if (fromHistory) {
      if (navigation && typeof navigation.canGoBack === 'function' && navigation.canGoBack()) {
        navigation.goBack();
      } else if (navigation && typeof navigation.navigate === 'function') {
        navigation.navigate('Main', { screen: 'HistoryTab' });
      }
    } else {
      if (navigation && typeof navigation.canGoBack === 'function' && navigation.canGoBack()) {
        navigation.goBack();
      } else if (navigation && typeof navigation.navigate === 'function') {
        navigation.navigate('Main', { screen: 'Feed' });
      }
    }
  };

  const handleBack = () => {
    if (fromHistory) {
      navigateBack();
    } else {
      setDiscardModalVisible(true);
    }
  };

  const handleSave = async () => {
    if (!ownerName.trim()) {
      Alert.alert('Required Field', 'Owner / Declarant Name is required.');
      setActiveTab(0);
      return;
    }

    const quarterNum = quarterEffectivity.includes('1') ? 1 : quarterEffectivity.includes('2') ? 2 : quarterEffectivity.includes('3') ? 3 : 4;

    const tdnDetails = machineryDetailsList.map((item, idx) => ({
      TDNMachineDetailId: idx + 1,
      TDNMachineId: parseInt(tdnMachineId, 10) || 0,
      KindOfMachinery: item.Classification || 'INDUSTRIAL',
      BrandModel: item.BrandModel || item.DescriptionBrand || '',
      Capacity: item.Capacity || '',
      AcquiredDate: item.AcquiredDate ? new Date(item.AcquiredDate) : null,
      DateOfInstallation: item.YearInstalled ? new Date(item.YearInstalled, 0, 1) : null,
      DateOfOperation: item.YearOfOperation ? new Date(item.YearOfOperation, 0, 1) : null,
      ConditionWhenAcquired: item.ConditionWhenAcquired || 'NEW',
      EconomicLifeEstimated: item.EconomicLifeEstimated || 20,
      OriginalCost: item.OriginalCost || 0,
      MarketValue: item.DepreciatedValue || item.MarketValue || 0,
      AssessmentLevel: 80,
      AssessedValue: Math.round((item.DepreciatedValue || item.MarketValue || 0) * 0.8 * 100) / 100,
      DepreciatedValue: item.DepreciatedValue || 0,
      DepreciationTotalPercentage: item.DepreciationTotalPercentage || 0
    }));

    const payload = {
      TDN: {
        TDNId: parseInt(tdnId, 10) || 0,
        PartitionItemIndex: parseInt(partitionIndexParam, 10) || 0,
        PartitionIndex: partitionLetterParam,
        TrackingNo: trackingNo || trackingNoParam,
        FormWorkflowId: formWorkflowIdParam,
        DeclarationNo: declarationNo,
        ARPNo: arpNo,
        PIN: pin,
        SectionNo: sectionNo,
        LotNo: lotNo,
        DisplayName: ownerName,
        OwnerGender: ownerGender,
        OwnerTIN: ownerTin,
        OwnerAddress: ownerAddress,
        OwnerContactNo: ownerContact,
        OwnerEmail: ownerEmail,
        IsAdministrator: isAdministrator,
        IsBeneficialUser: isBeneficialUser,
        IsPossessor: isPossessor,
        AdministratorName: adminName,
        AdministratorGender: adminGender,
        AdministratorTin: adminTin,
        AdministratorAddress: adminAddress,
        AdministratorContactNo: adminContact,
        AdministratorEmail: adminEmail,
        PropLocationStreet: streetLocation,
        DistrictOrBarangay: districtOrBarangay,
        CityMunicipality: cityMunicipality,
        Province: province,
        TotalMarketValue: grandMarketValue,
        TotalAssessedValue: grandAssessedValue,
        Taxability: taxability,
        EffectivityQuarter: quarterEffectivity,
        QuarterEffectivity: quarterNum,
        YearEffectivity: parseInt(yearEffectivity, 10) || new Date().getFullYear(),
        AssessmentBasis: assessmentBasis,
        BackTaxesYearFrom: backTaxesYearFrom ? parseInt(backTaxesYearFrom, 10) : null,
        BackTaxesYearTo: backTaxesYearTo ? parseInt(backTaxesYearTo, 10) : null,
        Memoranda: memoranda,
        Remarks: remarks,
        Attachments: attachments,
        MachineryAppraisalDetailsJson: JSON.stringify(machineryDetailsList),
        MachineryPropertyAssessmentsJson: JSON.stringify(machineryPropertyAssessments)
      },
      TDNMachine: {
        TDNMachineId: parseInt(tdnMachineId, 10) || 0,
        TDNId: parseInt(tdnId, 10) || 0,
        BuildingOwner: buildingReferenceName,
        BuildingOwnerPIN: buildingPin,
        BuildingTdNo: buildingTdNo,
        LandOwner: landOwnerName,
        LandOwnerPIN: landPin,
        LandTdNo: landTdNo,
        IsLandOwnerAlsoBuildingOwner: isLandOwnerAlsoBuildingOwner,
        IsLocallyManufactured: isLocallyManufactured,
        MachineryRegNo: machineryRegNo,
        MachineryDateRegistered: machineryDateRegistered ? new Date(machineryDateRegistered) : null,
        ImportationPermitNo: importationPermitNo,
        ImportationDateIssued: importationDateIssued ? new Date(importationDateIssued) : null,
        CountryOfOrigin: countryOfOrigin,
        SupplierSeller: supplierSeller,
        SupplierAddress: supplierAddress,
        AcquisitionCost: parseFloat(acquisitionCost) || 0,
        OrNo: orNo,
        OrDate: orDate ? new Date(orDate) : null,
        OrNoAndDate: orNoAndDate,
        LocalDateIssued: localDateIssued ? new Date(localDateIssued) : null,
        MachineryAppraisalDetailsJson: JSON.stringify(machineryDetailsList),
        MachineryPropertyAssessmentsJson: JSON.stringify(machineryPropertyAssessments)
      },
      TDNMachineDetail: tdnDetails
    };

    setSaving(true);
    await queueOfflineUpdate(
      'UPDATE_MACHINERY',
      payload,
      `Machinery FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Company'})`,
      null
    );
    setSaving(false);
    showCompleteModalAndGoBack(
      'Partition Saved Offline',
      `Edits for Partition ${partitionLetterParam} have been saved locally. To synchronize to the office database or transfer to supervisor, use the 3-dots (⋮) menu on the transaction card in My Transactions.`,
      'success',
      'cloud-offline-outline'
    );
  };

  const renderSelectField = (label, value, options, onSelect) => (
    <View style={styles.inputGroup}>
      <Text style={styles.inputLabel}>{label}</Text>
      <TouchableOpacity
        style={styles.selectBtn}
        onPress={() => openPicker(label, options, value, onSelect)}
      >
        <Text style={[styles.selectBtnText, !value && { color: colors.placeholder }]} numberOfLines={1}>
          {value || 'Select...'}
        </Text>
        <Ionicons name="chevron-down" size={16} color={colors.textMuted} />
      </TouchableOpacity>
    </View>
  );

  return (
    <View style={styles.container}>
      <HeaderBar
        title={fromHistory ? "VIEW MACHINERY RECORD" : "MACHINERY FAAS"}
        subtitle={`Tracking No: ${trackingNo || 'N/A'} • Partition ${partitionLetterParam}${declarationNo ? ` • TDN: ${declarationNo}` : ''}`}
        navigation={navigation}
        showBack={true}
        onBackPress={handleBack}
      />

      {/* Historical Record Ribbon */}
      {fromHistory && (
        <View style={styles.historyNoticeRibbon}>
          <Ionicons name="time-outline" size={16} color="#0369a1" />
          <Text style={styles.historyNoticeText}>
            Historical Record View • Completed transaction cannot be edited.
          </Text>
        </View>
      )}

      {/* Offline Status Ribbon */}
      <View style={[styles.ribbon, isDownloaded ? styles.ribbonDownloaded : styles.ribbonStandard]}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, flex: 1 }}>
          <Ionicons
            name={isDownloaded ? 'shield-checkmark' : 'cloud-outline'}
            size={16}
            color={isDownloaded ? (isDarkMode ? '#34d399' : '#15803d') : colors.primary}
          />
          <Text style={[styles.ribbonText, isDownloaded && { color: isDarkMode ? '#34d399' : '#15803d' }]}>
            {isDownloaded ? 'Downloaded Form • 100% Offline Field Ready' : 'Live Online Form'}
          </Text>
        </View>

        <TouchableOpacity
          style={[styles.ribbonNetTag, isOnline ? styles.ribbonNetOnline : styles.ribbonNetOffline]}
          onPress={() => checkConnectivity && checkConnectivity()}
        >
          <View style={[styles.ribbonNetDot, isOnline ? styles.ribbonNetDotOnline : styles.ribbonNetDotOffline]} />
          <Text style={[styles.ribbonNetText, isOnline ? styles.ribbonNetTextOnline : styles.ribbonNetTextOffline]}>
            {isOnline ? 'ONLINE' : 'OFFLINE'}
          </Text>
        </TouchableOpacity>
      </View>

      {/* Sticky Top Tabs Bar (Exactly 3 Tabs) */}
      <View style={styles.topTabsWrapper}>
        <View style={styles.bannerInfoBox}>
          <View style={styles.bannerInfoItem}>
            <Text style={styles.bannerInfoLabel}>TDN:</Text>
            <Text style={styles.bannerInfoValue}>{declarationNo || 'TDN System Pending'}</Text>
          </View>
          <View style={styles.bannerInfoItem}>
            <Text style={styles.bannerInfoLabel}>• ARP NO:</Text>
            <Text style={styles.bannerInfoValue}>{arpNo || '187-00-0000-000-000'}</Text>
          </View>
          <View style={styles.bannerInfoItem}>
            <Text style={styles.bannerInfoLabel}>• PIN:</Text>
            <Text style={styles.bannerInfoValue}>{pin || '187-00-0000-000'}</Text>
          </View>
        </View>

        <ScrollView
          ref={tabScrollRef}
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.sectionTabsScroll}
          style={styles.sectionTabsContainer}
        >
          {TABS.map((tab, idx) => {
            const isActive = activeTab === idx;
            return (
              <TouchableOpacity
                key={tab.label}
                style={[styles.sectionTabBtn, isActive && styles.sectionTabBtnActive]}
                onPress={() => setActiveTab(idx)}
                activeOpacity={0.7}
              >
                <Ionicons
                  name={tab.icon}
                  size={15}
                  color={isActive ? '#ffffff' : (isDarkMode ? colors.textDark : '#475569')}
                />
                <Text style={[styles.sectionTabText, isActive && styles.sectionTabTextActive]}>
                  {tab.label} {idx === 2 && attachments && attachments.length > 0 ? `(${attachments.length})` : ''}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {loading ? (
          <ActivityIndicator color={colors.primary} size="large" style={{ marginVertical: 30 }} />
        ) : notDownloadedError ? (
          <View style={styles.notDownloadedBox}>
            <Ionicons name="cloud-offline-outline" size={56} color="#d97706" />
            <Text style={styles.notDownloadedTitle}>Form Not Stored on Phone</Text>
            <Text style={styles.notDownloadedText}>
              This machinery transaction data has not been downloaded to your device storage. To view and inspect this form in offline mode, please connect to Wi-Fi/Mobile Data and tap "Download" in the transactions list.
            </Text>
            <TouchableOpacity style={styles.backBtn} onPress={navigateBack}>
              <Ionicons name="arrow-back" size={18} color="#ffffff" />
              <Text style={styles.backBtnText}>Return to Transactions</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            {/* ========================================================================= */}
            {/* TAB 0: 1. Property Information & Owner (CAST)                             */}
            {/* ========================================================================= */}
            {activeTab === 0 && (
              <View style={styles.tabContentBlock}>
                {/* Card 1: Machinery Identification & Parcel Reference */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Machinery Identification & Parcel Reference (CAST Reference)</Text>

                  {/* Readonly Numbers: ARP No & TDN */}
                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ARP NO.</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={arpNo}
                        editable={false}
                        placeholder="187-00-0000-000-000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TAX DECLARATION NO. (TDN)</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={declarationNo}
                        editable={false}
                        placeholder="Generated by Web Portal"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>PROPERTY INDEX NUMBER (PIN)</Text>
                    <TextInput
                      style={[styles.input, styles.fontMonoBold]}
                      value={pin}
                      onChangeText={setPin}
                      placeholder="187-00-0000-000-000"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>SECTION NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={sectionNo}
                        onChangeText={setSectionNo}
                        placeholder="000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>LOT / PARCEL NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={lotNo}
                        onChangeText={setLotNo}
                        placeholder="Lot 10-A / 001"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>
                </View>

                {/* Card 2: Owner & Declarant Demographics (CAST) */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.cardHeaderRow}>
                    <Text style={styles.cardHeaderTitle}>Owner & Declarant Demographics (CAST)</Text>
                    <View style={styles.rolePillGroup}>
                      <TouchableOpacity
                        style={[styles.rolePill, isAdministrator && styles.rolePillActive]}
                        onPress={() => setIsAdministrator(!isAdministrator)}
                      >
                        <Text style={[styles.rolePillText, isAdministrator && styles.rolePillTextActive]}>ADMINISTRATOR</Text>
                      </TouchableOpacity>
                      <TouchableOpacity
                        style={[styles.rolePill, isBeneficialUser && styles.rolePillActive]}
                        onPress={() => setIsBeneficialUser(!isBeneficialUser)}
                      >
                        <Text style={[styles.rolePillText, isBeneficialUser && styles.rolePillTextActive]}>BENEFICIAL USER</Text>
                      </TouchableOpacity>
                      <TouchableOpacity
                        style={[styles.rolePill, isPossessor && styles.rolePillActive]}
                        onPress={() => setIsPossessor(!isPossessor)}
                      >
                        <Text style={[styles.rolePillText, isPossessor && styles.rolePillTextActive]}>POSSESSOR</Text>
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>OWNER / DECLARANT NAME *</Text>
                    <TextInput
                      style={[styles.input, styles.fontBold]}
                      value={ownerName}
                      onChangeText={(val) => {
                        setOwnerName(val);
                        if (isLandOwnerAlsoBuildingOwner) {
                          if (!buildingReferenceName || buildingReferenceName === ownerName) {
                            setBuildingReferenceName(val);
                          }
                          if (!landOwnerName || landOwnerName === ownerName) {
                            setLandOwnerName(val);
                          }
                        }
                      }}
                      placeholder="Full name of owner / declarant"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={{ flex: 1 }}>
                      {renderSelectField('SEX', ownerGender, GENDER_OPTIONS, setOwnerGender)}
                    </View>
                    <View style={[styles.inputGroup, { flex: 1.2 }]}>
                      <Text style={styles.inputLabel}>TIN</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerTin}
                        onChangeText={setOwnerTin}
                        placeholder="000-000-000-000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>MAILING ADDRESS *</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerAddress}
                      onChangeText={setOwnerAddress}
                      placeholder="Street, Barangay, City, Province"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PHONE NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerContact}
                        onChangeText={setOwnerContact}
                        placeholder="09XX-XXX-XXXX"
                        keyboardType="phone-pad"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>EMAIL</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerEmail}
                        onChangeText={setOwnerEmail}
                        placeholder="owner@example.com"
                        keyboardType="email-address"
                        autoCapitalize="none"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  {/* Administrator / Beneficial User / Possessor Section (Toggled) */}
                  {(isAdministrator || isBeneficialUser || isPossessor) && (
                    <View style={styles.subCard}>
                      <Text style={styles.subCardTitle}>
                        <Ionicons name="shield-alert-outline" size={14} color="#d97706" />
                        {' '}Administrator / Beneficial User / Possessor Details
                      </Text>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>NAMES (ADMINISTRATOR / BENEFICIAL USER / POSSESSOR)</Text>
                        <TextInput
                          style={styles.input}
                          value={adminName}
                          onChangeText={setAdminName}
                          placeholder="Full name of administrator / beneficial user / possessor"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={{ flex: 1 }}>
                          {renderSelectField('SEX', adminGender, GENDER_OPTIONS, setAdminGender)}
                        </View>
                        <View style={[styles.inputGroup, { flex: 1.2 }]}>
                          <Text style={styles.inputLabel}>TIN</Text>
                          <TextInput
                            style={styles.input}
                            value={adminTin}
                            onChangeText={setAdminTin}
                            placeholder="000-000-000-000"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>MAILING ADDRESS</Text>
                        <TextInput
                          style={styles.input}
                          value={adminAddress}
                          onChangeText={setAdminAddress}
                          placeholder="Complete mailing address"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>PHONE NO.</Text>
                          <TextInput
                            style={styles.input}
                            value={adminContact}
                            onChangeText={setAdminContact}
                            placeholder="09XX-XXX-XXXX"
                            keyboardType="phone-pad"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>EMAIL</Text>
                          <TextInput
                            style={styles.input}
                            value={adminEmail}
                            onChangeText={setAdminEmail}
                            placeholder="admin@example.com"
                            keyboardType="email-address"
                            autoCapitalize="none"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                      </View>
                    </View>
                  )}
                </View>

                {/* Card 3: Property Registration Details (TDNMachineries) */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Registration Details (TDNMachineries)</Text>

                  {/* Origin Toggle: Imported vs Local */}
                  <View style={styles.originToggleRow}>
                    <TouchableOpacity
                      style={[styles.originToggleBtn, !isLocallyManufactured && styles.originToggleBtnActive]}
                      onPress={() => setIsLocallyManufactured(false)}
                    >
                      <Ionicons
                        name="globe-outline"
                        size={14}
                        color={!isLocallyManufactured ? '#ffffff' : colors.textDark}
                      />
                      <Text style={[styles.originToggleText, !isLocallyManufactured && styles.originToggleTextActive]}>
                        OUTSIDE THE PHILIPPINES (IMPORTED)
                      </Text>
                    </TouchableOpacity>

                    <TouchableOpacity
                      style={[styles.originToggleBtn, isLocallyManufactured && styles.originToggleBtnActive]}
                      onPress={() => setIsLocallyManufactured(true)}
                    >
                      <Ionicons
                        name="business-outline"
                        size={14}
                        color={isLocallyManufactured ? '#ffffff' : colors.textDark}
                      />
                      <Text style={[styles.originToggleText, isLocallyManufactured && styles.originToggleTextActive]}>
                        LOCAL MANUFACTURER
                      </Text>
                    </TouchableOpacity>
                  </View>

                  {/* Panel A: Outside the Philippines (Imported) */}
                  {!isLocallyManufactured ? (
                    <View style={styles.registrationPanel}>
                      <View style={styles.panelBadge}>
                        <Ionicons name="globe" size={13} color="#0284c7" />
                        <Text style={styles.panelBadgeText}>Outside the Philippines — Registration & Importation Details</Text>
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>REGISTRATION NO.</Text>
                          <TextInput
                            style={styles.input}
                            value={machineryRegNo}
                            onChangeText={setMachineryRegNo}
                            placeholder="Enter Registration No."
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>DATE REGISTERED</Text>
                          <TouchableOpacity
                            style={styles.datePickerBtn}
                            onPress={() => openDatePicker('Select Date Registered', 'machineryDateRegistered', machineryDateRegistered)}
                          >
                            <Text style={[styles.datePickerBtnText, !machineryDateRegistered && { color: colors.placeholder }]}>
                              {machineryDateRegistered || 'YYYY-MM-DD'}
                            </Text>
                            <Ionicons name="calendar-outline" size={16} color={colors.textMuted} />
                          </TouchableOpacity>
                        </View>
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>IMPORTATION PERMIT</Text>
                          <TextInput
                            style={styles.input}
                            value={importationPermitNo}
                            onChangeText={setImportationPermitNo}
                            placeholder="Enter Importation Permit No."
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>DATE ISSUED</Text>
                          <TouchableOpacity
                            style={styles.datePickerBtn}
                            onPress={() => openDatePicker('Select Date Issued', 'importationDateIssued', importationDateIssued)}
                          >
                            <Text style={[styles.datePickerBtnText, !importationDateIssued && { color: colors.placeholder }]}>
                              {importationDateIssued || 'YYYY-MM-DD'}
                            </Text>
                            <Ionicons name="calendar-outline" size={16} color={colors.textMuted} />
                          </TouchableOpacity>
                        </View>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>COUNTRY OF ORIGIN</Text>
                        <TextInput
                          style={styles.input}
                          value={countryOfOrigin}
                          onChangeText={setCountryOfOrigin}
                          placeholder="e.g. Japan, Germany, USA"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>SUPPLIER / SELLER</Text>
                        <TextInput
                          style={styles.input}
                          value={supplierSeller}
                          onChangeText={setSupplierSeller}
                          placeholder="Name of Foreign Supplier / Seller"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>ACQUISITION COST (₱)</Text>
                        <TextInput
                          style={[styles.input, styles.valuationInput]}
                          value={acquisitionCost}
                          onChangeText={setAcquisitionCost}
                          keyboardType="numeric"
                          placeholder="0.00"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>
                    </View>
                  ) : (
                    /* Panel B: Local Manufacturer */
                    <View style={styles.registrationPanel}>
                      <View style={[styles.panelBadge, { backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5', borderColor: '#10b981' }]}>
                        <Ionicons name="business" size={13} color="#059669" />
                        <Text style={[styles.panelBadgeText, { color: isDarkMode ? '#34d399' : '#047857' }]}>
                          Local Manufacturer — Official Receipt & Supplier Details
                        </Text>
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>OR NO.</Text>
                          <TextInput
                            style={styles.input}
                            value={orNo}
                            onChangeText={(val) => {
                              setOrNo(val);
                              if (val && orDate) setOrNoAndDate(`${val} / ${orDate}`);
                            }}
                            placeholder="Official Receipt No."
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>OR DATE</Text>
                          <TouchableOpacity
                            style={styles.datePickerBtn}
                            onPress={() => openDatePicker('Select OR Date', 'orDate', orDate)}
                          >
                            <Text style={[styles.datePickerBtnText, !orDate && { color: colors.placeholder }]}>
                              {orDate || 'YYYY-MM-DD'}
                            </Text>
                            <Ionicons name="calendar-outline" size={16} color={colors.textMuted} />
                          </TouchableOpacity>
                        </View>
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1.2 }]}>
                          <Text style={styles.inputLabel}>OR NO. AND DATE</Text>
                          <TextInput
                            style={styles.input}
                            value={orNoAndDate}
                            onChangeText={setOrNoAndDate}
                            placeholder="OR # / YYYY-MM-DD"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>DATE ISSUED</Text>
                          <TouchableOpacity
                            style={styles.datePickerBtn}
                            onPress={() => openDatePicker('Select Date Issued', 'localDateIssued', localDateIssued)}
                          >
                            <Text style={[styles.datePickerBtnText, !localDateIssued && { color: colors.placeholder }]}>
                              {localDateIssued || 'YYYY-MM-DD'}
                            </Text>
                            <Ionicons name="calendar-outline" size={16} color={colors.textMuted} />
                          </TouchableOpacity>
                        </View>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>ACQUISITION COST (₱)</Text>
                        <TextInput
                          style={[styles.input, styles.valuationInput]}
                          value={acquisitionCost}
                          onChangeText={setAcquisitionCost}
                          keyboardType="numeric"
                          placeholder="0.00"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>SUPPLIER / SELLER</Text>
                        <TextInput
                          style={styles.input}
                          value={supplierSeller}
                          onChangeText={setSupplierSeller}
                          placeholder="Name of Local Manufacturer / Supplier"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>ADDRESS OF SUPPLIER</Text>
                        <TextInput
                          style={styles.input}
                          value={supplierAddress}
                          onChangeText={setSupplierAddress}
                          placeholder="Complete Address of Supplier / Manufacturer"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>
                    </View>
                  )}
                </View>

                {/* Card 4: Property Location & Reference */}
                <View style={[styles.card, shadows.card]}>
                  <View style={{ gap: 8, borderBottomWidth: 1, borderBottomColor: colors.border, paddingBottom: 10 }}>
                    <Text style={styles.cardHeaderTitle}>Property Location & Reference</Text>
                    
                    {/* Toggle: Land Owner is also the Building Owner */}
                    <TouchableOpacity
                      style={[styles.checkboxPill, isLandOwnerAlsoBuildingOwner && styles.checkboxPillActive]}
                      onPress={handleToggleLandOwnerAlsoBuildingOwner}
                      activeOpacity={0.7}
                    >
                      <Ionicons
                        name={isLandOwnerAlsoBuildingOwner ? 'checkbox' : 'square-outline'}
                        size={18}
                        color={isLandOwnerAlsoBuildingOwner ? (isDarkMode ? '#34d399' : '#059669') : colors.textMuted}
                      />
                      <Text style={[styles.checkboxPillText, isLandOwnerAlsoBuildingOwner && styles.checkboxPillTextActive]}>
                        LAND OWNER IS ALSO THE BUILDING OWNER
                      </Text>
                      {isLandOwnerAlsoBuildingOwner && (
                        <View style={styles.syncedBadge}>
                          <Ionicons name="link" size={12} color={isDarkMode ? '#a7f3d0' : '#047857'} />
                          <Text style={styles.syncedBadgeText}>Synced</Text>
                        </View>
                      )}
                    </TouchableOpacity>
                  </View>

                  {/* Building Owner, Building PIN, Building TD No */}
                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>
                      BUILDING OWNER {isLandOwnerAlsoBuildingOwner && '(Synced with Land Owner)'}
                    </Text>
                    <TextInput
                      style={styles.input}
                      value={buildingReferenceName}
                      onChangeText={handleBuildingOwnerChange}
                      placeholder="Enter Building Owner Name"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1.2 }]}>
                      <Text style={styles.inputLabel}>BUILDING P.I.N</Text>
                      <TextInput
                        style={styles.input}
                        value={buildingPin}
                        onChangeText={setBuildingPin}
                        placeholder="187-00-0000-000-000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BUILDING TD NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={buildingTdNo}
                        onChangeText={setBuildingTdNo}
                        placeholder="TD-XXXX-XXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  {/* Land Owner, Land PIN, Land TD No */}
                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>
                      LAND OWNER {isLandOwnerAlsoBuildingOwner && '(Synced with Building Owner)'}
                    </Text>
                    <TextInput
                      style={styles.input}
                      value={landOwnerName}
                      onChangeText={handleLandOwnerChange}
                      placeholder="Enter Land Owner Name"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1.2 }]}>
                      <Text style={styles.inputLabel}>LAND OWNER P.I.N</Text>
                      <TextInput
                        style={styles.input}
                        value={landPin}
                        onChangeText={setLandPin}
                        placeholder="187-00-0000-000-000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>LAND TD NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={landTdNo}
                        onChangeText={setLandTdNo}
                        placeholder="TD-XXXX-XXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>BUILDING NO. / STREET</Text>
                    <TextInput
                      style={styles.input}
                      value={streetLocation}
                      onChangeText={setStreetLocation}
                      placeholder="e.g. National Highway, Makar"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  {renderSelectField('BARANGAY *', districtOrBarangay, BARANGAY_OPTIONS, setDistrictOrBarangay)}

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>CITY / MUNICIPALITY</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={cityMunicipality}
                        editable={false}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PROVINCE</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={province}
                        editable={false}
                      />
                    </View>
                  </View>
                </View>
              </View>
            )}

            {/* ========================================================================= */}
            {/* TAB 1: 2. Property Appraisal & Assessment Roll (CAST)                     */}
            {/* ========================================================================= */}
            {activeTab === 1 && (
              <View style={styles.tabContentBlock}>
                {/* Table 1: Property Appraisal — Machinery & Equipment Schedule */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.cardHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 1: Property Appraisal — Machinery Schedule</Text>
                      <Text style={styles.cardHeaderSubtitle}>CAST 16-Column Specification Schedule</Text>
                    </View>
                    <TouchableOpacity
                      style={styles.addBtn}
                      onPress={openAddDetailModal}
                    >
                      <Ionicons name="add-circle" size={16} color="#ffffff" />
                      <Text style={styles.addBtnText}>Add Item</Text>
                    </TouchableOpacity>
                  </View>

                  {machineryDetailsList.length === 0 ? (
                    <View style={styles.emptyTableBox}>
                      <Ionicons name="hardware-chip-outline" size={32} color={colors.textMuted} />
                      <Text style={styles.emptyTableText}>No machinery details added yet.</Text>
                      <TouchableOpacity style={styles.emptyAddBtn} onPress={openAddDetailModal}>
                        <Text style={styles.emptyAddBtnText}>+ Add First Machinery Detail</Text>
                      </TouchableOpacity>
                    </View>
                  ) : (
                    <View style={{ gap: 10 }}>
                      {machineryDetailsList.map((item, idx) => (
                        <View key={item.id || idx} style={styles.itemCard}>
                          <View style={styles.itemCardHeader}>
                            <View style={{ flex: 1 }}>
                              <Text style={styles.itemCardTitle}>
                                #{idx + 1}. {item.Classification || 'MACHINERY'} — {item.BrandModel || item.DescriptionBrand || 'Unnamed Item'}
                              </Text>
                              <Text style={styles.itemCardSubtitle}>
                                Capacity: {item.Capacity || 'N/A'} • Condition: {item.ConditionWhenAcquired || 'NEW'}
                              </Text>
                            </View>
                            <View style={styles.itemActionRow}>
                              <TouchableOpacity
                                style={styles.actionIconBtn}
                                onPress={() => openEditDetailModal(idx)}
                              >
                                <Ionicons name="pencil" size={16} color={colors.primary} />
                              </TouchableOpacity>
                              <TouchableOpacity
                                style={styles.actionIconBtn}
                                onPress={() => removeDetailItem(idx)}
                              >
                                <Ionicons name="trash-outline" size={16} color="#ef4444" />
                              </TouchableOpacity>
                            </View>
                          </View>

                          <View style={styles.itemSpecsGrid}>
                            <View style={styles.specItem}>
                              <Text style={styles.specLabel}>Acquired / Inst.</Text>
                              <Text style={styles.specValue}>{item.AcquiredDate ? item.AcquiredDate.substring(0, 10) : 'N/A'} ({item.YearInstalled || 'N/A'})</Text>
                            </View>
                            <View style={styles.specItem}>
                              <Text style={styles.specLabel}>Eco Life / Rem.</Text>
                              <Text style={styles.specValue}>{item.EconomicLifeEstimated || 20} Yrs / {item.RemainingEconomicLife || 20} Yrs</Text>
                            </View>
                            <View style={styles.specItem}>
                              <Text style={styles.specLabel}>Total Cost</Text>
                              <Text style={styles.specValue}>₱{(parseFloat(item.TotalCost) || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                            </View>
                            <View style={styles.specItem}>
                              <Text style={styles.specLabel}>Depr. Rate</Text>
                              <Text style={[styles.specValue, { color: '#ef4444' }]}>{item.DepreciationTotalPercentage || 0}%</Text>
                            </View>
                          </View>

                          <View style={styles.itemFooterRow}>
                            <Text style={styles.itemFooterLabel}>Market Value (Less Depr.):</Text>
                            <Text style={styles.itemFooterValue}>
                              ₱{(parseFloat(item.DepreciatedValue) || parseFloat(item.MarketValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}
                            </Text>
                          </View>
                        </View>
                      ))}

                      {/* Totals Summary Footer */}
                      <View style={styles.totalsSummaryBox}>
                        <View style={styles.totalsSummaryRow}>
                          <Text style={styles.totalsSummaryLabel}>TOTAL ORIGINAL COST:</Text>
                          <Text style={styles.totalsSummaryVal}>₱{totalsTable1.totalOrigCost.toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                        </View>
                        <View style={styles.totalsSummaryRow}>
                          <Text style={styles.totalsSummaryLabel}>TOTAL INSTALLATION & OTHER:</Text>
                          <Text style={styles.totalsSummaryVal}>₱{totalsTable1.totalInstallCost.toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                        </View>
                        <View style={styles.totalsSummaryRow}>
                          <Text style={styles.totalsSummaryLabel}>TOTAL ACCUMULATED DEPRECIATION:</Text>
                          <Text style={[styles.totalsSummaryVal, { color: '#ef4444' }]}>-₱{totalsTable1.totalDepValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                        </View>
                        <View style={[styles.totalsSummaryRow, styles.totalsSummaryGrand]}>
                          <Text style={styles.totalsSummaryGrandLabel}>DEPRECIATED MARKET VALUE:</Text>
                          <Text style={styles.totalsSummaryGrandVal}>₱{totalsTable1.totalDepreciatedValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                        </View>
                      </View>
                    </View>
                  )}
                </View>

                {/* Table 2: Property Assessment (Machinery Actual Use & Assessment Level) */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.cardHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 2: Property Assessment Roll</Text>
                      <Text style={styles.cardHeaderSubtitle}>Actual Use & Assessment Level (%)</Text>
                    </View>
                    <View style={{ flexDirection: 'row', gap: 6 }}>
                      <TouchableOpacity
                        style={styles.syncBtn}
                        onPress={syncMachineryAssessmentRollFromAppraisal}
                      >
                        <Ionicons name="refresh" size={14} color={colors.primary} />
                        <Text style={styles.syncBtnText}>Sync MV</Text>
                      </TouchableOpacity>
                      <TouchableOpacity
                        style={styles.addBtn}
                        onPress={addMachineryAssessmentRollRow}
                      >
                        <Ionicons name="add" size={16} color="#ffffff" />
                        <Text style={styles.addBtnText}>Add Row</Text>
                      </TouchableOpacity>
                    </View>
                  </View>

                  {machineryPropertyAssessments.length === 0 ? (
                    <View style={styles.emptyTableBox}>
                      <Text style={styles.emptyTableText}>Assessment roll is empty. Tap "Sync MV" to populate.</Text>
                      <TouchableOpacity style={styles.emptyAddBtn} onPress={syncMachineryAssessmentRollFromAppraisal}>
                        <Text style={styles.emptyAddBtnText}>Sync from Table 1 Market Value</Text>
                      </TouchableOpacity>
                    </View>
                  ) : (
                    <View style={{ gap: 10 }}>
                      {machineryPropertyAssessments.map((row, idx) => (
                        <View key={row.id || idx} style={styles.rollRowCard}>
                          <View style={styles.rollRowTop}>
                            <TouchableOpacity
                              style={[styles.selectBtn, { flex: 1, paddingVertical: 8 }]}
                              onPress={() => openPicker('Actual Use', MACHINERY_CLASSIFICATIONS, row.ActualUseName, (val) => updateRollRow(idx, 'ActualUseName', val))}
                            >
                              <Text style={styles.selectBtnText}>{row.ActualUseName || 'INDUSTRIAL'}</Text>
                              <Ionicons name="chevron-down" size={14} color={colors.textMuted} />
                            </TouchableOpacity>

                            <TouchableOpacity
                              style={styles.deleteRollBtn}
                              onPress={() => removeRollRow(idx)}
                            >
                              <Ionicons name="trash-outline" size={16} color="#ef4444" />
                            </TouchableOpacity>
                          </View>

                          <View style={styles.rowInputs}>
                            <View style={[styles.inputGroup, { flex: 1.2 }]}>
                              <Text style={styles.inputLabel}>MARKET VALUE (₱)</Text>
                              <TextInput
                                style={styles.input}
                                value={(row.MarketValue || 0).toString()}
                                onChangeText={(val) => updateRollRow(idx, 'MarketValue', val)}
                                keyboardType="numeric"
                              />
                            </View>
                            <View style={[styles.inputGroup, { flex: 0.8 }]}>
                              <Text style={styles.inputLabel}>ASSESS LEVEL (%)</Text>
                              <TextInput
                                style={styles.input}
                                value={(row.AssessmentLevelPercent || 80).toString()}
                                onChangeText={(val) => updateRollRow(idx, 'AssessmentLevelPercent', val)}
                                keyboardType="numeric"
                              />
                            </View>
                          </View>

                          <View style={styles.rollRowBottom}>
                            <Text style={styles.rollRowAvLabel}>ASSESSMENT VALUE (₱):</Text>
                            <Text style={styles.rollRowAvValue}>
                              ₱{(parseFloat(row.AssessmentValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}
                            </Text>
                          </View>
                        </View>
                      ))}

                      {/* Assessment Roll Totals */}
                      <View style={styles.totalsSummaryBox}>
                        <View style={styles.totalsSummaryRow}>
                          <Text style={styles.totalsSummaryLabel}>TOTAL ROLL MARKET VALUE:</Text>
                          <Text style={styles.totalsSummaryVal}>₱{totalsTable2.totalRollMv.toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                        </View>
                        <View style={[styles.totalsSummaryRow, styles.totalsSummaryGrand]}>
                          <Text style={styles.totalsSummaryGrandLabel}>TOTAL ASSESSMENT VALUE:</Text>
                          <Text style={styles.totalsSummaryGrandVal}>₱{totalsTable2.totalRollAv.toLocaleString('en-US', { minimumFractionDigits: 2 })}</Text>
                        </View>
                      </View>
                    </View>
                  )}
                </View>

                {/* Valuation Summary KPI Dashboard (Matching Web) */}
                <View style={styles.kpiGrid}>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>TOTAL ORIGINAL COST</Text>
                    <Text style={styles.kpiValue}>
                      ₱{totalsTable1.totalOrigCost.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>DEPRECIATED MV</Text>
                    <Text style={[styles.kpiValue, { color: '#0284c7' }]}>
                      ₱{grandMarketValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>TOTAL ASSESSED VALUE</Text>
                    <Text style={[styles.kpiValue, { color: isDarkMode ? '#34d399' : '#047857' }]}>
                      ₱{grandAssessedValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>ANNUAL TAX DUE (3%)</Text>
                    <Text style={[styles.kpiValue, { color: '#d97706' }]}>
                      ₱{annualTaxDue.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </Text>
                  </View>
                </View>
              </View>
            )}

            {/* ========================================================================= */}
            {/* TAB 2: 3. Memoranda, Effectivity & Back Taxes                             */}
            {/* ========================================================================= */}
            {activeTab === 2 && (
              <View style={styles.tabContentBlock}>
                {/* Effectivity & Taxability (CAST Standard) */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Effectivity & Taxability (CAST Standard)</Text>

                  <View style={styles.rowInputs}>
                    <View style={{ flex: 1 }}>
                      {renderSelectField('TAXABILITY STATUS', taxability ? 'Taxable' : 'Exempt', TAXABILITY_OPTIONS, (val) => setTaxability(val === 'Taxable'))}
                    </View>
                    <View style={{ flex: 1 }}>
                      {renderSelectField('EFFECTIVITY QUARTER', quarterEffectivity, EFFECTIVITY_QUARTERS, setQuarterEffectivity)}
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>EFFECTIVITY YEAR (YR.)</Text>
                      <TextInput
                        style={styles.input}
                        value={yearEffectivity}
                        onChangeText={setYearEffectivity}
                        keyboardType="numeric"
                        placeholder="YYYY"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1.2 }]}>
                      <Text style={styles.inputLabel}>BASIS OF ASSESSMENT</Text>
                      <TextInput
                        style={styles.input}
                        value={assessmentBasis}
                        onChangeText={setAssessmentBasis}
                        placeholder="Sec. 224-225 RA 7160"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BACK TAXES FROM YEAR</Text>
                      <TextInput
                        style={styles.input}
                        value={backTaxesYearFrom}
                        onChangeText={setBackTaxesYearFrom}
                        keyboardType="numeric"
                        placeholder="YYYY (Optional)"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BACK TAXES TO YEAR</Text>
                      <TextInput
                        style={styles.input}
                        value={backTaxesYearTo}
                        onChangeText={setBackTaxesYearTo}
                        keyboardType="numeric"
                        placeholder="YYYY (Optional)"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>
                </View>

                {/* Memoranda & Notes (CAST Reference) */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Memoranda (CAST Reference)</Text>
                  <View style={styles.inputGroup}>
                    <TextInput
                      style={[styles.input, styles.textArea]}
                      value={memoranda}
                      onChangeText={setMemoranda}
                      multiline
                      numberOfLines={4}
                      placeholder="ENTER MEMORANDA, INVOICE REFERENCES, SUPPLIER DETAILS, OR SWORN STATEMENT NOTES..."
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <Text style={[styles.cardHeaderTitle, { marginTop: 12 }]}>Field Inspector Remarks</Text>
                  <View style={styles.inputGroup}>
                    <TextInput
                      style={[styles.input, styles.textArea, { minHeight: 70 }]}
                      value={remarks}
                      onChangeText={setRemarks}
                      multiline
                      numberOfLines={3}
                      placeholder="Enter field inspection findings or appraisal notes..."
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                {/* Field Inspection Photos (AttachmentsSection) */}
                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  formWorkflowId={formWorkflowIdParam}
                  partitionIndex={parseInt(partitionIndexParam, 10) || 0}
                  attachments={attachments}
                  onAttachmentsChange={(newAtts) => {
                    setAttachments(newAtts);
                    savePartitionAttachmentsOffline(trackingNo || trackingNoParam, partitionIndexParam, newAtts, undefined);
                    if (typeof updatePartitionAttachmentsLocally === 'function') {
                      updatePartitionAttachmentsLocally(trackingNo || trackingNoParam, formWorkflowIdParam, partitionIndexParam, newAtts, undefined);
                    }
                  }}
                  isOffline={!isOnline}
                  propertyKind="Machinery"
                  showSketch={false}
                  showAttachments={true}
                />
              </View>
            )}
          </>
        )}

        {/* Action Buttons */}
        {!loading && !notDownloadedError && (
          <View style={styles.buttonContainer}>
            {fromHistory ? (
              <TouchableOpacity
                style={[styles.saveBtn, { backgroundColor: colors.primary }]}
                onPress={handleBack}
              >
                <Ionicons name="arrow-back" size={18} color="#ffffff" />
                <Text style={styles.saveBtnText}>Back to History</Text>
              </TouchableOpacity>
            ) : (
              <>
                <View style={styles.partitionNoticeRow}>
                  <Ionicons name="information-circle-outline" size={16} color={colors.primary} />
                  <Text style={styles.partitionNoticeText}>
                    Partition edits are saved locally to this device. Synchronizing to web and transferring to supervisor are handled per transaction via the 3-dots (⋮) menu on the transaction card in My Transactions.
                  </Text>
                </View>

                <TouchableOpacity
                  style={[styles.saveBtn, styles.saveBtnOffline]}
                  onPress={handleSave}
                  disabled={saving}
                >
                  {saving ? (
                    <ActivityIndicator color="#ffffff" size="small" />
                  ) : (
                    <>
                      <Ionicons name="cloud-offline-outline" size={18} color="#ffffff" />
                      <Text style={styles.saveBtnText}>Save Partition (Offline)</Text>
                    </>
                  )}
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.saveBtn, styles.cancelBtn]}
                  onPress={() => setDiscardModalVisible(true)}
                  disabled={saving}
                >
                  <Ionicons name="close-circle-outline" size={18} color={isDarkMode ? '#fda4af' : '#e11d48'} />
                  <Text style={styles.cancelBtnText}>Cancel & Discard Changes</Text>
                </TouchableOpacity>
              </>
            )}
          </View>
        )}
      </ScrollView>

      {/* ========================================================================= */}
      {/* MODAL: ADD / EDIT MACHINERY DETAIL (Table 1)                              */}
      {/* ========================================================================= */}
      <Modal
        visible={detailModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setDetailModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setDetailModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingDetailIndex !== null ? `Edit Machinery Item #${editingDetailIndex + 1}` : 'Add Machinery Appraisal Detail'}
                  </Text>
                  <Text style={styles.cardHeaderSubtitle}>CAST 16-Column Specification</Text>
                </View>
                <TouchableOpacity onPress={() => setDetailModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Group 1: Classification, Description/Brand & Capacity */}
                <View style={styles.modalSectionBox}>
                  <Text style={styles.modalSectionTitle}>1. Machinery Classification & Specification</Text>

                  {renderSelectField('CLASSIFICATION *', mClassification, MACHINERY_CLASSIFICATIONS, setMClassification)}

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>DESCRIPTION / BRAND *</Text>
                    <TextInput
                      style={styles.input}
                      value={mBrandModel}
                      onChangeText={setMBrandModel}
                      placeholder="e.g. GENERATOR SET - CATERPILLAR C15"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>RATED CAPACITY</Text>
                    <TextInput
                      style={styles.input}
                      value={mCapacity}
                      onChangeText={setMCapacity}
                      placeholder="e.g. 500 KVA / 150 HP"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                {/* Group 2: Condition, Acquisition & Economic Life */}
                <View style={styles.modalSectionBox}>
                  <Text style={styles.modalSectionTitle}>2. Condition, Acquisition & Economic Life</Text>

                  <View style={styles.rowInputs}>
                    <View style={{ flex: 1 }}>
                      {renderSelectField('CONDITION', mCondition, MACHINERY_CONDITIONS, setMCondition)}
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DATE ACQUIRED</Text>
                      <TouchableOpacity
                        style={styles.datePickerBtn}
                        onPress={() => openDatePicker('Select Date Acquired', 'mAcquiredDate', mAcquiredDate)}
                      >
                        <Text style={[styles.datePickerBtnText, !mAcquiredDate && { color: colors.placeholder }]}>
                          {mAcquiredDate || 'YYYY-MM-DD'}
                        </Text>
                        <Ionicons name="calendar-outline" size={16} color={colors.textMuted} />
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ECONOMIC LIFE (YRS)</Text>
                      <TextInput
                        style={styles.input}
                        value={mEstLife}
                        onChangeText={(val) => {
                          setMEstLife(val);
                          recomputeModalValues(mOriginalCost, mInstallationCost, mDepPercentage, val, mYearsUsed);
                        }}
                        keyboardType="numeric"
                        placeholder="20"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>YEAR INSTALLED</Text>
                      <TextInput
                        style={styles.input}
                        value={mYearInstalled}
                        onChangeText={setMYearInstalled}
                        keyboardType="numeric"
                        placeholder="YYYY"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>YEAR OPERATION</Text>
                      <TextInput
                        style={styles.input}
                        value={mYearOperation}
                        onChangeText={(val) => {
                          setMYearOperation(val);
                          const yOp = parseInt(val, 10);
                          if (yOp > 1900) {
                            const diff = Math.max(0, new Date().getFullYear() - yOp);
                            setMYearsUsed(diff.toString());
                            recomputeModalValues(mOriginalCost, mInstallationCost, mDepPercentage, mEstLife, diff);
                          }
                        }}
                        keyboardType="numeric"
                        placeholder="YYYY"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>NO. OF YEARS USED</Text>
                      <TextInput
                        style={styles.input}
                        value={mYearsUsed}
                        onChangeText={(val) => {
                          setMYearsUsed(val);
                          recomputeModalValues(mOriginalCost, mInstallationCost, mDepPercentage, mEstLife, val);
                        }}
                        keyboardType="numeric"
                        placeholder="0"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>REMAINING ECOLIFE (ECOLIFE - NO. OF YEARS)</Text>
                    <TextInput
                      style={[styles.input, styles.inputDisabled]}
                      value={mRemLife}
                      editable={false}
                    />
                  </View>
                </View>

                {/* Group 3: Cost, Depreciation & Market Value Computation */}
                <View style={styles.modalSectionBox}>
                  <Text style={styles.modalSectionTitle}>3. Cost, Depreciation & Market Value</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ORIGINAL COST (₱) *</Text>
                      <TextInput
                        style={[styles.input, styles.valuationInput]}
                        value={mOriginalCost}
                        onChangeText={(val) => {
                          setMOriginalCost(val);
                          recomputeModalValues(val, mInstallationCost, mDepPercentage, mEstLife, mYearsUsed);
                        }}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>INSTALLATION & OTHER (₱)</Text>
                      <TextInput
                        style={styles.input}
                        value={mInstallationCost}
                        onChangeText={(val) => {
                          setMInstallationCost(val);
                          recomputeModalValues(mOriginalCost, val, mDepPercentage, mEstLife, mYearsUsed);
                        }}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TOTAL COST (₱)</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={mTotalCost}
                        editable={false}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>RATE DEPRECIATION (%)</Text>
                      <TextInput
                        style={[styles.input, { color: '#ef4444', fontWeight: '800' }]}
                        value={mDepPercentage}
                        onChangeText={(val) => {
                          setMDepPercentage(val);
                          const total = parseFloat(mTotalCost) || 0;
                          const pct = parseFloat(val) || 0;
                          const dep = Math.round(total * (pct / 100) * 100) / 100;
                          const mv = Math.max(0, Math.round((total - dep) * 100) / 100);
                          setMDepValue(dep.toFixed(2));
                          setMDepreciatedValue(mv.toFixed(2));
                        }}
                        keyboardType="numeric"
                        placeholder="0"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.calculatedBox}>
                    <Text style={styles.calculatedLabel}>DEPRECIATION VALUE (AUTO):</Text>
                    <Text style={[styles.calculatedValue, { color: '#ef4444' }]}>₱{mDepValue}</Text>
                  </View>

                  <View style={[styles.calculatedBox, { backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5', borderColor: '#10b981' }]}>
                    <Text style={[styles.calculatedLabel, { color: isDarkMode ? '#34d399' : '#047857' }]}>
                      MARKET VALUE (LESS DEPRECIATION):
                    </Text>
                    <Text style={[styles.calculatedValue, { color: isDarkMode ? '#34d399' : '#047857' }]}>₱{mDepreciatedValue}</Text>
                  </View>
                </View>

                {/* Modal Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setDetailModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveDetailModal}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Machinery Detail</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

      {/* ========================================================================= */}
      {/* MODAL: SELECTION PICKER                                                   */}
      {/* ========================================================================= */}
      <Modal
        visible={pickerModal.visible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setPickerModal(prev => ({ ...prev, visible: false }))}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setPickerModal(prev => ({ ...prev, visible: false }))}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{pickerModal.title}</Text>
              <TouchableOpacity onPress={() => setPickerModal(prev => ({ ...prev, visible: false }))}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }}>
              {(pickerModal.options || []).map((option) => {
                const isSelected = pickerModal.selectedValue === option;
                return (
                  <TouchableOpacity
                    key={option}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (pickerModal.onSelect) pickerModal.onSelect(option);
                      setPickerModal(prev => ({ ...prev, visible: false }));
                    }}
                  >
                    <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                      {option}
                    </Text>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={18} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* DatePicker Modal */}
      <DatePickerModal
        visible={datePickerState.visible}
        title={datePickerState.title}
        currentDate={datePickerState.currentDate}
        onConfirm={handleDateConfirm}
        onCancel={() => setDatePickerState(prev => ({ ...prev, visible: false }))}
      />

      {/* Discard Changes Modal */}
      <DiscardChangesModal
        visible={discardModalVisible}
        onClose={() => setDiscardModalVisible(false)}
        onConfirmDiscard={navigateBack}
        partitionLetter={partitionLetterParam}
      />

      {/* Complete / Confirmation Modal */}
      <ActionModal
        visible={completeModalConfig.visible}
        title={completeModalConfig.title}
        message={completeModalConfig.message}
        icon={completeModalConfig.icon}
        iconType={completeModalConfig.iconType}
        buttons={[
          {
            text: 'Return to Transactions',
            style: 'primary',
            icon: 'arrow-back',
            onPress: () => {
              setCompleteModalConfig(prev => ({ ...prev, visible: false }));
              navigateBack();
            }
          }
        ]}
        onClose={() => {
          setCompleteModalConfig(prev => ({ ...prev, visible: false }));
          navigateBack();
        }}
      />
    </View>
  );
};

// =========================================================================
// STYLES
// =========================================================================
const getStyles = (colors, isDarkMode) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background
  },
  historyNoticeRibbon: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: isDarkMode ? '#0c4a6e33' : '#e0f2fe',
    borderBottomWidth: 1,
    borderBottomColor: isDarkMode ? '#0369a1' : '#bae6fd',
    paddingHorizontal: 16,
    paddingVertical: 10
  },
  historyNoticeText: {
    fontSize: 12,
    fontWeight: '700',
    color: isDarkMode ? '#7dd3fc' : '#0369a1'
  },
  ribbon: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderBottomWidth: 1
  },
  ribbonStandard: {
    backgroundColor: isDarkMode ? '#1e293b' : '#f0fdf4',
    borderBottomColor: isDarkMode ? '#334155' : '#bbf7d0'
  },
  ribbonDownloaded: {
    backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5',
    borderBottomColor: isDarkMode ? '#047857' : '#a7f3d0'
  },
  ribbonText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.primary
  },
  ribbonNetTag: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 8,
    gap: 4
  },
  ribbonNetOnline: {
    backgroundColor: isDarkMode ? '#065f46' : '#dcfce7'
  },
  ribbonNetOffline: {
    backgroundColor: isDarkMode ? '#78350f' : '#fef3c7'
  },
  ribbonNetDot: {
    width: 6,
    height: 6,
    borderRadius: 3
  },
  ribbonNetDotOnline: {
    backgroundColor: '#10b981'
  },
  ribbonNetDotOffline: {
    backgroundColor: '#f59e0b'
  },
  ribbonNetText: {
    fontSize: 10,
    fontWeight: '800'
  },
  ribbonNetTextOnline: {
    color: '#059669'
  },
  ribbonNetTextOffline: {
    color: '#d97706'
  },
  topTabsWrapper: {
    backgroundColor: colors.cardBg,
    borderBottomWidth: 1,
    borderBottomColor: colors.border
  },
  bannerInfoBox: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 6,
    backgroundColor: isDarkMode ? '#111827' : '#f8fafc',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    gap: 12,
    flexWrap: 'wrap'
  },
  bannerInfoItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  bannerInfoLabel: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textMuted
  },
  bannerInfoValue: {
    fontSize: 11,
    fontWeight: '800',
    fontFamily: Platform.OS === 'ios' ? 'Menlo' : 'monospace',
    color: isDarkMode ? '#34d399' : colors.primary
  },
  sectionTabsContainer: {
    paddingVertical: 8
  },
  sectionTabsScroll: {
    paddingHorizontal: 12,
    gap: 8
  },
  sectionTabBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9',
    gap: 6
  },
  sectionTabBtnActive: {
    backgroundColor: colors.primary
  },
  sectionTabText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textDark
  },
  sectionTabTextActive: {
    color: '#ffffff'
  },
  scrollContent: {
    padding: 16,
    paddingBottom: 40
  },
  tabContentBlock: {
    gap: 16
  },
  card: {
    backgroundColor: colors.cardBg,
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: colors.border,
    gap: 12
  },
  cardHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    flexWrap: 'wrap',
    gap: 8,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 8
  },
  cardHeaderTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : '#047857',
    textTransform: 'uppercase'
  },
  cardHeaderSubtitle: {
    fontSize: 11,
    color: colors.textMuted
  },
  rolePillGroup: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: 6
  },
  rolePill: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: isDarkMode ? '#1e293b' : '#f8fafc'
  },
  rolePillActive: {
    backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5',
    borderColor: '#10b981'
  },
  rolePillText: {
    fontSize: 10,
    fontWeight: '700',
    color: colors.textMuted
  },
  rolePillTextActive: {
    color: isDarkMode ? '#34d399' : '#047857',
    fontWeight: '800'
  },
  checkboxPill: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: isDarkMode ? '#1e293b' : '#f8fafc',
    gap: 8
  },
  checkboxPillActive: {
    backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5',
    borderColor: '#10b981'
  },
  checkboxPillText: {
    fontSize: 11,
    fontWeight: '800',
    color: colors.textDark,
    flex: 1
  },
  checkboxPillTextActive: {
    color: isDarkMode ? '#34d399' : '#047857'
  },
  syncedBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: isDarkMode ? '#065f46' : '#dcfce7',
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 6,
    gap: 4
  },
  syncedBadgeText: {
    fontSize: 10,
    fontWeight: '700',
    color: isDarkMode ? '#a7f3d0' : '#047857'
  },
  subCard: {
    marginTop: 6,
    padding: 12,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: isDarkMode ? '#78350f' : '#fef3c7',
    backgroundColor: isDarkMode ? '#291e0a' : '#fffbeb',
    gap: 10
  },
  subCardTitle: {
    fontSize: 11,
    fontWeight: '800',
    color: '#d97706',
    textTransform: 'uppercase'
  },
  originToggleRow: {
    flexDirection: 'row',
    gap: 8
  },
  originToggleBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 10,
    paddingHorizontal: 8,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: isDarkMode ? '#1e293b' : '#f8fafc',
    gap: 6
  },
  originToggleBtnActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary
  },
  originToggleText: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textDark,
    textAlign: 'center'
  },
  originToggleTextActive: {
    color: '#ffffff'
  },
  registrationPanel: {
    gap: 12,
    paddingTop: 4
  },
  panelBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 8,
    backgroundColor: isDarkMode ? '#082f49' : '#f0f9ff',
    borderWidth: 1,
    borderColor: isDarkMode ? '#0369a1' : '#bae6fd',
    gap: 6
  },
  panelBadgeText: {
    fontSize: 10,
    fontWeight: '800',
    color: isDarkMode ? '#38bdf8' : '#0369a1',
    textTransform: 'uppercase'
  },
  inputGroup: {
    gap: 4
  },
  inputLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textDark
  },
  input: {
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 13,
    color: colors.inputText,
    backgroundColor: isDarkMode ? colors.inputBg : '#f8fafc'
  },
  inputDisabled: {
    backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9',
    color: isDarkMode ? '#94a3b8' : '#64748b',
    borderColor: isDarkMode ? '#334155' : '#cbd5e1',
    fontWeight: '700'
  },
  fontMonoBold: {
    fontFamily: Platform.OS === 'ios' ? 'Menlo' : 'monospace',
    fontWeight: '800'
  },
  fontBold: {
    fontWeight: '800'
  },
  valuationInput: {
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : colors.primaryDark,
    backgroundColor: isDarkMode ? '#064e3b' : '#f0fdf4'
  },
  textArea: {
    minHeight: 80,
    textAlignVertical: 'top'
  },
  rowInputs: {
    flexDirection: 'row',
    gap: 10
  },
  selectBtn: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    backgroundColor: isDarkMode ? colors.inputBg : '#f8fafc'
  },
  selectBtnText: {
    fontSize: 13,
    color: colors.inputText,
    flex: 1
  },
  datePickerBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    backgroundColor: isDarkMode ? colors.inputBg : '#f8fafc'
  },
  datePickerBtnText: {
    fontSize: 13,
    color: colors.inputText
  },
  addBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.primary,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 10,
    gap: 4
  },
  addBtnText: {
    fontSize: 11,
    fontWeight: '800',
    color: '#ffffff'
  },
  syncBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9',
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 10,
    gap: 4
  },
  syncBtnText: {
    fontSize: 11,
    fontWeight: '800',
    color: colors.primary
  },
  emptyTableBox: {
    alignItems: 'center',
    paddingVertical: 24,
    gap: 8
  },
  emptyTableText: {
    fontSize: 12,
    color: colors.textMuted
  },
  emptyAddBtn: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 10,
    backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5',
    borderWidth: 1,
    borderColor: '#10b981'
  },
  emptyAddBtnText: {
    fontSize: 12,
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : '#047857'
  },
  itemCard: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: isDarkMode ? '#111827' : '#ffffff',
    padding: 12,
    gap: 8
  },
  itemCardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: 8
  },
  itemCardTitle: {
    fontSize: 12,
    fontWeight: '800',
    color: colors.textDark
  },
  itemCardSubtitle: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2
  },
  itemActionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  actionIconBtn: {
    padding: 6,
    borderRadius: 8,
    backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9'
  },
  itemSpecsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    paddingVertical: 4
  },
  specItem: {
    width: '48%',
    backgroundColor: isDarkMode ? '#1f2937' : '#f8fafc',
    padding: 6,
    borderRadius: 8
  },
  specLabel: {
    fontSize: 10,
    color: colors.textMuted,
    fontWeight: '600'
  },
  specValue: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textDark,
    marginTop: 2
  },
  itemFooterRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 6
  },
  itemFooterLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  itemFooterValue: {
    fontSize: 13,
    fontWeight: '900',
    color: isDarkMode ? '#34d399' : '#047857'
  },
  totalsSummaryBox: {
    borderRadius: 12,
    padding: 12,
    backgroundColor: isDarkMode ? '#111827' : '#f8fafc',
    borderWidth: 1,
    borderColor: colors.border,
    gap: 6
  },
  totalsSummaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  totalsSummaryLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  totalsSummaryVal: {
    fontSize: 12,
    fontWeight: '800',
    fontFamily: Platform.OS === 'ios' ? 'Menlo' : 'monospace',
    color: colors.textDark
  },
  totalsSummaryGrand: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 6,
    marginTop: 2
  },
  totalsSummaryGrandLabel: {
    fontSize: 11,
    fontWeight: '900',
    color: isDarkMode ? '#34d399' : '#047857'
  },
  totalsSummaryGrandVal: {
    fontSize: 14,
    fontWeight: '900',
    fontFamily: Platform.OS === 'ios' ? 'Menlo' : 'monospace',
    color: isDarkMode ? '#34d399' : '#047857'
  },
  rollRowCard: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: isDarkMode ? '#111827' : '#ffffff',
    padding: 10,
    gap: 8
  },
  rollRowTop: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  deleteRollBtn: {
    padding: 8,
    borderRadius: 8,
    backgroundColor: isDarkMode ? '#4c0519' : '#fef2f2'
  },
  rollRowBottom: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 6
  },
  rollRowAvLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  rollRowAvValue: {
    fontSize: 13,
    fontWeight: '900',
    color: isDarkMode ? '#34d399' : '#047857'
  },
  kpiGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8
  },
  kpiCard: {
    width: '48%',
    backgroundColor: colors.cardBg,
    borderRadius: 12,
    padding: 12,
    borderWidth: 1,
    borderColor: colors.border
  },
  kpiLabel: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textMuted,
    marginBottom: 4,
    textTransform: 'uppercase'
  },
  kpiValue: {
    fontSize: 14,
    fontWeight: '900',
    fontFamily: Platform.OS === 'ios' ? 'Menlo' : 'monospace',
    color: colors.textDark
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between'
  },
  switchTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: colors.textDark
  },
  switchSubtitle: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2
  },
  nextStepBox: {
    backgroundColor: isDarkMode ? '#111827' : '#f0fdf4',
    padding: 10,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: isDarkMode ? '#047857' : '#bbf7d0',
    gap: 4
  },
  nextStepLabel: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textMuted
  },
  nextStepBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  nextStepBadgeText: {
    fontSize: 12,
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : '#047857'
  },
  buttonContainer: {
    gap: 10,
    marginTop: 16
  },
  partitionNoticeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: isDarkMode ? '#1e293b' : '#f0fdf4',
    borderWidth: 1,
    borderColor: isDarkMode ? '#334155' : '#bbf7d0',
    borderRadius: 12,
    padding: 12,
    marginBottom: 4
  },
  partitionNoticeText: {
    flex: 1,
    fontSize: 12,
    lineHeight: 16,
    color: isDarkMode ? '#94a3b8' : '#166534',
    fontWeight: '500'
  },
  saveBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 14,
    borderRadius: 14,
    gap: 8
  },
  saveBtnPrimary: {
    backgroundColor: colors.primary
  },
  saveBtnTransfer: {
    backgroundColor: '#059669'
  },
  saveBtnOffline: {
    backgroundColor: '#0284c7'
  },
  saveBtnText: {
    color: '#ffffff',
    fontSize: 14,
    fontWeight: '800'
  },
  cancelBtn: {
    backgroundColor: isDarkMode ? '#4c0519' : '#fff1f2',
    borderWidth: 1,
    borderColor: isDarkMode ? '#881337' : '#fecdd3'
  },
  cancelBtnText: {
    color: isDarkMode ? '#fda4af' : '#e11d48',
    fontSize: 13,
    fontWeight: '800'
  },
  notDownloadedBox: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: 30,
    backgroundColor: colors.cardBg,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: colors.border,
    marginVertical: 20
  },
  notDownloadedTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: colors.textDark,
    marginTop: 16,
    marginBottom: 8
  },
  notDownloadedText: {
    fontSize: 13,
    color: colors.textMuted,
    textAlign: 'center',
    lineHeight: 20,
    marginBottom: 20
  },
  backBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.primary,
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 10,
    gap: 8
  },
  backBtnText: {
    color: '#ffffff',
    fontSize: 14,
    fontWeight: '600'
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    padding: 16
  },
  modalContent: {
    backgroundColor: colors.cardBg,
    borderRadius: 20,
    padding: 16,
    maxHeight: '85%'
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 10,
    marginBottom: 10
  },
  modalTitle: {
    fontSize: 14,
    fontWeight: '800',
    color: colors.textDark
  },
  modalSectionBox: {
    padding: 12,
    borderRadius: 12,
    backgroundColor: isDarkMode ? '#111827' : '#f8fafc',
    borderWidth: 1,
    borderColor: colors.border,
    gap: 10
  },
  modalSectionTitle: {
    fontSize: 11,
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : '#047857',
    textTransform: 'uppercase'
  },
  calculatedBox: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 10,
    borderRadius: 10,
    backgroundColor: isDarkMode ? '#1f2937' : '#f1f5f9',
    borderWidth: 1,
    borderColor: colors.border
  },
  calculatedLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  calculatedValue: {
    fontSize: 13,
    fontWeight: '900',
    fontFamily: Platform.OS === 'ios' ? 'Menlo' : 'monospace',
    color: colors.textDark
  },
  modalFooterBtnRow: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: 10,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: colors.border
  },
  modalCancelBtn: {
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border
  },
  modalCancelBtnText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textMuted
  },
  modalSaveBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 10,
    paddingHorizontal: 18,
    borderRadius: 10,
    backgroundColor: colors.primary,
    gap: 6
  },
  modalSaveBtnText: {
    fontSize: 12,
    fontWeight: '800',
    color: '#ffffff'
  },
  typeOptionItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 10,
    borderBottomWidth: 1,
    borderBottomColor: colors.border
  },
  typeOptionItemSelected: {
    backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5'
  },
  typeOptionText: {
    fontSize: 13,
    color: colors.textDark
  },
  typeOptionTextSelected: {
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : '#047857'
  }
});
