import React, { useState, useEffect, useContext, useRef } from 'react';
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
import { DatePickerModal } from '../components/DatePickerModal';
import { AttachmentsSection } from '../components/AttachmentsSection';
import { COLORS, SHADOWS } from '../constants/theme';
import { BARANGAY_OPTIONS, normalizeBarangay } from '../constants/locationData';
import {
  LAND_CLASSIFICATIONS,
  LAND_SUB_CLASSIFICATIONS,
  LAND_ACTUAL_USES,
  LAND_ASSESSMENT_LEVELS,
  LAND_ADJUSTMENT_FACTORS,
  LAND_IMPROVEMENT_TYPES,
  getAssessmentLevelForActualUse
} from '../constants/landScheduleData';

const cleanRegStatus = (val) => {
  if (!val) return 'Titled';
  const s = String(val).trim().toLowerCase();
  if (s.includes('untitl')) return 'Untitled';
  return 'Titled';
};

const cleanTitleType = (val) => {
  if (!val) return 'TCT';
  const s = String(val).trim().toUpperCase();
  if (s.includes('OCT')) return 'OCT';
  if (s.includes('CLOA')) return 'CLOA';
  if (s.includes('PATENT')) return 'Patent';
  return 'TCT';
};

const GENDER_OPTIONS = ['Male', 'Female'];

const TABS = [
  { id: 0, label: '1. Cadastral & Title', icon: 'map-outline' },
  { id: 1, label: '2. Location & Boundaries', icon: 'compass-outline' },
  { id: 2, label: '3. Owner & Demographics', icon: 'people-outline' },
  { id: 3, label: '4. Valuation (CAST)', icon: 'calculator-outline' },
  { id: 4, label: '5. Effectivity & Attachments', icon: 'calendar-outline' }
];

export const LandFormScreen = ({ route, navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const styles = React.useMemo(() => getStyles(colors, isDarkMode), [colors, isDarkMode]);
  const tabScrollRef = useRef(null);

  const trackingNoParam = route?.params?.trackingNo || '';
  const formWorkflowIdParam = route?.params?.formWorkflowId || null;
  const partitionIndexParam = route?.params?.partitionIndex ?? 0;
  const partitionLetterParam = route?.params?.partitionLetter || String.fromCharCode(65 + (parseInt(partitionIndexParam, 10) || 0));
  const {
    queueOfflineUpdate,
    markTransactionSynced,
    getDownloadedFormData,
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
  const [datePickerVisible, setDatePickerVisible] = useState(false);
  const [barangayModalVisible, setBarangayModalVisible] = useState(false);
  const [genderPickerVisible, setGenderPickerVisible] = useState(false);
  const [genderPickerTarget, setGenderPickerTarget] = useState('owner'); // 'owner' or 'admin'

  const openGenderPicker = (target) => {
    setGenderPickerTarget(target);
    setGenderPickerVisible(true);
  };
  const isDownloaded = isTransactionDownloaded(trackingNoParam, formWorkflowIdParam);

  // Active section tab (0 to 4)
  const [activeTab, setActiveTab] = useState(0);

  // 1. Cadastral & Title Registration (CAST Reference)
  const [tdnId, setTdnId] = useState(route?.params?.tdnId || null);
  const [tdnLandId, setTdnLandId] = useState(null);
  const [trackingNo, setTrackingNo] = useState(trackingNoParam);
  const [declarationNo, setDeclarationNo] = useState(''); // TDN: Auto-generated in Web -> Read-only
  const [arpNo, setArpNo] = useState('');                 // ARP No: Auto-generated in Web -> Read-only
  const [pin, setPin] = useState('');
  const [registrationStatus, setRegistrationStatus] = useState('Titled');
  const [titleType, setTitleType] = useState('TCT');
  const [tctNo, setTctNo] = useState('');
  const [tctDate, setTctDate] = useState('');
  const [cadastralSurveyNo, setCadastralSurveyNo] = useState('');
  const [sectionNo, setSectionNo] = useState('');
  const [lotNo, setLotNo] = useState('');
  const [blockNo, setBlockNo] = useState('');
  const [parcelNo, setParcelNo] = useState('');
  const [surveyNo, setSurveyNo] = useState('');
  const [transactionCode, setTransactionCode] = useState('');

  // 2. Property Location & Boundaries (CAST Reference)
  const [streetLocation, setStreetLocation] = useState('');
  const [purok, setPurok] = useState('');
  const [districtOrBarangay, setDistrictOrBarangay] = useState('BRGY. ZONE I');
  const [cityMunicipality, setCityMunicipality] = useState('Koronadal City');
  const [province, setProvince] = useState('South Cotabato');

  const [northBoundary, setNorthBoundary] = useState('');
  const [nRemarks, setNRemarks] = useState('');
  const [southBoundary, setSouthBoundary] = useState('');
  const [sRemarks, setSRemarks] = useState('');
  const [eastBoundary, setEastBoundary] = useState('');
  const [eRemarks, setERemarks] = useState('');
  const [westBoundary, setWestBoundary] = useState('');
  const [wRemarks, setWRemarks] = useState('');

  // 3. Owner & Declarant Demographics
  const [isAdministrator, setIsAdministrator] = useState(false);
  const [isBeneficialUser, setIsBeneficialUser] = useState(false);
  const [isPossessor, setIsPossessor] = useState(false);

  const [ownerName, setOwnerName] = useState('');
  const [ownerTin, setOwnerTin] = useState('');
  const [ownerContact, setOwnerContact] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerGender, setOwnerGender] = useState('Male');

  const [adminName, setAdminName] = useState('');
  const [adminGender, setAdminGender] = useState('Male');
  const [adminTin, setAdminTin] = useState('');
  const [adminAddress, setAdminAddress] = useState('');
  const [adminContact, setAdminContact] = useState('');
  const [adminEmail, setAdminEmail] = useState('');

  // 4. Valuation & 4 Dynamic Schedules (CAST Reference)
  const [classificationRows, setClassificationRows] = useState([
    {
      id: 'c_1',
      classificationId: 1,
      classificationName: 'RESIDENTIAL',
      subClassId: 1,
      subClassName: '1st Class Residential',
      actualUseName: 'RESIDENTIAL',
      areaSqMeters: 150,
      unitValue: 520,
      marketValue: 78000
    }
  ]);
  const [improvementRows, setImprovementRows] = useState([]);
  const [adjustmentRows, setAdjustmentRows] = useState([]);
  const [assessmentRollRows, setAssessmentRollRows] = useState([]);

  // Modals for Table dropdown selections
  const [classPickerVisible, setClassPickerVisible] = useState(false);
  const [subClassPickerVisible, setSubClassPickerVisible] = useState(false);
  const [actualUsePickerVisible, setActualUsePickerVisible] = useState(false);
  const [activeClassRowIndex, setActiveClassRowIndex] = useState(null);

  const [impTypePickerVisible, setImpTypePickerVisible] = useState(false);
  const [activeImpRowIndex, setActiveImpRowIndex] = useState(null);

  const [adjFactorPickerVisible, setAdjFactorPickerVisible] = useState(false);
  const [activeAdjRowIndex, setActiveAdjRowIndex] = useState(null);

  const [rollKindPickerVisible, setRollKindPickerVisible] = useState(false);
  const [rollActualUsePickerVisible, setRollActualUsePickerVisible] = useState(false);
  const [activeRollRowIndex, setActiveRollRowIndex] = useState(null);

  // Option B: Compact Master-Detail Row Editor Modals
  const [editClassModalVisible, setEditClassModalVisible] = useState(false);
  const [editingClassRow, setEditingClassRow] = useState(null);
  const [editingClassIndex, setEditingClassIndex] = useState(null);

  const [editImpModalVisible, setEditImpModalVisible] = useState(false);
  const [editingImpRow, setEditingImpRow] = useState(null);
  const [editingImpIndex, setEditingImpIndex] = useState(null);

  const [editAdjModalVisible, setEditAdjModalVisible] = useState(false);
  const [editingAdjRow, setEditingAdjRow] = useState(null);
  const [editingAdjIndex, setEditingAdjIndex] = useState(null);

  const [editRollModalVisible, setEditRollModalVisible] = useState(false);
  const [editingRollRow, setEditingRollRow] = useState(null);
  const [editingRollIndex, setEditingRollIndex] = useState(null);

  const [classification, setClassification] = useState('Residential');
  const [subClassification, setSubClassification] = useState('R-1 Residential');
  const [actualUse, setActualUse] = useState('Residential');
  const [area, setArea] = useState('150');
  const [unit, setUnit] = useState('SQM');
  const [baseMarketValue, setBaseMarketValue] = useState('520');
  const [marketValue, setMarketValue] = useState('78000');
  const [assessmentLevel, setAssessmentLevel] = useState('20');
  const [assessedValue, setAssessedValue] = useState('15600');
  const [taxability, setTaxability] = useState(true);

  // 5. Effectivity, Back Taxes & Notes
  const [quarterEffectivity, setQuarterEffectivity] = useState('1st Quarter');
  const [yearEffectivity, setYearEffectivity] = useState(new Date().getFullYear().toString());
  const [assessmentBasis, setAssessmentBasis] = useState('Sec. 217 RA 7160');
  const [backTaxesFrom, setBackTaxesFrom] = useState('');
  const [backTaxesTo, setBackTaxesTo] = useState('');
  const [memoranda, setMemoranda] = useState('');
  const [remarks, setRemarks] = useState('');

  // 6. Site Sketch & Field Attachments
  const [sketchUrl, setSketchUrl] = useState('');
  const [attachments, setAttachments] = useState([]);

  // Workflow Forwarding
  const [autoForward, setAutoForward] = useState(true);
  const [nextStatusId, setNextStatusId] = useState(4); // 4 = Checked / Recommend Approval

  const safeParseArray = (val) => {
    if (!val) return null;
    try {
      const parsed = typeof val === 'string' ? JSON.parse(val) : val;
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    } catch (e) { }
    return null;
  };

  const hydrateSchedules = (partData, tdnLandData) => {
    const rawClass = tdnLandData?.LandAppraisalDetailsJson || tdnLandData?.landAppraisalDetailsJson || partData?.landAppraisalDetailsJson || partData?.LandAppraisalDetailsJson;
    const rawImp = tdnLandData?.LandImprovementsJson || tdnLandData?.landImprovementsJson || partData?.landImprovementsJson || partData?.LandImprovementsJson;
    const rawAdj = tdnLandData?.LandAdjustmentsJson || tdnLandData?.landAdjustmentsJson || partData?.landAdjustmentsJson || partData?.LandAdjustmentsJson;
    const rawRoll = tdnLandData?.LandPropertyAssessmentsJson || tdnLandData?.landPropertyAssessmentsJson || partData?.landPropertyAssessmentsJson || partData?.LandPropertyAssessmentsJson;

    const parsedClass = safeParseArray(rawClass);
    if (parsedClass && parsedClass.length > 0) {
      setClassificationRows(parsedClass.map((c, idx) => ({
        id: c.id || ('c_' + idx),
        classificationId: c.ClassificationId || c.classificationId || 1,
        classificationName: c.ClassificationName || c.classificationName || 'RESIDENTIAL',
        subClassId: c.SubClassId || c.subClassId || 1,
        subClassName: c.SubClassName || c.subClassName || '1st Class Residential',
        actualUseName: c.ActualUseName || c.actualUseName || 'RESIDENTIAL',
        areaSqMeters: c.AreaSQM || c.areaSQM || c.areaSqMeters || 150,
        unitValue: c.UnitValue || c.unitValue || 520,
        marketValue: c.MarketValue || c.marketValue || ((c.AreaSQM || c.areaSQM || 150) * (c.UnitValue || 520))
      })));
    } else if (partData?.areaSqMeters || partData?.marketValue) {
      const a = parseFloat(partData.areaSqMeters) || 150;
      const u = parseFloat(partData.unitValue) || 520;
      setClassificationRows([{
        id: 'c_default',
        classificationId: 1,
        classificationName: partData.classification || 'RESIDENTIAL',
        subClassId: 1,
        subClassName: '1st Class Residential',
        actualUseName: partData.actualUse || 'RESIDENTIAL',
        areaSqMeters: a,
        unitValue: u,
        marketValue: a * u
      }]);
    }

    const parsedImp = safeParseArray(rawImp);
    if (parsedImp && parsedImp.length > 0) {
      setImprovementRows(parsedImp.map((i, idx) => ({
        id: i.id || ('imp_' + idx),
        typeId: i.TypeId || i.typeId || 1,
        typeDescription: i.TypeDescription || i.typeDescription || 'COCONUT TREE',
        isFruitBearing: i.IsFruitBearing !== false && i.isFruitBearing !== false,
        quantity: i.Quantity || i.quantity || 1,
        ageYears: i.AgeYears || i.ageYears || 5,
        unitValue: i.UnitValue || i.unitValue || 120,
        marketValue: i.MarketValue || i.marketValue || ((i.Quantity || 1) * (i.UnitValue || 120))
      })));
    } else {
      setImprovementRows([]);
    }

    const parsedAdj = safeParseArray(rawAdj);
    if (parsedAdj && parsedAdj.length > 0) {
      setAdjustmentRows(parsedAdj.map((a, idx) => ({
        id: a.id || ('adj_' + idx),
        classificationName: a.ClassificationName || a.classificationName || 'RESIDENTIAL',
        baseMarketValue: a.BaseMarketValue || a.baseMarketValue || 78000,
        adjustmentFactorId: a.AdjustmentFactorId || a.adjustmentFactorId || 1,
        adjustmentFactorDescription: a.AdjustmentFactorDescription || a.adjustmentFactorDescription || 'Corner Influence (Residential)',
        percentAdjustment: a.PercentAdjustment !== undefined ? a.PercentAdjustment : (a.percentAdjustment || 0),
        valueAdjustment: a.ValueAdjustment !== undefined ? a.ValueAdjustment : (a.valueAdjustment || 0),
        adjustedMarketValue: a.AdjustedMarketValue || a.adjustedMarketValue || 78000
      })));
    } else {
      setAdjustmentRows([]);
    }

    const parsedRoll = safeParseArray(rawRoll);
    if (parsedRoll && parsedRoll.length > 0) {
      setAssessmentRollRows(parsedRoll.map((r, idx) => ({
        id: r.id || ('roll_' + idx),
        kinds: r.Kinds || r.kinds || 'LAND',
        actualUseName: r.ActualUseName || r.actualUseName || 'RESIDENTIAL',
        marketValue: r.MarketValue || r.marketValue || 78000,
        assessmentLevelPercent: r.AssessmentLevelPercent || r.assessmentLevelPercent || 20,
        assessedValue: r.AssessmentValue || r.assessmentValue || r.AssessedValue || r.assessedValue || 15600
      })));
    } else if (partData?.areaSqMeters || partData?.marketValue) {
      const mv = parseFloat(partData?.marketValue) || 0;
      const lvl = parseFloat(partData?.assessmentLevelPercent) || 20;
      if (mv > 0) {
        setAssessmentRollRows([{
          id: 'roll_default',
          kinds: 'LAND',
          actualUseName: partData?.actualUse || 'RESIDENTIAL',
          marketValue: mv,
          assessmentLevelPercent: lvl,
          assessedValue: Math.round(mv * (lvl / 100) * 100) / 100
        }]);
      } else {
        setAssessmentRollRows([]);
      }
    } else {
      setAssessmentRollRows([]);
    }
  };

  const hydrateTab5 = (p) => {
    if (!p) return;
    if (p.effectivityQuarter || p.EffectivityQuarter || p.quarterEffectivity || p.QuarterEffectivity) {
      const rawQ = (p.effectivityQuarter || p.EffectivityQuarter || p.quarterEffectivity || p.QuarterEffectivity).toString();
      const cleanQ = rawQ.includes('Quarter') ? rawQ : `${rawQ}${rawQ === '1' ? 'st' : rawQ === '2' ? 'nd' : rawQ === '3' ? 'rd' : 'th'} Quarter`;
      setQuarterEffectivity(cleanQ);
    }
    if (p.effectivityYear != null || p.EffectivityYear != null || p.yearEffectivity != null || p.YearEffectivity != null) {
      setYearEffectivity((p.effectivityYear ?? p.EffectivityYear ?? p.yearEffectivity ?? p.YearEffectivity).toString());
    }
    if (p.taxability !== undefined && p.taxability !== null) {
      setTaxability(typeof p.taxability === 'boolean' ? p.taxability : String(p.taxability).toLowerCase().includes('taxable'));
    } else if (p.Taxability !== undefined && p.Taxability !== null) {
      setTaxability(typeof p.Taxability === 'boolean' ? p.Taxability : String(p.Taxability).toLowerCase().includes('taxable'));
    }
    if (p.assessmentBasis || p.AssessmentBasis) {
      setAssessmentBasis(p.assessmentBasis || p.AssessmentBasis);
    }
    if (p.backTaxesYearFrom != null || p.BackTaxesYearFrom != null || p.backTaxesFrom != null) {
      setBackTaxesFrom((p.backTaxesYearFrom ?? p.BackTaxesYearFrom ?? p.backTaxesFrom).toString());
    }
    if (p.backTaxesYearTo != null || p.BackTaxesYearTo != null || p.backTaxesTo != null) {
      setBackTaxesTo((p.backTaxesYearTo ?? p.BackTaxesYearTo ?? p.backTaxesTo).toString());
    }
    if (p.memoranda || p.Memoranda) {
      setMemoranda(p.memoranda || p.Memoranda);
    }
    if (p.remarks || p.Remarks) {
      setRemarks(p.remarks || p.Remarks);
    }
  };

  useEffect(() => {
    loadLandData();
  }, []);

  const loadLandData = async () => {
    setNotDownloadedError(false);

    // 1. Check if downloaded offline copy exists and populate immediately
    const downloaded = getDownloadedFormData(trackingNoParam, formWorkflowIdParam, partitionIndexParam);
    if (downloaded) {
      if (downloaded.owner) setOwnerName(downloaded.owner);
      if (downloaded.arpNo) setArpNo(downloaded.arpNo);
      if (downloaded.tdnId) setTdnId(downloaded.tdnId);
      if (downloaded.sketchUrl) setSketchUrl(downloaded.sketchUrl);
      if (downloaded.attachments && Array.isArray(downloaded.attachments)) setAttachments(downloaded.attachments);
      if (downloaded.trackingNo && !trackingNo) setTrackingNo(downloaded.trackingNo);
      if (downloaded.transItem?.TrackingNo && !trackingNo) setTrackingNo(downloaded.transItem.TrackingNo);

      // Populate partition base properties first
      if (downloaded.currentPartition) {
        const cp = downloaded.currentPartition;
        if (cp.systemGeneratedTDN) setDeclarationNo(cp.systemGeneratedTDN);
        if (cp.arpNo) setArpNo(cp.arpNo);
        if (cp.pin) setPin(cp.pin);
        if (cp.lotNo || cp.lotNumber) setLotNo(cp.lotNo || cp.lotNumber);
        if (cp.blockNo || cp.blockNumber) setBlockNo(cp.blockNo || cp.blockNumber);
        setSectionNo(cp.sectionNo || '');
        setParcelNo(cp.parcelNo || '');
        setSurveyNo(cp.surveyNo || cp.surveyNumber || '');
        setCadastralSurveyNo(cp.cadastralSurveyNo || '');
        setTctNo(cp.titleNo || '');
        if (cp.dateRegistered) setTctDate(cp.dateRegistered.substring(0, 10));
        if (cp.registrationStatus) setRegistrationStatus(cleanRegStatus(cp.registrationStatus));
        if (cp.titleType) setTitleType(cleanTitleType(cp.titleType));
        if (cp.streetLocation || cp.streetNo || cp.buildingNo) {
          setStreetLocation(cp.streetLocation || cp.streetNo || cp.buildingNo);
        }
        if (cp.purok) setPurok(cp.purok);
        if (cp.districtOrBarangay) setDistrictOrBarangay(normalizeBarangay(cp.districtOrBarangay));
        if (cp.cityMunicipality) setCityMunicipality(cp.cityMunicipality);
        if (cp.province) setProvince(cp.province);
        if (cp.ownerName || cp.newOwnerName) setOwnerName(cp.ownerName || cp.newOwnerName);
        if (cp.ownerAddress) setOwnerAddress(cp.ownerAddress);
        if (cp.ownerTin || cp.ownerTIN) setOwnerTin(cp.ownerTin || cp.ownerTIN);
        if (cp.ownerContactNo) setOwnerContact(cp.ownerContactNo);
        if (cp.ownerEmail) setOwnerEmail(cp.ownerEmail);
        if (cp.ownerGender) setOwnerGender(cp.ownerGender);
        setNorthBoundary(cp.boundaryNorth || '');
        setNRemarks(cp.nremarks || '');
        setSouthBoundary(cp.boundarySouth || '');
        setSRemarks(cp.sremarks || '');
        setEastBoundary(cp.boundaryEast || '');
        setERemarks(cp.eremarks || '');
        setWestBoundary(cp.boundaryWest || '');
        setWRemarks(cp.wremarks || '');
        hydrateSchedules(cp, downloaded.formData?.TDNLand);
        hydrateTab5(downloaded.formData?.TDN || cp);
      }

      // If formData exists (holding the latest offline edits), populate it second so offline changes take priority
      if (downloaded.formData) {
        populateFieldsFromDTO(downloaded.formData);
        if (downloaded.formData.TDNLand) {
          hydrateSchedules(downloaded.currentPartition, downloaded.formData.TDNLand);
        }
        if (downloaded.formData.TDN) {
          hydrateTab5(downloaded.formData.TDN);
        }
        if (downloaded.currentPartition?.systemGeneratedTDN) setDeclarationNo(downloaded.currentPartition.systemGeneratedTDN);
      }
    }

    // 2. If online and no unsynced offline edits, fetch latest live partition data
    const hasUnsynced = typeof hasPendingEdits === 'function' && hasPendingEdits(trackingNoParam, formWorkflowIdParam);
    if (!hasUnsynced && (trackingNoParam || formWorkflowIdParam)) {
      try {
        if (!downloaded) setLoading(true);
        const liveRes = await MobileApi.downloadTransactionDetails(formWorkflowIdParam, trackingNoParam, partitionIndexParam);
        if (liveRes && liveRes.success) {
          const targetIdx = parseInt(partitionIndexParam, 10) || 0;
          const livePart = (liveRes.partitions && liveRes.partitions[targetIdx]) || null;
          const liveAtts = livePart?.attachments || liveRes.attachments || [];
          const liveSketch = livePart?.sketchUrl || liveRes.sketchUrl;

          if (liveSketch) setSketchUrl(liveSketch);
          if (Array.isArray(liveAtts)) setAttachments(liveAtts);
          if (liveRes.tdnId) setTdnId(liveRes.tdnId);

          if (livePart) {
            if (livePart.tdnId) setTdnId(livePart.tdnId);
            if (livePart.tdnLandId) setTdnLandId(livePart.tdnLandId);
            else if (livePart.formData?.TDNLand?.TDNLandId) setTdnLandId(livePart.formData.TDNLand.TDNLandId);
            if (livePart.systemGeneratedTDN) setDeclarationNo(livePart.systemGeneratedTDN);
            if (livePart.arpNo) setArpNo(livePart.arpNo);
            if (livePart.pin) setPin(livePart.pin);
            if (livePart.blockNo || livePart.blockNumber) setBlockNo(livePart.blockNo || livePart.blockNumber);
            if (livePart.dateRegistered) setTctDate(livePart.dateRegistered.substring(0, 10));
            if (livePart.registrationStatus) setRegistrationStatus(cleanRegStatus(livePart.registrationStatus));
            if (livePart.titleType) setTitleType(cleanTitleType(livePart.titleType));
            setSectionNo(livePart.sectionNo || '');
            if (livePart.lotNo || livePart.lotNumber) setLotNo(livePart.lotNo || livePart.lotNumber);
            setParcelNo(livePart.parcelNo || '');
            setSurveyNo(livePart.surveyNo || livePart.surveyNumber || '');
            setTctNo(livePart.titleNo || '');
            setCadastralSurveyNo(livePart.cadastralSurveyNo || '');
            if (livePart.streetLocation || livePart.streetNo || livePart.buildingNo) {
              setStreetLocation(livePart.streetLocation || livePart.streetNo || livePart.buildingNo);
            }
            if (livePart.purok) setPurok(livePart.purok);
            if (livePart.districtOrBarangay) setDistrictOrBarangay(normalizeBarangay(livePart.districtOrBarangay));
            if (livePart.cityMunicipality) setCityMunicipality(livePart.cityMunicipality);
            if (livePart.province) setProvince(livePart.province);
            if (livePart.ownerName || livePart.newOwnerName) setOwnerName(livePart.ownerName || livePart.newOwnerName);
            if (livePart.ownerAddress) setOwnerAddress(livePart.ownerAddress);
            if (livePart.ownerTin || livePart.ownerTIN) setOwnerTin(livePart.ownerTin || livePart.ownerTIN);
            if (livePart.ownerContactNo) setOwnerContact(livePart.ownerContactNo);
            if (livePart.ownerEmail) setOwnerEmail(livePart.ownerEmail);
            if (livePart.ownerGender) setOwnerGender(livePart.ownerGender);
            setNorthBoundary(livePart.boundaryNorth || '');
            setNRemarks(livePart.nremarks || '');
            setSouthBoundary(livePart.boundarySouth || '');
            setSRemarks(livePart.sremarks || '');
            setEastBoundary(livePart.boundaryEast || '');
            setERemarks(livePart.eremarks || '');
            setWestBoundary(livePart.boundaryWest || '');
            setWRemarks(livePart.wremarks || '');
            // Strictly hydrate schedules from this partition's own formData DTO, NEVER from top-level/Partition 1
            hydrateSchedules(livePart, livePart.formData?.TDNLand);
            hydrateTab5(livePart);
          }

          // Update downloadedForms cache seamlessly
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

  const populateFieldsFromDTO = (d) => {
    if (d.TDN) {
      const t = d.TDN;
      if (t.TDNId) setTdnId(t.TDNId);
      if (t.DeclarationNo) setDeclarationNo(t.DeclarationNo);
      if (t.ARPNo) setArpNo(t.ARPNo);
      if (t.PIN) setPin(t.PIN);
      if (t.DisplayName) setOwnerName(t.DisplayName);
      if (t.OwnerAddress) setOwnerAddress(t.OwnerAddress);
      const hasAdmin = !!t.IsAdministrator || !!t.AdministratorName || !!t.PropAdminName;
      const hasBenef = !hasAdmin && !!t.IsBeneficialUser;
      const hasPoss = !hasAdmin && !hasBenef && !!t.IsPossessor;
      setIsAdministrator(hasAdmin);
      setIsBeneficialUser(hasBenef);
      setIsPossessor(hasPoss);
      if (t.AdministratorName) {
        setAdminName(t.AdministratorName);
      } else if (t.PropAdminName) {
        setAdminName(t.PropAdminName);
      }
      if (t.AdministratorGender) setAdminGender(t.AdministratorGender);
      if (t.AdministratorTin) setAdminTin(t.AdministratorTin);
      if (t.AdministratorContactNo || t.PropAdminContactNo) setAdminContact(t.AdministratorContactNo || t.PropAdminContactNo);
      if (t.AdministratorAddress || t.PropAdminAddress) setAdminAddress(t.AdministratorAddress || t.PropAdminAddress);
      if (t.AdministratorEmail) setAdminEmail(t.AdministratorEmail);
      if (t.PropLocationStreet) setStreetLocation(t.PropLocationStreet);
      else if (t.Location && !t.Location.includes(',')) setStreetLocation(t.Location);
      if (t.CadastralSurveyNo) setCadastralSurveyNo(t.CadastralSurveyNo);
      if (t.LotNo) setLotNo(t.LotNo);
      if (t.BlockNo) setBlockNo(t.BlockNo);
      if (t.SectionNo) setSectionNo(t.SectionNo);
      if (t.ParcelNo) setParcelNo(t.ParcelNo);
      if (t.SurveyNo) setSurveyNo(t.SurveyNo);
      if (t.TCTNo || t.TitleNo) setTctNo(t.TCTNo || t.TitleNo);
      if (t.DateRegistered) setTctDate(t.DateRegistered.toString().substring(0, 10));
      else if (t.TCTDate) setTctDate(t.TCTDate.toString().substring(0, 10));
      else if (t.DateRegistration) setTctDate(t.DateRegistration.toString().substring(0, 10));
      if (t.RegistrationStatus) setRegistrationStatus(cleanRegStatus(t.RegistrationStatus));
      if (t.TitleType) setTitleType(cleanTitleType(t.TitleType));
      if (t.TransactionCode) setTransactionCode(t.TransactionCode);
      if (t.YearEffectivity != null) setYearEffectivity(t.YearEffectivity.toString());
      if (t.QuarterEffectivity != null) {
        const qVal = t.QuarterEffectivity.toString();
        setQuarterEffectivity(qVal.includes('Quarter') ? qVal : `${qVal}${qVal === '1' ? 'st' : qVal === '2' ? 'nd' : qVal === '3' ? 'rd' : 'th'} Quarter`);
      }
      if (t.TotalArea != null) setArea(t.TotalArea.toString());
      if (t.Unit) setUnit(t.Unit);
      if (t.TotalMarketValue != null) setMarketValue(t.TotalMarketValue.toString());
      if (t.TotalAssessedValue != null) setAssessedValue(t.TotalAssessedValue.toString());
      if (t.Memoranda) setMemoranda(t.Memoranda);
      if (t.DistrictOrBarangay) setDistrictOrBarangay(normalizeBarangay(t.DistrictOrBarangay));
      if (t.CityMunicipality) setCityMunicipality(t.CityMunicipality);
      if (t.Province) setProvince(t.Province);
      if (t.AssessmentBasis) setAssessmentBasis(t.AssessmentBasis);
      if (t.BackTaxesYearFrom) setBackTaxesFrom(t.BackTaxesYearFrom.toString());
      if (t.BackTaxesYearTo) setBackTaxesTo(t.BackTaxesYearTo.toString());
      if (t.Taxability != null) setTaxability(!!t.Taxability);
      if (t.FloorPlanSketchPath) setSketchUrl(t.FloorPlanSketchPath);
      const rawAtts = t.Attachments || t.attachments;
      if (rawAtts && Array.isArray(rawAtts) && rawAtts.length > 0) setAttachments(rawAtts);
    }

    if (d.currentPartition) {
      const p = d.currentPartition;
      if (p.systemGeneratedTDN) setDeclarationNo(p.systemGeneratedTDN);
      if (p.arpNo) setArpNo(p.arpNo);
      if (p.pin) setPin(p.pin);
      if (p.lotNo || p.lotNumber) setLotNo(p.lotNo || p.lotNumber);
      if (p.blockNo || p.blockNumber) setBlockNo(p.blockNo || p.blockNumber);
      if (p.dateRegistered) setTctDate(p.dateRegistered.toString().substring(0, 10));
      else if (p.tctDate) setTctDate(p.tctDate.toString().substring(0, 10));
      if (p.registrationStatus) setRegistrationStatus(cleanRegStatus(p.registrationStatus));
      if (p.titleType) setTitleType(cleanTitleType(p.titleType));
      setSectionNo(p.sectionNo || '');
      setParcelNo(p.parcelNo || '');
      setSurveyNo(p.surveyNo || p.surveyNumber || '');
      setTctNo(p.titleNo || '');
      if (p.streetLocation || p.streetNo || p.buildingNo) setStreetLocation(p.streetLocation || p.streetNo || p.buildingNo);
      if (p.purok) setPurok(p.purok);
      if (p.districtOrBarangay) setDistrictOrBarangay(normalizeBarangay(p.districtOrBarangay));
      if (p.newOwnerName || p.ownerName) setOwnerName(p.newOwnerName || p.ownerName);
      const pAdmin = !!p.isAdministrator || !!p.administratorName;
      const pBenef = !pAdmin && !!p.isBeneficialUser;
      const pPoss = !pAdmin && !pBenef && !!p.isPossessor;
      setIsAdministrator(pAdmin);
      setIsBeneficialUser(pBenef);
      setIsPossessor(pPoss);
      if (p.administratorName) {
        setAdminName(p.administratorName);
      }
      if (p.administratorGender) setAdminGender(p.administratorGender);
      if (p.administratorTin) setAdminTin(p.administratorTin);
      if (p.administratorContactNo) setAdminContact(p.administratorContactNo);
      if (p.administratorAddress) setAdminAddress(p.administratorAddress);
      if (p.administratorEmail) setAdminEmail(p.administratorEmail);
      if (p.ownerAddress) setOwnerAddress(p.ownerAddress);
      if (p.ownerTIN || p.ownerTin) setOwnerTin(p.ownerTIN || p.ownerTin);
      if (p.ownerContactNo) setOwnerContact(p.ownerContactNo);
      if (p.ownerEmail) setOwnerEmail(p.ownerEmail);
      if (p.ownerGender) setOwnerGender(p.ownerGender);
      if (p.boundaryNorth) setNorthBoundary(p.boundaryNorth);
      if (p.nremarks) setNRemarks(p.nremarks);
      if (p.boundarySouth) setSouthBoundary(p.boundarySouth);
      if (p.sremarks) setSRemarks(p.sremarks);
      if (p.boundaryEast) setEastBoundary(p.boundaryEast);
      if (p.eremarks) setERemarks(p.eremarks);
      if (p.boundaryWest) setWestBoundary(p.boundaryWest);
      if (p.wremarks) setWRemarks(p.wremarks);
      if (p.areaSqMeters) setArea(p.areaSqMeters.toString());
      if (p.marketValue) setMarketValue(p.marketValue.toString());
      if (p.assessedValue) setAssessedValue(p.assessedValue.toString());
      hydrateTab5(p);
    }

    if (d.refOwner) {
      const o = d.refOwner;
      if (!ownerName && (o.OwnerName || o.FullName)) setOwnerName(o.OwnerName || o.FullName);
      if (o.TaxIdentNo) setOwnerTin(o.TaxIdentNo);
      if (o.ContactNo) setOwnerContact(o.ContactNo);
      if (o.Email) setOwnerEmail(o.Email);
    }

    if (d.TDNLand) {
      const l = d.TDNLand;
      if (l.TDNLandId) setTdnLandId(l.TDNLandId);
      setNorthBoundary(l.NorthBoundary || '');
      setNRemarks(l.Nremarks || '');
      setSouthBoundary(l.SouthBoundary || '');
      setSRemarks(l.Sremarks || '');
      setEastBoundary(l.EastBoundary || '');
      setERemarks(l.Eremarks || '');
      setWestBoundary(l.WestBoundary || '');
      setWRemarks(l.Wremarks || '');
    }

    if (d.TDNLandDetails && d.TDNLandDetails.length > 0) {
      const first = d.TDNLandDetails[0];
      if (first.Area != null && !area) setArea(first.Area.toString());
      if (first.Unit) setUnit(first.Unit);
      if (first.BaseMarketValue != null) setBaseMarketValue(first.BaseMarketValue.toString());
      if (first.TotalMarketValue != null && !marketValue) setMarketValue(first.TotalMarketValue.toString());
      if (first.AssessmentLevel != null) setAssessmentLevel(first.AssessmentLevel.toString());
      if (first.TotalAssessedValue != null && !assessedValue) setAssessedValue(first.TotalAssessedValue.toString());
      if (first.ClassificationName) setClassification(first.ClassificationName);
      if (first.ActualUse) setActualUse(first.ActualUse);
    }
  };

  const handleAreaChange = (val) => {
    setArea(val);
    const a = parseFloat(val) || 0;
    const b = parseFloat(baseMarketValue) || 0;
    if (a > 0 && b > 0) {
      const mv = a * b;
      setMarketValue(mv.toFixed(2));
      const al = (parseFloat(assessmentLevel) || 20) / 100;
      setAssessedValue((mv * al).toFixed(2));
    }
  };

  // Table 1 (Classification Rows) Actions
  const openAddClassRow = () => {
    setEditingClassIndex(null);
    setEditingClassRow({
      id: 'c_' + Date.now(),
      classificationId: 1,
      classificationName: 'RESIDENTIAL',
      subClassId: 1,
      subClassName: '1st Class Residential',
      actualUseName: 'RESIDENTIAL',
      areaSqMeters: '100',
      unitValue: '520',
      marketValue: 52000
    });
    setEditClassModalVisible(true);
  };

  const openEditClassRow = (idx) => {
    setEditingClassIndex(idx);
    const r = classificationRows[idx];
    setEditingClassRow({
      ...r,
      areaSqMeters: r.areaSqMeters !== undefined && r.areaSqMeters !== null ? String(r.areaSqMeters) : '',
      unitValue: r.unitValue !== undefined && r.unitValue !== null ? String(r.unitValue) : ''
    });
    setEditClassModalVisible(true);
  };

  const saveClassRow = () => {
    if (!editingClassRow) return;
    const a = parseFloat(editingClassRow.areaSqMeters) || 0;
    const u = parseFloat(editingClassRow.unitValue) || 0;
    const item = {
      ...editingClassRow,
      areaSqMeters: a,
      unitValue: u,
      marketValue: Math.round(a * u * 100) / 100
    };
    if (editingClassIndex !== null) {
      const updated = [...classificationRows];
      updated[editingClassIndex] = item;
      setClassificationRows(updated);
    } else {
      setClassificationRows([...classificationRows, item]);
    }
    setEditClassModalVisible(false);
  };

  const addClassificationRow = () => {
    openAddClassRow();
  };

  const updateClassificationRow = (index, updates) => {
    const updated = [...classificationRows];
    const item = { ...updated[index], ...updates };
    const a = parseFloat(item.areaSqMeters) || 0;
    const u = parseFloat(item.unitValue) || 0;
    item.marketValue = Math.round(a * u * 100) / 100;
    updated[index] = item;
    setClassificationRows(updated);
  };

  const removeClassificationRow = (index) => {
    if (classificationRows.length <= 1) {
      Alert.alert('Required', 'At least one land classification schedule row is required.');
      return;
    }
    setClassificationRows(classificationRows.filter((_, idx) => idx !== index));
  };

  // Table 2 (Improvements Rows) Actions
  const openAddImpRow = () => {
    const defaultType = LAND_IMPROVEMENT_TYPES[0];
    setEditingImpIndex(null);
    setEditingImpRow({
      id: 'imp_' + Date.now(),
      typeId: defaultType.id,
      typeDescription: defaultType.description,
      isFruitBearing: true,
      quantity: '10',
      ageYears: '5',
      unitValue: String(defaultType.baseValue),
      marketValue: 10 * defaultType.baseValue
    });
    setEditImpModalVisible(true);
  };

  const openEditImpRow = (idx) => {
    setEditingImpIndex(idx);
    const r = improvementRows[idx];
    setEditingImpRow({
      ...r,
      quantity: r.quantity !== undefined && r.quantity !== null ? String(r.quantity) : '',
      ageYears: r.ageYears !== undefined && r.ageYears !== null ? String(r.ageYears) : '',
      unitValue: r.unitValue !== undefined && r.unitValue !== null ? String(r.unitValue) : ''
    });
    setEditImpModalVisible(true);
  };

  const saveImpRow = () => {
    if (!editingImpRow) return;
    const q = parseFloat(editingImpRow.quantity) || 0;
    const u = parseFloat(editingImpRow.unitValue) || 0;
    const age = parseInt(editingImpRow.ageYears) || 0;
    const item = {
      ...editingImpRow,
      quantity: q,
      ageYears: age,
      unitValue: u,
      marketValue: Math.round(q * u * 100) / 100
    };
    if (editingImpIndex !== null) {
      const updated = [...improvementRows];
      updated[editingImpIndex] = item;
      setImprovementRows(updated);
    } else {
      setImprovementRows([...improvementRows, item]);
    }
    setEditImpModalVisible(false);
  };

  const addImprovementRow = () => {
    openAddImpRow();
  };

  const updateImprovementRow = (index, updates) => {
    const updated = [...improvementRows];
    const item = { ...updated[index], ...updates };
    const q = parseFloat(item.quantity) || 0;
    const u = parseFloat(item.unitValue) || 0;
    item.marketValue = Math.round(q * u * 100) / 100;
    updated[index] = item;
    setImprovementRows(updated);
  };

  const removeImprovementRow = (index) => {
    setImprovementRows(improvementRows.filter((_, idx) => idx !== index));
  };

  // Table 3 (Adjustment Rows) Actions
  const openAddAdjRow = () => {
    const defaultFactor = LAND_ADJUSTMENT_FACTORS[0];
    const baseMv = totalBaseMarketValue > 0 ? totalBaseMarketValue : 78000;
    const valAdj = Math.round(baseMv * (defaultFactor.percent / 100) * 100) / 100;
    setEditingAdjIndex(null);
    setEditingAdjRow({
      id: 'adj_' + Date.now(),
      classificationName: classificationRows[0]?.classificationName || 'RESIDENTIAL',
      baseMarketValue: String(baseMv),
      adjustmentFactorId: defaultFactor.id,
      adjustmentFactorDescription: defaultFactor.description,
      percentAdjustment: String(defaultFactor.percent),
      valueAdjustment: valAdj,
      adjustedMarketValue: Math.round((baseMv + valAdj) * 100) / 100
    });
    setEditAdjModalVisible(true);
  };

  const openEditAdjRow = (idx) => {
    setEditingAdjIndex(idx);
    const r = adjustmentRows[idx];
    setEditingAdjRow({
      ...r,
      baseMarketValue: r.baseMarketValue !== undefined && r.baseMarketValue !== null ? String(r.baseMarketValue) : '',
      percentAdjustment: r.percentAdjustment !== undefined && r.percentAdjustment !== null ? String(r.percentAdjustment) : ''
    });
    setEditAdjModalVisible(true);
  };

  const saveAdjRow = () => {
    if (!editingAdjRow) return;
    const baseMv = parseFloat(editingAdjRow.baseMarketValue) || 0;
    const pct = parseFloat(editingAdjRow.percentAdjustment) || 0;
    const valAdj = Math.round(baseMv * (pct / 100) * 100) / 100;
    const item = {
      ...editingAdjRow,
      baseMarketValue: baseMv,
      percentAdjustment: pct,
      valueAdjustment: valAdj,
      adjustedMarketValue: Math.round((baseMv + valAdj) * 100) / 100
    };
    if (editingAdjIndex !== null) {
      const updated = [...adjustmentRows];
      updated[editingAdjIndex] = item;
      setAdjustmentRows(updated);
    } else {
      setAdjustmentRows([...adjustmentRows, item]);
    }
    setEditAdjModalVisible(false);
  };

  const addAdjustmentRow = () => {
    openAddAdjRow();
  };

  const updateAdjustmentRow = (index, updates) => {
    const updated = [...adjustmentRows];
    const item = { ...updated[index], ...updates };
    const baseMv = parseFloat(item.baseMarketValue) || 0;
    const pct = parseFloat(item.percentAdjustment) || 0;
    const valAdj = Math.round(baseMv * (pct / 100) * 100) / 100;
    item.valueAdjustment = valAdj;
    item.adjustedMarketValue = Math.round((baseMv + valAdj) * 100) / 100;
    updated[index] = item;
    setAdjustmentRows(updated);
  };

  const removeAdjustmentRow = (index) => {
    setAdjustmentRows(adjustmentRows.filter((_, idx) => idx !== index));
  };

  const syncAdjustmentsFromClassifications = () => {
    if (classificationRows.length === 0) return;
    const map = {};
    classificationRows.forEach(r => {
      const name = r.classificationName || 'RESIDENTIAL';
      map[name] = (map[name] || 0) + (parseFloat(r.marketValue) || 0);
    });
    const defaultFactor = LAND_ADJUSTMENT_FACTORS[0];
    const newRows = Object.keys(map).map((k, idx) => {
      const baseMv = map[k];
      return {
        id: 'adj_sync_' + Date.now() + '_' + idx,
        classificationName: k,
        baseMarketValue: baseMv,
        adjustmentFactorId: defaultFactor.id,
        adjustmentFactorDescription: defaultFactor.description,
        percentAdjustment: 0,
        valueAdjustment: 0,
        adjustedMarketValue: baseMv
      };
    });
    setAdjustmentRows(newRows);
  };

  // Table 4 (Assessment Roll Rows) Actions
  const openAddRollRow = () => {
    const mv = totalPropertyMarketValue > 0 ? totalPropertyMarketValue : 78000;
    const lvl = 20;
    setEditingRollIndex(null);
    setEditingRollRow({
      id: 'roll_' + Date.now(),
      kinds: 'LAND',
      actualUseName: 'RESIDENTIAL',
      marketValue: String(mv),
      assessmentLevelPercent: String(lvl),
      assessedValue: Math.round(mv * (lvl / 100) * 100) / 100
    });
    setEditRollModalVisible(true);
  };

  const openEditRollRow = (idx) => {
    setEditingRollIndex(idx);
    const r = assessmentRollRows[idx];
    setEditingRollRow({
      ...r,
      marketValue: r.marketValue !== undefined && r.marketValue !== null ? String(r.marketValue) : '',
      assessmentLevelPercent: r.assessmentLevelPercent !== undefined && r.assessmentLevelPercent !== null ? String(r.assessmentLevelPercent) : ''
    });
    setEditRollModalVisible(true);
  };

  const saveRollRow = () => {
    if (!editingRollRow) return;
    const mv = parseFloat(editingRollRow.marketValue) || 0;
    const lvl = parseFloat(editingRollRow.assessmentLevelPercent) || 0;
    const item = {
      ...editingRollRow,
      marketValue: mv,
      assessmentLevelPercent: lvl,
      assessedValue: Math.round(mv * (lvl / 100) * 100) / 100
    };
    if (editingRollIndex !== null) {
      const updated = [...assessmentRollRows];
      updated[editingRollIndex] = item;
      setAssessmentRollRows(updated);
    } else {
      setAssessmentRollRows([...assessmentRollRows, item]);
    }
    setEditRollModalVisible(false);
  };

  const addAssessmentRollRow = () => {
    openAddRollRow();
  };

  const updateAssessmentRollRow = (index, updates) => {
    const updated = [...assessmentRollRows];
    const item = { ...updated[index], ...updates };
    const mv = parseFloat(item.marketValue) || 0;
    const lvl = parseFloat(item.assessmentLevelPercent) || 0;
    item.assessedValue = Math.round(mv * (lvl / 100) * 100) / 100;
    updated[index] = item;
    setAssessmentRollRows(updated);
  };

  const removeAssessmentRollRow = (index) => {
    setAssessmentRollRows(assessmentRollRows.filter((_, idx) => idx !== index));
  };

  const autoGenerateAssessmentRoll = () => {
    const rows = [];
    const landMv = effectiveLandMarketValue > 0 ? effectiveLandMarketValue : totalBaseMarketValue;
    if (landMv > 0) {
      const actUse = classificationRows[0]?.actualUseName || 'RESIDENTIAL';
      const lvl = getAssessmentLevelForActualUse(actUse);
      rows.push({
        id: 'roll_land_' + Date.now(),
        kinds: 'LAND',
        actualUseName: actUse,
        marketValue: landMv,
        assessmentLevelPercent: lvl,
        assessedValue: Math.round(landMv * (lvl / 100) * 100) / 100
      });
    }
    if (totalImprovementsMarketValue > 0) {
      rows.push({
        id: 'roll_imp_' + Date.now(),
        kinds: 'PLANTS / TREES',
        actualUseName: 'AGRICULTURAL',
        marketValue: totalImprovementsMarketValue,
        assessmentLevelPercent: 40,
        assessedValue: Math.round(totalImprovementsMarketValue * 0.40 * 100) / 100
      });
    }
    if (rows.length === 0) {
      rows.push({
        id: 'roll_default_' + Date.now(),
        kinds: 'LAND',
        actualUseName: 'RESIDENTIAL',
        marketValue: 78000,
        assessmentLevelPercent: 20,
        assessedValue: 15600
      });
    }
    setAssessmentRollRows(rows);
  };

  // Computed Totals for all 4 tables
  const totalLandArea = classificationRows.reduce((sum, r) => sum + (parseFloat(r.areaSqMeters) || 0), 0);
  const totalBaseMarketValue = classificationRows.reduce((sum, r) => sum + (parseFloat(r.marketValue) || 0), 0);
  const totalImprovementsMarketValue = improvementRows.reduce((sum, r) => sum + (parseFloat(r.marketValue) || 0), 0);
  const totalAdjustedMarketValue = adjustmentRows.reduce((sum, r) => sum + (parseFloat(r.adjustedMarketValue) || 0), 0);
  const totalPercentAdjustment = adjustmentRows.reduce((sum, r) => sum + (parseFloat(r.percentAdjustment) || 0), 0);

  const effectiveLandMarketValue = adjustmentRows.length > 0 ? totalAdjustedMarketValue : totalBaseMarketValue;
  const totalAssessmentRollMarketValue = assessmentRollRows.reduce((sum, r) => sum + (parseFloat(r.marketValue) || 0), 0);
  const totalAssessmentRollAssessedValue = assessmentRollRows.reduce((sum, r) => sum + (parseFloat(r.assessedValue) || 0), 0);

  const totalPropertyMarketValue = assessmentRollRows.length > 0
    ? totalAssessmentRollMarketValue
    : (effectiveLandMarketValue + totalImprovementsMarketValue);

  const totalPropertyAssessedValue = assessmentRollRows.length > 0
    ? totalAssessmentRollAssessedValue
    : Math.round(totalPropertyMarketValue * (parseFloat(assessmentLevel) || 20) / 100 * 100) / 100;

  const numMarketValue = totalPropertyMarketValue;
  const numAssessedValue = totalPropertyAssessedValue;
  const numBasicRpt = Math.round(totalPropertyAssessedValue * 0.02 * 100) / 100;
  const numAnnualTax = Math.round(totalPropertyAssessedValue * 0.03 * 100) / 100;

  const navigateBack = () => {
    setDiscardModalVisible(false);
    if (navigation && typeof navigation.navigate === 'function') {
      navigation.navigate('Main', { screen: 'Feed' });
    } else if (navigation && typeof navigation.goBack === 'function') {
      navigation.goBack();
    }
  };

  const [completeModalConfig, setCompleteModalConfig] = useState({
    visible: false,
    title: '',
    message: '',
    icon: 'cloud-done-outline',
    iconType: 'success'
  });

  const showCompleteModalAndGoBack = (title, message, iconType = 'success', icon = 'cloud-done-outline') => {
    setCompleteModalConfig({
      visible: true,
      title,
      message,
      icon,
      iconType
    });
  };

  const handleCancel = () => {
    setDiscardModalVisible(true);
  };

  const handleSave = async (forceOffline = false, transferStage = false) => {
    const qNum = quarterEffectivity.includes('1') ? 1 : quarterEffectivity.includes('2') ? 2 : quarterEffectivity.includes('3') ? 3 : 4;
    const computedUnitValue = totalLandArea > 0 ? Math.round((totalBaseMarketValue / totalLandArea) * 100) / 100 : (parseFloat(baseMarketValue) || 520);
    const mainClass = classificationRows[0]?.classificationName || classification;
    const mainActualUse = classificationRows[0]?.actualUseName || actualUse;
    const mainLvl = assessmentRollRows[0]?.assessmentLevelPercent || parseFloat(assessmentLevel) || 20;

    const payload = {
      TDN: {
        TDNId: parseInt(tdnId) || 0,
        PartitionItemIndex: parseInt(partitionIndexParam, 10) || 0,
        PartitionIndex: partitionLetterParam,
        TrackingNo: trackingNo || trackingNoParam,
        FormWorkflowId: formWorkflowIdParam,
        DeclarationNo: declarationNo,
        ARPNo: arpNo,
        PIN: pin,
        DisplayName: ownerName,
        OwnerAddress: ownerAddress,
        PropAdminName: adminName,
        PropAdminAddress: adminAddress,
        PropAdminContactNo: adminContact,
        PropLocationStreet: streetLocation,
        Location: streetLocation,
        CadastralSurveyNo: cadastralSurveyNo,
        LotNo: lotNo,
        BlockNo: blockNo,
        SectionNo: sectionNo,
        ParcelNo: parcelNo,
        SurveyNo: surveyNo,
        TCTNo: tctNo,
        TitleNo: tctNo,
        TCTDate: tctDate || null,
        DateRegistered: tctDate || null,
        RegistrationStatus: registrationStatus,
        TitleType: titleType,
        TransactionCode: transactionCode,
        TotalArea: totalLandArea > 0 ? totalLandArea : (parseFloat(area) || 0),
        Unit: unit,
        UnitValue: computedUnitValue,
        TotalMarketValue: totalPropertyMarketValue,
        TotalAssessedValue: totalPropertyAssessedValue,
        Classification: mainClass,
        ActualUse: mainActualUse,
        AssessmentLevelPercent: mainLvl,
        LandAppraisalDetailsJson: JSON.stringify(classificationRows),
        LandImprovementsJson: JSON.stringify(improvementRows),
        LandAdjustmentsJson: JSON.stringify(adjustmentRows),
        LandPropertyAssessmentsJson: JSON.stringify(assessmentRollRows),
        Purok: purok,
        OwnerTIN: ownerTin,
        OwnerContactNo: ownerContact,
        OwnerEmail: ownerEmail,
        OwnerGender: ownerGender,
        IsAdministrator: isAdministrator,
        IsBeneficialUser: isBeneficialUser,
        IsPossessor: isPossessor,
        AdministratorName: adminName,
        AdministratorAddress: adminAddress,
        AdministratorContactNo: adminContact,
        AdministratorTin: adminTin,
        AdministratorEmail: adminEmail,
        AdministratorGender: adminGender,
        FloorPlanSketchPath: sketchUrl,
        Attachments: attachments,
        YearEffectivity: parseInt(yearEffectivity) || null,
        QuarterEffectivity: qNum,
        DistrictOrBarangay: districtOrBarangay,
        CityMunicipality: cityMunicipality,
        Province: province,
        AssessmentBasis: assessmentBasis,
        BackTaxesYearFrom: parseInt(backTaxesFrom) || null,
        BackTaxesYearTo: parseInt(backTaxesTo) || null,
        Memoranda: memoranda,
        Taxability: taxability
      },
      TDNLand: {
        TDNLandId: parseInt(tdnLandId) || 0,
        TDNId: parseInt(tdnId) || 0,
        NorthBoundary: northBoundary,
        Nremarks: nRemarks,
        SouthBoundary: southBoundary,
        Sremarks: sRemarks,
        EastBoundary: eastBoundary,
        Eremarks: eRemarks,
        WestBoundary: westBoundary,
        Wremarks: wRemarks,
        Classification: mainClass,
        ActualUse: mainActualUse,
        UnitValue: computedUnitValue,
        AssessmentLevelPercent: mainLvl,
        LandAppraisalDetailsJson: JSON.stringify(classificationRows),
        LandImprovementsJson: JSON.stringify(improvementRows),
        LandAdjustmentsJson: JSON.stringify(adjustmentRows),
        LandPropertyAssessmentsJson: JSON.stringify(assessmentRollRows)
      }
    };

    const workflowForwardPayload = transferStage && formWorkflowIdParam ? {
      formWorkflowId: formWorkflowIdParam,
      nextStatusId: nextStatusId,
      remarks: remarks || 'Field land appraisal completed and transferred.'
    } : null;

    setSaving(true);

    if (forceOffline) {
      await queueOfflineUpdate(
        'UPDATE_LAND',
        payload,
        `Land Appraisal [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        workflowForwardPayload
      );
      setSaving(false);
      showCompleteModalAndGoBack(
        'Saved to Offline Queue',
        `Edits for Partition ${partitionLetterParam} have been saved locally in the offline queue and will sync to the Web Portal once you are online.`,
        'warning',
        'cloud-offline-outline'
      );
      return;
    }

    try {
      const res = await MobileApi.updateLandForm(payload);
      if (res && res.success) {
        if (workflowForwardPayload) {
          try {
            await MobileApi.forwardWorkflow(
              workflowForwardPayload.formWorkflowId,
              workflowForwardPayload.nextStatusId,
              workflowForwardPayload.remarks
            );
          } catch (fwdErr) {
            // Workflow forward handled
          }
        }
        if (typeof markTransactionSynced === 'function') {
          await markTransactionSynced(trackingNo || trackingNoParam, formWorkflowIdParam, transferStage, payload);
        }
        setSaving(false);
        showCompleteModalAndGoBack(
          'Land Appraisal Saved & Synced',
          `Partition ${partitionLetterParam} updates were successfully saved and synchronized directly with the City Assessor Web Portal.`,
          'success',
          'cloud-done-outline'
        );
        return;
      } else {
        // Server rejected live sync: preserve all data safely in the offline queue
        await queueOfflineUpdate(
          'UPDATE_LAND',
          payload,
          `Land Appraisal [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
          workflowForwardPayload
        );
        setSaving(false);
        showCompleteModalAndGoBack(
          'Saved to Offline Queue (Sync Rejected)',
          `Live sync could not complete (${res?.message || 'Server rejected the land form update'}). Your edits have been safely preserved in the offline queue and can be re-synced anytime.`,
          'warning',
          'cloud-offline-outline'
        );
        return;
      }
    } catch (err) {
      const serverMsg = err.response?.data?.message || err.response?.data || err.message || 'Server returned an error.';
      // Live sync failed with error: preserve all data safely in the offline queue
      await queueOfflineUpdate(
        'UPDATE_LAND',
        payload,
        `Land Appraisal [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        workflowForwardPayload
      );
      setSaving(false);
      showCompleteModalAndGoBack(
        'Saved to Offline Queue (Sync Failed)',
        `Live sync failed (${serverMsg}). Your edits have been safely preserved in the offline queue and can be re-synced anytime.`,
        'warning',
        'cloud-offline-outline'
      );
      return;
    }

    // Fallback: Only queue offline if completely unreachable or network disconnected
    await queueOfflineUpdate(
      'UPDATE_LAND',
      payload,
      `Land Appraisal [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
      workflowForwardPayload
    );
    setSaving(false);
    showCompleteModalAndGoBack(
      'Saved to Offline Queue',
      `Device is currently offline or the server was unreachable. Your Partition ${partitionLetterParam} edits have been stored safely in the offline queue and will automatically sync once connected.`,
      'warning',
      'cloud-offline-outline'
    );
  };

  return (
    <View style={styles.container}>
      <HeaderBar
        title="LAND APPRAISAL"
        subtitle={`Tracking No: ${trackingNo || 'N/A'} • Partition ${partitionLetterParam}${declarationNo ? ` • TDN: ${declarationNo}` : ''}`}
        navigation={navigation}
        showBack={true}
        onBackPress={handleCancel}
      />

      {/* Offline Status Ribbon */}
      <View style={[styles.ribbon, isDownloaded ? styles.ribbonDownloaded : styles.ribbonStandard]}>
        <View style={styles.ribbonLeft}>
          <Ionicons name={isDownloaded ? "shield-checkmark" : "cloud-outline"} size={16} color={isDownloaded ? (isDarkMode ? "#34d399" : "#15803d") : colors.primary} />
          <Text style={[styles.ribbonText, isDownloaded && { color: isDarkMode ? '#34d399' : '#15803d' }]}>
            {isDownloaded ? "Downloaded Form • 100% Offline Field Ready" : "Live Online Form"}
          </Text>
        </View>

        {/* Live Network Status Tag */}
        <TouchableOpacity
          style={[styles.ribbonNetTag, isOnline ? styles.ribbonNetOnline : styles.ribbonNetOffline]}
          onPress={() => checkConnectivity && checkConnectivity()}
          accessibilityLabel={`Network Status: ${isOnline ? 'Online' : 'Offline'}. Tap to test.`}
        >
          <View style={[styles.ribbonNetDot, isOnline ? styles.ribbonNetDotOnline : styles.ribbonNetDotOffline]} />
          <Text style={[styles.ribbonNetText, isOnline ? styles.ribbonNetTextOnline : styles.ribbonNetTextOffline]}>
            {isOnline ? 'ONLINE' : 'OFFLINE'}
          </Text>
        </TouchableOpacity>
      </View>

      {/* Sticky, Well-Positioned Tab Bar Header */}
      <View style={styles.topTabsWrapper}>
        {/* Banner summary bar */}
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
            <Text style={styles.bannerInfoValue}>{pin || '126-00-0000-000'}</Text>
          </View>
        </View>

        {/* Scrollable Tab Pills */}
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
                  {tab.label} {idx === 4 && attachments && attachments.length > 0 ? `(${attachments.length})` : ''}
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
              This land appraisal data has not been downloaded to your device storage. To view and inspect this form in offline mode, please connect to Wi-Fi/Mobile Data and tap "Download" in the transactions list.
            </Text>
            <TouchableOpacity style={styles.backBtn} onPress={navigateBack}>
              <Ionicons name="arrow-back" size={18} color="#ffffff" />
              <Text style={styles.backBtnText}>Return to Transactions</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            {/* ========================================================= */}
            {/* TAB 0: 1. CADASTRAL DETAILS & TITLE REGISTRATION (CAST)   */}
            {/* ========================================================= */}
            {activeTab === 0 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Cadastral Details & Title Registration (CAST Reference)</Text>

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

                  {/* Registration Status & Title Type Selectors */}
                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>REGISTRATION STATUS</Text>
                      <View style={styles.optionPillRow}>
                        {['Titled', 'Untitled'].map((status) => {
                          const isMatch = registrationStatus.toLowerCase() === status.toLowerCase();
                          return (
                            <TouchableOpacity
                              key={status}
                              style={[styles.optionPill, isMatch && styles.optionPillActive]}
                              onPress={() => setRegistrationStatus(status)}
                            >
                              {isMatch && <Ionicons name="checkmark-circle" size={13} color="#ffffff" style={{ marginRight: 3 }} />}
                              <Text style={[styles.optionPillText, isMatch && styles.optionPillTextActive]}>
                                {status === 'Titled' ? 'Titled Land' : 'Untitled'}
                              </Text>
                            </TouchableOpacity>
                          );
                        })}
                      </View>
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TITLE TYPE</Text>
                      <View style={styles.optionPillRow}>
                        {['OCT', 'TCT', 'CLOA', 'Patent'].map((tType) => {
                          const isMatch = titleType.toUpperCase() === tType.toUpperCase();
                          return (
                            <TouchableOpacity
                              key={tType}
                              style={[styles.optionPill, isMatch && styles.optionPillActive]}
                              onPress={() => setTitleType(tType)}
                            >
                              {isMatch && <Ionicons name="checkmark-circle" size={13} color="#ffffff" style={{ marginRight: 3 }} />}
                              <Text style={[styles.optionPillText, isMatch && styles.optionPillTextActive]}>
                                {tType}
                              </Text>
                            </TouchableOpacity>
                          );
                        })}
                      </View>
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>OCT / TCT / CLOA NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={tctNo}
                        onChangeText={setTctNo}
                        placeholder="T-104928"
                      />
                    </View>

                    {/* Date of Title Registration with UI Date Picker */}
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DATE OF TITLE REGISTRATION</Text>
                      <View style={styles.dateInputWrapper}>
                        <TextInput
                          placeholderTextColor={colors.placeholder}
                          style={[styles.input, styles.dateInputControl]}
                          value={tctDate}
                          onChangeText={setTctDate}
                          placeholder="YYYY-MM-DD"
                        />
                        <TouchableOpacity
                          style={styles.datePickerTriggerBtn}
                          onPress={() => setDatePickerVisible(true)}
                          activeOpacity={0.7}
                        >
                          <Ionicons name="calendar-outline" size={18} color="#ffffff" />
                        </TouchableOpacity>
                      </View>
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>CADASTRAL SURVEY NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={cadastralSurveyNo}
                        onChangeText={setCadastralSurveyNo}
                        placeholder="Cad-12-00412"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>SECTION NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={sectionNo}
                        onChangeText={setSectionNo}
                        placeholder="Section 02"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>LOT NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={lotNo}
                        onChangeText={setLotNo}
                        placeholder="Lot 10-A"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BLOCK NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={blockNo}
                        onChangeText={setBlockNo}
                        placeholder="Block 4"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PARCEL NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={parcelNo}
                        onChangeText={setParcelNo}
                        placeholder="Parcel 01"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>SURVEY NO.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={surveyNo}
                        onChangeText={setSurveyNo}
                        placeholder="Psu-12345"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>PROPERTY INDEX NUMBER (PIN)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={pin}
                        onChangeText={setPin}
                        placeholder="126-00-0000-000"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TRANSACTION CODE</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={transactionCode}
                        onChangeText={setTransactionCode}
                        placeholder="e.g. TR-LAND"
                      />
                    </View>
                  </View>
                </View>
              </View>
            )}

            {/* ========================================================= */}
            {/* TAB 1: 2. LOCATION & BOUNDARIES (CAST)                    */}
            {/* ========================================================= */}
            {activeTab === 1 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Location Details (City of Koronadal Standard)</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>BUILDING NO. / STREET / ROAD</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={streetLocation}
                      onChangeText={setStreetLocation}
                      placeholder="e.g. Pioneer Avenue, Corner Salazar St."
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PUROK / SITIO</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={purok}
                        onChangeText={setPurok}
                        placeholder="e.g. Purok Malakas"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BARANGAY *</Text>
                      <TouchableOpacity
                        style={styles.selectBtn}
                        onPress={() => setBarangayModalVisible(true)}
                        activeOpacity={0.7}
                      >
                        <Text style={[styles.selectBtnText, !districtOrBarangay && { color: colors.placeholder }]} numberOfLines={1}>
                          {districtOrBarangay || 'Select Barangay...'}
                        </Text>
                        <Ionicons name="chevron-down" size={16} color={colors.textMuted} />
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>CITY / MUNICIPALITY</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={cityMunicipality || 'Koronadal City'}
                        editable={false}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PROVINCE</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={province || 'South Cotabato'}
                        editable={false}
                      />
                    </View>
                  </View>
                </View>

                {/* Boundaries & Remarks Card */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Boundaries & Adjacent Lots (with Boundary Remarks)</Text>

                  {/* North */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>NORTH BOUNDARY</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={northBoundary}
                      onChangeText={setNorthBoundary}
                      placeholder="Lot 101"
                    />
                    <Text style={[styles.inputLabel, { marginTop: 4, fontSize: 11 }]}>NORTH REMARKS</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={nRemarks}
                      onChangeText={setNRemarks}
                      placeholder="e.g. Juan dela Cruz (Lot 101)"
                    />
                  </View>

                  {/* South */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>SOUTH BOUNDARY</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={southBoundary}
                      onChangeText={setSouthBoundary}
                      placeholder="Lot 103"
                    />
                    <Text style={[styles.inputLabel, { marginTop: 4, fontSize: 11 }]}>SOUTH REMARKS</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={sRemarks}
                      onChangeText={setSRemarks}
                      placeholder="e.g. Existing Residential Lot"
                    />
                  </View>

                  {/* East */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>EAST BOUNDARY</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={eastBoundary}
                      onChangeText={setEastBoundary}
                      placeholder="Road Lot"
                    />
                    <Text style={[styles.inputLabel, { marginTop: 4, fontSize: 11 }]}>EAST REMARKS</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={eRemarks}
                      onChangeText={setERemarks}
                      placeholder="e.g. 10m Wide Concrete Road"
                    />
                  </View>

                  {/* West */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>WEST BOUNDARY</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={westBoundary}
                      onChangeText={setWestBoundary}
                      placeholder="Creek / Lot 104"
                    />
                    <Text style={[styles.inputLabel, { marginTop: 4, fontSize: 11 }]}>WEST REMARKS</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={wRemarks}
                      onChangeText={setWRemarks}
                      placeholder="e.g. Natural Drainage Creek"
                    />
                  </View>
                </View>

                {/* 2nd Tab: Site Sketch Attachment */}
                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  sketchUrl={sketchUrl}
                  attachments={attachments}
                  onAttachmentsChange={setAttachments}
                  propertyKind="Land"
                  showSketch={true}
                  showAttachments={false}
                />
              </View>
            )}

            {/* ========================================================= */}
            {/* TAB 2: 3. OWNER & DEMOGRAPHICS (CAST)                     */}
            {/* ========================================================= */}
            {activeTab === 2 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <View style={{ borderBottomWidth: 1, borderBottomColor: colors.border, paddingBottom: 10, marginBottom: 12 }}>
                    <Text style={[styles.cardHeaderTitle, { borderBottomWidth: 0, paddingBottom: 0, marginBottom: 8 }]}>
                      Owner & Declarant Demographics
                    </Text>

                    {/* 3 Options: Administrator, Beneficial User, Possessor (Check only one) */}
                    <View style={styles.rolePillsRow}>
                      <TouchableOpacity
                        style={[styles.rolePill, isAdministrator && styles.rolePillActive]}
                        onPress={() => {
                          if (isAdministrator) {
                            setIsAdministrator(false);
                          } else {
                            setIsAdministrator(true);
                            setIsBeneficialUser(false);
                            setIsPossessor(false);
                          }
                        }}
                        activeOpacity={0.7}
                      >
                        <Ionicons
                          name={isAdministrator ? "checkbox" : "square-outline"}
                          size={16}
                          color={isAdministrator ? colors.primary : colors.textMuted}
                        />
                        <Text style={[styles.rolePillText, isAdministrator && styles.rolePillTextActive]}>
                          ADMINISTRATOR
                        </Text>
                      </TouchableOpacity>

                      <TouchableOpacity
                        style={[styles.rolePill, isBeneficialUser && styles.rolePillActive]}
                        onPress={() => {
                          if (isBeneficialUser) {
                            setIsBeneficialUser(false);
                          } else {
                            setIsBeneficialUser(true);
                            setIsAdministrator(false);
                            setIsPossessor(false);
                          }
                        }}
                        activeOpacity={0.7}
                      >
                        <Ionicons
                          name={isBeneficialUser ? "checkbox" : "square-outline"}
                          size={16}
                          color={isBeneficialUser ? colors.primary : colors.textMuted}
                        />
                        <Text style={[styles.rolePillText, isBeneficialUser && styles.rolePillTextActive]}>
                          BENEFICIAL USER
                        </Text>
                      </TouchableOpacity>

                      <TouchableOpacity
                        style={[styles.rolePill, isPossessor && styles.rolePillActive]}
                        onPress={() => {
                          if (isPossessor) {
                            setIsPossessor(false);
                          } else {
                            setIsPossessor(true);
                            setIsAdministrator(false);
                            setIsBeneficialUser(false);
                          }
                        }}
                        activeOpacity={0.7}
                      >
                        <Ionicons
                          name={isPossessor ? "checkbox" : "square-outline"}
                          size={16}
                          color={isPossessor ? colors.primary : colors.textMuted}
                        />
                        <Text style={[styles.rolePillText, isPossessor && styles.rolePillTextActive]}>
                          POSSESSOR
                        </Text>
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>OWNER / DECLARANT NAME</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={ownerName}
                      onChangeText={setOwnerName}
                      placeholder="Full name of owner / declarant"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TAX IDENTIFICATION NO. (TIN)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={ownerTin}
                        onChangeText={setOwnerTin}
                        placeholder="000-000-000-000"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>CONTACT NUMBER</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={ownerContact}
                        onChangeText={setOwnerContact}
                        placeholder="09XX-XXX-XXXX"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>OWNER RESIDENTIAL ADDRESS</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={ownerAddress}
                      onChangeText={setOwnerAddress}
                      placeholder="Street, Barangay, City, Province"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>EMAIL ADDRESS</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={ownerEmail}
                        onChangeText={setOwnerEmail}
                        placeholder="owner@example.com"
                        keyboardType="email-address"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>SEX / GENDER</Text>
                      <TouchableOpacity
                        style={styles.selectBtn}
                        onPress={() => openGenderPicker('owner')}
                        activeOpacity={0.7}
                      >
                        <Text style={[styles.selectBtnText, !ownerGender && { color: colors.placeholder }]} numberOfLines={1}>
                          {ownerGender || 'Select Gender...'}
                        </Text>
                        <Ionicons name="chevron-down" size={16} color={colors.textMuted} />
                      </TouchableOpacity>
                    </View>
                  </View>

                  {(isAdministrator || isBeneficialUser || isPossessor) && (
                    <View style={{ marginTop: 16, paddingTop: 16, borderTopWidth: 1, borderTopColor: colors.border }}>
                      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 12 }}>
                        <Ionicons name="shield-checkmark-outline" size={16} color={colors.primary} />
                        <Text style={{ fontSize: 12, fontWeight: '700', color: colors.primary, textTransform: 'uppercase' }}>
                          {isAdministrator ? 'Administrator Details' : isBeneficialUser ? 'Beneficial User Details' : 'Possessor Details'}
                        </Text>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>
                          NAME ({isAdministrator ? 'ADMINISTRATOR' : isBeneficialUser ? 'BENEFICIAL USER' : 'POSSESSOR'})
                        </Text>
                        <TextInput
                          placeholderTextColor={colors.placeholder}
                          style={styles.input}
                          value={adminName}
                          onChangeText={setAdminName}
                          placeholder="Full name of administrator / representative"
                        />
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>TAX IDENTIFICATION NO. (TIN)</Text>
                          <TextInput
                            placeholderTextColor={colors.placeholder}
                            style={styles.input}
                            value={adminTin}
                            onChangeText={setAdminTin}
                            placeholder="000-000-000-000"
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>CONTACT NUMBER</Text>
                          <TextInput
                            placeholderTextColor={colors.placeholder}
                            style={styles.input}
                            value={adminContact}
                            onChangeText={setAdminContact}
                            placeholder="09XX-XXX-XXXX"
                          />
                        </View>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>ADMINISTRATOR ADDRESS</Text>
                        <TextInput
                          placeholderTextColor={colors.placeholder}
                          style={styles.input}
                          value={adminAddress}
                          onChangeText={setAdminAddress}
                          placeholder="House No., Street, Barangay, City, Province"
                        />
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 2 }]}>
                          <Text style={styles.inputLabel}>EMAIL ADDRESS</Text>
                          <TextInput
                            placeholderTextColor={colors.placeholder}
                            style={styles.input}
                            value={adminEmail}
                            onChangeText={setAdminEmail}
                            placeholder="admin@example.com"
                            keyboardType="email-address"
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>SEX / GENDER</Text>
                          <TouchableOpacity
                            style={styles.selectBtn}
                            onPress={() => openGenderPicker('admin')}
                            activeOpacity={0.7}
                          >
                            <Text style={[styles.selectBtnText, !adminGender && { color: colors.placeholder }]} numberOfLines={1}>
                              {adminGender || 'Select Gender...'}
                            </Text>
                            <Ionicons name="chevron-down" size={16} color={colors.textMuted} />
                          </TouchableOpacity>
                        </View>
                      </View>
                    </View>
                  )}
                </View>
              </View>
            )}

            {/* ========================================================= */}
            {/* TAB 3: 4. VALUATION & 4 SCHEDULES (CAST)                  */}
            {/* ========================================================= */}
            {activeTab === 3 && (
              <View style={styles.tabContentBlock}>
                {/* --------------------------------------------------------- */}
                {/* TABLE 1: Land Appraisal Schedule (SMV)                    */}
                {/* --------------------------------------------------------- */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.tableHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 1: Land Appraisal Schedule (SMV)</Text>
                      <Text style={styles.tableHeaderSubtitle}>Classification schedule & unit values per sq.m.</Text>
                    </View>
                    <TouchableOpacity
                      style={[styles.btnSm, styles.btnSmPrimary]}
                      onPress={openAddClassRow}
                    >
                      <Ionicons name="add-circle-outline" size={16} color="#ffffff" />
                      <Text style={styles.btnSmText}>Add Land</Text>
                    </TouchableOpacity>
                  </View>

                  <View style={styles.compactTableList}>
                    {classificationRows.map((row, idx) => {
                      const area = parseFloat(row.areaSqMeters) || 0;
                      const unitVal = parseFloat(row.unitValue) || 0;
                      const mv = parseFloat(row.marketValue) || 0;
                      return (
                        <TouchableOpacity
                          key={row.id || idx}
                          style={styles.compactTableRow}
                          activeOpacity={0.7}
                          onPress={() => openEditClassRow(idx)}
                        >
                          <View style={styles.compactRowMain}>
                            <View style={styles.compactRowHeader}>
                              <View style={styles.compactIndexBadge}>
                                <Text style={styles.compactIndexText}>#{idx + 1}</Text>
                              </View>
                              <View style={styles.compactBadge}>
                                <Text style={styles.compactBadgeText} numberOfLines={1}>
                                  {row.classificationName || 'LAND'}
                                </Text>
                              </View>
                              {row.subClassName ? (
                                <View style={[styles.compactBadge, { backgroundColor: isDarkMode ? '#0c4a6e' : '#e0f2fe' }]}>
                                  <Text style={[styles.compactBadgeText, { color: isDarkMode ? '#38bdf8' : '#0369a1' }]} numberOfLines={1}>
                                    {row.subClassName}
                                  </Text>
                                </View>
                              ) : null}
                            </View>

                            <Text style={styles.compactRowSubtext}>
                              {area.toLocaleString('en-US', { minimumFractionDigits: 2 })} sq.m. @ ₱{unitVal.toLocaleString('en-US', { minimumFractionDigits: 2 })} • {row.actualUseName || 'RESIDENTIAL'}
                            </Text>

                            <Text style={styles.compactRowValue}>
                              ₱{mv.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </Text>
                          </View>

                          <View style={styles.compactRowActions}>
                            <TouchableOpacity
                              style={styles.compactActionBtn}
                              onPress={() => openEditClassRow(idx)}
                              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                            >
                              <Ionicons name="pencil-outline" size={16} color={colors.primary} />
                            </TouchableOpacity>
                            {classificationRows.length > 1 && (
                              <TouchableOpacity
                                style={styles.compactActionBtnDanger}
                                onPress={() => removeClassificationRow(idx)}
                                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                              >
                                <Ionicons name="trash-outline" size={16} color="#ef4444" />
                              </TouchableOpacity>
                            )}
                          </View>
                        </TouchableOpacity>
                      );
                    })}
                  </View>

                  {/* Table 1 Summary Footer */}
                  <View style={styles.tableSummaryBox}>
                    <View style={styles.tableSummaryRow}>
                      <Text style={styles.tableSummaryLabel}>Total Land Area:</Text>
                      <Text style={styles.tableSummaryVal}>
                        {totalLandArea.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} sq.m.
                      </Text>
                    </View>
                    <View style={styles.tableSummaryRow}>
                      <Text style={styles.tableSummaryLabel}>Total Base Market Value:</Text>
                      <Text style={[styles.tableSummaryVal, { color: isDarkMode ? '#34d399' : '#059669', fontWeight: '900' }]}>
                        ₱{totalBaseMarketValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                  </View>
                </View>

                {/* --------------------------------------------------------- */}
                {/* TABLE 2: Other Improvements / Plants & Trees Appraisal    */}
                {/* --------------------------------------------------------- */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.tableHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 2: Other Improvements / Plants & Trees</Text>
                      <Text style={styles.tableHeaderSubtitle}>Perennial crops, fruit trees, and structural improvements</Text>
                    </View>
                    <TouchableOpacity
                      style={[styles.btnSm, styles.btnSmPrimary]}
                      onPress={openAddImpRow}
                    >
                      <Ionicons name="add-circle-outline" size={16} color="#ffffff" />
                      <Text style={styles.btnSmText}>Add Plant/Tree</Text>
                    </TouchableOpacity>
                  </View>

                  {improvementRows.length === 0 ? (
                    <View style={styles.tableEmptyCard}>
                      <Ionicons name="leaf-outline" size={28} color={colors.textMuted} />
                      <Text style={styles.tableEmptyTitle}>No Improvements / Plants & Trees</Text>
                      <Text style={styles.tableEmptySubtitle}>
                        Tap "+ Add Plant/Tree" if there are perennial trees, coconut, or improvements on this land.
                      </Text>
                    </View>
                  ) : (
                    <View style={styles.compactTableList}>
                      {improvementRows.map((row, idx) => {
                        const q = parseFloat(row.quantity) || 0;
                        const age = parseInt(row.ageYears) || 0;
                        const u = parseFloat(row.unitValue) || 0;
                        const mv = parseFloat(row.marketValue) || 0;
                        return (
                          <TouchableOpacity
                            key={row.id || idx}
                            style={styles.compactTableRow}
                            activeOpacity={0.7}
                            onPress={() => openEditImpRow(idx)}
                          >
                            <View style={styles.compactRowMain}>
                              <View style={styles.compactRowHeader}>
                                <View style={styles.compactIndexBadge}>
                                  <Text style={styles.compactIndexText}>#{idx + 1}</Text>
                                </View>
                                <View style={styles.compactBadge}>
                                  <Text style={styles.compactBadgeText} numberOfLines={1}>
                                    {row.typeDescription || 'Improvement'}
                                  </Text>
                                </View>
                                <View style={[
                                  styles.compactBadge,
                                  { backgroundColor: row.isFruitBearing ? (isDarkMode ? '#064e3b' : '#ecfdf5') : (isDarkMode ? '#334155' : '#f1f5f9') }
                                ]}>
                                  <Text style={[
                                    styles.compactBadgeText,
                                    { color: row.isFruitBearing ? (isDarkMode ? '#6ee7b7' : '#047857') : colors.textMuted }
                                  ]}>
                                    {row.isFruitBearing ? 'Fruit-Bearing' : 'Immature'}
                                  </Text>
                                </View>
                              </View>

                              <Text style={styles.compactRowSubtext}>
                                {q} trees/hills • {age} yrs @ ₱{u.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                              </Text>

                              <Text style={styles.compactRowValue}>
                                ₱{mv.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                              </Text>
                            </View>

                            <View style={styles.compactRowActions}>
                              <TouchableOpacity
                                style={styles.compactActionBtn}
                                onPress={() => openEditImpRow(idx)}
                                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                              >
                                <Ionicons name="pencil-outline" size={16} color={colors.primary} />
                              </TouchableOpacity>
                              <TouchableOpacity
                                style={styles.compactActionBtnDanger}
                                onPress={() => removeImprovementRow(idx)}
                                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                              >
                                <Ionicons name="trash-outline" size={16} color="#ef4444" />
                              </TouchableOpacity>
                            </View>
                          </TouchableOpacity>
                        );
                      })}
                    </View>
                  )}

                  {improvementRows.length > 0 && (
                    <View style={styles.tableSummaryBox}>
                      <View style={styles.tableSummaryRow}>
                        <Text style={styles.tableSummaryLabel}>Total Improvements Market Value:</Text>
                        <Text style={[styles.tableSummaryVal, { color: isDarkMode ? '#34d399' : '#059669', fontWeight: '900' }]}>
                          ₱{totalImprovementsMarketValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Text>
                      </View>
                    </View>
                  )}
                </View>

                {/* --------------------------------------------------------- */}
                {/* TABLE 3: Base Market Value Adjustments                     */}
                {/* --------------------------------------------------------- */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.tableHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 3: Value Adjustments (Topography / Access)</Text>
                      <Text style={styles.tableHeaderSubtitle}>Corner influence, elevation, road access, and grade factors</Text>
                    </View>
                    <View style={{ flexDirection: 'row', gap: 6 }}>
                      <TouchableOpacity
                        style={[styles.btnSm, styles.btnSmSecondary]}
                        onPress={syncAdjustmentsFromClassifications}
                      >
                        <Ionicons name="sync-outline" size={14} color={colors.primary} />
                        <Text style={styles.btnSmSecondaryText}>Sync</Text>
                      </TouchableOpacity>
                      <TouchableOpacity
                        style={[styles.btnSm, styles.btnSmPrimary]}
                        onPress={openAddAdjRow}
                      >
                        <Ionicons name="add-circle-outline" size={16} color="#ffffff" />
                        <Text style={styles.btnSmText}>Add Adj</Text>
                      </TouchableOpacity>
                    </View>
                  </View>

                  {adjustmentRows.length === 0 ? (
                    <View style={styles.tableEmptyCard}>
                      <Ionicons name="trending-up-outline" size={28} color={colors.textMuted} />
                      <Text style={styles.tableEmptyTitle}>No Value Adjustments</Text>
                      <Text style={styles.tableEmptySubtitle}>
                        Tap "Sync" to copy classifications or "+ Add Adj" to apply corner, road, or topography factors.
                      </Text>
                    </View>
                  ) : (
                    <View style={styles.compactTableList}>
                      {adjustmentRows.map((row, idx) => {
                        const baseMv = parseFloat(row.baseMarketValue) || 0;
                        const pct = parseFloat(row.percentAdjustment) || 0;
                        const adjMv = parseFloat(row.adjustedMarketValue) || 0;
                        const valAdj = parseFloat(row.valueAdjustment) || 0;
                        return (
                          <TouchableOpacity
                            key={row.id || idx}
                            style={styles.compactTableRow}
                            activeOpacity={0.7}
                            onPress={() => openEditAdjRow(idx)}
                          >
                            <View style={styles.compactRowMain}>
                              <View style={styles.compactRowHeader}>
                                <View style={styles.compactIndexBadge}>
                                  <Text style={styles.compactIndexText}>#{idx + 1}</Text>
                                </View>
                                <View style={styles.compactBadge}>
                                  <Text style={styles.compactBadgeText} numberOfLines={1}>
                                    {row.classificationName || 'LAND'}
                                  </Text>
                                </View>
                                <View style={[
                                  styles.compactBadge,
                                  { backgroundColor: pct >= 0 ? (isDarkMode ? '#064e3b' : '#ecfdf5') : (isDarkMode ? '#450a0a' : '#fee2e2') }
                                ]}>
                                  <Text style={[
                                    styles.compactBadgeText,
                                    { color: pct >= 0 ? (isDarkMode ? '#6ee7b7' : '#047857') : '#ef4444', fontWeight: '800' }
                                  ]}>
                                    {pct >= 0 ? `+${pct}%` : `${pct}%`}
                                  </Text>
                                </View>
                              </View>

                              <Text style={styles.compactRowSubtext}>
                                {row.adjustmentFactorDescription || 'Factor'} • Base: ₱{baseMv.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                              </Text>

                              <Text style={styles.compactRowValue}>
                                ₱{adjMv.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                <Text style={[styles.compactRowSubtext, { color: valAdj >= 0 ? (isDarkMode ? '#6ee7b7' : '#047857') : '#ef4444' }]}>
                                  {` (${valAdj >= 0 ? '+' : ''}₱${valAdj.toLocaleString('en-US', { minimumFractionDigits: 2 })})`}
                                </Text>
                              </Text>
                            </View>

                            <View style={styles.compactRowActions}>
                              <TouchableOpacity
                                style={styles.compactActionBtn}
                                onPress={() => openEditAdjRow(idx)}
                                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                              >
                                <Ionicons name="pencil-outline" size={16} color={colors.primary} />
                              </TouchableOpacity>
                              <TouchableOpacity
                                style={styles.compactActionBtnDanger}
                                onPress={() => removeAdjustmentRow(idx)}
                                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                              >
                                <Ionicons name="trash-outline" size={16} color="#ef4444" />
                              </TouchableOpacity>
                            </View>
                          </TouchableOpacity>
                        );
                      })}
                    </View>
                  )}

                  {adjustmentRows.length > 0 && (
                    <View style={styles.tableSummaryBox}>
                      <View style={styles.tableSummaryRow}>
                        <Text style={styles.tableSummaryLabel}>Net Adjustment %:</Text>
                        <Text style={styles.tableSummaryVal}>
                          {totalPercentAdjustment >= 0 ? `+${totalPercentAdjustment}%` : `${totalPercentAdjustment}%`}
                        </Text>
                      </View>
                      <View style={styles.tableSummaryRow}>
                        <Text style={styles.tableSummaryLabel}>Total Adjusted Market Value:</Text>
                        <Text style={[styles.tableSummaryVal, { color: isDarkMode ? '#34d399' : '#059669', fontWeight: '900' }]}>
                          ₱{totalAdjustedMarketValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        </Text>
                      </View>
                    </View>
                  )}
                </View>

                {/* --------------------------------------------------------- */}
                {/* TABLE 4: Property Assessment Roll & Tax Due Calculation   */}
                {/* --------------------------------------------------------- */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.tableHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 4: Property Assessment Roll & Tax Due</Text>
                      <Text style={styles.tableHeaderSubtitle}>Statutory assessment levels & taxable assessed values</Text>
                    </View>
                    <View style={{ flexDirection: 'row', gap: 6 }}>
                      <TouchableOpacity
                        style={[styles.btnSm, styles.btnSmSecondary]}
                        onPress={autoGenerateAssessmentRoll}
                      >
                        <Ionicons name="flash-outline" size={14} color={colors.primary} />
                        <Text style={styles.btnSmSecondaryText}>Auto-Roll</Text>
                      </TouchableOpacity>
                      <TouchableOpacity
                        style={[styles.btnSm, styles.btnSmPrimary]}
                        onPress={openAddRollRow}
                      >
                        <Ionicons name="add-circle-outline" size={16} color="#ffffff" />
                        <Text style={styles.btnSmText}>Add Roll</Text>
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.compactTableList}>
                    {assessmentRollRows.map((row, idx) => {
                      const mv = parseFloat(row.marketValue) || 0;
                      const lvl = parseFloat(row.assessmentLevelPercent) || 0;
                      const av = parseFloat(row.assessedValue) || 0;
                      return (
                        <TouchableOpacity
                          key={row.id || idx}
                          style={styles.compactTableRow}
                          activeOpacity={0.7}
                          onPress={() => openEditRollRow(idx)}
                        >
                          <View style={styles.compactRowMain}>
                            <View style={styles.compactRowHeader}>
                              <View style={styles.compactIndexBadge}>
                                <Text style={styles.compactIndexText}>#{idx + 1}</Text>
                              </View>
                              <View style={styles.compactBadge}>
                                <Text style={styles.compactBadgeText} numberOfLines={1}>
                                  {row.kinds || 'LAND'}
                                </Text>
                              </View>
                              <View style={[styles.compactBadge, { backgroundColor: isDarkMode ? '#1e1b4b' : '#ede9fe' }]}>
                                <Text style={[styles.compactBadgeText, { color: isDarkMode ? '#a78bfa' : '#6d28d9' }]} numberOfLines={1}>
                                  {row.actualUseName || 'RESIDENTIAL'}
                                </Text>
                              </View>
                            </View>

                            <Text style={styles.compactRowSubtext}>
                              Market Value: ₱{mv.toLocaleString('en-US', { minimumFractionDigits: 2 })} • Level: {lvl}%
                            </Text>

                            <Text style={[styles.compactRowValue, { color: isDarkMode ? '#34d399' : '#059669' }]}>
                              Assessed Value: ₱{av.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </Text>
                          </View>

                          <View style={styles.compactRowActions}>
                            <TouchableOpacity
                              style={styles.compactActionBtn}
                              onPress={() => openEditRollRow(idx)}
                              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                            >
                              <Ionicons name="pencil-outline" size={16} color={colors.primary} />
                            </TouchableOpacity>
                            {assessmentRollRows.length > 1 && (
                              <TouchableOpacity
                                style={styles.compactActionBtnDanger}
                                onPress={() => removeAssessmentRollRow(idx)}
                                hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                              >
                                <Ionicons name="trash-outline" size={16} color="#ef4444" />
                              </TouchableOpacity>
                            )}
                          </View>
                        </TouchableOpacity>
                      );
                    })}
                  </View>

                  <View style={styles.switchRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.switchTitle}>PROPERTY TAXABILITY</Text>
                      <Text style={styles.switchSubtitle}>
                        {taxability ? 'Taxable (Standard Real Property Tax)' : 'Exempt from Real Property Tax'}
                      </Text>
                    </View>
                    <Switch
                      value={taxability}
                      onValueChange={setTaxability}
                      trackColor={{ false: colors.borderDark, true: colors.primary }}
                    />
                  </View>

                  {/* Valuation Summary KPI Dashboard (Matching Web) */}
                  <View style={styles.kpiGrid}>
                    <View style={styles.kpiCard}>
                      <Text style={styles.kpiLabel}>Total Market Value</Text>
                      <Text style={styles.kpiValue}>
                        ₱{numMarketValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                    <View style={styles.kpiCard}>
                      <Text style={styles.kpiLabel}>Total Assessed Value</Text>
                      <Text style={[styles.kpiValue, { color: isDarkMode ? '#34d399' : '#059669' }]}>
                        ₱{numAssessedValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                    <View style={styles.kpiCard}>
                      <Text style={styles.kpiLabel}>Basic RPT (2.0%)</Text>
                      <Text style={[styles.kpiValue, { color: isDarkMode ? '#38bdf8' : '#0284c7' }]}>
                        ₱{numBasicRpt.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                    <View style={styles.kpiCard}>
                      <Text style={styles.kpiLabel}>Total Annual Tax Due</Text>
                      <Text style={[styles.kpiValue, { color: isDarkMode ? '#fbbf24' : '#d97706' }]}>
                        ₱{numAnnualTax.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                  </View>
                </View>
              </View>
            )}

            {/* ========================================================= */}
            {/* TAB 4: 5. EFFECTIVITY, BACK TAXES & NOTES                 */}
            {/* ========================================================= */}
            {activeTab === 4 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Effectivity, Taxability & Assessment Basis (CAST Standard)</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>EFFECTIVITY QUARTER</Text>
                      <View style={styles.optionPillRow}>
                        {['1st Quarter', '2nd Quarter', '3rd Quarter', '4th Quarter'].map((q) => (
                          <TouchableOpacity
                            key={q}
                            style={[styles.optionPill, quarterEffectivity === q && styles.optionPillActive]}
                            onPress={() => setQuarterEffectivity(q)}
                          >
                            <Text style={[styles.optionPillText, quarterEffectivity === q && styles.optionPillTextActive]}>
                              {q.replace(' Quarter', '')}
                            </Text>
                          </TouchableOpacity>
                        ))}
                      </View>
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>EFFECTIVITY YEAR</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={yearEffectivity}
                        onChangeText={setYearEffectivity}
                        keyboardType="numeric"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>TAXABILITY STATUS</Text>
                    <View style={styles.optionPillRow}>
                      <TouchableOpacity
                        style={[styles.optionPill, taxability && styles.optionPillActive]}
                        onPress={() => setTaxability(true)}
                      >
                        <Ionicons name={taxability ? 'checkmark-circle' : 'ellipse-outline'} size={14} color={taxability ? '#ffffff' : colors.textMuted} />
                        <Text style={[styles.optionPillText, taxability && styles.optionPillTextActive]}>
                          Taxable (Standard)
                        </Text>
                      </TouchableOpacity>
                      <TouchableOpacity
                        style={[styles.optionPill, !taxability && styles.optionPillActive]}
                        onPress={() => setTaxability(false)}
                      >
                        <Ionicons name={!taxability ? 'checkmark-circle' : 'ellipse-outline'} size={14} color={!taxability ? '#ffffff' : colors.textMuted} />
                        <Text style={[styles.optionPillText, !taxability && styles.optionPillTextActive]}>
                          Exempt from RPT
                        </Text>
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>BASIS OF ASSESSMENT</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={assessmentBasis}
                      onChangeText={setAssessmentBasis}
                      placeholder="Sec. 217 RA 7160"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BACK TAXES FROM YEAR</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={backTaxesFrom}
                        onChangeText={setBackTaxesFrom}
                        placeholder="YYYY (Optional)"
                        keyboardType="numeric"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BACK TAXES TO YEAR</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={backTaxesTo}
                        onChangeText={setBackTaxesTo}
                        placeholder="YYYY (Optional)"
                        keyboardType="numeric"
                      />
                    </View>
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Memoranda & Assessment Notes</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>MEMORANDA & ASSESSMENT NOTES</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={[styles.input, { height: 70, textAlignVertical: 'top' }]}
                      value={memoranda}
                      onChangeText={setMemoranda}
                      multiline
                      placeholder="Enter notes, registration references, partition directives, or sworn statement details..."
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>INSPECTOR FIELD REMARKS</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={[styles.input, { height: 60, textAlignVertical: 'top' }]}
                      value={remarks}
                      onChangeText={setRemarks}
                      multiline
                      placeholder="Field inspection observations..."
                    />
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Office Workflow Transfer</Text>

                  <View style={styles.switchRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.switchTitle}>Transfer to Next Step on Sync</Text>
                      <Text style={styles.switchSubtitle}>Automatically forwards workflow after office sync</Text>
                    </View>
                    <Switch
                      value={autoForward}
                      onValueChange={setAutoForward}
                      trackColor={{ false: colors.borderDark, true: colors.primary }}
                    />
                  </View>

                  {autoForward && (
                    <View style={styles.nextStepBox}>
                      <Text style={styles.nextStepLabel}>Target Next Stage:</Text>
                      <View style={styles.nextStepBadge}>
                        <Ionicons name="checkmark-circle-outline" size={16} color={colors.primary} />
                        <Text style={styles.nextStepBadgeText}>Inspection Checked / Recommend Approval</Text>
                      </View>
                    </View>
                  )}
                </View>

                {/* 5th Tab: Field Photos & Attachments */}
                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  sketchUrl={sketchUrl}
                  attachments={attachments}
                  onAttachmentsChange={setAttachments}
                  propertyKind="Land"
                  showSketch={false}
                  showAttachments={true}
                />
              </View>
            )}

            {/* Action Buttons */}
            {!isOnline && (
              <View style={styles.offlineNoticeRow}>
                <Ionicons name="cloud-offline-outline" size={15} color="#b45309" />
                <Text style={styles.offlineNoticeText}>
                  Offline Mode: Changes will be saved locally to this device.
                </Text>
              </View>
            )}
            <View style={styles.buttonContainer}>
              {/* Button 1: Save & Sync to Web (Draft - No Transfer) */}
              <TouchableOpacity
                style={[
                  styles.saveBtn,
                  styles.saveBtnPrimary,
                  !isOnline && styles.saveBtnOffline
                ]}
                onPress={() => handleSave(false, false)}
                disabled={saving}
              >
                {saving ? (
                  <ActivityIndicator color="#ffffff" />
                ) : (
                  <>
                    <Ionicons
                      name={isOnline ? "cloud-upload" : "file-tray-full-outline"}
                      size={18}
                      color="#ffffff"
                    />
                    <Text style={styles.saveBtnText}>
                      {isOnline
                        ? "Save & Sync to Web (Draft)"
                        : "Save to Offline Queue (No Transfer)"}
                    </Text>
                  </>
                )}
              </TouchableOpacity>

              {/* Button 2: Save & Transfer to Supervisor (Advances Workflow) */}
              <TouchableOpacity
                style={[styles.saveBtn, styles.saveBtnTransfer]}
                onPress={() => handleSave(false, true)}
                disabled={saving}
              >
                <Ionicons name="send" size={16} color="#ffffff" />
                <Text style={styles.saveBtnText}>
                  {isOnline
                    ? "Save & Transfer to Supervisor"
                    : "Queue & Transfer to Supervisor"}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.saveBtn, styles.saveBtnSecondary]}
                onPress={() => handleSave(true)}
              >
                <Ionicons name="save-outline" size={18} color={colors.primary} />
                <Text style={styles.saveBtnSecondaryText}>Save to Offline Queue Only</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.saveBtn, styles.cancelBtn]}
                onPress={handleCancel}
                disabled={saving}
              >
                <Ionicons name="close-circle-outline" size={18} color="#e11d48" />
                <Text style={styles.cancelBtnText}>Cancel & Discard Changes</Text>
              </TouchableOpacity>
            </View>
          </>
        )}
      </ScrollView>

      {/* Date Picker Modal for Title Registration Date */}
      <DatePickerModal
        visible={datePickerVisible}
        onClose={() => setDatePickerVisible(false)}
        value={tctDate}
        onSelectDate={setTctDate}
        title="Date of Title Registration"
      />

      {/* Barangay Picker Modal */}
      <Modal
        visible={barangayModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setBarangayModalVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setBarangayModalVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Barangay</Text>
              <TouchableOpacity onPress={() => setBarangayModalVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {BARANGAY_OPTIONS.map((option) => {
                const isSelected = districtOrBarangay === option;
                return (
                  <TouchableOpacity
                    key={option}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      setDistrictOrBarangay(option);
                      setBarangayModalVisible(false);
                    }}
                  >
                    <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                      {option}
                    </Text>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Gender Picker Modal */}
      <Modal
        visible={genderPickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setGenderPickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setGenderPickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>
                {genderPickerTarget === 'owner' ? 'Select Owner Gender' : 'Select Gender'}
              </Text>
              <TouchableOpacity onPress={() => setGenderPickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <View style={{ paddingVertical: 8 }}>
              {GENDER_OPTIONS.map((option) => {
                const currentVal = genderPickerTarget === 'owner' ? ownerGender : adminGender;
                const isSelected = currentVal === option;
                return (
                  <TouchableOpacity
                    key={option}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (genderPickerTarget === 'owner') {
                        setOwnerGender(option);
                      } else {
                        setAdminGender(option);
                      }
                      setGenderPickerVisible(false);
                    }}
                  >
                    <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                      {option}
                    </Text>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </View>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* ========================================================= */}
      {/* 4 ROW EDITOR MODALS (OPTION B: COMPACT MASTER-DETAIL)     */}
      {/* ========================================================= */}

      {/* 1. Edit/Add Land Classification Schedule Modal */}
      <Modal
        visible={editClassModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setEditClassModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setEditClassModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingClassIndex !== null ? `Edit Land Item #${editingClassIndex + 1}` : 'Add Land Item'}
                  </Text>
                  <Text style={styles.tableHeaderSubtitle}>Classification schedule & unit values per sq.m.</Text>
                </View>
                <TouchableOpacity onPress={() => setEditClassModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Classification Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>CLASSIFICATION</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setClassPickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText}>
                      {editingClassRow?.classificationName || 'Select Classification'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Sub-Classification (SMV) Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>SUB-CLASSIFICATION (SMV)</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setSubClassPickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText}>
                      {editingClassRow?.subClassName || 'Select Sub-Classification'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Actual Use Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>ACTUAL USE</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setActualUsePickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText}>
                      {editingClassRow?.actualUseName || 'Select Actual Use'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Area & Unit Value */}
                <View style={styles.rowInputs}>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>AREA (SQ.M.)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingClassRow?.areaSqMeters !== undefined && editingClassRow?.areaSqMeters !== null ? String(editingClassRow.areaSqMeters) : ''}
                      onChangeText={(val) => {
                        const a = parseFloat(val) || 0;
                        const u = parseFloat(editingClassRow?.unitValue) || 0;
                        setEditingClassRow({
                          ...editingClassRow,
                          areaSqMeters: val,
                          marketValue: Math.round(a * u * 100) / 100
                        });
                      }}
                      keyboardType="numeric"
                      placeholder="0.00"
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>UNIT VALUE (₱)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingClassRow?.unitValue !== undefined && editingClassRow?.unitValue !== null ? String(editingClassRow.unitValue) : ''}
                      onChangeText={(val) => {
                        const a = parseFloat(editingClassRow?.areaSqMeters) || 0;
                        const u = parseFloat(val) || 0;
                        setEditingClassRow({
                          ...editingClassRow,
                          unitValue: val,
                          marketValue: Math.round(a * u * 100) / 100
                        });
                      }}
                      keyboardType="numeric"
                      placeholder="0.00"
                    />
                  </View>
                </View>

                {/* Calculated Base Market Value */}
                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>BASE MARKET VALUE:</Text>
                  <Text style={styles.calculatedValue}>
                    ₱{(parseFloat(editingClassRow?.marketValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                {/* Modal Footer Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setEditClassModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveClassRow}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Land Item</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

      {/* 2. Edit/Add Improvements / Plants & Trees Modal */}
      <Modal
        visible={editImpModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setEditImpModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setEditImpModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingImpIndex !== null ? `Edit Improvement #${editingImpIndex + 1}` : 'Add Plant / Tree / Improvement'}
                  </Text>
                  <Text style={styles.tableHeaderSubtitle}>Perennial crops, fruit trees, and structural improvements</Text>
                </View>
                <TouchableOpacity onPress={() => setEditImpModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Improvement Type Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>IMPROVEMENT TYPE / SPECIE</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setImpTypePickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText}>
                      {editingImpRow?.typeDescription || 'Select Improvement Type'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Fruit-Bearing Switch */}
                <View style={styles.switchRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={styles.switchTitle}>STATUS</Text>
                    <Text style={styles.switchSubtitle}>
                      {editingImpRow?.isFruitBearing ? 'Fruit-Bearing / Productive' : 'Non-Fruit Bearing / Immature'}
                    </Text>
                  </View>
                  <Switch
                    value={Boolean(editingImpRow?.isFruitBearing)}
                    onValueChange={(val) => setEditingImpRow({ ...editingImpRow, isFruitBearing: val })}
                    trackColor={{ false: colors.borderDark, true: colors.primary }}
                  />
                </View>

                {/* Quantity, Age & Unit Value */}
                <View style={styles.rowInputs}>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>QUANTITY (HILLS/TREES)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingImpRow?.quantity !== undefined && editingImpRow?.quantity !== null ? String(editingImpRow.quantity) : ''}
                      onChangeText={(val) => {
                        const q = parseFloat(val) || 0;
                        const u = parseFloat(editingImpRow?.unitValue) || 0;
                        setEditingImpRow({
                          ...editingImpRow,
                          quantity: val,
                          marketValue: Math.round(q * u * 100) / 100
                        });
                      }}
                      keyboardType="numeric"
                      placeholder="0"
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>AGE (YEARS)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingImpRow?.ageYears !== undefined && editingImpRow?.ageYears !== null ? String(editingImpRow.ageYears) : ''}
                      onChangeText={(val) => setEditingImpRow({ ...editingImpRow, ageYears: val })}
                      keyboardType="numeric"
                      placeholder="0"
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>UNIT VALUE (₱)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingImpRow?.unitValue !== undefined && editingImpRow?.unitValue !== null ? String(editingImpRow.unitValue) : ''}
                      onChangeText={(val) => {
                        const q = parseFloat(editingImpRow?.quantity) || 0;
                        const u = parseFloat(val) || 0;
                        setEditingImpRow({
                          ...editingImpRow,
                          unitValue: val,
                          marketValue: Math.round(q * u * 100) / 100
                        });
                      }}
                      keyboardType="numeric"
                      placeholder="0.00"
                    />
                  </View>
                </View>

                {/* Calculated Improvements Market Value */}
                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>IMPROVEMENTS MARKET VALUE:</Text>
                  <Text style={styles.calculatedValue}>
                    ₱{(parseFloat(editingImpRow?.marketValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                {/* Modal Footer Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setEditImpModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveImpRow}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Improvement</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

      {/* 3. Edit/Add Value Adjustments Modal */}
      <Modal
        visible={editAdjModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setEditAdjModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setEditAdjModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingAdjIndex !== null ? `Edit Adjustment #${editingAdjIndex + 1}` : 'Add Value Adjustment'}
                  </Text>
                  <Text style={styles.tableHeaderSubtitle}>Corner influence, elevation, road access, and grade factors</Text>
                </View>
                <TouchableOpacity onPress={() => setEditAdjModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Adjustment Factor Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>ADJUSTMENT FACTOR</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setAdjFactorPickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText} numberOfLines={1}>
                      {editingAdjRow?.adjustmentFactorDescription || 'Select Adjustment Factor'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Base Market Value (₱) */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>BASE MARKET VALUE (₱)</Text>
                  <TextInput
                    placeholderTextColor={colors.placeholder}
                    style={styles.input}
                    value={editingAdjRow?.baseMarketValue !== undefined && editingAdjRow?.baseMarketValue !== null ? String(editingAdjRow.baseMarketValue) : ''}
                    onChangeText={(val) => {
                      const baseMv = parseFloat(val) || 0;
                      const pct = parseFloat(editingAdjRow?.percentAdjustment) || 0;
                      const valAdj = Math.round(baseMv * (pct / 100) * 100) / 100;
                      setEditingAdjRow({
                        ...editingAdjRow,
                        baseMarketValue: val,
                        valueAdjustment: valAdj,
                        adjustedMarketValue: Math.round((baseMv + valAdj) * 100) / 100
                      });
                    }}
                    keyboardType="numeric"
                    placeholder="0.00"
                  />
                </View>

                {/* % Adjustment */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>% ADJUSTMENT (E.G. +10 OR -5)</Text>
                  <TextInput
                    placeholderTextColor={colors.placeholder}
                    style={styles.input}
                    value={editingAdjRow?.percentAdjustment !== undefined && editingAdjRow?.percentAdjustment !== null ? String(editingAdjRow.percentAdjustment) : ''}
                    onChangeText={(val) => {
                      const baseMv = parseFloat(editingAdjRow?.baseMarketValue) || 0;
                      const pct = parseFloat(val) || 0;
                      const valAdj = Math.round(baseMv * (pct / 100) * 100) / 100;
                      setEditingAdjRow({
                        ...editingAdjRow,
                        percentAdjustment: val,
                        valueAdjustment: valAdj,
                        adjustedMarketValue: Math.round((baseMv + valAdj) * 100) / 100
                      });
                    }}
                    keyboardType="numeric"
                    placeholder="0"
                  />
                </View>

                {/* Value Adjustment & Adjusted Market Value */}
                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>VALUE ADJUSTMENT:</Text>
                  <Text style={[styles.calculatedValue, { color: (parseFloat(editingAdjRow?.valueAdjustment) || 0) >= 0 ? (isDarkMode ? '#34d399' : '#047857') : '#ef4444' }]}>
                    {(parseFloat(editingAdjRow?.valueAdjustment) || 0) >= 0 ? '+' : ''}₱{(parseFloat(editingAdjRow?.valueAdjustment) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>ADJUSTED MARKET VALUE:</Text>
                  <Text style={styles.calculatedValue}>
                    ₱{(parseFloat(editingAdjRow?.adjustedMarketValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                {/* Modal Footer Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setEditAdjModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveAdjRow}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Adjustment</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

      {/* 4. Edit/Add Property Assessment Roll Modal */}
      <Modal
        visible={editRollModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setEditRollModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setEditRollModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingRollIndex !== null ? `Edit Roll Item #${editingRollIndex + 1}` : 'Add Roll Item'}
                  </Text>
                  <Text style={styles.tableHeaderSubtitle}>Statutory assessment levels & taxable assessed values</Text>
                </View>
                <TouchableOpacity onPress={() => setEditRollModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Kind / Property Component Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>PROPERTY COMPONENT (KIND)</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setRollKindPickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText}>
                      {editingRollRow?.kinds || 'Select Component'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Actual Use Dropdown */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>ACTUAL USE</Text>
                  <TouchableOpacity
                    style={styles.selectBtn}
                    onPress={() => setRollActualUsePickerVisible(true)}
                  >
                    <Text style={styles.selectBtnText}>
                      {editingRollRow?.actualUseName || 'Select Actual Use'}
                    </Text>
                    <Ionicons name="chevron-down" size={18} color={colors.textMuted} />
                  </TouchableOpacity>
                </View>

                {/* Market Value & Assessment Level */}
                <View style={styles.rowInputs}>
                  <View style={[styles.inputGroup, { flex: 1.4 }]}>
                    <Text style={styles.inputLabel}>MARKET VALUE (₱)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingRollRow?.marketValue !== undefined && editingRollRow?.marketValue !== null ? String(editingRollRow.marketValue) : ''}
                      onChangeText={(val) => {
                        const mv = parseFloat(val) || 0;
                        const lvl = parseFloat(editingRollRow?.assessmentLevelPercent) || 0;
                        setEditingRollRow({
                          ...editingRollRow,
                          marketValue: val,
                          assessedValue: Math.round(mv * (lvl / 100) * 100) / 100
                        });
                      }}
                      keyboardType="numeric"
                      placeholder="0.00"
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>LEVEL (%)</Text>
                    <TextInput
                      placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={editingRollRow?.assessmentLevelPercent !== undefined && editingRollRow?.assessmentLevelPercent !== null ? String(editingRollRow.assessmentLevelPercent) : ''}
                      onChangeText={(val) => {
                        const mv = parseFloat(editingRollRow?.marketValue) || 0;
                        const lvl = parseFloat(val) || 0;
                        setEditingRollRow({
                          ...editingRollRow,
                          assessmentLevelPercent: val,
                          assessedValue: Math.round(mv * (lvl / 100) * 100) / 100
                        });
                      }}
                      keyboardType="numeric"
                      placeholder="20"
                    />
                  </View>
                </View>

                {/* Calculated Assessed Value */}
                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>ASSESSED VALUE:</Text>
                  <Text style={[styles.calculatedValue, { color: isDarkMode ? '#34d399' : '#059669' }]}>
                    ₱{(parseFloat(editingRollRow?.assessedValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                {/* Modal Footer Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setEditRollModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveRollRow}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Roll Item</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

      {/* ========================================================= */}
      {/* 7 DROPDOWN PICKER MODALS                                  */}
      {/* ========================================================= */}

      {/* Classification Picker Modal */}
      <Modal
        visible={classPickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setClassPickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setClassPickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Land Classification</Text>
              <TouchableOpacity onPress={() => setClassPickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {LAND_CLASSIFICATIONS.map((c) => {
                const currentRow = editingClassRow || (activeClassRowIndex !== null ? classificationRows[activeClassRowIndex] : null);
                const isSelected = currentRow?.classificationId === c.id;
                return (
                  <TouchableOpacity
                    key={c.id}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      const firstSub = LAND_SUB_CLASSIFICATIONS.find(s => s.classificationId === c.id) || {
                        id: 1, subClass: c.name, unitValue: 520
                      };
                      if (editingClassRow) {
                        const a = parseFloat(editingClassRow.areaSqMeters) || 0;
                        const u = firstSub.unitValue;
                        setEditingClassRow({
                          ...editingClassRow,
                          classificationId: c.id,
                          classificationName: c.name,
                          subClassId: firstSub.id,
                          subClassName: firstSub.subClass,
                          unitValue: String(u),
                          marketValue: Math.round(a * u * 100) / 100
                        });
                      } else if (activeClassRowIndex !== null) {
                        updateClassificationRow(activeClassRowIndex, {
                          classificationId: c.id,
                          classificationName: c.name,
                          subClassId: firstSub.id,
                          subClassName: firstSub.subClass,
                          unitValue: firstSub.unitValue
                        });
                      }
                      setClassPickerVisible(false);
                    }}
                  >
                    <View style={{ flex: 1 }}>
                      <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                        {c.name}
                      </Text>
                      <Text style={styles.typeOptionSubtext}>Code: {c.code}</Text>
                    </View>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Sub-Classification (SMV) Picker Modal */}
      <Modal
        visible={subClassPickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setSubClassPickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setSubClassPickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Sub-Classification (SMV)</Text>
              <TouchableOpacity onPress={() => setSubClassPickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {(() => {
                const currentRow = editingClassRow || (activeClassRowIndex !== null ? classificationRows[activeClassRowIndex] : null);
                const filteredSubs = currentRow?.classificationId
                  ? LAND_SUB_CLASSIFICATIONS.filter(s => s.classificationId === currentRow.classificationId)
                  : LAND_SUB_CLASSIFICATIONS;
                const subsToShow = filteredSubs.length > 0 ? filteredSubs : LAND_SUB_CLASSIFICATIONS;

                return subsToShow.map((sub) => {
                  const isSelected = currentRow?.subClassId === sub.id;
                  return (
                    <TouchableOpacity
                      key={sub.id}
                      style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                      onPress={() => {
                        if (editingClassRow) {
                          const a = parseFloat(editingClassRow.areaSqMeters) || 0;
                          const u = sub.unitValue;
                          setEditingClassRow({
                            ...editingClassRow,
                            subClassId: sub.id,
                            subClassName: sub.subClass,
                            unitValue: String(u),
                            marketValue: Math.round(a * u * 100) / 100
                          });
                        } else if (activeClassRowIndex !== null) {
                          updateClassificationRow(activeClassRowIndex, {
                            subClassId: sub.id,
                            subClassName: sub.subClass,
                            unitValue: sub.unitValue
                          });
                        }
                        setSubClassPickerVisible(false);
                      }}
                    >
                      <View style={{ flex: 1 }}>
                        <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                          {sub.subClass}
                        </Text>
                        <Text style={styles.typeOptionSubtext}>
                          Unit Value: ₱{sub.unitValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                        </Text>
                      </View>
                      {isSelected && (
                        <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                      )}
                    </TouchableOpacity>
                  );
                });
              })()}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Actual Use Picker Modal */}
      <Modal
        visible={actualUsePickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setActualUsePickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setActualUsePickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Actual Use</Text>
              <TouchableOpacity onPress={() => setActualUsePickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {LAND_ACTUAL_USES.map((u) => {
                const currentRow = editingClassRow || (activeClassRowIndex !== null ? classificationRows[activeClassRowIndex] : null);
                const isSelected = currentRow?.actualUseName === u;
                return (
                  <TouchableOpacity
                    key={u}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (editingClassRow) {
                        setEditingClassRow({
                          ...editingClassRow,
                          actualUseName: u
                        });
                      } else if (activeClassRowIndex !== null) {
                        updateClassificationRow(activeClassRowIndex, { actualUseName: u });
                      }
                      setActualUsePickerVisible(false);
                    }}
                  >
                    <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                      {u}
                    </Text>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Improvement Type Picker Modal */}
      <Modal
        visible={impTypePickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setImpTypePickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setImpTypePickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Plant / Improvement Type</Text>
              <TouchableOpacity onPress={() => setImpTypePickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {LAND_IMPROVEMENT_TYPES.map((imp) => {
                const currentRow = editingImpRow || (activeImpRowIndex !== null ? improvementRows[activeImpRowIndex] : null);
                const isSelected = currentRow?.typeId === imp.id;
                return (
                  <TouchableOpacity
                    key={imp.id}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (editingImpRow) {
                        const q = parseFloat(editingImpRow.quantity) || 0;
                        setEditingImpRow({
                          ...editingImpRow,
                          typeId: imp.id,
                          typeDescription: imp.description,
                          unitValue: String(imp.baseValue),
                          marketValue: Math.round(q * imp.baseValue * 100) / 100
                        });
                      } else if (activeImpRowIndex !== null) {
                        updateImprovementRow(activeImpRowIndex, {
                          typeId: imp.id,
                          typeDescription: imp.description,
                          unitValue: imp.baseValue
                        });
                      }
                      setImpTypePickerVisible(false);
                    }}
                  >
                    <View style={{ flex: 1 }}>
                      <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                        {imp.description}
                      </Text>
                      <Text style={styles.typeOptionSubtext}>
                        Rate: ₱{imp.baseValue.toLocaleString('en-US', { minimumFractionDigits: 2 })} • Kind: {imp.kind}
                      </Text>
                    </View>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Adjustment Factor Picker Modal */}
      <Modal
        visible={adjFactorPickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setAdjFactorPickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setAdjFactorPickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Adjustment Factor</Text>
              <TouchableOpacity onPress={() => setAdjFactorPickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {LAND_ADJUSTMENT_FACTORS.map((adj) => {
                const currentRow = editingAdjRow || (activeAdjRowIndex !== null ? adjustmentRows[activeAdjRowIndex] : null);
                const isSelected = currentRow?.adjustmentFactorId === adj.id;
                return (
                  <TouchableOpacity
                    key={adj.id}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (editingAdjRow) {
                        const baseMv = parseFloat(editingAdjRow.baseMarketValue) || 0;
                        const valAdj = Math.round(baseMv * (adj.percent / 100) * 100) / 100;
                        setEditingAdjRow({
                          ...editingAdjRow,
                          adjustmentFactorId: adj.id,
                          adjustmentFactorDescription: adj.description,
                          percentAdjustment: String(adj.percent),
                          valueAdjustment: valAdj,
                          adjustedMarketValue: Math.round((baseMv + valAdj) * 100) / 100
                        });
                      } else if (activeAdjRowIndex !== null) {
                        updateAdjustmentRow(activeAdjRowIndex, {
                          adjustmentFactorId: adj.id,
                          adjustmentFactorDescription: adj.description,
                          percentAdjustment: adj.percent
                        });
                      }
                      setAdjFactorPickerVisible(false);
                    }}
                  >
                    <View style={{ flex: 1 }}>
                      <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                        {adj.description}
                      </Text>
                      <Text style={[
                        styles.typeOptionSubtext,
                        { color: adj.percent >= 0 ? (isDarkMode ? '#34d399' : '#059669') : '#ef4444', fontWeight: '700' }
                      ]}>
                        {adj.percent >= 0 ? `+${adj.percent}%` : `${adj.percent}%`} Adjustment
                      </Text>
                    </View>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Assessment Roll Kind Picker Modal */}
      <Modal
        visible={rollKindPickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setRollKindPickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setRollKindPickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Property Component</Text>
              <TouchableOpacity onPress={() => setRollKindPickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {['LAND', 'PLANTS / TREES', 'OTHER IMPROVEMENTS', 'MACHINERY / OTHERS'].map((k) => {
                const currentRow = editingRollRow || (activeRollRowIndex !== null ? assessmentRollRows[activeRollRowIndex] : null);
                const isSelected = currentRow?.kinds === k;
                return (
                  <TouchableOpacity
                    key={k}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (editingRollRow) {
                        setEditingRollRow({
                          ...editingRollRow,
                          kinds: k
                        });
                      } else if (activeRollRowIndex !== null) {
                        updateAssessmentRollRow(activeRollRowIndex, { kinds: k });
                      }
                      setRollKindPickerVisible(false);
                    }}
                  >
                    <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                      {k}
                    </Text>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Assessment Roll Actual Use Picker Modal */}
      <Modal
        visible={rollActualUsePickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setRollActualUsePickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setRollActualUsePickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Actual Use & Assessment Level</Text>
              <TouchableOpacity onPress={() => setRollActualUsePickerVisible(false)}>
                <Ionicons name="close" size={24} color={colors.textMuted} />
              </TouchableOpacity>
            </View>
            <ScrollView style={{ maxHeight: 380 }} showsVerticalScrollIndicator={false}>
              {LAND_ACTUAL_USES.map((u) => {
                const currentRow = editingRollRow || (activeRollRowIndex !== null ? assessmentRollRows[activeRollRowIndex] : null);
                const isSelected = currentRow?.actualUseName === u;
                const level = getAssessmentLevelForActualUse(u);
                return (
                  <TouchableOpacity
                    key={u}
                    style={[styles.typeOptionItem, isSelected && styles.typeOptionItemSelected]}
                    onPress={() => {
                      if (editingRollRow) {
                        const mv = parseFloat(editingRollRow.marketValue) || 0;
                        setEditingRollRow({
                          ...editingRollRow,
                          actualUseName: u,
                          assessmentLevelPercent: String(level),
                          assessedValue: Math.round(mv * (level / 100) * 100) / 100
                        });
                      } else if (activeRollRowIndex !== null) {
                        updateAssessmentRollRow(activeRollRowIndex, {
                          actualUseName: u,
                          assessmentLevelPercent: level
                        });
                      }
                      setRollActualUsePickerVisible(false);
                    }}
                  >
                    <View style={{ flex: 1 }}>
                      <Text style={[styles.typeOptionText, isSelected && styles.typeOptionTextSelected]}>
                        {u}
                      </Text>
                      <Text style={styles.typeOptionSubtext}>
                        Statutory Assessment Level: {level}%
                      </Text>
                    </View>
                    {isSelected && (
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Discard Changes Modal */}
      <DiscardChangesModal
        visible={discardModalVisible}
        onClose={() => setDiscardModalVisible(false)}
        onConfirmDiscard={navigateBack}
        partitionLetter={partitionLetterParam}
      />

      {/* Save & Offline Queue Completion Modal */}
      <ActionModal
        visible={completeModalConfig.visible}
        title={completeModalConfig.title}
        message={completeModalConfig.message}
        icon={completeModalConfig.icon}
        iconType={completeModalConfig.iconType}
        buttons={[
          {
            text: 'OK',
            style: 'primary',
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

const getStyles = (colors, isDarkMode) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.bgLight
  },
  ribbon: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 8,
    gap: 8,
    borderBottomWidth: 1
  },
  ribbonLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flex: 1
  },
  ribbonNetTag: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 7,
    paddingVertical: 3,
    borderRadius: 10,
    borderWidth: 1,
    gap: 4
  },
  ribbonNetOnline: {
    backgroundColor: isDarkMode ? 'rgba(5, 150, 105, 0.2)' : '#ecfdf5',
    borderColor: isDarkMode ? '#059669' : '#a7f3d0'
  },
  ribbonNetOffline: {
    backgroundColor: isDarkMode ? 'rgba(217, 119, 6, 0.2)' : '#fffbeb',
    borderColor: isDarkMode ? '#d97706' : '#fde68a'
  },
  ribbonNetDot: {
    width: 6,
    height: 6,
    borderRadius: 3
  },
  ribbonNetDotOnline: {
    backgroundColor: '#059669'
  },
  ribbonNetDotOffline: {
    backgroundColor: '#d97706'
  },
  ribbonNetText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.5
  },
  ribbonNetTextOnline: {
    color: isDarkMode ? '#34d399' : '#059669'
  },
  ribbonNetTextOffline: {
    color: isDarkMode ? '#fbbf24' : '#d97706'
  },
  ribbonDownloaded: {
    backgroundColor: isDarkMode ? '#064e3b' : '#dcfce7',
    borderBottomColor: isDarkMode ? '#065f46' : '#bbf7d0'
  },
  ribbonStandard: {
    backgroundColor: isDarkMode ? '#0c4a6e' : '#e0f2fe',
    borderBottomColor: isDarkMode ? '#075985' : '#bae6fd'
  },
  ribbonText: {
    fontSize: 12,
    fontWeight: '800',
    color: isDarkMode ? '#38bdf8' : colors.primaryDark
  },
  topTabsWrapper: {
    backgroundColor: colors.cardBg,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: isDarkMode ? 0.3 : 0.06,
    shadowRadius: 3,
    elevation: 3,
    zIndex: 10
  },
  bannerInfoBox: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    paddingHorizontal: 16,
    paddingVertical: 6,
    backgroundColor: isDarkMode ? '#064e3b22' : '#ecfdf5',
    borderBottomWidth: 1,
    borderBottomColor: isDarkMode ? '#065f4633' : '#a7f3d0'
  },
  bannerInfoItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4
  },
  bannerInfoLabel: {
    fontSize: 11,
    fontWeight: '800',
    color: isDarkMode ? '#6ee7b7' : '#047857'
  },
  bannerInfoValue: {
    fontSize: 11,
    fontWeight: '800',
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace',
    color: isDarkMode ? '#a7f3d0' : '#065f46'
  },
  sectionTabsContainer: {
    backgroundColor: colors.cardBg
  },
  sectionTabsScroll: {
    paddingHorizontal: 10,
    paddingVertical: 8,
    gap: 8,
    alignItems: 'center'
  },
  sectionTabBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 12,
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f1f5f9',
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  sectionTabBtnActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
    shadowColor: colors.primary,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3
  },
  sectionTabText: {
    fontSize: 12,
    fontWeight: '700',
    color: isDarkMode ? colors.textDark : '#475569'
  },
  sectionTabTextActive: {
    color: '#ffffff',
    fontWeight: '800'
  },
  scrollContent: {
    padding: 16,
    gap: 14,
    paddingBottom: 50
  },
  tabContentBlock: {
    gap: 14
  },
  card: {
    backgroundColor: colors.cardBg,
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: colors.border,
    gap: 12
  },
  cardHeaderTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: isDarkMode ? colors.primary : colors.primaryDark,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 8,
    textTransform: 'uppercase'
  },
  cardHeaderRow: {
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 8
  },
  boundarySection: {
    backgroundColor: colors.surfaceSecondary,
    padding: 10,
    borderRadius: 10,
    gap: 4
  },
  boundaryTitle: {
    fontSize: 11,
    fontWeight: '800',
    color: isDarkMode ? colors.primary : colors.primaryDark,
    letterSpacing: 0.5
  },
  inputGroup: {
    gap: 4
  },
  inputLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textDark,
    textTransform: 'uppercase'
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
    backgroundColor: isDarkMode ? '#1e293b66' : '#f1f5f9',
    borderColor: isDarkMode ? '#334155' : '#e2e8f0',
    color: isDarkMode ? '#94a3b8' : '#64748b',
    fontWeight: '700',
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace'
  },
  dateInputWrapper: {
    flexDirection: 'row',
    alignItems: 'stretch',
    width: '100%'
  },
  dateInputControl: {
    flex: 1,
    borderTopRightRadius: 0,
    borderBottomRightRadius: 0,
    borderRightWidth: 0
  },
  datePickerTriggerBtn: {
    backgroundColor: colors.primary,
    width: 38,
    alignItems: 'center',
    justifyContent: 'center',
    borderTopRightRadius: 10,
    borderBottomRightRadius: 10,
    borderWidth: 1,
    borderColor: colors.primary
  },
  valuationInput: {
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : colors.primaryDark,
    backgroundColor: isDarkMode ? '#064e3b' : '#f0fdf4'
  },
  rowInputs: {
    flexDirection: 'row',
    gap: 10
  },
  optionPillRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
    marginTop: 2
  },
  optionPill: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f1f5f9',
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  optionPillActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary
  },
  optionPillText: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  optionPillTextActive: {
    color: '#ffffff',
    fontWeight: '800'
  },
  rolePillsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    paddingBottom: 4
  },
  rolePill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f8fafc',
    borderWidth: 1,
    borderColor: colors.border
  },
  rolePillActive: {
    backgroundColor: isDarkMode ? '#064e3b33' : '#ecfdf5',
    borderColor: colors.primary
  },
  rolePillText: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  rolePillTextActive: {
    color: isDarkMode ? '#34d399' : '#047857',
    fontWeight: '800'
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 4
  },
  switchTitle: {
    fontSize: 12,
    fontWeight: '800',
    color: colors.textDark,
    textTransform: 'uppercase'
  },
  switchSubtitle: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2
  },
  kpiGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    marginTop: 8
  },
  kpiCard: {
    flex: 1,
    minWidth: '45%',
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f8fafc',
    borderRadius: 12,
    padding: 10,
    borderWidth: 1,
    borderColor: colors.border
  },
  kpiLabel: {
    fontSize: 10,
    fontWeight: '800',
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginBottom: 2
  },
  kpiValue: {
    fontSize: 13,
    fontWeight: '900',
    color: colors.textDark,
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace'
  },
  nextStepBox: {
    backgroundColor: colors.surfaceSecondary,
    padding: 10,
    borderRadius: 10,
    gap: 4
  },
  nextStepLabel: {
    fontSize: 11,
    fontWeight: '700',
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
    color: colors.primary
  },
  buttonContainer: {
    gap: 10,
    marginTop: 8
  },
  saveBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 14,
    borderRadius: 14,
    gap: 8
  },
  offlineNoticeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fef3c7',
    borderWidth: 1,
    borderColor: '#fde68a',
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    marginHorizontal: 16,
    marginBottom: 8,
    gap: 6
  },
  offlineNoticeText: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
    color: '#92400e'
  },
  saveBtnTransfer: {
    backgroundColor: '#059669'
  },
  saveBtnOffline: {
    backgroundColor: '#0284c7'
  },
  saveBtnPrimary: {
    backgroundColor: colors.primary
  },
  saveBtnText: {
    color: '#ffffff',
    fontSize: 14,
    fontWeight: '800'
  },
  saveBtnSecondary: {
    backgroundColor: colors.cardBg,
    borderWidth: 1,
    borderColor: colors.primary
  },
  saveBtnSecondaryText: {
    color: colors.primary,
    fontSize: 13,
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
    backgroundColor: colors.cardBg,
    borderRadius: 18,
    padding: 24,
    marginTop: 20,
    borderWidth: 1,
    borderColor: colors.border
  },
  notDownloadedTitle: {
    fontSize: 17,
    fontWeight: '800',
    color: colors.textDark,
    marginTop: 12,
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
    gap: 8,
    backgroundColor: colors.primary,
    paddingHorizontal: 18,
    paddingVertical: 12,
    borderRadius: 12
  },
  backBtnText: {
    color: '#ffffff',
    fontWeight: '800',
    fontSize: 13
  },
  selectBtn: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    backgroundColor: isDarkMode ? colors.inputBg : '#f8fafc'
  },
  selectBtnText: {
    fontSize: 13,
    color: colors.inputText,
    flex: 1
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: colors.modalOverlay || 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end'
  },
  modalContent: {
    backgroundColor: colors.cardBg,
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
    maxHeight: '75%'
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 10
  },
  modalTitle: {
    fontSize: 15,
    fontWeight: '800',
    color: isDarkMode ? colors.primary : colors.primaryDark
  },
  typeOptionItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 12,
    borderRadius: 8,
    marginBottom: 4
  },
  typeOptionItemSelected: {
    backgroundColor: colors.primaryLight
  },
  typeOptionText: {
    fontSize: 13,
    color: colors.textDark,
    fontWeight: '500',
    flex: 1
  },
  typeOptionTextSelected: {
    color: isDarkMode ? colors.primary : colors.primaryDark,
    fontWeight: '800'
  },
  typeOptionSubtext: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2
  },
  tableHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 8,
    gap: 8
  },
  tableHeaderSubtitle: {
    fontSize: 11,
    color: colors.textMuted,
    marginTop: 2
  },
  tableItemCard: {
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f8fafc',
    borderRadius: 12,
    padding: 12,
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0',
    gap: 10,
    marginTop: 8
  },
  tableItemHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingBottom: 6,
    borderBottomWidth: 1,
    borderBottomColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  tableItemIndexBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    flex: 1
  },
  tableItemIndexText: {
    fontSize: 11,
    fontWeight: '800',
    color: '#ffffff',
    backgroundColor: colors.primary,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6
  },
  tableItemTitleText: {
    fontSize: 12,
    fontWeight: '800',
    color: colors.textDark,
    flex: 1
  },
  tableRemoveBtn: {
    padding: 6,
    borderRadius: 6,
    backgroundColor: isDarkMode ? '#450a0a' : '#fee2e2'
  },
  tableEmptyCard: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 24,
    paddingHorizontal: 16,
    borderRadius: 12,
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f8fafc',
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0',
    borderStyle: 'dashed',
    marginTop: 8,
    gap: 6
  },
  tableEmptyTitle: {
    fontSize: 13,
    fontWeight: '800',
    color: colors.textDark,
    marginTop: 4
  },
  tableEmptySubtitle: {
    fontSize: 11,
    color: colors.textMuted,
    textAlign: 'center',
    lineHeight: 16
  },
  tableSummaryBox: {
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f1f5f9',
    borderRadius: 10,
    padding: 10,
    marginTop: 6,
    gap: 4,
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  tableSummaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  tableSummaryLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
  },
  tableSummaryVal: {
    fontSize: 12,
    fontWeight: '800',
    color: colors.textDark,
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace'
  },
  calculatedBox: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 8,
    backgroundColor: isDarkMode ? '#064e3b33' : '#ecfdf5',
    borderWidth: 1,
    borderColor: isDarkMode ? '#065f4655' : '#a7f3d0'
  },
  calculatedLabel: {
    fontSize: 10,
    fontWeight: '800',
    color: isDarkMode ? '#6ee7b7' : '#047857',
    textTransform: 'uppercase'
  },
  calculatedValue: {
    fontSize: 12,
    fontWeight: '900',
    color: isDarkMode ? '#34d399' : '#047857',
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace'
  },
  btnSm: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8
  },
  btnSmPrimary: {
    backgroundColor: colors.primary
  },
  btnSmSecondary: {
    backgroundColor: colors.cardBg,
    borderWidth: 1,
    borderColor: colors.primary
  },
  btnSmText: {
    color: '#ffffff',
    fontSize: 11,
    fontWeight: '800'
  },
  btnSmSecondaryText: {
    color: colors.primary,
    fontSize: 11,
    fontWeight: '800'
  },
  compactTableList: {
    gap: 8,
    marginTop: 8
  },
  compactTableRow: {
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#ffffff',
    borderRadius: 12,
    padding: 12,
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10
  },
  compactRowMain: {
    flex: 1,
    gap: 4
  },
  compactRowHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: 6
  },
  compactIndexBadge: {
    backgroundColor: colors.primary,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6
  },
  compactIndexText: {
    color: '#ffffff',
    fontSize: 10,
    fontWeight: '800'
  },
  compactBadge: {
    backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9',
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: isDarkMode ? '#334155' : '#cbd5e1'
  },
  compactBadgeText: {
    fontSize: 10,
    fontWeight: '700',
    color: colors.textDark
  },
  compactRowSubtext: {
    fontSize: 11,
    color: colors.textMuted
  },
  compactRowValue: {
    fontSize: 13,
    fontWeight: '900',
    color: isDarkMode ? '#34d399' : '#059669',
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace'
  },
  compactRowActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  compactActionBtn: {
    padding: 8,
    borderRadius: 8,
    backgroundColor: isDarkMode ? '#1e293b' : '#f1f5f9',
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  compactActionBtnDanger: {
    padding: 8,
    borderRadius: 8,
    backgroundColor: isDarkMode ? '#450a0a' : '#fee2e2',
    borderWidth: 1,
    borderColor: isDarkMode ? '#7f1d1d' : '#fecaca'
  },
  modalFooterBtnRow: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 8,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border
  },
  modalCancelBtn: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f8fafc'
  },
  modalCancelBtnText: {
    fontSize: 13,
    fontWeight: '700',
    color: colors.textMuted
  },
  modalSaveBtn: {
    flex: 2,
    paddingVertical: 12,
    borderRadius: 10,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    flexDirection: 'row',
    gap: 6
  },
  modalSaveBtnText: {
    fontSize: 13,
    fontWeight: '800',
    color: '#ffffff'
  }
});
