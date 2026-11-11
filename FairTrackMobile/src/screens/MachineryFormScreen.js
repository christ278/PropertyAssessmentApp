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
  Switch
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SyncContext } from '../context/SyncContext';
import { useTheme } from '../context/ThemeContext';
import { MobileApi } from '../api/mobileApi';
import { HeaderBar } from '../components/HeaderBar';
import { DiscardChangesModal } from '../components/DiscardChangesModal';
import { AttachmentsSection } from '../components/AttachmentsSection';
import { DatePickerModal } from '../components/DatePickerModal';
import { COLORS, SHADOWS } from '../constants/theme';

export const MachineryFormScreen = ({ route, navigation }) => {
  const { isDarkMode, colors, shadows } = useTheme();
  const styles = React.useMemo(() => getStyles(colors, isDarkMode), [colors, isDarkMode]);
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
  const isDownloaded = isTransactionDownloaded(trackingNoParam, formWorkflowIdParam);

  // Active section tab (0: Identification & Owner, 1: Machine Specs, 2: Valuation, 3: Memoranda & Transfer)
  const [activeTab, setActiveTab] = useState(0);

  // 1. Property Identification & Ownership
  const [tdnId, setTdnId] = useState(route?.params?.tdnId || null);
  const [tdnMachineId, setTdnMachineId] = useState(null);
  const [tdnMachineDetailId, setTdnMachineDetailId] = useState(null);
  const [trackingNo, setTrackingNo] = useState(trackingNoParam);
  const [declarationNo, setDeclarationNo] = useState('');
  const [arpNo, setArpNo] = useState('');
  const [pin, setPin] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerTin, setOwnerTin] = useState('');
  const [ownerContact, setOwnerContact] = useState('');
  const [plantLocation, setPlantLocation] = useState('');
  const [cadastralSurveyNo, setCadastralSurveyNo] = useState('');
  const [lotNo, setLotNo] = useState('');
  const [landOwner, setLandOwner] = useState('');
  const [landOwnerPin, setLandOwnerPin] = useState('');
  const [buildingOwner, setBuildingOwner] = useState('');
  const [buildingOwnerPin, setBuildingOwnerPin] = useState('');
  const [transactionCode, setTransactionCode] = useState('');

  // 2. Machinery Specifications
  const [kindOfMachinery, setKindOfMachinery] = useState('');
  const [brandModel, setBrandModel] = useState('');
  const [capacity, setCapacity] = useState('');
  const [dateAcquired, setDateAcquired] = useState('');
  const [dateInstalled, setDateInstalled] = useState('');
  const [dateOperational, setDateOperational] = useState('');
  const [activeDatePicker, setActiveDatePicker] = useState(null); // 'acquired' | 'installed' | 'operational' | null
  const [conditionWhenAcquired, setConditionWhenAcquired] = useState('New');
  const [economicLifeEstimated, setEconomicLifeEstimated] = useState('10');

  // 3. Valuation & Depreciation
  const [originalCost, setOriginalCost] = useState('');
  const [freightCost, setFreightCost] = useState('');
  const [marketValue, setMarketValue] = useState('');
  const [depreciationRate, setDepreciationRate] = useState('');
  const [depreciatedValue, setDepreciatedValue] = useState('');
  const [assessmentLevel, setAssessmentLevel] = useState('80');
  const [assessedValue, setAssessedValue] = useState('');
  const [taxability, setTaxability] = useState(true);
  const [sketchUrl, setSketchUrl] = useState('');
  const [attachments, setAttachments] = useState([]); // true = Taxable, false = Exempt

  // 4. Effectivity & Memoranda
  const [yearEffectivity, setYearEffectivity] = useState(new Date().getFullYear().toString());
  const [quarterEffectivity, setQuarterEffectivity] = useState('1');
  const [memoranda, setMemoranda] = useState('');
  const [remarks, setRemarks] = useState('');

  // Workflow Forwarding
  const [autoForward, setAutoForward] = useState(true);
  const [nextStatusId, setNextStatusId] = useState(4); // 4 = Checked / Recommend Approval

  useEffect(() => {
    loadMachineryData();
  }, []);

  const loadMachineryData = async () => {
    setNotDownloadedError(false);

    // 1. Check if downloaded offline copy exists and populate immediately
    const downloaded = getDownloadedFormData(trackingNoParam, formWorkflowIdParam, partitionIndexParam);
    if (downloaded) {
      if (downloaded.owner) setOwnerName(downloaded.owner);
      if (downloaded.arpNo) setArpNo(downloaded.arpNo);
      if (downloaded.tdnId) setTdnId(downloaded.tdnId);
      if (downloaded.sketchUrl) setSketchUrl(downloaded.sketchUrl);
      if (downloaded.attachments && Array.isArray(downloaded.attachments)) setAttachments(downloaded.attachments);

      if (downloaded.currentPartition) {
        const cp = downloaded.currentPartition;
        if (cp.systemGeneratedTDN) setDeclarationNo(cp.systemGeneratedTDN);
        if (cp.arpNo) setArpNo(cp.arpNo);
        if (cp.pin) setPin(cp.pin);
        if (cp.streetLocation || cp.streetNo || cp.propertyLocation) {
          setPlantLocation(cp.streetLocation || cp.streetNo || cp.propertyLocation);
        }
        if (cp.lotNo || cp.lotNumber) setLotNo(cp.lotNo || cp.lotNumber);
        if (cp.surveyNo || cp.surveyNumber || cp.cadastralSurveyNo) setCadastralSurveyNo(cp.surveyNo || cp.surveyNumber || cp.cadastralSurveyNo);
      }

      if (downloaded.formData) {
        populateFieldsFromDTO(downloaded.formData);
      }
    }

    // 2. If online and no unsynced offline edits, fetch latest live partition data (including newly uploaded web photos & sketches)
    const hasUnsynced = typeof hasPendingEdits === 'function' && hasPendingEdits(trackingNoParam, formWorkflowIdParam);
    if (!hasUnsynced && (trackingNoParam || formWorkflowIdParam)) {
      try {
        if (!downloaded) setLoading(true);
        const liveRes = await MobileApi.downloadTransactionDetails(formWorkflowIdParam, trackingNoParam);
        if (liveRes && liveRes.success) {
          const targetIdx = parseInt(partitionIndexParam, 10) || 0;
          const livePart = (liveRes.partitions && liveRes.partitions[targetIdx]) || null;
          const liveAtts = livePart?.attachments || liveRes.attachments || [];
          const liveSketch = livePart?.sketchUrl || liveRes.sketchUrl;

          if (liveSketch) setSketchUrl(liveSketch);
          if (Array.isArray(liveAtts)) setAttachments(liveAtts);

          if (livePart) {
            if (livePart.systemGeneratedTDN) setDeclarationNo(livePart.systemGeneratedTDN);
            if (livePart.arpNo) setArpNo(livePart.arpNo);
            if (livePart.pin) setPin(livePart.pin);
            if (livePart.streetLocation || livePart.streetNo || livePart.propertyLocation) {
              setPlantLocation(livePart.streetLocation || livePart.streetNo || livePart.propertyLocation);
            }
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
      if (t.DisplayName) setOwnerName(t.DisplayName);
      if (t.OwnerAddress) setOwnerAddress(t.OwnerAddress);
      if (t.Location || t.PropLocationStreet) setPlantLocation(t.Location || t.PropLocationStreet);
      if (t.CadastralSurveyNo) setCadastralSurveyNo(t.CadastralSurveyNo);
      if (t.LotNo) setLotNo(t.LotNo);
      if (t.TransactionCode) setTransactionCode(t.TransactionCode);
      if (t.YearEffectivity != null) setYearEffectivity(t.YearEffectivity.toString());
      if (t.QuarterEffectivity != null) setQuarterEffectivity(t.QuarterEffectivity.toString());
      if (t.TotalMarketValue != null) setMarketValue(t.TotalMarketValue.toString());
      if (t.TotalAssessedValue != null) setAssessedValue(t.TotalAssessedValue.toString());
      if (t.Memoranda) setMemoranda(t.Memoranda);
      if (t.Taxability != null) setTaxability(!!t.Taxability);
      if (t.FloorPlanSketchPath) setSketchUrl(t.FloorPlanSketchPath);
      const rawAtts = t.Attachments || t.attachments;
      if (rawAtts && Array.isArray(rawAtts) && rawAtts.length > 0) setAttachments(rawAtts);
    }

    if (d.refOwner) {
      const o = d.refOwner;
      if (!ownerName && (o.OwnerName || o.FullName)) setOwnerName(o.OwnerName || o.FullName);
      if (o.TaxIdentNo) setOwnerTin(o.TaxIdentNo);
      if (o.ContactNo) setOwnerContact(o.ContactNo);
    }

    if (d.TDNMachine) {
      const m = d.TDNMachine;
      if (m.TDNMachineId) setTdnMachineId(m.TDNMachineId);
      if (m.LandOwner) setLandOwner(m.LandOwner);
      if (m.LandOwnerPIN) setLandOwnerPin(m.LandOwnerPIN);
      if (m.BuildingOwner) setBuildingOwner(m.BuildingOwner);
      if (m.BuildingOwnerPIN) setBuildingOwnerPin(m.BuildingOwnerPIN);
    }

    if (d.TDNMachineDetail && d.TDNMachineDetail.length > 0) {
      const md = d.TDNMachineDetail[0];
      if (md.TDNMachineDetailId) setTdnMachineDetailId(md.TDNMachineDetailId);
      if (md.KindOfMachinery) setKindOfMachinery(md.KindOfMachinery);
      if (md.BrandModel) setBrandModel(md.BrandModel);
      if (md.Capacity) setCapacity(md.Capacity);
      if (md.AcquiredDate) setDateAcquired(md.AcquiredDate.toString().substring(0, 10));
      if (md.DateOfInstallation) setDateInstalled(md.DateOfInstallation.toString().substring(0, 10));
      if (md.DateOfOperation) setDateOperational(md.DateOfOperation.toString().substring(0, 10));
      if (md.ConditionWhenAcquired) setConditionWhenAcquired(md.ConditionWhenAcquired);
      if (md.EconomicLifeEstimated != null) setEconomicLifeEstimated(md.EconomicLifeEstimated.toString());
      if (md.OriginalCost != null) setOriginalCost(md.OriginalCost.toString());
      if (md.MarketValue != null && !marketValue) setMarketValue(md.MarketValue.toString());
      if (md.AssessmentLevel != null) setAssessmentLevel(md.AssessmentLevel.toString());
      if (md.AssessedValue != null && !assessedValue) setAssessedValue(md.AssessedValue.toString());
      if (md.DepreciatedValue != null) setDepreciatedValue(md.DepreciatedValue.toString());
      if (md.DepreciationTotalPercentage != null) setDepreciationRate(md.DepreciationTotalPercentage.toString());
    }
  };

      const handleGenerateCastArp = () => {
    // CAST Combination #1: {LGUCode}-{DistrictMunicipalityCode}-{BarangayCode:D4}-{SectionNo:D3}-{LotOrParcelNo:D3}
    const secClean = (sectionNo || '').replace(/\D/g, '') || '0';
    const sec3 = secClean.padStart(3, '0').slice(-3);
    const lotClean = (lotNo || parcelNo || '').replace(/\D/g, '') || '0';
    const lot3 = lotClean.padStart(3, '0').slice(-3);

    let brgy4 = '0000';
    if (arpNo && arpNo.includes('-')) {
      const parts = arpNo.split('-');
      if (parts.length >= 3 && parts[2]) {
        brgy4 = parts[2].padStart(4, '0').slice(-4);
      }
    }
    const formatted = `187-00-${brgy4}-${sec3}-${lot3}`;
    setArpNo(formatted);
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
        DisplayName: ownerName,
        OwnerAddress: ownerAddress,
        Location: plantLocation,
        PropLocationStreet: plantLocation,
        CadastralSurveyNo: cadastralSurveyNo,
        LotNo: lotNo,
        TransactionCode: transactionCode,
        TotalMarketValue: parseFloat(marketValue) || 0,
        TotalAssessedValue: parseFloat(assessedValue) || 0,
        FloorPlanSketchPath: sketchUrl,
        Attachments: attachments,
        YearEffectivity: parseInt(yearEffectivity) || null,
        QuarterEffectivity: parseInt(quarterEffectivity) || null,
        Memoranda: memoranda,
        Taxability: taxability
      },
      TDNMachine: {
        TDNMachineId: parseInt(tdnMachineId) || 0,
        TDNId: parseInt(tdnId) || 0,
        LandOwner: landOwner,
        LandOwnerPIN: landOwnerPin,
        BuildingOwner: buildingOwner,
        BuildingOwnerPIN: buildingOwnerPin
      },
      TDNMachineDetail: [
        {
          TDNMachineDetailId: parseInt(tdnMachineDetailId) || 0,
          TDNMachineId: parseInt(tdnMachineId) || 0,
          KindOfMachinery: kindOfMachinery,
          BrandModel: brandModel,
          Capacity: capacity,
          AcquiredDate: dateAcquired || null,
          DateOfInstallation: dateInstalled || null,
          DateOfOperation: dateOperational || null,
          ConditionWhenAcquired: conditionWhenAcquired,
          EconomicLifeEstimated: parseInt(economicLifeEstimated) || null,
          OriginalCost: parseFloat(originalCost) || null,
          MarketValue: parseFloat(marketValue) || 0,
          AssessmentLevel: parseInt(assessmentLevel) || null,
          AssessedValue: parseFloat(assessedValue) || 0,
          DepreciatedValue: parseFloat(depreciatedValue) || null,
          DepreciationTotalPercentage: parseInt(depreciationRate) || null
        }
      ]
    };

    const workflowForwardPayload = transferStage && formWorkflowIdParam ? {
      formWorkflowId: formWorkflowIdParam,
      nextStatusId: nextStatusId,
      remarks: remarks || 'Field machinery appraisal completed and transferred.'
    } : null;

    if (forceOffline) {
      setSaving(true);
      await queueOfflineUpdate(
        'UPDATE_MACHINERY',
        payload,
        `Machinery FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Company'})`,
        workflowForwardPayload
      );
      setSaving(false);
      navigateBack();
      return;
    }

    setSaving(true);

    try {
      const res = await MobileApi.updateMachineryForm(payload);
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
        navigateBack();
        return;
      }
    } catch (err) {
      // Fallback to offline queue
    }

    // Fallback: Queue offline safely without alert
    await queueOfflineUpdate(
      'UPDATE_MACHINERY',
      payload,
      `Machinery FAAS [Part ${partitionLetterParam}]: ${trackingNo || arpNo} (${ownerName || 'Company'})`,
      workflowForwardPayload
    );
    setSaving(false);
    navigateBack();
  };

  return (
    <View style={styles.container}>
      <HeaderBar title="MACHINERY FAAS" subtitle={`Tracking No: ${trackingNo || 'N/A'} • Partition ${partitionLetterParam}${declarationNo ? ` • TDN: ${declarationNo}` : ''}`} navigation={navigation}
        showBack={true}
        onBackPress={handleCancel}
      />

      {/* Offline Status Ribbon */}
      <View style={[styles.ribbon, isDownloaded ? styles.ribbonDownloaded : styles.ribbonStandard]}>
        <Ionicons name={isDownloaded ? "shield-checkmark" : "cloud-outline"} size={16} color={isDownloaded ? (isDarkMode ? "#34d399" : "#15803d") : colors.primary} />
        <Text style={[styles.ribbonText, isDownloaded && { color: isDarkMode ? '#34d399' : '#15803d' }]}>
          {isDownloaded ? "Downloaded Form • 100% Offline Field Ready" : "Live Online Form"}
        </Text>
      </View>


      {/* Form Navigation Tabs */}
      <View style={styles.sectionTabs}>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 0 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(0)}
        >
          <Text style={[styles.sectionTabText, activeTab === 0 && styles.sectionTabTextActive]}>1. Ident</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 1 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(1)}
        >
          <Text style={[styles.sectionTabText, activeTab === 1 && styles.sectionTabTextActive]}>2. Machine Specs</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 2 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(2)}
        >
          <Text style={[styles.sectionTabText, activeTab === 2 && styles.sectionTabTextActive]}>3. Valuation</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 3 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(3)}
        >
          <Text style={[styles.sectionTabText, activeTab === 3 && styles.sectionTabTextActive]}>
            4. Photos & Sketch {attachments && attachments.length > 0 ? `(${attachments.length})` : ''}
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 4 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(4)}
        >
          <Text style={[styles.sectionTabText, activeTab === 4 && styles.sectionTabTextActive]}>5. Transfer</Text>
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
              This machinery assessment data has not been downloaded to your device storage. To view and inspect this form in offline mode, please connect to Wi-Fi/Mobile Data and tap "Download" in the transactions list.
            </Text>
            <TouchableOpacity style={styles.backBtn} onPress={navigateBack}>
              <Ionicons name="arrow-back" size={18} color="#ffffff" />
              <Text style={styles.backBtnText}>Return to Transactions</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            {/* TAB 0: PROPERTY IDENTIFICATION & OWNERSHIP */}
            {activeTab === 0 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Numbers & References</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ARP No.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={arpNo}
                        onChangeText={setArpNo}
                        placeholder="02-0003-XXXX"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PIN</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={pin}
                        onChangeText={setPin}
                        placeholder="PIN No."
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Declaration No.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={declarationNo}
                        editable={false}
                        placeholder="Generated by Web Portal"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Transaction Code</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={transactionCode}
                        onChangeText={setTransactionCode}
                        placeholder="e.g. TR-MACH"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Cadastral Survey No.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={cadastralSurveyNo}
                        onChangeText={setCadastralSurveyNo}
                        placeholder="Survey No."
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Lot No.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={lotNo}
                        onChangeText={setLotNo}
                        placeholder="Lot No."
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Plant / Installation Location</Text>
                    <TextInput
                        placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={plantLocation}
                      onChangeText={setPlantLocation}
                      placeholder="Street, Sitio, or Barangay"
                    />
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Machinery Owner & Facility References</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Owner / Company Name</Text>
                    <TextInput
                        placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={ownerName}
                      onChangeText={setOwnerName}
                      placeholder="Company / Owner Name"
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Owner Address</Text>
                    <TextInput
                        placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={ownerAddress}
                      onChangeText={setOwnerAddress}
                      placeholder="Business / Mailing Address"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Owner TIN</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={ownerTin}
                        onChangeText={setOwnerTin}
                        placeholder="TIN No."
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Contact No.</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={ownerContact}
                        onChangeText={setOwnerContact}
                        placeholder="Phone / Mobile"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Land Owner</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={landOwner}
                        onChangeText={setLandOwner}
                        placeholder="Land Owner"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Land Owner PIN</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={landOwnerPin}
                        onChangeText={setLandOwnerPin}
                        placeholder="PIN"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Building Owner</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={buildingOwner}
                        onChangeText={setBuildingOwner}
                        placeholder="Building Owner"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Building Owner PIN</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={buildingOwnerPin}
                        onChangeText={setBuildingOwnerPin}
                        placeholder="PIN"
                      />
                    </View>
                  </View>
                </View>
              </View>
            )}

            {/* TAB 1: MACHINERY SPECIFICATIONS */}
            {activeTab === 1 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Equipment Description & Specs</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Kind / Description of Machinery</Text>
                    <TextInput
                        placeholderTextColor={colors.placeholder}
                      style={styles.input}
                      value={kindOfMachinery}
                      onChangeText={setKindOfMachinery}
                      placeholder="e.g. Rice Mill Generator, Excavator, Lathe"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Brand & Model</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={brandModel}
                        onChangeText={setBrandModel}
                        placeholder="e.g. Caterpillar 320D"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Capacity / Rating</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={capacity}
                        onChangeText={setCapacity}
                        placeholder="e.g. 500 kVA / 20 HP"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Condition Acquired</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={conditionWhenAcquired}
                        onChangeText={setConditionWhenAcquired}
                        placeholder="New / Second Hand"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Economic Life (Yrs)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={economicLifeEstimated}
                        onChangeText={setEconomicLifeEstimated}
                        keyboardType="numeric"
                      />
                    </View>
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Acquisition & Operation Timeline</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Date Acquired (YYYY-MM-DD)</Text>
                    <View style={{ flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, { flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0, borderRightWidth: 0 }]}
                        value={dateAcquired}
                        onChangeText={setDateAcquired}
                        placeholder="YYYY-MM-DD"
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
                        onPress={() => setActiveDatePicker('acquired')}
                      >
                        <Ionicons name="calendar-outline" size={17} color="#ffffff" />
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Date of Installation (YYYY-MM-DD)</Text>
                    <View style={{ flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, { flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0, borderRightWidth: 0 }]}
                        value={dateInstalled}
                        onChangeText={setDateInstalled}
                        placeholder="YYYY-MM-DD"
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
                        onPress={() => setActiveDatePicker('installed')}
                      >
                        <Ionicons name="calendar-outline" size={17} color="#ffffff" />
                      </TouchableOpacity>
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Date of Operation (YYYY-MM-DD)</Text>
                    <View style={{ flexDirection: 'row', alignItems: 'stretch', width: '100%' }}>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, { flex: 1, borderTopRightRadius: 0, borderBottomRightRadius: 0, borderRightWidth: 0 }]}
                        value={dateOperational}
                        onChangeText={setDateOperational}
                        placeholder="YYYY-MM-DD"
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
                        onPress={() => setActiveDatePicker('operational')}
                      >
                        <Ionicons name="calendar-outline" size={17} color="#ffffff" />
                      </TouchableOpacity>
                    </View>
                  </View>
                </View>
              </View>
            )}

            {/* TAB 2: APPRAISAL & VALUATION */}
            {activeTab === 2 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Acquisition Cost & Market Valuation</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Original Cost (PHP)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, styles.valuationInput]}
                        value={originalCost}
                        onChangeText={setOriginalCost}
                        keyboardType="numeric"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Freight / Setup (PHP)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, styles.valuationInput]}
                        value={freightCost}
                        onChangeText={setFreightCost}
                        keyboardType="numeric"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Total Market Value / Replacement Cost New (PHP)</Text>
                    <TextInput
                        placeholderTextColor={colors.placeholder}
                      style={[styles.input, styles.valuationInput]}
                      value={marketValue}
                      onChangeText={setMarketValue}
                      keyboardType="numeric"
                    />
                  </View>
                </View>

                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Depreciation & Assessed Value</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Depreciation Rate (%)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={depreciationRate}
                        onChangeText={setDepreciationRate}
                        keyboardType="numeric"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Depreciated Value (PHP)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, styles.valuationInput]}
                        value={depreciatedValue}
                        onChangeText={setDepreciatedValue}
                        keyboardType="numeric"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Assessment Level (%)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={assessmentLevel}
                        onChangeText={setAssessmentLevel}
                        keyboardType="numeric"
                        placeholder="e.g. 80"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>Total Assessed Value (PHP)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={[styles.input, styles.valuationInput]}
                        value={assessedValue}
                        onChangeText={setAssessedValue}
                        keyboardType="numeric"
                      />
                    </View>
                  </View>

                  <View style={styles.switchRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.switchTitle}>Property Taxability</Text>
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

            {/* TAB 3: SITE SKETCH & FIELD INSPECTION ATTACHMENTS */}
            {activeTab === 3 && (
              <View style={styles.tabContentBlock}>
                <AttachmentsSection
                  trackingNo={trackingNo || trackingNoParam}
                  sketchUrl={sketchUrl}
                  attachments={attachments}
                  onAttachmentsChange={setAttachments}
                  propertyKind="Machinery"
                />
              </View>
            )}

            {/* TAB 4: EFFECTIVITY, MEMORANDA & WORKFLOW */}
            {activeTab === 4 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, shadows.card]}>
                  <Text style={styles.cardHeaderTitle}>Tax Effectivity & Memoranda</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Year Effectivity</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={yearEffectivity}
                        onChangeText={setYearEffectivity}
                        keyboardType="numeric"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Quarter Effectivity (1-4)</Text>
                      <TextInput
                        placeholderTextColor={colors.placeholder}
                        style={styles.input}
                        value={quarterEffectivity}
                        onChangeText={setQuarterEffectivity}
                        keyboardType="numeric"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Memoranda</Text>
                    <TextInput
                        placeholderTextColor={colors.placeholder}
                      style={[styles.input, { height: 60, textAlignVertical: 'top' }]}
                      value={memoranda}
                      onChangeText={setMemoranda}
                      multiline
                      placeholder="Superceded assessment memoranda..."
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Inspector Field Remarks</Text>
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
          {/* Date Picker Modal for Machinery Dates */}
      <DatePickerModal
        visible={!!activeDatePicker}
        onClose={() => setActiveDatePicker(null)}
        value={
          activeDatePicker === 'acquired' ? dateAcquired :
          activeDatePicker === 'installed' ? dateInstalled :
          activeDatePicker === 'operational' ? dateOperational : ''
        }
        onSelectDate={(newDate) => {
          if (activeDatePicker === 'acquired') setDateAcquired(newDate);
          else if (activeDatePicker === 'installed') setDateInstalled(newDate);
          else if (activeDatePicker === 'operational') setDateOperational(newDate);
        }}
        title={
          activeDatePicker === 'acquired' ? 'Date Acquired' :
          activeDatePicker === 'installed' ? 'Date of Installation' :
          activeDatePicker === 'operational' ? 'Date of Operation' : 'Select Date'
        }
      />

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
    alignItems: 'center',
    borderRadius: 8
  },
  sectionTabBtnActive: {
    backgroundColor: colors.primaryLight
  },
  sectionTabText: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted
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
  }
});
