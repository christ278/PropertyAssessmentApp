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
  KeyboardAvoidingView,
  useWindowDimensions,
  Keyboard
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

const TABS = [
  { label: '1. Property Info & Taxpayer (CCT)', icon: 'information-circle-outline' },
  { label: '2. Structural Specs & 17 Materials', icon: 'business-outline' },
  { label: '3. Floor Levels, Additions & Valuation', icon: 'calculator-outline' },
  { label: '4. Back Taxes, Effectivity & Attachments', icon: 'document-text-outline' },
];

const FLOOR_LEVEL_OPTIONS = [
  '1st Floor',
  '2nd Floor',
  '3rd Floor',
  '4th Floor',
  '5th Floor',
  '6th Floor',
  'Basement',
  'Mezzanine',
  'Roof Deck',
  'Penthouse'
];

const RATE_MODE_OPTIONS = ['Min', 'Average', 'Max'];

const STANDARD_ADDITIONAL_ITEMS = [
  { desc: 'Fence (Concrete Hollow Block / Iron Grill)', rate: 1200 },
  { desc: 'Terrace / Open Balcony', rate: 4500 },
  { desc: 'Covered Carport / Garage', rate: 3500 },
  { desc: 'Mezzanine / Loft Structure', rate: 5500 },
  { desc: 'Pavement / Concrete Driveway', rate: 850 },
  { desc: 'Swimming Pool & Pump Structure', rate: 9500 },
  { desc: 'Water Tank & Elevated Tower', rate: 15000 },
  { desc: 'Custom Additional Item', rate: 1000 }
];

const GENDER_OPTIONS = ['Male', 'Female'];

const CIVIL_STATUS_OPTIONS = [
  'Single',
  'Married',
  'Widowed',
  'Divorced',
  'Legally Separated',
  'N/A (Corporate / Entity)'
];

const BUILDING_PROPERTY_TYPES = [
  'Residential Condominium',
  'Townhouse',
  'Commercial Condominium',
  'Industrial Condominium',
  'Single Detached House',
  'Two Family Dwelling',
  'Apartment / Rowhouse',
  'Boarding / Lodging House',
  'Warehouse / Factory',
  'Commercial Mall / Store',
  'Office / Bank Building',
  'School / Institutional',
  'Hospital / Clinic',
  'Other Type of Building and/or Structure'
];

const STRUCTURAL_TYPES = [
  'I-A',
  'I-B',
  'II-A',
  'II-B',
  'III-A',
  'IV-A'
];

const BUILDING_CLASSIFICATIONS = [
  'Class A - Reinforced Concrete',
  'Class B - Semi-Concrete / Hollow Blocks',
  'Class C - Reinforced Concrete & Wood',
  'Class D - Wood / Timber Frame',
  'Class E - Light Materials / Mixed'
];

const STRUCTURAL_MATERIAL_OPTIONS = {
  foundation: ['Reinforced Concrete (RC)', 'Plain Concrete (PC)', 'Stone / Rubble Footing'],
  columns: ['Reinforced Concrete (RC)', 'Steel (S)', 'Wood / Timber (W)'],
  beams: ['Reinforced Concrete (RC)', 'Steel (S)', 'Wood / Timber (W)'],
  trussRoof: ['Steel Truss (S)', 'Reinforced Concrete (RC)', 'Wood / Timber (W)'],
  roof: [
    'Long Span G.I. Sheet (LGI)',
    'Corrugated G.I. Sheet (CGI)',
    'Concrete Slab (CS)',
    'Ceramic / Clay Tiles (CT)',
    'Solid Polycarbonate (SPC)',
    'Aluminum (Al)'
  ],
  exteriorWalls: [
    'Concrete Hollow Block (CHB)',
    'Reinforced Concrete (RC)',
    'Aluminum Composite (ALP)',
    'Plain Concrete (PC)',
    'Wood / Timber (W)'
  ],
  flooring: ['Reinforced Concrete Slab (RC)', 'Plain Cement (PC)', 'Wood Planks / T&G (W)'],
  doors: ['Wood Panel (WP)', 'Glass / Aluminum (G/Al)', 'Steel Door (S)', 'Flush Door (FD)'],
  ceiling: [
    'Hardiflex / Fiber Cement (Hx)',
    'Gypsum Board / Duraflex (D)',
    'Plywood (Pw)',
    'Aluminum Framing (AF)'
  ],
  windows: [
    'Aluminum Casement / Sliding (AC)',
    'Steel Casement with Grills (SCG)',
    'Glass Jalousie / Fixed (G)',
    'Wood Casement (WC)'
  ],
  stairs: ['Reinforced Concrete (RC)', 'Steel (S)', 'Wood (W)'],
  partitionWalls: [
    'Drywall Hardiflex (Hx)',
    'Concrete Hollow Block (CHB)',
    'Plywood / Plyboard (Pw)',
    'Aluminum Glass Panel (AIF)'
  ],
  floorFinish: [
    'Ceramic / Granite Tiles (T)',
    'Marble Slabs / Tiles (M)',
    'Plain Cement Plaster (PC)',
    'Wood Vinyl / Parquet (W)'
  ],
  wallFinish: [
    'Cement Plaster Paint Finish (CP)',
    'Decorative Tiles / Stones (T)',
    'Washout Pebbles (WOP)'
  ],
  electrical: [
    'Poly Vinyl Carbonate Conduit (EPVC)',
    'Metal Conduit / Molding (MM)',
    'Concealed Knob & Tube'
  ],
  toiletAndBath: [
    'Ceramic Tiled with Fixtures (CT)',
    'Marble with Premium Fixtures (M)',
    'Plain Cement with Basic Fixtures (PC)'
  ],
  plumbing: [
    'PVC Pipes to Septic Tank (PVCP)',
    'Galvanized Iron Pipes (GIP)',
    'Cast Iron / Concrete Pipes (CP)'
  ]
};

export const BuildingFormScreen = ({ route, navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const { width } = useWindowDimensions();
  const isLargeScreen = width >= 600;
  const styles = React.useMemo(() => getStyles(colors, isDarkMode, isLargeScreen), [colors, isDarkMode, isLargeScreen]);

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
  const isDownloaded = isTransactionDownloaded(trackingNoParam, formWorkflowIdParam);

  // Active section tab (0: Property Info & Taxpayer (CCT), 1: Structural Specs, 2: Valuation, 3: Back Taxes & Notes)
  const [activeTab, setActiveTab] = useState(0);
  const tabScrollRef = useRef(null);

  // Reusable Picker Modal State
  const [pickerModal, setPickerModal] = useState({
    visible: false,
    title: '',
    options: [],
    selectedValue: '',
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

  // 1. Property Identification & Ownership
  const [tdnId, setTdnId] = useState(route?.params?.tdnId || null);
  const [castTdnBuildingId, setCastTdnBuildingId] = useState(null);
  const [trackingNo, setTrackingNo] = useState(trackingNoParam);
  const [declarationNo, setDeclarationNo] = useState('');
  const [arpNo, setArpNo] = useState('');
  const [pin, setPin] = useState('');
  const [propertyTypeAccordUse, setPropertyTypeAccordUse] = useState('Residential Condominium');
  const [otherPropertyTypeAccordUse, setOtherPropertyTypeAccordUse] = useState('');
  const [areaSqMeters, setAreaSqMeters] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [isAdministrator, setIsAdministrator] = useState(false);
  const [isBeneficialUser, setIsBeneficialUser] = useState(false);
  const [adminName, setAdminName] = useState('');
  const [adminGender, setAdminGender] = useState('Male');
  const [adminCivilStatus, setAdminCivilStatus] = useState('Single');
  const [adminTin, setAdminTin] = useState('');
  const [adminContact, setAdminContact] = useState('');
  const [adminEmail, setAdminEmail] = useState('');
  const [adminAddress, setAdminAddress] = useState('');
  const [ownerGender, setOwnerGender] = useState('Male');
  const [ownerCivilStatus, setOwnerCivilStatus] = useState('Single');
  const [ownerTin, setOwnerTin] = useState('');
  const [ownerContact, setOwnerContact] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [streetLocation, setStreetLocation] = useState('');
  const [districtOrBarangay, setDistrictOrBarangay] = useState('');
  const [cityMunicipality, setCityMunicipality] = useState('Koronadal City');
  const [province, setProvince] = useState('South Cotabato');
  const [landOwnerName, setLandOwnerName] = useState('');
  const [landTdNo, setLandTdNo] = useState('');
  const [landPin, setLandPin] = useState('');
  const [isCct, setIsCct] = useState(false);
  const [cctTitleNo, setCctTitleNo] = useState('');
  const [cctDateRegistered, setCctDateRegistered] = useState('');
  const [ownerNameReflectedInCct, setOwnerNameReflectedInCct] = useState('');
  const [buildingClassification, setBuildingClassification] = useState('Class A - Reinforced Concrete');
  const [occupancyPermitNo, setOccupancyPermitNo] = useState('');

  // 17 Structural Parts & Materials (CAST Reference)
  const [structuralFoundation, setStructuralFoundation] = useState('Reinforced Concrete (RC)');
  const [structuralColumns, setStructuralColumns] = useState('Reinforced Concrete (RC)');
  const [structuralBeams, setStructuralBeams] = useState('Reinforced Concrete (RC)');
  const [structuralTrussRoof, setStructuralTrussRoof] = useState('Steel Truss (S)');
  const [structuralRoof, setStructuralRoof] = useState('Long Span G.I. Sheet (LGI)');
  const [structuralExteriorWalls, setStructuralExteriorWalls] = useState('Concrete Hollow Block (CHB)');
  const [structuralFlooring, setStructuralFlooring] = useState('Reinforced Concrete Slab (RC)');
  const [doorMain, setDoorMain] = useState('Wood Panel (WP)');
  const [ceiling, setCeiling] = useState('Hardiflex / Fiber Cement (Hx)');
  const [structuralWindows, setStructuralWindows] = useState('Aluminum Casement / Sliding (AC)');
  const [structuralStairs, setStructuralStairs] = useState('Reinforced Concrete (RC)');
  const [structuralPartitionWalls, setStructuralPartitionWalls] = useState('Drywall Hardiflex (Hx)');
  const [structuralFloorFinish, setStructuralFloorFinish] = useState('Ceramic / Granite Tiles (T)');
  const [structuralFinishFront, setStructuralFinishFront] = useState('Cement Plaster Paint Finish (CP)');
  const [electricalSystem, setElectricalSystem] = useState('Poly Vinyl Carbonate Conduit (EPVC)');
  const [toiletAndBathroom, setToiletAndBathroom] = useState('Ceramic Tiled with Fixtures (CT)');
  const [plumbingSewer, setPlumbingSewer] = useState('PVC Pipes to Septic Tank (PVCP)');

  // Back Taxes & Assessment Basis
  const [assessmentBasis, setAssessmentBasis] = useState('Sec. 217 RA 7160');
  const [backTaxesYearFrom, setBackTaxesYearFrom] = useState('');
  const [backTaxesYearTo, setBackTaxesYearTo] = useState('');
  const [unitValue, setUnitValue] = useState('');
  const [surveyNo, setSurveyNo] = useState('');
  const [lotNo, setLotNo] = useState('');
  const [sectionNo, setSectionNo] = useState('');
  const [parcelNo, setParcelNo] = useState('');
  const [tctNo, setTctNo] = useState('');
  const [transactionCode, setTransactionCode] = useState('');

  // 2. Building Structure & Characteristics
  const [structuralType, setStructuralType] = useState('I-A');
  const [buildingMakeId, setBuildingMakeId] = useState('');
  const [classificationId, setClassificationId] = useState('');
  const [permitNo, setPermitNo] = useState('');
  const [dateConstructed, setDateConstructed] = useState('');
  const [dateCompleted, setDateCompleted] = useState('');
  const [dateOccupied, setDateOccupied] = useState('');
  const [activeDatePicker, setActiveDatePicker] = useState(null); // 'constructed' | 'completed' | 'occupied' | null
  const [storeyNo, setStoreyNo] = useState('1');
  const [areaPerFloor, setAreaPerFloor] = useState('');
  const [totalFloorArea, setTotalFloorArea] = useState('');
  const [landArea, setLandArea] = useState('');
  const [age, setAge] = useState('');

  // 3. Appraisal & Valuation
  const [subtotalValCompArea, setSubtotalValCompArea] = useState('');
  const [totalAddItem, setTotalAddItem] = useState('');
  const [depreciationRate, setDepreciationRate] = useState('');
  const [depreciationAccumulated, setDepreciationAccumulated] = useState('');
  const [depreciationMarketval, setDepreciationMarketval] = useState('');
  const [marketValue, setMarketValue] = useState('');
  const [assessmentLevel, setAssessmentLevel] = useState('20');
  const [assessedValue, setAssessedValue] = useState('');
  const [taxability, setTaxability] = useState(true);
  const [sketchUrl, setSketchUrl] = useState('');
  const [sketchBase64, setSketchBase64] = useState(null);
  const [sketchFileName, setSketchFileName] = useState('');
  const [attachments, setAttachments] = useState([]);

  // Floor Levels & Additional Improvements (CAST Dynamic Schedules)
  const [buildingFloors, setBuildingFloors] = useState([]);
  const [buildingAdditions, setBuildingAdditions] = useState([]);

  // Floor Modal States
  const [floorModalVisible, setFloorModalVisible] = useState(false);
  const [editingFloorIndex, setEditingFloorIndex] = useState(null);
  const [floorLevel, setFloorLevel] = useState('1st Floor');
  const [floorBuildingClass, setFloorBuildingClass] = useState('Class A - Reinforced Concrete');
  const [floorRateMode, setFloorRateMode] = useState('Min');
  const [floorArea, setFloorArea] = useState('');
  const [floorUnitCost, setFloorUnitCost] = useState('10000');
  const [floorDesc, setFloorDesc] = useState('');

  // Addition Modal States
  const [additionModalVisible, setAdditionModalVisible] = useState(false);
  const [editingAdditionIndex, setEditingAdditionIndex] = useState(null);
  const [additionDesc, setAdditionDesc] = useState(STANDARD_ADDITIONAL_ITEMS[0].desc);
  const [additionQuantity, setAdditionQuantity] = useState('1');
  const [additionRate, setAdditionRate] = useState(STANDARD_ADDITIONAL_ITEMS[0].rate.toString());
  const [additionMultiplier, setAdditionMultiplier] = useState('100');

  const safeParseArray = (val) => {
    if (!val) return null;
    try {
      const parsed = typeof val === 'string' ? JSON.parse(val) : val;
      if (Array.isArray(parsed) && parsed.length > 0) return parsed;
    } catch (e) {}
    return null;
  };

  // Live Valuation Calculations (CAST Aligned)
  const totalFloorAreaComputed = React.useMemo(() => {
    if (buildingFloors.length > 0) {
      return buildingFloors.reduce((sum, f) => sum + (parseFloat(f.floorAreaSqm) || 0), 0);
    }
    return parseFloat(totalFloorArea || areaSqMeters) || 0;
  }, [buildingFloors, totalFloorArea, areaSqMeters]);

  const baseRcnComputed = React.useMemo(() => {
    if (buildingFloors.length > 0) {
      return buildingFloors.reduce((sum, f) => sum + (parseFloat(f.marketValue) || 0), 0);
    }
    const a = parseFloat(totalFloorArea || areaSqMeters) || 0;
    const u = parseFloat(unitValue) || 0;
    return a * u || (parseFloat(subtotalValCompArea) || 0);
  }, [buildingFloors, totalFloorArea, areaSqMeters, unitValue, subtotalValCompArea]);

  const totalAdditionsComputed = React.useMemo(() => {
    if (buildingAdditions.length > 0) {
      return buildingAdditions.reduce((sum, a) => sum + (parseFloat(a.totalValue) || 0), 0);
    }
    return parseFloat(totalAddItem) || 0;
  }, [buildingAdditions, totalAddItem]);

  const deprRateNum = parseFloat(depreciationRate) || 0;
  const accDeprComputed = Math.round(baseRcnComputed * (deprRateNum / 100) * 100) / 100;
  const depreciatedCoreComputed = Math.max(0, Math.round((baseRcnComputed - accDeprComputed) * 100) / 100);
  const grandMarketValueComputed = Math.round((depreciatedCoreComputed + totalAdditionsComputed) * 100) / 100;

  const assessLevelNum = parseFloat(assessmentLevel) || 20;
  const assessedValueComputed = Math.round(grandMarketValueComputed * (assessLevelNum / 100) * 100) / 100;

  const basicRptComputed = Math.round(assessedValueComputed * 0.02 * 100) / 100;
  const sefTaxComputed = Math.round(assessedValueComputed * 0.01 * 100) / 100;
  const annualTaxDueComputed = Math.round((basicRptComputed + sefTaxComputed) * 100) / 100;

  const openAddFloorModal = () => {
    setEditingFloorIndex(null);
    const nextNum = buildingFloors.length + 1;
    const levelStr = nextNum === 1 ? '1st Floor' : nextNum === 2 ? '2nd Floor' : nextNum === 3 ? '3rd Floor' : `${nextNum}th Floor`;
    setFloorLevel(levelStr);
    setFloorBuildingClass(buildingClassification || 'Class A - Reinforced Concrete');
    setFloorRateMode('Min');
    setFloorArea('');
    setFloorUnitCost(unitValue || '10000');
    setFloorDesc(`${levelStr} (${buildingClassification || 'General Structure'})`);
    setFloorModalVisible(true);
  };

  const openEditFloorModal = (index) => {
    const f = buildingFloors[index];
    if (!f) return;
    setEditingFloorIndex(index);
    setFloorLevel(f.floorNumber || '1st Floor');
    setFloorBuildingClass(f.buildingClassification || buildingClassification || 'Class A - Reinforced Concrete');
    setFloorRateMode(f.rateMode || 'Min');
    setFloorArea((f.floorAreaSqm || '').toString());
    setFloorUnitCost((f.baseUnitCost || '').toString());
    setFloorDesc(f.description || '');
    setFloorModalVisible(true);
  };

  const saveFloorLevel = () => {
    const a = parseFloat(floorArea) || 0;
    const u = parseFloat(floorUnitCost) || 0;
    if (a <= 0) {
      Alert.alert('Required', 'Please enter a valid floor area in sq.m.');
      return;
    }
    const mv = Math.round(a * u * 100) / 100;
    const newFloor = {
      id: editingFloorIndex !== null && buildingFloors[editingFloorIndex]?.id ? buildingFloors[editingFloorIndex].id : 'flr_' + Date.now(),
      floorNumber: floorLevel,
      buildingClassification: floorBuildingClass,
      rateMode: floorRateMode,
      description: floorDesc || `${floorLevel} (${floorBuildingClass})`,
      floorAreaSqm: a,
      baseUnitCost: u,
      marketValue: mv
    };
    let updated;
    if (editingFloorIndex !== null) {
      updated = [...buildingFloors];
      updated[editingFloorIndex] = newFloor;
    } else {
      updated = [...buildingFloors, newFloor];
    }
    setBuildingFloors(updated);
    if (updated.length > parseInt(storeyNo || '1', 10)) {
      setStoreyNo(updated.length.toString());
    }
    setFloorModalVisible(false);
  };

  const removeFloor = (index) => {
    const updated = buildingFloors.filter((_, idx) => idx !== index);
    setBuildingFloors(updated);
  };

  const openAddAdditionModal = () => {
    setEditingAdditionIndex(null);
    setAdditionDesc(STANDARD_ADDITIONAL_ITEMS[0].desc);
    setAdditionQuantity('1');
    setAdditionRate(STANDARD_ADDITIONAL_ITEMS[0].rate.toString());
    setAdditionMultiplier('100');
    setAdditionModalVisible(true);
  };

  const openEditAdditionModal = (index) => {
    const a = buildingAdditions[index];
    if (!a) return;
    setEditingAdditionIndex(index);
    setAdditionDesc(a.description || '');
    setAdditionQuantity((a.quantity || '1').toString());
    setAdditionRate((a.unitRate || '0').toString());
    setAdditionMultiplier((a.multiplier || '100').toString());
    setAdditionModalVisible(true);
  };

  const saveAdditionItem = () => {
    const q = parseFloat(additionQuantity) || 1;
    const r = parseFloat(additionRate) || 0;
    const m = parseFloat(additionMultiplier) || 100;
    const total = Math.round((q * r) * (m / 100.0) * 100) / 100;

    const newItem = {
      id: editingAdditionIndex !== null && buildingAdditions[editingAdditionIndex]?.id ? buildingAdditions[editingAdditionIndex].id : 'add_' + Date.now(),
      description: additionDesc || 'Additional Item',
      quantity: q,
      unitRate: r,
      multiplier: m,
      totalValue: total
    };
    let updated;
    if (editingAdditionIndex !== null) {
      updated = [...buildingAdditions];
      updated[editingAdditionIndex] = newItem;
    } else {
      updated = [...buildingAdditions, newItem];
    }
    setBuildingAdditions(updated);
    setAdditionModalVisible(false);
  };

  const removeAddition = (index) => {
    const updated = buildingAdditions.filter((_, idx) => idx !== index);
    setBuildingAdditions(updated);
  };

  // 4. Effectivity & Memoranda
  const [yearEffectivity, setYearEffectivity] = useState(new Date().getFullYear().toString());
  const [quarterEffectivity, setQuarterEffectivity] = useState('1');
  const [memoranda, setMemoranda] = useState('');
  const [remarks, setRemarks] = useState('');

  // Workflow Forwarding
  const [autoForward, setAutoForward] = useState(true);
  const [nextStatusId, setNextStatusId] = useState(4); // 4 = Checked / Recommend Approval

  const hydrateEffectivityAndNotes = (p) => {
    if (!p) return;
    if (p.effectivityQuarter || p.EffectivityQuarter || p.quarterEffectivity || p.QuarterEffectivity) {
      const rawQ = (p.effectivityQuarter || p.EffectivityQuarter || p.quarterEffectivity || p.QuarterEffectivity).toString();
      const match = rawQ.match(/\d+/);
      setQuarterEffectivity(match ? match[0] : '1');
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
      setBackTaxesYearFrom((p.backTaxesYearFrom ?? p.BackTaxesYearFrom ?? p.backTaxesFrom).toString());
    }
    if (p.backTaxesYearTo != null || p.BackTaxesYearTo != null || p.backTaxesTo != null) {
      setBackTaxesYearTo((p.backTaxesYearTo ?? p.BackTaxesYearTo ?? p.backTaxesTo).toString());
    }
    if (p.memoranda || p.Memoranda) {
      setMemoranda(p.memoranda || p.Memoranda);
    }
    if (p.remarks || p.Remarks) {
      setRemarks(p.remarks || p.Remarks);
    }
  };

  useEffect(() => {
    loadBuildingData();
  }, []);

  const loadBuildingData = async () => {
    setNotDownloadedError(false);
    const targetIdx = parseInt(partitionIndexParam, 10) || 0;

    // 0. Load any locally saved partition offline attachments and sketch
    const offlineAttData = await loadPartitionAttachmentsOffline(trackingNoParam, partitionIndexParam);
    setAttachments(offlineAttData.attachments || []);
    setSketchUrl(offlineAttData.sketchUrl || '');

    // 1. Check if downloaded offline copy exists and populate immediately
    const downloaded = getDownloadedFormData(trackingNoParam, formWorkflowIdParam, partitionIndexParam);
    if (downloaded) {
      if (targetIdx === 0) {
        if (downloaded.owner) setOwnerName(downloaded.owner);
        if (downloaded.arpNo) setArpNo(downloaded.arpNo);
        if (downloaded.tdnId) setTdnId(downloaded.tdnId);
      }
      setSketchUrl(downloaded.sketchUrl || '');
      setAttachments(Array.isArray(downloaded.attachments) ? downloaded.attachments : []);
      if (downloaded.trackingNo && !trackingNo) setTrackingNo(downloaded.trackingNo);
      if (downloaded.transItem?.TrackingNo && !trackingNo) setTrackingNo(downloaded.transItem.TrackingNo);

      // Populate downloaded.formData only if targetIdx === 0 or if there is no partition isolation conflict
      if (downloaded.formData && (targetIdx === 0 || !downloaded.currentPartition)) {
        populateFieldsFromDTO(downloaded.formData);
        if (downloaded.formData.TDN) {
          hydrateEffectivityAndNotes(downloaded.formData.TDN);
        }
      }

      if (downloaded.currentPartition) {
        const cp = downloaded.currentPartition;
        if (cp.streetLocation || cp.streetNo || cp.buildingNo) {
          setStreetLocation(cp.streetLocation || cp.streetNo || cp.buildingNo);
        }
        if (cp.districtOrBarangay) {
          setDistrictOrBarangay(normalizeBarangay(cp.districtOrBarangay));
        }
        if (cp.cityMunicipality) setCityMunicipality(cp.cityMunicipality);
        if (cp.province) setProvince(cp.province);
        if (cp.propertyTypeAccordUse) setPropertyTypeAccordUse(cp.propertyTypeAccordUse);
        if (cp.otherPropertyTypeAccordUse) setOtherPropertyTypeAccordUse(cp.otherPropertyTypeAccordUse);
        if (cp.pin) setPin(cp.pin);
        const targetPartIdx = parseInt(partitionIndexParam, 10) || 0;
        if (targetPartIdx > 0) {
          // Partition 1's top-level formData was applied above; wipe it for later partitions
          setParcelNo(cp.parcelNo || '');
          setPin(cp.pin || '');
          setArpNo(cp.arpNo || '');
          setOwnerName(cp.ownerName || cp.newOwnerName || '');
          setOwnerAddress(cp.ownerAddress || '');
          setOwnerTin(cp.ownerTin || '');
          setOwnerContact(cp.ownerContactNo || cp.ownerContact || '');
          setOwnerEmail(cp.ownerEmail || '');
          setStreetLocation(cp.streetLocation || cp.streetNo || cp.buildingNo || '');
          setDistrictOrBarangay(cp.districtOrBarangay ? normalizeBarangay(cp.districtOrBarangay) : '');
          setLandOwnerName(cp.landOwnerName || '');
          setLandTdNo(cp.landTdNo || '');
          setLandPin(cp.landPin || '');
          setSurveyNo(cp.surveyNo || cp.surveyNumber || '');
          setLotNo(cp.lotNo || cp.lotNumber || '');
          setTctNo(cp.titleNo || '');
          setSectionNo(cp.sectionNo || '');
        } else {
          if (cp.surveyNo || cp.surveyNumber) setSurveyNo(cp.surveyNo || cp.surveyNumber);
          if (cp.lotNo || cp.lotNumber) setLotNo(cp.lotNo || cp.lotNumber);
          if (cp.titleNo) setTctNo(cp.titleNo);
          if (cp.sectionNo) setSectionNo(cp.sectionNo);
        }
        if (cp.arpNo) setArpNo(cp.arpNo);
        if (cp.systemGeneratedTDN) setDeclarationNo(cp.systemGeneratedTDN);
        const hasAdmin = !!cp.isAdministrator || !!cp.administratorName;
        const hasBenef = !hasAdmin && !!cp.isBeneficialUser;
        setIsAdministrator(hasAdmin);
        setIsBeneficialUser(hasBenef);
        if (cp.administratorName) setAdminName(cp.administratorName);
        if (cp.administratorAddress || cp.propAdminAddress) setAdminAddress(cp.administratorAddress || cp.propAdminAddress);
        if (cp.administratorContactNo || cp.propAdminContactNo) setAdminContact(cp.administratorContactNo || cp.propAdminContactNo);
        if (cp.administratorTin) setAdminTin(cp.administratorTin);
        if (cp.administratorEmail) setAdminEmail(cp.administratorEmail);
        if (cp.administratorGender) setAdminGender(cp.administratorGender);
        if (cp.administratorCivilStatus) setAdminCivilStatus(cp.administratorCivilStatus);
        if (cp.ownerGender) setOwnerGender(cp.ownerGender);
        if (cp.ownerCivilStatus) setOwnerCivilStatus(cp.ownerCivilStatus);
        if (cp.ownerTin) setOwnerTin(cp.ownerTin);
        if (cp.ownerContactNo || cp.ownerContact) setOwnerContact(cp.ownerContactNo || cp.ownerContact);
        if (cp.ownerEmail) setOwnerEmail(cp.ownerEmail);
        if (cp.ownerAddress) setOwnerAddress(cp.ownerAddress);
        if (cp.landOwnerName) setLandOwnerName(cp.landOwnerName);
        if (cp.landTdNo) setLandTdNo(cp.landTdNo);
        if (cp.landPin) setLandPin(cp.landPin);
        if (cp.isCct != null) setIsCct(!!cp.isCct);
        if (cp.cctTitleNo) setCctTitleNo(cp.cctTitleNo);
        if (cp.cctDateRegistered) setCctDateRegistered(cp.cctDateRegistered.toString().substring(0, 10));
        if (cp.ownerNameReflectedInCct) setOwnerNameReflectedInCct(cp.ownerNameReflectedInCct);
        if (cp.areaSqMeters != null && cp.areaSqMeters > 0) {
          setAreaSqMeters(cp.areaSqMeters.toString());
          setTotalFloorArea(cp.areaSqMeters.toString());
        }
        hydrateEffectivityAndNotes(cp);
        if (cp.systemGeneratedTDN) setDeclarationNo(cp.systemGeneratedTDN);
      }
    }

    // 2. If online and no unsynced offline edits, fetch latest live partition data (including newly uploaded web photos & sketches)
    const hasUnsynced = typeof hasPendingEdits === 'function' && hasPendingEdits(trackingNoParam, formWorkflowIdParam);
    if (!hasUnsynced && (trackingNoParam || formWorkflowIdParam)) {
      try {
        if (!downloaded) setLoading(true);
        const liveRes = await MobileApi.downloadTransactionDetails(formWorkflowIdParam, trackingNoParam, partitionIndexParam);
        if (liveRes && liveRes.success) {
          const targetIdx = parseInt(partitionIndexParam, 10) || 0;
          const livePart = (liveRes.partitions && liveRes.partitions[targetIdx]) || null;
          const liveAtts = Array.isArray(livePart?.attachments) ? livePart.attachments : (targetIdx === 0 ? (liveRes.attachments || []) : []);
          const liveSketch = livePart?.sketchUrl || (targetIdx === 0 ? liveRes.sketchUrl : null);

          const localAtts = (offlineAttData.attachments && offlineAttData.attachments.length > 0)
            ? offlineAttData.attachments
            : (downloaded?.currentPartition?.attachments || (targetIdx === 0 ? downloaded?.attachments : []) || []);

          if (localAtts.length > 0) {
            const merged = [...localAtts];
            if (Array.isArray(liveAtts)) {
              for (const la of liveAtts) {
                const laId = la?.id || la?.Id || la?.fileUrl || la?.FileUrl;
                if (!merged.some(m => (m?.id || m?.Id || m?.fileUrl || m?.FileUrl) === laId)) {
                  merged.push(la);
                }
              }
            }
            setAttachments(merged);
          } else {
            setAttachments(liveAtts);
          }

          if (offlineAttData.sketchUrl) {
            setSketchUrl(offlineAttData.sketchUrl);
          } else if (liveSketch) {
            setSketchUrl(liveSketch);
          } else if (downloaded?.currentPartition?.sketchUrl) {
            setSketchUrl(downloaded.currentPartition.sketchUrl);
          } else if (targetIdx === 0 && downloaded?.sketchUrl) {
            setSketchUrl(downloaded.sketchUrl);
          } else {
            setSketchUrl('');
          }

          if (liveRes.tdnId) setTdnId(liveRes.tdnId);

          if (livePart) {
            if (livePart.tdnId) setTdnId(livePart.tdnId);
            if (livePart.castTdnBuildingId) setCastTdnBuildingId(livePart.castTdnBuildingId);
            else if (livePart.formData?.CAST_TDNBuildingDTO?.CAST_TDNBuildingId) setCastTdnBuildingId(livePart.formData.CAST_TDNBuildingDTO.CAST_TDNBuildingId);
            if (livePart.systemGeneratedTDN) setDeclarationNo(livePart.systemGeneratedTDN);
            if (livePart.arpNo) setArpNo(livePart.arpNo);
            if (livePart.pin) setPin(livePart.pin);
            if (targetIdx > 0) {
              setSurveyNo(livePart.surveyNo || livePart.surveyNumber || '');
              setLotNo(livePart.lotNo || livePart.lotNumber || '');
              setTctNo(livePart.titleNo || '');
              setSectionNo(livePart.sectionNo || '');
            } else {
              if (livePart.surveyNo || livePart.surveyNumber) setSurveyNo(livePart.surveyNo || livePart.surveyNumber);
              if (livePart.lotNo || livePart.lotNumber) setLotNo(livePart.lotNo || livePart.lotNumber);
              if (livePart.titleNo) setTctNo(livePart.titleNo);
              if (livePart.sectionNo) setSectionNo(livePart.sectionNo);
            }
            const pAdmin = !!livePart.isAdministrator || !!livePart.administratorName;
            const pBenef = !pAdmin && !!livePart.isBeneficialUser;
            setIsAdministrator(pAdmin);
            setIsBeneficialUser(pBenef);
            if (livePart.administratorName) setAdminName(livePart.administratorName);
            if (livePart.administratorAddress || livePart.propAdminAddress) setAdminAddress(livePart.administratorAddress || livePart.propAdminAddress);
            if (livePart.administratorContactNo || livePart.propAdminContactNo) setAdminContact(livePart.administratorContactNo || livePart.propAdminContactNo);
            if (livePart.administratorTin) setAdminTin(livePart.administratorTin);
            if (livePart.administratorEmail) setAdminEmail(livePart.administratorEmail);
            if (livePart.administratorGender) setAdminGender(livePart.administratorGender);
            if (livePart.administratorCivilStatus) setAdminCivilStatus(livePart.administratorCivilStatus);
            if (livePart.ownerGender) setOwnerGender(livePart.ownerGender);
            if (livePart.ownerCivilStatus) setOwnerCivilStatus(livePart.ownerCivilStatus);
            if (livePart.ownerEmail) setOwnerEmail(livePart.ownerEmail);
            if (livePart.landOwnerName) setLandOwnerName(livePart.landOwnerName);
            if (livePart.landTdNo) setLandTdNo(livePart.landTdNo);
            if (livePart.landPin) setLandPin(livePart.landPin);
            if (livePart.isCct != null) setIsCct(!!livePart.isCct);
            if (livePart.cctTitleNo) setCctTitleNo(livePart.cctTitleNo);
            if (livePart.cctDateRegistered) setCctDateRegistered(livePart.cctDateRegistered.toString().substring(0, 10));
            if (livePart.ownerNameReflectedInCct) setOwnerNameReflectedInCct(livePart.ownerNameReflectedInCct);
            hydrateEffectivityAndNotes(livePart);
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
        // Offline or network error: gracefully keep using the offline copy
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
      if (t.PropertyTypeAccordUse) setPropertyTypeAccordUse(t.PropertyTypeAccordUse);
      if (t.OtherPropertyTypeAccordUse) setOtherPropertyTypeAccordUse(t.OtherPropertyTypeAccordUse);
      if (t.TotalArea != null && t.TotalArea > 0) {
        setAreaSqMeters(t.TotalArea.toString());
        setTotalFloorArea(t.TotalArea.toString());
      }
      if (t.DisplayName) setOwnerName(t.DisplayName);
      if (t.OwnerAddress) setOwnerAddress(t.OwnerAddress);
      if (t.OwnerGender) setOwnerGender(t.OwnerGender);
      if (t.OwnerCivilStatus) setOwnerCivilStatus(t.OwnerCivilStatus);
      if (t.OwnerTIN) setOwnerTin(t.OwnerTIN);
      if (t.OwnerContactNo) setOwnerContact(t.OwnerContactNo);
      if (t.OwnerEmail) setOwnerEmail(t.OwnerEmail);
      const tAdmin = !!t.IsAdministrator || !!t.AdministratorName || !!t.PropAdminName;
      const tBenef = !tAdmin && !!t.IsBeneficialUser;
      setIsAdministrator(tAdmin);
      setIsBeneficialUser(tBenef);
      if (t.AdministratorName || t.PropAdminName) setAdminName(t.AdministratorName || t.PropAdminName);
      if (t.AdministratorAddress || t.PropAdminAddress) setAdminAddress(t.AdministratorAddress || t.PropAdminAddress);
      if (t.AdministratorContactNo || t.PropAdminContactNo) setAdminContact(t.AdministratorContactNo || t.PropAdminContactNo);
      if (t.AdministratorTin) setAdminTin(t.AdministratorTin);
      if (t.AdministratorEmail) setAdminEmail(t.AdministratorEmail);
      if (t.AdministratorGender) setAdminGender(t.AdministratorGender);
      if (t.AdministratorCivilStatus) setAdminCivilStatus(t.AdministratorCivilStatus);
      if (t.PropLocationStreet) setStreetLocation(t.PropLocationStreet);
      else if (t.Location && !t.Location.includes(',')) setStreetLocation(t.Location);
      if (t.DistrictOrBarangay || t.Barangay) setDistrictOrBarangay(normalizeBarangay(t.DistrictOrBarangay || t.Barangay));
      if (t.CityMunicipality || t.Municipality) setCityMunicipality(t.CityMunicipality || t.Municipality);
      if (t.Province) setProvince(t.Province);
      if (t.AssessmentBasis) setAssessmentBasis(t.AssessmentBasis);
      if (t.BackTaxesYearFrom) setBackTaxesYearFrom(t.BackTaxesYearFrom.toString());
      if (t.BackTaxesYearTo) setBackTaxesYearTo(t.BackTaxesYearTo.toString());
      if (t.UnitValue) setUnitValue(t.UnitValue.toString());
      if (t.CadastralSurveyNo || t.SurveyNo) setSurveyNo(t.CadastralSurveyNo || t.SurveyNo);
      if (t.LotNo) setLotNo(t.LotNo);
      if (t.SectionNo) setSectionNo(t.SectionNo);
      if (t.ParcelNo) setParcelNo(t.ParcelNo);
      if (t.TCTNo || t.TitleNo) setTctNo(t.TCTNo || t.TitleNo);
      if (t.TransactionCode) setTransactionCode(t.TransactionCode);
      if (t.YearEffectivity != null) setYearEffectivity(t.YearEffectivity.toString());
      if (t.QuarterEffectivity != null) setQuarterEffectivity(t.QuarterEffectivity.toString());
      if (t.Memoranda) setMemoranda(t.Memoranda);
      if (t.Taxability != null) setTaxability(!!t.Taxability);
      if (t.FloorPlanSketchPath) setSketchUrl(t.FloorPlanSketchPath);
      const rawAtts = t.Attachments || t.attachments;
      if (rawAtts && Array.isArray(rawAtts) && rawAtts.length > 0) setAttachments(rawAtts);
    }

    if (d.refOwner) {
      const o = d.refOwner;
      if (!ownerName && (o.OwnerName || o.FullName)) setOwnerName(o.OwnerName || o.FullName);
      if (o.Gender && !ownerGender) setOwnerGender(o.Gender);
      if (o.CivilStatus && !ownerCivilStatus) setOwnerCivilStatus(o.CivilStatus);
      if (o.TaxIdentNo) setOwnerTin(o.TaxIdentNo);
      if (o.ContactNo) setOwnerContact(o.ContactNo);
      if (o.Email && !ownerEmail) setOwnerEmail(o.Email);
    }

    if (d.CAST_TDNBuildingDTO) {
      const b = d.CAST_TDNBuildingDTO;
      if (b.CAST_TDNBuildingId) setCastTdnBuildingId(b.CAST_TDNBuildingId);
      if (b.PropertyTypeAccordUse) setPropertyTypeAccordUse(b.PropertyTypeAccordUse);
      if (b.OtherPropertyTypeAccordUse) setOtherPropertyTypeAccordUse(b.OtherPropertyTypeAccordUse);
      if (b.SurveyNo && !surveyNo) setSurveyNo(b.SurveyNo);
      if (b.LotNo && !lotNo) setLotNo(b.LotNo);
      if (b.TCTNo && !tctNo) setTctNo(b.TCTNo);
      if (b.ARPNo && !arpNo) setArpNo(b.ARPNo);
      if (b.LandOwnerName) setLandOwnerName(b.LandOwnerName);
      if (b.LandTdNo) setLandTdNo(b.LandTdNo);
      if (b.LandPin) setLandPin(b.LandPin);
      if (b.IsCct != null) setIsCct(!!b.IsCct);
      if (b.CctTitleNo) setCctTitleNo(b.CctTitleNo);
      if (b.CctDateRegistered) setCctDateRegistered(b.CctDateRegistered.toString().substring(0, 10));
      if (b.OwnerNameReflectedInCct) setOwnerNameReflectedInCct(b.OwnerNameReflectedInCct);
      if (b.StucturalType) setStructuralType(b.StucturalType);
      if (b.BuildingClassification) setBuildingClassification(b.BuildingClassification);
      if (b.OccupancyPermitNo) setOccupancyPermitNo(b.OccupancyPermitNo);
      if (b.StructuralFoundation) setStructuralFoundation(b.StructuralFoundation);
      if (b.StructuralColumns) setStructuralColumns(b.StructuralColumns);
      if (b.StructuralBeams) setStructuralBeams(b.StructuralBeams);
      if (b.StructuralTrussRoof) setStructuralTrussRoof(b.StructuralTrussRoof);
      if (b.StructuralRoof) setStructuralRoof(b.StructuralRoof);
      if (b.StructuralExteriorWalls) setStructuralExteriorWalls(b.StructuralExteriorWalls);
      if (b.StructuralFlooring) setStructuralFlooring(b.StructuralFlooring);
      if (b.DoorMain) setDoorMain(b.DoorMain);
      if (b.Ceiling) setCeiling(b.Ceiling);
      if (b.StructuralWindows) setStructuralWindows(b.StructuralWindows);
      if (b.StructuralStairs) setStructuralStairs(b.StructuralStairs);
      if (b.StructuralPartitionWalls) setStructuralPartitionWalls(b.StructuralPartitionWalls);
      if (b.StructuralFloorFinish) setStructuralFloorFinish(b.StructuralFloorFinish);
      if (b.StructuralFinishFront) setStructuralFinishFront(b.StructuralFinishFront);
      if (b.ElectricalSystem) setElectricalSystem(b.ElectricalSystem);
      if (b.ToiletAndBathroom) setToiletAndBathroom(b.ToiletAndBathroom);
      if (b.PlumbingSewer) setPlumbingSewer(b.PlumbingSewer);
      if (b.BuildingMakeId != null) setBuildingMakeId(b.BuildingMakeId.toString());
      if (b.ClassificationId != null) setClassificationId(b.ClassificationId.toString());
      if (b.PermitNo) setPermitNo(b.PermitNo);
      if (b.DateConstructed) setDateConstructed(b.DateConstructed.toString().substring(0, 10));
      if (b.DateCompleted) setDateCompleted(b.DateCompleted.toString().substring(0, 10));
      if (b.DateOccupied) setDateOccupied(b.DateOccupied.toString().substring(0, 10));
      if (b.StoreyNo != null) setStoreyNo(b.StoreyNo.toString());
      if (b.AreaPerFloor != null) setAreaPerFloor(b.AreaPerFloor.toString());
      if (b.TotalbldgArea != null) setTotalFloorArea(b.TotalbldgArea.toString());
      if (b.LandArea != null) setLandArea(b.LandArea.toString());
      if (b.Age != null) setAge(b.Age.toString());
      if (b.SubtotalValCompArea != null) setSubtotalValCompArea(b.SubtotalValCompArea.toString());
      if (b.TotalAddItem != null) setTotalAddItem(b.TotalAddItem.toString());
      if (b.DepreciationRate != null) setDepreciationRate(b.DepreciationRate.toString());
      if (b.DepreciationAccumulated != null) setDepreciationAccumulated(b.DepreciationAccumulated.toString());
      if (b.DepreciationMarketval != null) setDepreciationMarketval(b.DepreciationMarketval.toString());
      if (b.TotalMarketVal != null) setMarketValue(b.TotalMarketVal.toString());
      if (b.TotalAssessedVal != null) setAssessedValue(b.TotalAssessedVal.toString());
      if (b.STREET && !streetLocation) setStreetLocation(b.STREET);
      if (b.MailingAddress && !ownerAddress) setOwnerAddress(b.MailingAddress);
      if (b.TIN && !ownerTin) setOwnerTin(b.TIN);
      if (b.MobileNo && !ownerContact) setOwnerContact(b.MobileNo);
      if (b.Email && !ownerEmail) setOwnerEmail(b.Email);
      if (b.Gender && !ownerGender) setOwnerGender(b.Gender);
      if (b.CivilStatus && !ownerCivilStatus) setOwnerCivilStatus(b.CivilStatus);
    }

    if (d.currentPartition) {
      const cp = d.currentPartition;
      if (cp.streetLocation || cp.streetNo || cp.buildingNo) setStreetLocation(cp.streetLocation || cp.streetNo || cp.buildingNo);
      if (cp.districtOrBarangay) setDistrictOrBarangay(normalizeBarangay(cp.districtOrBarangay));
      if (cp.cityMunicipality) setCityMunicipality(cp.cityMunicipality);
      if (cp.province) setProvince(cp.province);
      const cpAdmin = !!cp.isAdministrator || !!cp.administratorName;
      const cpBenef = !cpAdmin && !!cp.isBeneficialUser;
      setIsAdministrator(cpAdmin);
      setIsBeneficialUser(cpBenef);
      if (cp.administratorName) setAdminName(cp.administratorName);
      if (cp.administratorAddress || cp.propAdminAddress) setAdminAddress(cp.administratorAddress || cp.propAdminAddress);
      if (cp.administratorContactNo || cp.propAdminContactNo) setAdminContact(cp.administratorContactNo || cp.propAdminContactNo);
      if (cp.administratorTin) setAdminTin(cp.administratorTin);
      if (cp.administratorEmail) setAdminEmail(cp.administratorEmail);
      if (cp.administratorGender) setAdminGender(cp.administratorGender);
      if (cp.administratorCivilStatus) setAdminCivilStatus(cp.administratorCivilStatus);
      if (cp.ownerGender) setOwnerGender(cp.ownerGender);
      if (cp.ownerCivilStatus) setOwnerCivilStatus(cp.ownerCivilStatus);
      if (cp.ownerTin) setOwnerTin(cp.ownerTin);
      if (cp.ownerContactNo || cp.ownerContact) setOwnerContact(cp.ownerContactNo || cp.ownerContact);
      if (cp.ownerEmail) setOwnerEmail(cp.ownerEmail);
      if (cp.ownerAddress) setOwnerAddress(cp.ownerAddress);
      if (cp.landOwnerName) setLandOwnerName(cp.landOwnerName);
      if (cp.landTdNo) setLandTdNo(cp.landTdNo);
      if (cp.landPin) setLandPin(cp.landPin);
      if (cp.isCct != null) setIsCct(!!cp.isCct);
      if (cp.cctTitleNo) setCctTitleNo(cp.cctTitleNo);
      if (cp.cctDateRegistered) setCctDateRegistered(cp.cctDateRegistered.toString().substring(0, 10));
      if (cp.ownerNameReflectedInCct) setOwnerNameReflectedInCct(cp.ownerNameReflectedInCct);
      hydrateEffectivityAndNotes(cp);
    }

    // Hydrate Floor Levels & Additional Improvements (CAST Schedules)
    const rawFloors = d?.CAST_TDNBuildingDTO?.FloorBreakdownJson || d?.currentPartition?.buildingFloorBreakdownJson || d?.currentPartition?.BuildingFloorBreakdownJson || d?.TDN?.FloorBreakdownJson;
    const rawAdditions = d?.CAST_TDNBuildingDTO?.AdditionalItemsJson || d?.currentPartition?.buildingAdditionalItemsJson || d?.currentPartition?.BuildingAdditionalItemsJson || d?.TDN?.AdditionalItemsJson;

    const parsedFloors = safeParseArray(rawFloors);
    if (parsedFloors && parsedFloors.length > 0) {
      setBuildingFloors(parsedFloors.map((f, idx) => ({
        id: f.id || ('flr_' + idx),
        floorNumber: f.floorNumber || f.floorLevel || `Storey ${idx + 1}`,
        buildingClassification: f.buildingClassification || buildingClassification,
        rateMode: f.rateMode || 'Min',
        description: f.description || '',
        floorAreaSqm: parseFloat(f.floorAreaSqm || f.floorArea || 0),
        baseUnitCost: parseFloat(f.baseUnitCost || f.unitRate || 0),
        marketValue: parseFloat(f.marketValue || ((f.floorAreaSqm || 0) * (f.baseUnitCost || 0)))
      })));
    } else if (d?.CAST_TDNBuildingDTO?.TotalbldgArea || d?.TDN?.TotalArea) {
      const a = parseFloat(d?.CAST_TDNBuildingDTO?.TotalbldgArea || d?.TDN?.TotalArea || 0);
      const u = parseFloat(d?.TDN?.UnitValue || (d?.CAST_TDNBuildingDTO?.SubtotalValCompArea ? ((d?.CAST_TDNBuildingDTO?.SubtotalValCompArea || 0) / (a || 1)) : 10000));
      if (a > 0) {
        setBuildingFloors([{
          id: 'flr_default',
          floorNumber: '1st Floor',
          buildingClassification: buildingClassification || 'Class A - Reinforced Concrete',
          rateMode: 'Min',
          description: 'Ground Floor / Main Structure',
          floorAreaSqm: a,
          baseUnitCost: u > 0 ? u : 10000,
          marketValue: a * (u > 0 ? u : 10000)
        }]);
      }
    }

    const parsedAdditions = safeParseArray(rawAdditions);
    if (parsedAdditions && parsedAdditions.length > 0) {
      setBuildingAdditions(parsedAdditions.map((a, idx) => ({
        id: a.id || ('add_' + idx),
        description: a.description || a.itemDescription || 'Additional Item',
        quantity: parseFloat(a.quantity || a.area || 1),
        unitRate: parseFloat(a.unitRate || a.rate || 0),
        multiplier: parseFloat(a.multiplier || a.multiplierPercent || 100),
        totalValue: parseFloat(a.totalValue || a.total || 0)
      })));
    }
  };

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
    if (fromHistory) {
      navigateBack();
    } else {
      setDiscardModalVisible(true);
    }
  };

  const handleSave = async (forceOffline = false, transferStage = false) => {
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
        PropertyTypeAccordUse: propertyTypeAccordUse,
        OtherPropertyTypeAccordUse: otherPropertyTypeAccordUse,
        TotalArea: parseFloat(areaSqMeters || totalFloorArea) || 0,
        TitleNo: tctNo,
        SurveyNo: surveyNo,
        DisplayName: ownerName,
        OwnerGender: ownerGender,
        OwnerCivilStatus: ownerCivilStatus,
        OwnerTIN: ownerTin,
        OwnerContactNo: ownerContact,
        OwnerEmail: ownerEmail,
        OwnerAddress: ownerAddress,
        IsAdministrator: isAdministrator,
        IsBeneficialUser: isBeneficialUser,
        AdministratorName: (isAdministrator || isBeneficialUser) ? adminName : '',
        PropAdminName: (isAdministrator || isBeneficialUser) ? adminName : '',
        AdministratorAddress: (isAdministrator || isBeneficialUser) ? adminAddress : '',
        PropAdminAddress: (isAdministrator || isBeneficialUser) ? adminAddress : '',
        AdministratorContactNo: (isAdministrator || isBeneficialUser) ? adminContact : '',
        PropAdminContactNo: (isAdministrator || isBeneficialUser) ? adminContact : '',
        AdministratorTin: (isAdministrator || isBeneficialUser) ? adminTin : '',
        AdministratorEmail: (isAdministrator || isBeneficialUser) ? adminEmail : '',
        AdministratorGender: (isAdministrator || isBeneficialUser) ? adminGender : '',
        AdministratorCivilStatus: (isAdministrator || isBeneficialUser) ? adminCivilStatus : '',
        PropLocationStreet: streetLocation,
        Location: streetLocation,
        DistrictOrBarangay: districtOrBarangay,
        CityMunicipality: cityMunicipality,
        Province: province,
        LandOwnerName: landOwnerName,
        LandTdNo: landTdNo,
        LandPin: landPin,
        IsCct: isCct,
        CctTitleNo: cctTitleNo,
        CctDateRegistered: cctDateRegistered,
        OwnerNameReflectedInCct: ownerNameReflectedInCct,
        AssessmentBasis: assessmentBasis,
        BackTaxesYearFrom: parseInt(backTaxesYearFrom) || null,
        BackTaxesYearTo: parseInt(backTaxesYearTo) || null,
        UnitValue: parseFloat(unitValue) || null,
        CadastralSurveyNo: surveyNo,
        LotNo: lotNo,
        SectionNo: sectionNo,
        ParcelNo: parcelNo,
        TCTNo: tctNo,
        TransactionCode: transactionCode,
        YearEffectivity: parseInt(yearEffectivity) || null,
        QuarterEffectivity: parseInt(quarterEffectivity) || null,
        Memoranda: memoranda,
        Taxability: taxability,
        AssessmentLevelPercent: parseFloat(assessmentLevel) || 20,
        TotalMarketValue: grandMarketValueComputed > 0 ? grandMarketValueComputed : (parseFloat(marketValue) || 0),
        TotalAssessedValue: assessedValueComputed > 0 ? assessedValueComputed : (parseFloat(assessedValue) || 0),
        FloorBreakdownJson: JSON.stringify(buildingFloors),
        AdditionalItemsJson: JSON.stringify(buildingAdditions),
        FloorPlanSketchPath: sketchUrl,
        FloorPlanSketchBase64: sketchBase64,
        FloorPlanSketchFileName: sketchFileName,
        Attachments: attachments
      },
      CAST_TDNBuildingDTO: {
        CAST_TDNBuildingId: parseInt(castTdnBuildingId) || 0,
        TDNId: parseInt(tdnId) || 0,
        AssessmentLevelPercent: parseFloat(assessmentLevel) || 20,
        PropertyTypeAccordUse: propertyTypeAccordUse,
        OtherPropertyTypeAccordUse: otherPropertyTypeAccordUse,
        StucturalType: structuralType,
        BuildingClassification: buildingClassification,
        OccupancyPermitNo: occupancyPermitNo,
        StructuralFoundation: structuralFoundation,
        StructuralColumns: structuralColumns,
        StructuralBeams: structuralBeams,
        StructuralTrussRoof: structuralTrussRoof,
        StructuralRoof: structuralRoof,
        StructuralExteriorWalls: structuralExteriorWalls,
        StructuralFlooring: structuralFlooring,
        DoorMain: doorMain,
        Ceiling: ceiling,
        StructuralWindows: structuralWindows,
        StructuralStairs: structuralStairs,
        StructuralPartitionWalls: structuralPartitionWalls,
        StructuralFloorFinish: structuralFloorFinish,
        StructuralFinishFront: structuralFinishFront,
        ElectricalSystem: electricalSystem,
        ToiletAndBathroom: toiletAndBathroom,
        PlumbingSewer: plumbingSewer,
        BuildingMakeId: parseInt(buildingMakeId) || null,
        ClassificationId: parseInt(classificationId) || null,
        PermitNo: permitNo,
        DateConstructed: dateConstructed,
        DateCompleted: dateCompleted,
        DateOccupied: dateOccupied,
        StoreyNo: buildingFloors.length > 0 ? buildingFloors.length : (parseInt(storeyNo) || 1),
        AreaPerFloor: buildingFloors.length > 0 ? Math.round((totalFloorAreaComputed / buildingFloors.length) * 100) / 100 : (parseFloat(areaPerFloor) || null),
        TotalbldgArea: totalFloorAreaComputed > 0 ? totalFloorAreaComputed : (parseFloat(totalFloorArea || areaSqMeters) || 0),
        ARPNo: arpNo,
        LandArea: landArea,
        Age: parseInt(age) || null,
        SubtotalValCompArea: baseRcnComputed,
        TotalAddItem: totalAdditionsComputed,
        DepreciationRate: parseFloat(depreciationRate) || null,
        DepreciationAccumulated: accDeprComputed,
        DepreciationMarketval: depreciatedCoreComputed,
        TotalMarketVal: grandMarketValueComputed > 0 ? grandMarketValueComputed : (parseFloat(marketValue) || 0),
        TotalAssessedVal: assessedValueComputed > 0 ? assessedValueComputed : (parseFloat(assessedValue) || 0),
        FloorBreakdownJson: JSON.stringify(buildingFloors),
        AdditionalItemsJson: JSON.stringify(buildingAdditions),
        TCTNo: tctNo,
        LotNo: lotNo,
        SurveyNo: surveyNo,
        STREET: streetLocation,
        MailingAddress: ownerAddress,
        TIN: ownerTin,
        MobileNo: ownerContact,
        Email: ownerEmail,
        Gender: ownerGender,
        CivilStatus: ownerCivilStatus,
        IsAdministrator: isAdministrator,
        IsBeneficialUser: isBeneficialUser,
        AdministratorName: (isAdministrator || isBeneficialUser) ? adminName : '',
        LandOwnerName: landOwnerName,
        LandTdNo: landTdNo,
        LandPin: landPin,
        IsCct: isCct,
        CctTitleNo: cctTitleNo,
        CctDateRegistered: cctDateRegistered,
        OwnerNameReflectedInCct: ownerNameReflectedInCct,
        TransactionCode: transactionCode
      }
    };

    const workflowForwardPayload = transferStage && (trackingNoParam || formWorkflowIdParam) ? {
      formWorkflowId: formWorkflowIdParam,
      trackingNo: trackingNo || trackingNoParam,
      nextStatusId: nextStatusId,
      remarks: remarks || 'Field building inspection completed and transferred.'
    } : null;

    if (forceOffline) {
      setSaving(true);
      await queueOfflineUpdate(
        'UPDATE_BUILDING',
        payload,
        `Building FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        null
      );
      setSaving(false);
      showCompleteModalAndGoBack(
        'Partition Saved Offline',
        `Edits for Partition ${partitionLetterParam} have been saved locally. To synchronize to the office database or transfer to supervisor, use the 3-dots (⋮) menu on the transaction card in My Transactions.`,
        'success',
        'cloud-offline-outline'
      );
      return;
    }

    setSaving(true);

    try {
      const res = await MobileApi.updateBuildingForm(payload);
      if (res && res.success) {
        if (workflowForwardPayload) {
          try {
            await MobileApi.forwardWorkflow(
              workflowForwardPayload.formWorkflowId,
              workflowForwardPayload.nextStatusId,
              workflowForwardPayload.remarks,
              workflowForwardPayload.trackingNo
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
          'Building FAAS Saved & Synced',
          `Partition ${partitionLetterParam} updates were successfully saved and synchronized directly with the City Assessor Web Portal.`,
          'success',
          'cloud-done-outline'
        );
        return;
      } else {
        // Server rejected live sync: preserve all data safely in the offline queue
        await queueOfflineUpdate(
          'UPDATE_BUILDING',
          payload,
          `Building FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
          workflowForwardPayload
        );
        setSaving(false);
        showCompleteModalAndGoBack(
          'Saved to Offline Queue (Sync Rejected)',
          `Live sync could not complete (${res?.message || 'Server rejected the building form update'}). Your edits have been safely preserved in the offline queue and can be re-synced anytime.`,
          'warning',
          'cloud-offline-outline'
        );
        return;
      }
    } catch (err) {
      const serverMsg = err.response?.data?.message || err.response?.data || err.message || 'Server returned an error.';
      // Live sync failed with error: preserve all data safely in the offline queue
      await queueOfflineUpdate(
        'UPDATE_BUILDING',
        payload,
        `Building FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
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

    // Offline fallback: Only queue offline if completely unreachable or network disconnected
    await queueOfflineUpdate(
      'UPDATE_BUILDING',
      payload,
      `Building FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
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
        navigation={navigation}
        showBack={true}
        onBackPress={handleCancel}
      />

      {/* In-Page Form Header */}
      <View style={styles.formScreenHeader}>
        <View style={styles.formScreenHeaderRow}>
          <View style={{ flex: 1 }}>
            <Text style={[styles.formScreenTitle, { color: colors.textDark }]}>
              {fromHistory ? "View Building Record" : "Building FAAS"}
            </Text>
            <Text style={[styles.formScreenSubtitle, { color: colors.textMuted }]}>
              {`Tracking No: ${trackingNo || 'N/A'} • Partition ${partitionLetterParam}${declarationNo ? ` • TDN: ${declarationNo}` : ''}`}
            </Text>
          </View>
          <View style={[styles.partitionBadgeHero, { backgroundColor: isDarkMode ? '#134e4a' : colors.primaryLight }]}>
            <Text style={[styles.partitionBadgeHeroText, { color: colors.primary }]}>
              Lot {partitionLetterParam}
            </Text>
          </View>
        </View>
      </View>

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
        <View style={styles.ribbonLeft}>
          <Ionicons
            name={isDownloaded ? 'shield-checkmark' : 'cloud-outline'}
            size={16}
            color={isDownloaded ? (isDarkMode ? '#34d399' : '#15803d') : colors.primary}
          />
          <Text style={[styles.ribbonText, isDownloaded && { color: isDarkMode ? '#34d399' : '#15803d' }]}>
            {isDownloaded ? 'Downloaded Form • 100% Offline Field Ready' : 'Live Online Form'}
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
                  {tab.label} {idx === 3 && attachments && attachments.length > 0 ? `(${attachments.length})` : ''}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>
      </View>

      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
        keyboardDismissMode="on-drag"
        automaticallyAdjustKeyboardInsets={Platform.OS === 'ios'}
        showsVerticalScrollIndicator={true}
      >
        {loading ? (
          <ActivityIndicator color={colors.primary} size="large" style={{ marginVertical: 30 }} />
        ) : notDownloadedError ? (
          <View style={styles.notDownloadedBox}>
            <Ionicons name="cloud-offline-outline" size={56} color="#d97706" />
            <Text style={styles.notDownloadedTitle}>Form Not Stored on Phone</Text>
            <Text style={styles.notDownloadedText}>
              This building transaction data has not been downloaded to your device storage. To view and inspect this form in offline mode, please connect to Wi-Fi/Mobile Data and tap "Download" in the transactions list.
            </Text>
            <TouchableOpacity style={styles.backBtn} onPress={navigateBack}>
              <Ionicons name="arrow-back" size={18} color="#ffffff" />
              <Text style={styles.backBtnText}>Return to Transactions</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            {/* TAB 0: 1. Property Info & Taxpayer (CCT) */}
            {activeTab === 0 && (
              <View style={styles.tabContentBlock}>
                {/* Dedicated Card for Key Property Identifiers (TDN, ARP, PIN) */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Identification &amp; Reference Numbers</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>TAX DECLARATION NO. (TDN)</Text>
                    <TextInput
                      style={[styles.input, styles.inputDisabled]}
                      value={declarationNo}
                      editable={false}
                      placeholder="TDN Pending Office Approval"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

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
                      <Text style={styles.inputLabel}>PROPERTY INDEX NUMBER (PIN)</Text>
                      <TextInput
                        style={[styles.input, { fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace', fontWeight: '700' }]}
                        value={pin}
                        onChangeText={setPin}
                        placeholder="126-00-0000-000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>
                </View>

                {/* Building Property Classification & Details */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Building Classification &amp; Property Details</Text>

                  {renderSelectField(
                    'PROPERTY TYPE ACCORDING TO USE',
                    propertyTypeAccordUse,
                    BUILDING_PROPERTY_TYPES,
                    setPropertyTypeAccordUse
                  )}

                  {propertyTypeAccordUse === 'Other Type of Building and/or Structure' && (
                    <View style={styles.inputGroup}>
                      <Text style={styles.inputLabel}>SPECIFY OTHER TYPE</Text>
                      <TextInput
                        style={styles.input}
                        value={otherPropertyTypeAccordUse}
                        onChangeText={setOtherPropertyTypeAccordUse}
                        placeholder="Specify other building / structure type..."
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  )}

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>SURVEY NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={surveyNo}
                        onChangeText={setSurveyNo}
                        placeholder="e.g. Psu-12345"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>LOT NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={lotNo}
                        onChangeText={setLotNo}
                        placeholder="e.g. Lot 10-A"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TITLE NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={tctNo}
                        onChangeText={setTctNo}
                        placeholder="e.g. TCT-123456 / CCT-7890"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>AREA IN SQR. (SQ.M.)</Text>
                      <TextInput
                        style={styles.input}
                        value={areaSqMeters}
                        onChangeText={setAreaSqMeters}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>SECTION NO.</Text>
                    <TextInput
                      style={styles.input}
                      value={sectionNo}
                      onChangeText={setSectionNo}
                      placeholder="e.g. 001"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <View style={{ borderBottomWidth: 1, borderBottomColor: colors.border, paddingBottom: 10, marginBottom: 12 }}>
                    <Text style={[styles.cardHeaderTitle, { borderBottomWidth: 0, paddingBottom: 0, marginBottom: 8 }]}>
                      Owner & Declarant Demographics
                    </Text>

                    {/* 2 Options: Administrator, Beneficial User (Check only one) */}
                    <View style={styles.rolePillsRow}>
                      <TouchableOpacity
                        style={[styles.rolePill, isAdministrator && styles.rolePillActive]}
                        onPress={() => {
                          if (isAdministrator) {
                            setIsAdministrator(false);
                          } else {
                            setIsAdministrator(true);
                            setIsBeneficialUser(false);
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
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>OWNER / DECLARANT NAME</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerName}
                      onChangeText={setOwnerName}
                      placeholder="Full name of owner / declarant"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={{ flex: 1 }}>
                      {renderSelectField('GENDER / SEX', ownerGender, GENDER_OPTIONS, setOwnerGender)}
                    </View>
                    <View style={{ flex: 1 }}>
                      {renderSelectField('CIVIL STATUS', ownerCivilStatus, CIVIL_STATUS_OPTIONS, setOwnerCivilStatus)}
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TIN NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerTin}
                        onChangeText={setOwnerTin}
                        placeholder="000-000-000-000"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>MOBILE / CONTACT NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerContact}
                        onChangeText={setOwnerContact}
                        placeholder="09XXXXXXXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>EMAIL ADDRESS</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerEmail}
                      onChangeText={setOwnerEmail}
                      placeholder="email@example.com"
                      placeholderTextColor={colors.placeholder}
                      keyboardType="email-address"
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>MAILING ADDRESS</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerAddress}
                      onChangeText={setOwnerAddress}
                      placeholder="House / Unit No., Street, Barangay, City, Province"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  {(isAdministrator || isBeneficialUser) && (
                    <View style={{ marginTop: 16, paddingTop: 16, borderTopWidth: 1, borderTopColor: colors.border }}>
                      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 12 }}>
                        <Ionicons name="shield-checkmark-outline" size={16} color={colors.primary} />
                        <Text style={{ fontSize: 12, fontWeight: '700', color: colors.primary, textTransform: 'uppercase' }}>
                          {isAdministrator ? 'Administrator Details' : 'Beneficial User Details'}
                        </Text>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>
                          NAME ({isAdministrator ? 'ADMINISTRATOR' : 'BENEFICIAL USER'})
                        </Text>
                        <TextInput
                          style={styles.input}
                          value={adminName}
                          onChangeText={setAdminName}
                          placeholder={`Full name of ${isAdministrator ? 'administrator' : 'beneficial user'} / representative`}
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={{ flex: 1 }}>
                          {renderSelectField('SEX / GENDER', adminGender, GENDER_OPTIONS, setAdminGender)}
                        </View>
                        <View style={{ flex: 1 }}>
                          {renderSelectField('CIVIL STATUS', adminCivilStatus, CIVIL_STATUS_OPTIONS, setAdminCivilStatus)}
                        </View>
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>TAX IDENTIFICATION NO. (TIN)</Text>
                          <TextInput
                            style={styles.input}
                            value={adminTin}
                            onChangeText={setAdminTin}
                            placeholder="000-000-000-000"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>CONTACT NUMBER</Text>
                          <TextInput
                            style={styles.input}
                            value={adminContact}
                            onChangeText={setAdminContact}
                            placeholder="09XX-XXX-XXXX"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>
                          {isAdministrator ? 'ADMINISTRATOR ADDRESS' : 'BENEFICIAL USER ADDRESS'}
                        </Text>
                        <TextInput
                          style={styles.input}
                          value={adminAddress}
                          onChangeText={setAdminAddress}
                          placeholder="House No., Street, Barangay, City, Province"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>EMAIL ADDRESS</Text>
                        <TextInput
                          style={styles.input}
                          value={adminEmail}
                          onChangeText={setAdminEmail}
                          placeholder="admin@example.com"
                          placeholderTextColor={colors.placeholder}
                          keyboardType="email-address"
                        />
                      </View>
                    </View>
                  )}
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Location of Property</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>BUILDING NO. / STREET / ROAD</Text>
                    <TextInput
                      style={styles.input}
                      value={streetLocation}
                      onChangeText={setStreetLocation}
                      placeholder="e.g. Building 4, Pioneer Avenue, Corner Salazar St."
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BARANGAY</Text>
                      <TouchableOpacity
                        style={styles.selectBtn}
                        onPress={() => openPicker('BARANGAY', BARANGAY_OPTIONS, districtOrBarangay, setDistrictOrBarangay)}
                        activeOpacity={0.7}
                      >
                        <Text style={[styles.selectBtnText, !districtOrBarangay && { color: colors.placeholder }]} numberOfLines={1}>
                          {districtOrBarangay || 'Select Barangay...'}
                        </Text>
                        <Ionicons name="chevron-down" size={16} color={colors.textMuted} />
                      </TouchableOpacity>
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>CITY / MUNICIPALITY</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={cityMunicipality || 'Koronadal City'}
                        editable={false}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>PROVINCE</Text>
                    <TextInput
                      style={[styles.input, styles.inputDisabled]}
                      value={province || 'South Cotabato'}
                      editable={false}
                    />
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Land Reference (Where Erected)</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>LAND OWNER NAME</Text>
                    <TextInput
                      style={styles.input}
                      value={landOwnerName}
                      onChangeText={setLandOwnerName}
                      placeholder="Land owner name"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
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
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>LAND PIN</Text>
                      <TextInput
                        style={styles.input}
                        value={landPin}
                        onChangeText={setLandPin}
                        placeholder="126-00-XXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', borderBottomWidth: 1, borderBottomColor: colors.border, paddingBottom: 8 }}>
                    <Text style={[styles.cardHeaderTitle, { borderBottomWidth: 0, paddingBottom: 0 }]}>Condominium Title (CCT)</Text>
                    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                      <Text style={[styles.inputLabel, { fontSize: 11 }]}>CONDOMINIUM UNIT (CCT)</Text>
                      <Switch
                        value={isCct}
                        onValueChange={setIsCct}
                        trackColor={{ false: colors.borderDark, true: colors.primary }}
                      />
                    </View>
                  </View>

                  {isCct && (
                    <>
                      <View style={styles.inputGroup}>
                        <Text style={styles.inputLabel}>CCT TITLE NO.</Text>
                        <TextInput
                          style={styles.input}
                          value={cctTitleNo}
                          onChangeText={setCctTitleNo}
                          placeholder="e.g. CCT-00123"
                          placeholderTextColor={colors.placeholder}
                        />
                      </View>

                      <View style={styles.rowInputs}>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>DATE REGISTERED</Text>
                          <TextInput
                            style={styles.input}
                            value={cctDateRegistered}
                            onChangeText={setCctDateRegistered}
                            placeholder="YYYY-MM-DD"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                        <View style={[styles.inputGroup, { flex: 1 }]}>
                          <Text style={styles.inputLabel}>OWNER REFLECTED IN CCT</Text>
                          <TextInput
                            style={styles.input}
                            value={ownerNameReflectedInCct}
                            onChangeText={setOwnerNameReflectedInCct}
                            placeholder="Name as shown on CCT"
                            placeholderTextColor={colors.placeholder}
                          />
                        </View>
                      </View>
                    </>
                  )}
                </View>

                {/* Floor Plan / Building Sketch */}
                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  formWorkflowId={formWorkflowIdParam}
                  partitionIndex={parseInt(partitionIndexParam, 10) || 0}
                  sketchUrl={sketchUrl}
                  sketchBase64={sketchBase64}
                  attachments={attachments}
                  onAttachmentsChange={(newAtts) => {
                    setAttachments(newAtts);
                    savePartitionAttachmentsOffline(trackingNo || trackingNoParam, partitionIndexParam, newAtts, sketchUrl);
                    if (typeof updatePartitionAttachmentsLocally === 'function') {
                      updatePartitionAttachmentsLocally(trackingNo || trackingNoParam, formWorkflowIdParam, partitionIndexParam, newAtts, sketchUrl);
                    }
                  }}
                  onSketchChange={({ uri, base64, fileName }) => {
                    const newSketch = uri || '';
                    setSketchUrl(newSketch);
                    setSketchBase64(base64 || null);
                    setSketchFileName(fileName || '');
                    savePartitionAttachmentsOffline(trackingNo || trackingNoParam, partitionIndexParam, attachments, newSketch);
                    if (typeof updatePartitionAttachmentsLocally === 'function') {
                      updatePartitionAttachmentsLocally(trackingNo || trackingNoParam, formWorkflowIdParam, partitionIndexParam, attachments, newSketch);
                    }
                  }}
                  propertyKind="Building"
                  showSketch={true}
                  showAttachments={false}
                />
              </View>
            )}

            {/* TAB 1: 2. Structural Specs & 17 Materials */}
            {activeTab === 1 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Structural Type, Permits & Timeline</Text>

                  {renderSelectField(
                    'STRUCTURAL TYPE (BUCC CLASS)',
                    structuralType,
                    STRUCTURAL_TYPES,
                    setStructuralType
                  )}

                  {renderSelectField(
                    'BUILDING CLASSIFICATION',
                    buildingClassification,
                    BUILDING_CLASSIFICATIONS,
                    setBuildingClassification
                  )}

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TOTAL STOREY COUNT</Text>
                      <TextInput
                        style={styles.input}
                        value={storeyNo}
                        onChangeText={setStoreyNo}
                        keyboardType="numeric"
                        placeholder="1"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BUILDING AGE (YEARS)</Text>
                      <TextInput
                        style={styles.input}
                        value={age}
                        onChangeText={setAge}
                        keyboardType="numeric"
                        placeholder="0"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BUILDING PERMIT NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={permitNo}
                        onChangeText={setPermitNo}
                        placeholder="BP-XXXX-XXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>OCCUPANCY PERMIT NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={occupancyPermitNo}
                        onChangeText={setOccupancyPermitNo}
                        placeholder="OP-XXXX-XXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DATE CONSTRUCTED / STARTED</Text>
                      <View style={{ flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                        <TextInput
                          style={[styles.input, { flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0, borderRightWidth: 0 }]}
                          value={dateConstructed}
                          onChangeText={setDateConstructed}
                          placeholder="YYYY-MM-DD"
                          placeholderTextColor={colors.placeholder}
                        />
                        <TouchableOpacity
                          style={{
                            backgroundColor: colors.primary,
                            width: 38,
                            borderTopRightRadius: 8,
                            borderBottomRightRadius: 8,
                            justifyContent: 'center',
                            alignItems: 'center'
                          }}
                          onPress={() => setActiveDatePicker('constructed')}
                        >
                          <Ionicons name="calendar-outline" size={17} color="#ffffff" />
                        </TouchableOpacity>
                      </View>
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DATE COMPLETED</Text>
                      <View style={{ flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                        <TextInput
                          style={[styles.input, { flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0, borderRightWidth: 0 }]}
                          value={dateCompleted}
                          onChangeText={setDateCompleted}
                          placeholder="YYYY-MM-DD"
                          placeholderTextColor={colors.placeholder}
                        />
                        <TouchableOpacity
                          style={{
                            backgroundColor: colors.primary,
                            width: 38,
                            borderTopRightRadius: 8,
                            borderBottomRightRadius: 8,
                            justifyContent: 'center',
                            alignItems: 'center'
                          }}
                          onPress={() => setActiveDatePicker('completed')}
                        >
                          <Ionicons name="calendar-outline" size={17} color="#ffffff" />
                        </TouchableOpacity>
                      </View>
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>DATE OCCUPIED</Text>
                    <View style={{ flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                      <TextInput
                        style={[styles.input, { flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0, borderRightWidth: 0 }]}
                        value={dateOccupied}
                        onChangeText={setDateOccupied}
                        placeholder="YYYY-MM-DD"
                        placeholderTextColor={colors.placeholder}
                      />
                      <TouchableOpacity
                        style={{
                          backgroundColor: colors.primary,
                          width: 38,
                          borderTopRightRadius: 8,
                          borderBottomRightRadius: 8,
                          justifyContent: 'center',
                          alignItems: 'center'
                        }}
                        onPress={() => setActiveDatePicker('occupied')}
                      >
                        <Ionicons name="calendar-outline" size={17} color="#ffffff" />
                      </TouchableOpacity>
                    </View>
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>17 Structural Parts & Materials Checklist (CAST Reference)</Text>

                  {renderSelectField('1. FOUNDATION', structuralFoundation, STRUCTURAL_MATERIAL_OPTIONS.foundation, setStructuralFoundation)}
                  {renderSelectField('2. COLUMNS', structuralColumns, STRUCTURAL_MATERIAL_OPTIONS.columns, setStructuralColumns)}
                  {renderSelectField('3. BEAMS', structuralBeams, STRUCTURAL_MATERIAL_OPTIONS.beams, setStructuralBeams)}
                  {renderSelectField('4. ROOF FRAMING / TRUSS', structuralTrussRoof, STRUCTURAL_MATERIAL_OPTIONS.trussRoof, setStructuralTrussRoof)}
                  {renderSelectField('5. ROOFING', structuralRoof, STRUCTURAL_MATERIAL_OPTIONS.roof, setStructuralRoof)}
                  {renderSelectField('6. EXTERIOR WALLS', structuralExteriorWalls, STRUCTURAL_MATERIAL_OPTIONS.exteriorWalls, setStructuralExteriorWalls)}
                  {renderSelectField('7. FLOORING', structuralFlooring, STRUCTURAL_MATERIAL_OPTIONS.flooring, setStructuralFlooring)}
                  {renderSelectField('8. DOORS', doorMain, STRUCTURAL_MATERIAL_OPTIONS.doors, setDoorMain)}
                  {renderSelectField('9. CEILING', ceiling, STRUCTURAL_MATERIAL_OPTIONS.ceiling, setCeiling)}
                  {renderSelectField('10. WINDOWS', structuralWindows, STRUCTURAL_MATERIAL_OPTIONS.windows, setStructuralWindows)}
                  {renderSelectField('11. STAIRS', structuralStairs, STRUCTURAL_MATERIAL_OPTIONS.stairs, setStructuralStairs)}
                  {renderSelectField('12. PARTITION', structuralPartitionWalls, STRUCTURAL_MATERIAL_OPTIONS.partitionWalls, setStructuralPartitionWalls)}
                  {renderSelectField('13. FLOOR FINISH', structuralFloorFinish, STRUCTURAL_MATERIAL_OPTIONS.floorFinish, setStructuralFloorFinish)}
                  {renderSelectField('14. WALL FINISH', structuralFinishFront, STRUCTURAL_MATERIAL_OPTIONS.wallFinish, setStructuralFinishFront)}
                  {renderSelectField('15. ELECTRICAL CONDUIT', electricalSystem, STRUCTURAL_MATERIAL_OPTIONS.electrical, setElectricalSystem)}
                  {renderSelectField('16. TOILET & BATH', toiletAndBathroom, STRUCTURAL_MATERIAL_OPTIONS.toiletAndBath, setToiletAndBathroom)}
                  {renderSelectField('17. PLUMBING & SERVICES', plumbingSewer, STRUCTURAL_MATERIAL_OPTIONS.plumbing, setPlumbingSewer)}
                </View>
              </View>
            )}

            {/* TAB 2: 3. Floor Levels, Additions & Valuation (CAST Standard) */}
            {activeTab === 2 && (
              <View style={styles.tabContentBlock}>
                {/* LIVE VALUATION KPI OVERVIEW (CAST ALIGNED) */}
                <View style={styles.kpiGrid}>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Base RCN</Text>
                    <Text style={styles.kpiValue}>
                      ₱{baseRcnComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Accumulated Depr.</Text>
                    <Text style={[styles.kpiValue, { color: '#ef4444' }]}>
                      -₱{accDeprComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ({deprRateNum}%)
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Depreciated Core MV</Text>
                    <Text style={[styles.kpiValue, { color: isDarkMode ? '#34d399' : '#059669' }]}>
                      ₱{depreciatedCoreComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Standard Additions</Text>
                    <Text style={[styles.kpiValue, { color: isDarkMode ? '#38bdf8' : '#0284c7' }]}>
                      +₱{totalAdditionsComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Grand Combined FMV</Text>
                    <Text style={[styles.kpiValue, { color: isDarkMode ? '#34d399' : '#059669' }]}>
                      ₱{grandMarketValueComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Assessed Value (AV)</Text>
                    <Text style={[styles.kpiValue, { color: isDarkMode ? '#fbbf24' : '#d97706' }]}>
                      ₱{assessedValueComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ({assessLevelNum}%)
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Basic RPT (2%) / SEF (1%)</Text>
                    <Text style={[styles.kpiValue, { fontSize: 11 }]}>
                      ₱{basicRptComputed.toLocaleString('en-US', { minimumFractionDigits: 2 })} / ₱{sefTaxComputed.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </Text>
                  </View>
                  <View style={styles.kpiCard}>
                    <Text style={styles.kpiLabel}>Annual Tax Due (3%)</Text>
                    <Text style={[styles.kpiValue, { color: isDarkMode ? '#38bdf8' : '#0284c7' }]}>
                      ₱{annualTaxDueComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </Text>
                  </View>
                </View>

                {/* 1. Floor-by-Floor Breakdown Table */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.tableHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 1: Floor Levels & BUCC Rate Schedule</Text>
                      <Text style={styles.tableHeaderSubtitle}>Specify floor areas and base construction unit costs</Text>
                    </View>
                    <TouchableOpacity
                      style={[styles.btnSm, styles.btnSmPrimary]}
                      onPress={openAddFloorModal}
                    >
                      <Ionicons name="add-circle-outline" size={16} color="#ffffff" />
                      <Text style={styles.btnSmText}>Add Floor</Text>
                    </TouchableOpacity>
                  </View>

                  {buildingFloors.length === 0 ? (
                    <View style={styles.tableEmptyCard}>
                      <Ionicons name="layers-outline" size={32} color={colors.textMuted} />
                      <Text style={styles.tableEmptyTitle}>No Floor Levels Added</Text>
                      <Text style={styles.tableEmptySubtitle}>Tap "+ Add Floor" above to add floor levels and BUCC rates.</Text>
                    </View>
                  ) : (
                    <View style={styles.compactTableList}>
                      {buildingFloors.map((floor, idx) => (
                        <TouchableOpacity
                          key={floor.id || idx}
                          style={styles.compactTableRow}
                          onPress={() => openEditFloorModal(idx)}
                          activeOpacity={0.7}
                        >
                          <View style={styles.compactRowMain}>
                            <View style={styles.compactRowHeader}>
                              <View style={styles.compactIndexBadge}>
                                <Text style={styles.compactIndexText}>#{idx + 1}</Text>
                              </View>
                              <View style={styles.compactBadge}>
                                <Text style={styles.compactBadgeText} numberOfLines={1}>
                                  {floor.floorNumber}
                                </Text>
                              </View>
                              <View style={[styles.compactBadge, { backgroundColor: isDarkMode ? '#064e3b' : '#ecfdf5' }]}>
                                <Text style={[styles.compactBadgeText, { color: isDarkMode ? '#34d399' : '#047857' }]} numberOfLines={1}>
                                  {floor.rateMode || 'Min'}
                                </Text>
                              </View>
                            </View>
                            <Text style={styles.compactRowSubtext}>
                              {floor.buildingClassification || 'Class A'} • {floor.floorAreaSqm} sq.m. @ ₱{(parseFloat(floor.baseUnitCost) || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}
                            </Text>
                            <Text style={[styles.compactRowValue, { color: isDarkMode ? '#34d399' : '#059669' }]}>
                              ₱{(parseFloat(floor.marketValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </Text>
                          </View>
                          <View style={styles.compactRowActions}>
                            <TouchableOpacity
                              style={styles.compactActionBtn}
                              onPress={() => openEditFloorModal(idx)}
                              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                            >
                              <Ionicons name="pencil-outline" size={16} color={colors.primary} />
                            </TouchableOpacity>
                            <TouchableOpacity
                              style={styles.compactActionBtnDanger}
                              onPress={() => removeFloor(idx)}
                              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                            >
                              <Ionicons name="trash-outline" size={16} color="#ef4444" />
                            </TouchableOpacity>
                          </View>
                        </TouchableOpacity>
                      ))}
                    </View>
                  )}

                  <View style={styles.tableSummaryBox}>
                    <View style={styles.tableSummaryRow}>
                      <Text style={styles.tableSummaryLabel}>Total Floor Area:</Text>
                      <Text style={styles.tableSummaryVal}>
                        {totalFloorAreaComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} sq.m.
                      </Text>
                    </View>
                    <View style={styles.tableSummaryRow}>
                      <Text style={styles.tableSummaryLabel}>Base RCN:</Text>
                      <Text style={[styles.tableSummaryVal, { color: isDarkMode ? '#34d399' : '#059669', fontWeight: '900' }]}>
                        ₱{baseRcnComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PARTITION ASSESSED AREA (SQ.M.)</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={totalFloorAreaComputed.toFixed(2)}
                        editable={false}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BASE UNIT VALUE (PHP/SQ.M.)</Text>
                      <TextInput
                        style={styles.input}
                        value={unitValue}
                        onChangeText={setUnitValue}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>
                </View>

                {/* 2. Additional Items & Improvements Table */}
                <View style={[styles.card, shadows.card]}>
                  <View style={styles.tableHeaderRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.cardHeaderTitle}>Table 2: Additional Items & Improvements</Text>
                      <Text style={styles.tableHeaderSubtitle}>Standard items (Fence, Terraces, Balconies, Tiles, Carports)</Text>
                    </View>
                    <TouchableOpacity
                      style={[styles.btnSm, styles.btnSmPrimary, { backgroundColor: '#0284c7' }]}
                      onPress={openAddAdditionModal}
                    >
                      <Ionicons name="add-circle-outline" size={16} color="#ffffff" />
                      <Text style={styles.btnSmText}>Add Item</Text>
                    </TouchableOpacity>
                  </View>

                  {buildingAdditions.length === 0 ? (
                    <View style={styles.tableEmptyCard}>
                      <Ionicons name="cube-outline" size={32} color={colors.textMuted} />
                      <Text style={styles.tableEmptyTitle}>No Additional Items Added</Text>
                      <Text style={styles.tableEmptySubtitle}>Tap "+ Add Item" above to add fences, terraces, or carports.</Text>
                    </View>
                  ) : (
                    <View style={styles.compactTableList}>
                      {buildingAdditions.map((item, idx) => (
                        <TouchableOpacity
                          key={item.id || idx}
                          style={styles.compactTableRow}
                          onPress={() => openEditAdditionModal(idx)}
                          activeOpacity={0.7}
                        >
                          <View style={styles.compactRowMain}>
                            <View style={styles.compactRowHeader}>
                              <View style={[styles.compactIndexBadge, { backgroundColor: '#0284c7' }]}>
                                <Text style={styles.compactIndexText}>#{idx + 1}</Text>
                              </View>
                              <Text style={[styles.compactRowSubtext, { fontWeight: '800', color: colors.textDark, flex: 1 }]} numberOfLines={1}>
                                {item.description}
                              </Text>
                            </View>
                            <Text style={styles.compactRowSubtext}>
                              Qty: {item.quantity} • Rate: ₱{(parseFloat(item.unitRate) || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })} • Multiplier: {item.multiplier}%
                            </Text>
                            <Text style={[styles.compactRowValue, { color: isDarkMode ? '#38bdf8' : '#0284c7' }]}>
                              ₱{(parseFloat(item.totalValue) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </Text>
                          </View>
                          <View style={styles.compactRowActions}>
                            <TouchableOpacity
                              style={styles.compactActionBtn}
                              onPress={() => openEditAdditionModal(idx)}
                              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                            >
                              <Ionicons name="pencil-outline" size={16} color={colors.primary} />
                            </TouchableOpacity>
                            <TouchableOpacity
                              style={styles.compactActionBtnDanger}
                              onPress={() => removeAddition(idx)}
                              hitSlop={{ top: 8, bottom: 8, left: 8, right: 8 }}
                            >
                              <Ionicons name="trash-outline" size={16} color="#ef4444" />
                            </TouchableOpacity>
                          </View>
                        </TouchableOpacity>
                      ))}
                    </View>
                  )}

                  <View style={styles.tableSummaryBox}>
                    <View style={styles.tableSummaryRow}>
                      <Text style={styles.tableSummaryLabel}>Subtotal Standard Additions:</Text>
                      <Text style={[styles.tableSummaryVal, { color: isDarkMode ? '#38bdf8' : '#0284c7', fontWeight: '900' }]}>
                        ₱{totalAdditionsComputed.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                      </Text>
                    </View>
                  </View>
                </View>

                {/* 3. Building Depreciation & Final Assessment Roll */}
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Depreciation & Final Assessed Value Roll</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DEPRECIATION RATE (%)</Text>
                      <TextInput
                        style={styles.input}
                        value={depreciationRate}
                        onChangeText={setDepreciationRate}
                        keyboardType="numeric"
                        placeholder="0.0"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ACCUMULATED DEPR. (PHP)</Text>
                      <TextInput
                        style={[styles.input, styles.inputDisabled]}
                        value={accDeprComputed.toFixed(2)}
                        editable={false}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>FINAL MARKET VALUE (FMV)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={marketValue || grandMarketValueComputed.toFixed(2)}
                      onChangeText={setMarketValue}
                      keyboardType="numeric"
                      placeholder="0.00"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ASSESSMENT LEVEL (%)</Text>
                      <TextInput
                        style={styles.input}
                        value={assessmentLevel}
                        onChangeText={setAssessmentLevel}
                        keyboardType="numeric"
                        placeholder="20"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ASSESSED VALUE (AV)</Text>
                      <TextInput
                        style={[styles.input, styles.valuationInput]}
                        value={assessedValue || assessedValueComputed.toFixed(2)}
                        onChangeText={setAssessedValue}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.switchRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.switchTitle}>PROPERTY TAXABILITY</Text>
                      <Text style={styles.switchSubtitle}>{taxability ? 'Taxable (Standard Real Property Tax)' : 'Exempt from Real Property Tax'}</Text>
                    </View>
                    <Switch
                      value={taxability}
                      onValueChange={setTaxability}
                      trackColor={{ false: colors.borderDark, true: colors.primary }}
                    />
                  </View>
                </View>
              </View>
            )}

            {/* TAB 3: 4. Back Taxes, Effectivity & Notes */}
            {activeTab === 3 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Effectivity of Assessment, Taxability & Back Taxes</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>EFFECTIVITY YEAR</Text>
                      <TextInput
                        style={styles.input}
                        value={yearEffectivity}
                        onChangeText={setYearEffectivity}
                        keyboardType="numeric"
                        placeholder="2026"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>EFFECTIVITY QUARTER</Text>
                      <TextInput
                        style={styles.input}
                        value={quarterEffectivity}
                        onChangeText={setQuarterEffectivity}
                        keyboardType="numeric"
                        placeholder="1"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>BASIS OF ASSESSMENT</Text>
                    <TextInput
                      style={styles.input}
                      value={assessmentBasis}
                      onChangeText={setAssessmentBasis}
                      placeholder="Sec. 217 RA 7160"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BACK TAXES FROM YEAR</Text>
                      <TextInput
                        style={styles.input}
                        value={backTaxesYearFrom}
                        onChangeText={setBackTaxesYearFrom}
                        keyboardType="numeric"
                        placeholder="YYYY"
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
                        placeholder="YYYY"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>MEMORANDA & LEGAL ANNOTATIONS</Text>
                    <TextInput
                      style={[styles.input, { height: 60, textAlignVertical: 'top' }]}
                      value={memoranda}
                      onChangeText={setMemoranda}
                      multiline
                      placeholder="Enter legal memoranda, boundaries, or depreciation notes..."
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>FIELD INSPECTOR REMARKS</Text>
                    <TextInput
                      style={[styles.input, { height: 60, textAlignVertical: 'top' }]}
                      value={remarks}
                      onChangeText={setRemarks}
                      multiline
                      placeholder="Field inspection observations..."
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                {/* Field Photos & Attachments */}
                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  formWorkflowId={formWorkflowIdParam}
                  partitionIndex={parseInt(partitionIndexParam, 10) || 0}
                  sketchUrl={sketchUrl}
                  sketchBase64={sketchBase64}
                  attachments={attachments}
                  onAttachmentsChange={(newAtts) => {
                    setAttachments(newAtts);
                    savePartitionAttachmentsOffline(trackingNo || trackingNoParam, partitionIndexParam, newAtts, sketchUrl);
                    if (typeof updatePartitionAttachmentsLocally === 'function') {
                      updatePartitionAttachmentsLocally(trackingNo || trackingNoParam, formWorkflowIdParam, partitionIndexParam, newAtts, sketchUrl);
                    }
                  }}
                  propertyKind="Building"
                  showSketch={false}
                  showAttachments={true}
                />
              </View>
            )}

            {/* Action Buttons */}
            <View style={styles.buttonContainer}>
              {fromHistory ? (
                <TouchableOpacity
                  style={[styles.saveBtn, { backgroundColor: colors.primary }]}
                  onPress={handleCancel}
                >
                  <Ionicons name="arrow-back" size={18} color="#ffffff" />
                  <Text style={styles.saveBtnText}>Back to History</Text>
                </TouchableOpacity>
              ) : (
                <>
                  <TouchableOpacity
                    style={[styles.saveBtn, styles.saveBtnOffline]}
                    onPress={() => handleSave(true, false)}
                    disabled={saving}
                  >
                    {saving ? (
                      <ActivityIndicator color="#ffffff" />
                    ) : (
                      <>
                        <Ionicons name="cloud-offline-outline" size={18} color="#ffffff" />
                        <Text style={styles.saveBtnText}>Save Partition (Offline)</Text>
                      </>
                    )}
                  </TouchableOpacity>

                  <TouchableOpacity
                    style={[styles.saveBtn, styles.cancelBtn]}
                    onPress={handleCancel}
                    disabled={saving}
                  >
                    <Ionicons name="close-circle-outline" size={18} color="#e11d48" />
                    <Text style={styles.cancelBtnText}>Cancel & Discard Changes</Text>
                  </TouchableOpacity>
                </>
              )}
            </View>
          </>
        )}
      </ScrollView>

      {/* Reusable Option Selection Modal */}
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
                      <Ionicons name="checkmark-circle" size={20} color={colors.primary} />
                    )}
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          </View>
        </TouchableOpacity>
      </Modal>
          {/* Date Picker Modal for Building Dates */}
      <DatePickerModal
        visible={!!activeDatePicker}
        onClose={() => setActiveDatePicker(null)}
        value={
          activeDatePicker === 'constructed' ? dateConstructed :
          activeDatePicker === 'completed' ? dateCompleted :
          activeDatePicker === 'occupied' ? dateOccupied : ''
        }
        onSelectDate={(newDate) => {
          if (activeDatePicker === 'constructed') setDateConstructed(newDate);
          else if (activeDatePicker === 'completed') setDateCompleted(newDate);
          else if (activeDatePicker === 'occupied') setDateOccupied(newDate);
        }}
        title={
          activeDatePicker === 'constructed' ? 'Date Constructed / Started' :
          activeDatePicker === 'completed' ? 'Date Completed' :
          activeDatePicker === 'occupied' ? 'Date Occupied' : 'Select Date'
        }
      />

      <DiscardChangesModal
        visible={discardModalVisible}
        onClose={() => setDiscardModalVisible(false)}
        onConfirmDiscard={navigateBack}
        partitionLetter={partitionLetterParam}
      />

      {/* Floor Level & BUCC Rate Schedule Modal */}
      <Modal
        visible={floorModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setFloorModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setFloorModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingFloorIndex !== null ? `Edit Floor Level #${editingFloorIndex + 1}` : 'Add Floor Level & BUCC Rate'}
                  </Text>
                  <Text style={styles.tableHeaderSubtitle}>BUCC Schedule of Unit Base Construction Costs</Text>
                </View>
                <TouchableOpacity onPress={() => setFloorModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Storey / Floor Level */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>STOREY / FLOOR LEVEL</Text>
                  <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: 6, paddingVertical: 2 }}>
                    {FLOOR_LEVEL_OPTIONS.map((lvl) => {
                      const isSel = floorLevel === lvl;
                      return (
                        <TouchableOpacity
                          key={lvl}
                          style={[styles.pillOption, isSel && styles.pillOptionSelected]}
                          onPress={() => {
                            setFloorLevel(lvl);
                            if (!floorDesc || FLOOR_LEVEL_OPTIONS.some(o => floorDesc.startsWith(o))) {
                              setFloorDesc(`${lvl} (${floorBuildingClass || 'General Structure'})`);
                            }
                          }}
                        >
                          <Text style={[styles.pillOptionText, isSel && styles.pillOptionTextSelected]}>{lvl}</Text>
                        </TouchableOpacity>
                      );
                    })}
                  </ScrollView>
                  <TextInput
                    style={styles.input}
                    value={floorLevel}
                    onChangeText={setFloorLevel}
                    placeholder="e.g. 1st Floor, Mezzanine, Ground Floor..."
                    placeholderTextColor={colors.placeholder}
                  />
                </View>

                {/* Building Classification */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>BUILDING CLASSIFICATION</Text>
                  <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: 6, paddingVertical: 2 }}>
                    {BUILDING_CLASSIFICATIONS.map((cls) => {
                      const isSel = floorBuildingClass === cls;
                      return (
                        <TouchableOpacity
                          key={cls}
                          style={[styles.pillOption, isSel && styles.pillOptionSelected]}
                          onPress={() => {
                            setFloorBuildingClass(cls);
                            if (floorLevel) {
                              setFloorDesc(`${floorLevel} (${cls})`);
                            }
                          }}
                        >
                          <Text style={[styles.pillOptionText, isSel && styles.pillOptionTextSelected]} numberOfLines={1}>{cls.split(' - ')[0]}</Text>
                        </TouchableOpacity>
                      );
                    })}
                  </ScrollView>
                  <TextInput
                    style={styles.input}
                    value={floorBuildingClass}
                    onChangeText={setFloorBuildingClass}
                    placeholder="Class A, Class B..."
                    placeholderTextColor={colors.placeholder}
                  />
                </View>

                {/* Rate Mode */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>BUCC RATE MODE</Text>
                  <View style={styles.segmentedRow}>
                    {RATE_MODE_OPTIONS.map((mode) => {
                      const isSel = floorRateMode === mode;
                      return (
                        <TouchableOpacity
                          key={mode}
                          style={[styles.segmentedBtn, isSel && styles.segmentedBtnSelected]}
                          onPress={() => setFloorRateMode(mode)}
                        >
                          <Text style={[styles.segmentedBtnText, isSel && styles.segmentedBtnTextSelected]}>{mode}</Text>
                        </TouchableOpacity>
                      );
                    })}
                  </View>
                </View>

                {/* Area & Unit Cost */}
                <View style={styles.rowInputs}>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>FLOOR AREA (SQ.M.) *</Text>
                    <TextInput
                      style={styles.input}
                      value={floorArea}
                      onChangeText={setFloorArea}
                      keyboardType="numeric"
                      placeholder="0.00"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>BASE UNIT COST (₱/SQ.M.)</Text>
                    <TextInput
                      style={styles.input}
                      value={floorUnitCost}
                      onChangeText={setFloorUnitCost}
                      keyboardType="numeric"
                      placeholder="10000"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                {/* Description / Specific Use */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>DESCRIPTION / USAGE</Text>
                  <TextInput
                    style={styles.input}
                    value={floorDesc}
                    onChangeText={setFloorDesc}
                    placeholder="e.g. Ground Floor Living / Commercial Area"
                    placeholderTextColor={colors.placeholder}
                  />
                </View>

                {/* Calculated Floor Market Value */}
                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>CALCULATED BASE VALUE (RCN):</Text>
                  <Text style={styles.calculatedValue}>
                    ₱{((parseFloat(floorArea) || 0) * (parseFloat(floorUnitCost) || 0)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                {/* Modal Footer Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setFloorModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveFloorLevel}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Floor Level</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

      {/* Additional Improvement Item Modal */}
      <Modal
        visible={additionModalVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setAdditionModalVisible(false)}
      >
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <TouchableOpacity
            style={styles.modalOverlay}
            activeOpacity={1}
            onPress={() => setAdditionModalVisible(false)}
          >
            <TouchableOpacity
              activeOpacity={1}
              style={[styles.modalContent, { maxHeight: '90%' }]}
              onPress={(e) => e.stopPropagation()}
            >
              <View style={styles.modalHeader}>
                <View style={{ flex: 1 }}>
                  <Text style={styles.modalTitle}>
                    {editingAdditionIndex !== null ? `Edit Additional Item #${editingAdditionIndex + 1}` : 'Add Additional Improvement Item'}
                  </Text>
                  <Text style={styles.tableHeaderSubtitle}>Fences, balconies, carports, swimming pools, paving</Text>
                </View>
                <TouchableOpacity onPress={() => setAdditionModalVisible(false)}>
                  <Ionicons name="close" size={24} color={colors.textMuted} />
                </TouchableOpacity>
              </View>

              <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: 12, paddingVertical: 4 }}>
                {/* Standard Preset Chips */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>QUICK SELECT PRESET</Text>
                  <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: 6, paddingVertical: 2 }}>
                    {STANDARD_ADDITIONAL_ITEMS.map((item, idx) => {
                      const isSel = additionDesc === item.desc;
                      return (
                        <TouchableOpacity
                          key={idx}
                          style={[styles.pillOption, isSel && styles.pillOptionSelected]}
                          onPress={() => {
                            setAdditionDesc(item.desc);
                            setAdditionRate(item.rate.toString());
                          }}
                        >
                          <Text style={[styles.pillOptionText, isSel && styles.pillOptionTextSelected]}>{item.desc.split(' (')[0]}</Text>
                        </TouchableOpacity>
                      );
                    })}
                  </ScrollView>
                </View>

                {/* Item Description */}
                <View style={styles.inputGroup}>
                  <Text style={styles.inputLabel}>ITEM DESCRIPTION *</Text>
                  <TextInput
                    style={styles.input}
                    value={additionDesc}
                    onChangeText={setAdditionDesc}
                    placeholder="Enter item description..."
                    placeholderTextColor={colors.placeholder}
                  />
                </View>

                {/* Quantity, Unit Rate & Multiplier */}
                <View style={styles.rowInputs}>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>QUANTITY / AREA</Text>
                    <TextInput
                      style={styles.input}
                      value={additionQuantity}
                      onChangeText={setAdditionQuantity}
                      keyboardType="numeric"
                      placeholder="1"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1.2 }]}>
                    <Text style={styles.inputLabel}>UNIT RATE (₱)</Text>
                    <TextInput
                      style={styles.input}
                      value={additionRate}
                      onChangeText={setAdditionRate}
                      keyboardType="numeric"
                      placeholder="0.00"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                  <View style={[styles.inputGroup, { flex: 1 }]}>
                    <Text style={styles.inputLabel}>MULTIPLIER (%)</Text>
                    <TextInput
                      style={styles.input}
                      value={additionMultiplier}
                      onChangeText={setAdditionMultiplier}
                      keyboardType="numeric"
                      placeholder="100"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                {/* Calculated Item Total Value */}
                <View style={styles.calculatedBox}>
                  <Text style={styles.calculatedLabel}>CALCULATED ITEM VALUE:</Text>
                  <Text style={styles.calculatedValue}>
                    ₱{(((parseFloat(additionQuantity) || 0) * (parseFloat(additionRate) || 0)) * ((parseFloat(additionMultiplier) || 100) / 100)).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </Text>
                </View>

                {/* Modal Footer Buttons */}
                <View style={styles.modalFooterBtnRow}>
                  <TouchableOpacity
                    style={styles.modalCancelBtn}
                    onPress={() => setAdditionModalVisible(false)}
                  >
                    <Text style={styles.modalCancelBtnText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.modalSaveBtn}
                    onPress={saveAdditionItem}
                  >
                    <Ionicons name="checkmark-circle-outline" size={18} color="#ffffff" />
                    <Text style={styles.modalSaveBtnText}>Save Item</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </TouchableOpacity>
          </TouchableOpacity>
        </KeyboardAvoidingView>
      </Modal>

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

const getStyles = (colors, isDarkMode, isLargeScreen = false) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.bgLight
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
  inputDisabled: {
    backgroundColor: isDarkMode ? '#1e293b66' : '#f1f5f9',
    borderColor: isDarkMode ? '#334155' : '#e2e8f0',
    color: isDarkMode ? '#94a3b8' : '#64748b',
    fontWeight: '700',
    fontFamily: Platform.OS === 'ios' ? 'Courier' : 'monospace'
  },
  scrollContent: {
    padding: 16,
    gap: 14,
    paddingBottom: 220
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
    fontSize: 14,
    fontWeight: '800',
    color: isDarkMode ? colors.primary : colors.primaryDark,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 8
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
    backgroundColor: isDarkMode ? (colors.surfaceSecondary || '#1e293b') : '#f8fafc',
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
  inputGroup: {
    gap: 4
  },
  inputLabel: {
    fontSize: 12,
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
  valuationInput: {
    fontWeight: '800',
    color: isDarkMode ? '#34d399' : colors.primaryDark,
    backgroundColor: isDarkMode ? '#064e3b' : '#f0fdf4'
  },
  rowInputs: {
    flexDirection: isLargeScreen ? 'row' : 'column',
    gap: isLargeScreen ? 10 : 12
  },
  formScreenHeader: {
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 8,
    backgroundColor: colors.cardBg,
    borderBottomWidth: 1,
    borderBottomColor: colors.border
  },
  formScreenHeaderRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12
  },
  formScreenTitle: {
    fontSize: 18,
    fontWeight: '800',
    letterSpacing: -0.2
  },
  formScreenSubtitle: {
    fontSize: 12,
    marginTop: 2,
    fontWeight: '500'
  },
  partitionBadgeHero: {
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: 8
  },
  partitionBadgeHeroText: {
    fontSize: 12,
    fontWeight: '800'
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 4
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
  modalOverlay: {
    flex: 1,
    backgroundColor: colors.modalOverlay,
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
  pillOption: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 8,
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f1f5f9',
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  pillOptionSelected: {
    backgroundColor: isDarkMode ? '#065f46' : '#dcfce7',
    borderColor: colors.primary
  },
  pillOptionText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textMuted
  },
  pillOptionTextSelected: {
    color: isDarkMode ? '#34d399' : '#047857',
    fontWeight: '800'
  },
  segmentedRow: {
    flexDirection: 'row',
    gap: 8
  },
  segmentedBtn: {
    flex: 1,
    paddingVertical: 8,
    borderRadius: 8,
    alignItems: 'center',
    backgroundColor: isDarkMode ? colors.surfaceSecondary : '#f1f5f9',
    borderWidth: 1,
    borderColor: isDarkMode ? colors.border : '#e2e8f0'
  },
  segmentedBtnSelected: {
    backgroundColor: colors.primary,
    borderColor: colors.primary
  },
  segmentedBtnText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textMuted
  },
  segmentedBtnTextSelected: {
    color: '#ffffff',
    fontWeight: '800'
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
  btnSmText: {
    color: '#ffffff',
    fontSize: 11,
    fontWeight: '800'
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
