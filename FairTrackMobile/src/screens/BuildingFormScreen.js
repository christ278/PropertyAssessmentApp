import React, { useState, useEffect, useContext } from 'react';
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
  Modal
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { MobileApi } from '../api/mobileApi';
import { HeaderBar } from '../components/HeaderBar';
import { DiscardChangesModal } from '../components/DiscardChangesModal';
import { AttachmentsSection } from '../components/AttachmentsSection';

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
  const styles = React.useMemo(() => getStyles(colors, isDarkMode), [colors, isDarkMode]);

  const trackingNoParam = route?.params?.trackingNo || '';
  const formWorkflowIdParam = route?.params?.formWorkflowId || null;
  const partitionIndexParam = route?.params?.partitionIndex ?? 0;
  const partitionLetterParam = route?.params?.partitionLetter || String.fromCharCode(65 + (parseInt(partitionIndexParam, 10) || 0));
  const { queueOfflineUpdate, getDownloadedFormData, isTransactionDownloaded } = useContext(SyncContext);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [notDownloadedError, setNotDownloadedError] = useState(false);
  const [discardModalVisible, setDiscardModalVisible] = useState(false);
  const isDownloaded = isTransactionDownloaded(trackingNoParam, formWorkflowIdParam);

  // Active section tab (0: Property Info, 1: Structural, 2: Valuation, 3: Back Taxes, 4: Photos & Sketch)
  const [activeTab, setActiveTab] = useState(0);

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
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerTin, setOwnerTin] = useState('');
  const [ownerContact, setOwnerContact] = useState('');
  const [adminName, setAdminName] = useState('');
  const [adminAddress, setAdminAddress] = useState('');
  const [adminContact, setAdminContact] = useState('');
  const [streetLocation, setStreetLocation] = useState('');
  const [districtOrBarangay, setDistrictOrBarangay] = useState('');
  const [cityMunicipality, setCityMunicipality] = useState('');
  const [province, setProvince] = useState('');
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
  const [assessmentLevel, setAssessmentLevel] = useState('');
  const [assessedValue, setAssessedValue] = useState('');
  const [taxability, setTaxability] = useState(true);
  const [sketchUrl, setSketchUrl] = useState('');
  const [attachments, setAttachments] = useState([]);

  // 4. Effectivity & Memoranda
  const [yearEffectivity, setYearEffectivity] = useState(new Date().getFullYear().toString());
  const [quarterEffectivity, setQuarterEffectivity] = useState('1');
  const [memoranda, setMemoranda] = useState('');
  const [remarks, setRemarks] = useState('');

  // Workflow Forwarding
  const [autoForward, setAutoForward] = useState(true);
  const [nextStatusId, setNextStatusId] = useState(4); // 4 = Checked / Recommend Approval

  useEffect(() => {
    loadBuildingData();
  }, []);

  const loadBuildingData = async () => {
    setNotDownloadedError(false);

    // 1. Check if downloaded offline copy exists
    const downloaded = getDownloadedFormData(trackingNoParam, formWorkflowIdParam, partitionIndexParam);
    if (downloaded) {
      if (downloaded.owner) setOwnerName(downloaded.owner);
      if (downloaded.arpNo) setArpNo(downloaded.arpNo);
      if (downloaded.tdnId) setTdnId(downloaded.tdnId);
      if (downloaded.sketchUrl) setSketchUrl(downloaded.sketchUrl);
      if (downloaded.attachments && Array.isArray(downloaded.attachments)) setAttachments(downloaded.attachments);

      if (downloaded.currentPartition) {
        const cp = downloaded.currentPartition;
        if (cp.propertyTypeAccordUse) setPropertyTypeAccordUse(cp.propertyTypeAccordUse);
        if (cp.otherPropertyTypeAccordUse) setOtherPropertyTypeAccordUse(cp.otherPropertyTypeAccordUse);
        if (cp.pin) setPin(cp.pin);
        if (cp.surveyNo) setSurveyNo(cp.surveyNo);
        if (cp.lotNo) setLotNo(cp.lotNo);
        if (cp.titleNo) setTctNo(cp.titleNo);
        if (cp.sectionNo) setSectionNo(cp.sectionNo);
        if (cp.arpNo) setArpNo(cp.arpNo);
        if (cp.systemGeneratedTDN) setDeclarationNo(cp.systemGeneratedTDN);
        if (cp.areaSqMeters != null && cp.areaSqMeters > 0) {
          setAreaSqMeters(cp.areaSqMeters.toString());
          setTotalFloorArea(cp.areaSqMeters.toString());
        }
      }

      if (downloaded.formData) {
        populateFieldsFromDTO(downloaded.formData);
        if (downloaded.currentPartition?.systemGeneratedTDN) setDeclarationNo(downloaded.currentPartition.systemGeneratedTDN);
      }
      return;
    }

    // 2. If not pre-downloaded, try fetching live if online
    try {
      setLoading(true);
      const res = await MobileApi.getBuildingForm(tdnId || 1, castTdnBuildingId || 1);
      if (res && res.success && res.data) {
        populateFieldsFromDTO(res.data);
      } else {
        setNotDownloadedError(true);
      }
    } catch (err) {
      console.warn('Cannot fetch live form and not downloaded:', err);
      setNotDownloadedError(true);
    } finally {
      setLoading(false);
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
      if (t.PropAdminName) setAdminName(t.PropAdminName);
      if (t.PropAdminAddress) setAdminAddress(t.PropAdminAddress);
      if (t.PropAdminContactNo) setAdminContact(t.PropAdminContactNo);
      if (t.PropLocationStreet || t.Location) setStreetLocation(t.PropLocationStreet || t.Location);
      if (t.DistrictOrBarangay || t.Barangay) setDistrictOrBarangay(t.DistrictOrBarangay || t.Barangay);
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
      if (t.Attachments && Array.isArray(t.Attachments)) setAttachments(t.Attachments);
    }

    if (d.refOwner) {
      const o = d.refOwner;
      if (!ownerName && (o.OwnerName || o.FullName)) setOwnerName(o.OwnerName || o.FullName);
      if (o.TaxIdentNo) setOwnerTin(o.TaxIdentNo);
      if (o.ContactNo) setOwnerContact(o.ContactNo);
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
    }
  };

  const navigateBack = () => {
    setDiscardModalVisible(false);
    if (navigation && typeof navigation.navigate === 'function') {
      navigation.navigate('Main', { screen: 'Feed' });
    } else if (navigation && typeof navigation.goBack === 'function') {
      navigation.goBack();
    }
  };

  const showCompleteAlertAndGoBack = (title, message) => {
    if (Platform.OS === 'web') {
      if (typeof window !== 'undefined' && typeof window.alert === 'function') {
        window.alert(`${title}\n\n${message}`);
      }
      navigateBack();
    } else {
      Alert.alert(title, message, [{ text: 'OK', onPress: navigateBack }]);
    }
  };

  const handleCancel = () => {
    setDiscardModalVisible(true);
  };

  const handleSave = async (forceOffline = false) => {
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
        OwnerAddress: ownerAddress,
        PropAdminName: adminName,
        PropAdminAddress: adminAddress,
        PropAdminContactNo: adminContact,
        PropLocationStreet: streetLocation,
        Location: streetLocation,
        DistrictOrBarangay: districtOrBarangay,
        CityMunicipality: cityMunicipality,
        Province: province,
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
        TotalMarketValue: parseFloat(marketValue) || 0,
        TotalAssessedValue: parseFloat(assessedValue) || 0,
        FloorPlanSketchPath: sketchUrl,
        Attachments: attachments
      },
      CAST_TDNBuildingDTO: {
        CAST_TDNBuildingId: parseInt(castTdnBuildingId) || 0,
        TDNId: parseInt(tdnId) || 0,
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
        StoreyNo: parseInt(storeyNo) || 1,
        AreaPerFloor: parseFloat(areaPerFloor) || null,
        TotalbldgArea: parseFloat(totalFloorArea || areaSqMeters) || 0,
        ARPNo: arpNo,
        LandArea: landArea,
        Age: parseInt(age) || null,
        SubtotalValCompArea: parseFloat(subtotalValCompArea) || null,
        TotalAddItem: parseFloat(totalAddItem) || null,
        DepreciationRate: parseFloat(depreciationRate) || null,
        DepreciationAccumulated: parseFloat(depreciationAccumulated) || null,
        DepreciationMarketval: parseFloat(depreciationMarketval) || null,
        TotalMarketVal: parseFloat(marketValue) || 0,
        TotalAssessedVal: parseFloat(assessedValue) || 0,
        TCTNo: tctNo,
        LotNo: lotNo,
        SurveyNo: surveyNo,
        STREET: streetLocation,
        MailingAddress: ownerAddress,
        TIN: ownerTin,
        MobileNo: ownerContact,
        TransactionCode: transactionCode
      }
    };

    const workflowForwardPayload = autoForward && formWorkflowIdParam ? {
      formWorkflowId: formWorkflowIdParam,
      nextStatusId: nextStatusId,
      remarks: remarks || 'Field building inspection completed.'
    } : null;

    if (forceOffline) {
      await queueOfflineUpdate(
        'UPDATE_BUILDING',
        payload,
        `Building FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        workflowForwardPayload
      );
      showCompleteAlertAndGoBack('Queued for Office Sync', 'Your field building inspection changes have been saved locally on this phone. It will automatically upload and transfer to the next step when you return to the office.');
      return;
    }

    try {
      setSaving(true);
      const res = await MobileApi.updateBuildingForm(payload);
      if (res && res.success) {
        if (workflowForwardPayload) {
          try {
            await MobileApi.forwardWorkflow(
              workflowForwardPayload.formWorkflowId,
              workflowForwardPayload.nextStatusId,
              workflowForwardPayload.remarks
            );
          } catch (fwdErr) {
            console.warn('Forward workflow warning:', fwdErr);
          }
        }
        showCompleteAlertAndGoBack('Saved to Server', 'Building assessment updated and transferred successfully.');
      } else {
        throw new Error(res?.message || 'Server error');
      }
    } catch (err) {
      // Offline fallback: Queue automatically
      await queueOfflineUpdate(
        'UPDATE_BUILDING',
        payload,
        `Building FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        workflowForwardPayload
      );
      showCompleteAlertAndGoBack('Saved Offline', 'Network disconnected. Your changes and next-step transfer are safely queued on this phone. Tap Sync All when you return to the office.');
    } finally {
      setSaving(false);
    }
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
        title="BUILDING FAAS"
        subtitle={`Tracking No: ${trackingNo || 'N/A'} • Partition ${partitionLetterParam}${declarationNo ? ` • TDN: ${declarationNo}` : ''}`}
        navigation={navigation}
        showBack={true}
        onBackPress={handleCancel}
      />

      {/* Offline Status Ribbon */}
      <View style={[styles.ribbon, isDownloaded ? styles.ribbonDownloaded : styles.ribbonStandard]}>
        <Ionicons
          name={isDownloaded ? 'shield-checkmark' : 'cloud-outline'}
          size={16}
          color={isDownloaded ? (isDarkMode ? '#34d399' : '#15803d') : colors.primary}
        />
        <Text style={[styles.ribbonText, isDownloaded && { color: isDarkMode ? '#34d399' : '#15803d' }]}>
          {isDownloaded ? 'Downloaded Form • 100% Offline Field Ready' : 'Live Online Form'}
        </Text>
      </View>

      {/* Form Navigation Tabs */}
      <View style={styles.sectionTabs}>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 0 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(0)}
        >
          <Text style={[styles.sectionTabText, activeTab === 0 && styles.sectionTabTextActive]}>1. Property Info & Taxpayer (CCT)</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 1 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(1)}
        >
          <Text style={[styles.sectionTabText, activeTab === 1 && styles.sectionTabTextActive]}>2. Structural Specs & 17 Materials</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 2 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(2)}
        >
          <Text style={[styles.sectionTabText, activeTab === 2 && styles.sectionTabTextActive]}>3. Floor Levels, Additions & Valuation</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 3 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(3)}
        >
          <Text style={[styles.sectionTabText, activeTab === 3 && styles.sectionTabTextActive]}>4. Back Taxes, Effectivity & Notes</Text>
        </TouchableOpacity>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
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
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Identification & Partition TDN</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PARTITION LOT</Text>
                      <TextInput
                        style={styles.input}
                        value={`Partition Lot ${partitionLetterParam}`}
                        editable={false}
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>TDN (WEB PORTAL)</Text>
                      <TextInput
                        style={styles.input}
                        value={declarationNo}
                        editable={false}
                        placeholder="TDN System Pending"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

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

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>PROPERTY INDEX NUMBER (PIN)</Text>
                    <TextInput
                      style={styles.input}
                      value={pin}
                      onChangeText={setPin}
                      placeholder="126-00-0000-000"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

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

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>SECTION NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={sectionNo}
                        onChangeText={setSectionNo}
                        placeholder="e.g. 001"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PARCEL NO.</Text>
                      <TextInput
                        style={styles.input}
                        value={parcelNo}
                        onChangeText={setParcelNo}
                        placeholder="Parcel"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>ARP NO. (CAST FORMAT: LGU-DIST-BRGY-SEC-LOT)</Text>
                    <TextInput
                      style={styles.input}
                      value={arpNo}
                      onChangeText={setArpNo}
                      placeholder="187-00-0000-000-000"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Owner & Administrator Demographics</Text>

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

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>ADMINISTRATOR NAME</Text>
                    <TextInput
                      style={styles.input}
                      value={adminName}
                      onChangeText={setAdminName}
                      placeholder="Administrator name (if applicable)"
                      placeholderTextColor={colors.placeholder}
                    />
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
                    <Text style={styles.inputLabel}>MAILING ADDRESS</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerAddress}
                      onChangeText={setOwnerAddress}
                      placeholder="House / Unit No., Street, Barangay, City, Province"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>ADMINISTRATOR ADDRESS</Text>
                      <TextInput
                        style={styles.input}
                        value={adminAddress}
                        onChangeText={setAdminAddress}
                        placeholder="Administrator mailing address"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ADMIN CONTACT</Text>
                      <TextInput
                        style={styles.input}
                        value={adminContact}
                        onChangeText={setAdminContact}
                        placeholder="09XXXXXXXXX"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Location of Property</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>STREET ADDRESS / NO.</Text>
                    <TextInput
                      style={styles.input}
                      value={streetLocation}
                      onChangeText={setStreetLocation}
                      placeholder="e.g. 123 Pioneer Avenue"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>BARANGAY</Text>
                      <TextInput
                        style={styles.input}
                        value={districtOrBarangay}
                        onChangeText={setDistrictOrBarangay}
                        placeholder="e.g. Barangay Dadiangas South"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>CITY / MUNICIPALITY</Text>
                      <TextInput
                        style={styles.input}
                        value={cityMunicipality}
                        onChangeText={setCityMunicipality}
                        placeholder="e.g. General Santos City"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>PROVINCE</Text>
                    <TextInput
                      style={styles.input}
                      value={province}
                      onChangeText={setProvince}
                      placeholder="e.g. South Cotabato"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  sketchUrl={sketchUrl}
                  attachments={attachments}
                  onAttachmentsChange={setAttachments}
                  propertyKind="Building"
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
                      <Text style={styles.inputLabel}>TOTAL FLOOR AREA (SQ.M.)</Text>
                      <TextInput
                        style={styles.input}
                        value={totalFloorArea}
                        onChangeText={setTotalFloorArea}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>AREA PER FLOOR (SQ.M.)</Text>
                      <TextInput
                        style={styles.input}
                        value={areaPerFloor}
                        onChangeText={setAreaPerFloor}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DATE CONSTRUCTED / STARTED</Text>
                      <TextInput
                        style={styles.input}
                        value={dateConstructed}
                        onChangeText={setDateConstructed}
                        placeholder="YYYY-MM-DD"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>DATE COMPLETED</Text>
                      <TextInput
                        style={styles.input}
                        value={dateCompleted}
                        onChangeText={setDateCompleted}
                        placeholder="YYYY-MM-DD"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>DATE OCCUPIED</Text>
                    <TextInput
                      style={styles.input}
                      value={dateOccupied}
                      onChangeText={setDateOccupied}
                      placeholder="YYYY-MM-DD"
                      placeholderTextColor={colors.placeholder}
                    />
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

            {/* TAB 2: 3. Floor Levels, Additions & Valuation */}
            {activeTab === 2 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Base Construction & Additional Costs</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>REPLACEMENT COST NEW (RCN) / SUBTOTAL (PHP)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={subtotalValCompArea}
                      onChangeText={setSubtotalValCompArea}
                      keyboardType="numeric"
                      placeholder="0.00"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>ADDITIONAL IMPROVEMENTS SUB-TOTAL (PHP)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={totalAddItem}
                      onChangeText={setTotalAddItem}
                      keyboardType="numeric"
                      placeholder="0.00"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>BASE UNIT VALUE (PHP/SQ.M.)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={unitValue}
                      onChangeText={setUnitValue}
                      keyboardType="numeric"
                      placeholder="0.00"
                      placeholderTextColor={colors.placeholder}
                    />
                  </View>
                </View>

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
                      <Text style={styles.inputLabel}>ACCUMULATED DEPRECIATION (PHP)</Text>
                      <TextInput
                        style={styles.input}
                        value={depreciationAccumulated}
                        onChangeText={setDepreciationAccumulated}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>FINAL MARKET VALUE (FMV)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={marketValue}
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
                        placeholder="0.0"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ASSESSED VALUE (AV)</Text>
                      <TextInput
                        style={[styles.input, styles.valuationInput]}
                        value={assessedValue}
                        onChangeText={setAssessedValue}
                        keyboardType="numeric"
                        placeholder="0.00"
                        placeholderTextColor={colors.placeholder}
                      />
                    </View>
                  </View>

                  <View style={styles.switchRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.switchTitle}>TAXABILITY STATUS</Text>
                      <Text style={styles.switchSubtitle}>{taxability ? 'TAXABLE Property' : 'EXEMPT from Real Property Tax'}</Text>
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
                        <Text style={styles.nextStepBadgeText}>
                          Inspection Checked / Recommend Approval
                        </Text>
                      </View>
                    </View>
                  )}
                </View>
              </View>
            )}

            {/* Action Buttons */}
            <View style={styles.buttonContainer}>
              <TouchableOpacity
                style={[styles.saveBtn, styles.saveBtnPrimary]}
                onPress={() => handleSave(false)}
                disabled={saving}
              >
                {saving ? (
                  <ActivityIndicator color="#ffffff" />
                ) : (
                  <>
                    <Ionicons name="cloud-upload" size={18} color="#ffffff" />
                    <Text style={styles.saveBtnText}>Save Assessment & Stage Transfer</Text>
                  </>
                )}
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
          <DiscardChangesModal
        visible={discardModalVisible}
        onClose={() => setDiscardModalVisible(false)}
        onConfirmDiscard={navigateBack}
        partitionLetter={partitionLetterParam}
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
    paddingHorizontal: 16,
    paddingVertical: 8,
    gap: 8,
    borderBottomWidth: 1
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
  sectionTabs: {
    flexDirection: 'row',
    backgroundColor: colors.cardBg,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingHorizontal: 8,
    paddingVertical: 6,
    gap: 6
  },
  sectionTabBtn: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 4,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 8
  },
  sectionTabBtnActive: {
    backgroundColor: colors.primaryLight
  },
  sectionTabText: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted,
    textAlign: 'center'
  },
  sectionTabTextActive: {
    color: isDarkMode ? colors.primary : colors.primaryDark,
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
    fontSize: 14,
    fontWeight: '800',
    color: isDarkMode ? colors.primary : colors.primaryDark,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: 8
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
    flexDirection: 'row',
    gap: 10
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
  }
});
