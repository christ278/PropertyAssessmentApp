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
  Switch
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { SyncContext } from '../context/SyncContext';
import { MobileApi } from '../api/mobileApi';
import { HeaderBar } from '../components/HeaderBar';
import { COLORS, SHADOWS } from '../constants/theme';

export const LandFormScreen = ({ route, navigation }) => {
  const trackingNoParam = route?.params?.trackingNo || '';
  const formWorkflowIdParam = route?.params?.formWorkflowId || null;
  const { queueOfflineUpdate, getDownloadedFormData, isTransactionDownloaded } = useContext(SyncContext);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [notDownloadedError, setNotDownloadedError] = useState(false);
  const isDownloaded = isTransactionDownloaded(trackingNoParam, formWorkflowIdParam);

  // Active section tab (0: Identification & Owner, 1: Boundaries, 2: Appraisal & Valuation, 3: Memoranda & Transfer)
  const [activeTab, setActiveTab] = useState(0);

  // 1. Property Identification & Ownership
  const [tdnId, setTdnId] = useState(route?.params?.tdnId || null);
  const [tdnLandId, setTdnLandId] = useState(null);
  const [trackingNo, setTrackingNo] = useState(trackingNoParam);
  const [declarationNo, setDeclarationNo] = useState('');
  const [arpNo, setArpNo] = useState('');
  const [pin, setPin] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerTin, setOwnerTin] = useState('');
  const [ownerContact, setOwnerContact] = useState('');
  const [adminName, setAdminName] = useState('');
  const [adminAddress, setAdminAddress] = useState('');
  const [adminContact, setAdminContact] = useState('');
  const [streetLocation, setStreetLocation] = useState('');
  const [cadastralSurveyNo, setCadastralSurveyNo] = useState('');
  const [lotNo, setLotNo] = useState('');
  const [sectionNo, setSectionNo] = useState('');
  const [parcelNo, setParcelNo] = useState('');
  const [tctNo, setTctNo] = useState('');
  const [tctDate, setTctDate] = useState('');
  const [transactionCode, setTransactionCode] = useState('');

  // 2. Boundaries
  const [northBoundary, setNorthBoundary] = useState('');
  const [nRemarks, setNRemarks] = useState('');
  const [southBoundary, setSouthBoundary] = useState('');
  const [sRemarks, setSRemarks] = useState('');
  const [eastBoundary, setEastBoundary] = useState('');
  const [eRemarks, setERemarks] = useState('');
  const [westBoundary, setWestBoundary] = useState('');
  const [wRemarks, setWRemarks] = useState('');

  // 3. Appraisal & Valuation
  const [classification, setClassification] = useState('Residential');
  const [actualUse, setActualUse] = useState('Residential');
  const [area, setArea] = useState('');
  const [unit, setUnit] = useState('SQM');
  const [baseMarketValue, setBaseMarketValue] = useState('');
  const [marketValue, setMarketValue] = useState('');
  const [assessmentLevel, setAssessmentLevel] = useState('20');
  const [assessedValue, setAssessedValue] = useState('');
  const [taxability, setTaxability] = useState(true); // true = Taxable, false = Exempt

  // 4. Effectivity & Memoranda
  const [yearEffectivity, setYearEffectivity] = useState(new Date().getFullYear().toString());
  const [quarterEffectivity, setQuarterEffectivity] = useState('1');
  const [memoranda, setMemoranda] = useState('');
  const [remarks, setRemarks] = useState('');

  // Workflow Forwarding
  const [autoForward, setAutoForward] = useState(true);
  const [nextStatusId, setNextStatusId] = useState(4); // 4 = Checked / Recommend Approval

  useEffect(() => {
    loadLandData();
  }, []);

  const loadLandData = async () => {
    setNotDownloadedError(false);

    // 1. Check if downloaded offline copy exists
    const downloaded = getDownloadedFormData(trackingNoParam, formWorkflowIdParam);
    if (downloaded) {
      if (downloaded.owner) setOwnerName(downloaded.owner);
      if (downloaded.arpNo) setArpNo(downloaded.arpNo);
      if (downloaded.tdnId) setTdnId(downloaded.tdnId);

      if (downloaded.formData) {
        populateFieldsFromDTO(downloaded.formData);
      }
      return;
    }

    // 2. If not downloaded, fetch from API if online
    try {
      setLoading(true);
      const res = await MobileApi.getLandForm(tdnId || 1, tdnLandId || 1);
      if (res && res.success && res.data) {
        populateFieldsFromDTO(res.data);
      } else {
        setNotDownloadedError(true);
      }
    } catch (err) {
      console.warn('Cannot fetch live land form and not downloaded:', err);
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
      if (t.DisplayName) setOwnerName(t.DisplayName);
      if (t.OwnerAddress) setOwnerAddress(t.OwnerAddress);
      if (t.PropAdminName) setAdminName(t.PropAdminName);
      if (t.PropAdminAddress) setAdminAddress(t.PropAdminAddress);
      if (t.PropAdminContactNo) setAdminContact(t.PropAdminContactNo);
      if (t.PropLocationStreet || t.Location) setStreetLocation(t.PropLocationStreet || t.Location);
      if (t.CadastralSurveyNo) setCadastralSurveyNo(t.CadastralSurveyNo);
      if (t.LotNo) setLotNo(t.LotNo);
      if (t.SectionNo) setSectionNo(t.SectionNo);
      if (t.ParcelNo) setParcelNo(t.ParcelNo);
      if (t.TCTNo) setTctNo(t.TCTNo);
      if (t.TCTDate) setTctDate(t.TCTDate.toString().substring(0, 10));
      if (t.TransactionCode) setTransactionCode(t.TransactionCode);
      if (t.YearEffectivity != null) setYearEffectivity(t.YearEffectivity.toString());
      if (t.QuarterEffectivity != null) setQuarterEffectivity(t.QuarterEffectivity.toString());
      if (t.TotalArea != null) setArea(t.TotalArea.toString());
      if (t.Unit) setUnit(t.Unit);
      if (t.TotalMarketValue != null) setMarketValue(t.TotalMarketValue.toString());
      if (t.TotalAssessedValue != null) setAssessedValue(t.TotalAssessedValue.toString());
      if (t.Memoranda) setMemoranda(t.Memoranda);
      if (t.Taxability != null) setTaxability(!!t.Taxability);
    }

    if (d.refOwner) {
      const o = d.refOwner;
      if (!ownerName && (o.OwnerName || o.FullName)) setOwnerName(o.OwnerName || o.FullName);
      if (o.TaxIdentNo) setOwnerTin(o.TaxIdentNo);
      if (o.ContactNo) setOwnerContact(o.ContactNo);
    }

    if (d.TDNLand) {
      const l = d.TDNLand;
      if (l.TDNLandId) setTdnLandId(l.TDNLandId);
      if (l.NorthBoundary) setNorthBoundary(l.NorthBoundary);
      if (l.Nremarks) setNRemarks(l.Nremarks);
      if (l.SouthBoundary) setSouthBoundary(l.SouthBoundary);
      if (l.Sremarks) setSRemarks(l.Sremarks);
      if (l.EastBoundary) setEastBoundary(l.EastBoundary);
      if (l.Eremarks) setERemarks(l.Eremarks);
      if (l.WestBoundary) setWestBoundary(l.WestBoundary);
      if (l.Wremarks) setWRemarks(l.Wremarks);
    }

    // Check TDNLandDetails
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

  const handleSave = async (forceOffline = false) => {
    const payload = {
      TDN: {
        TDNId: parseInt(tdnId) || 0,
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
        SectionNo: sectionNo,
        ParcelNo: parcelNo,
        TCTNo: tctNo,
        TCTDate: tctDate || null,
        TransactionCode: transactionCode,
        TotalArea: parseFloat(area) || 0,
        Unit: unit,
        TotalMarketValue: parseFloat(marketValue) || 0,
        TotalAssessedValue: parseFloat(assessedValue) || 0,
        YearEffectivity: parseInt(yearEffectivity) || null,
        QuarterEffectivity: parseInt(quarterEffectivity) || null,
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
        Wremarks: wRemarks
      }
    };

    const workflowForwardPayload = autoForward && formWorkflowIdParam ? {
      formWorkflowId: formWorkflowIdParam,
      nextStatusId: nextStatusId,
      remarks: remarks || 'Field land appraisal completed.'
    } : null;

    if (forceOffline) {
      await queueOfflineUpdate(
        'UPDATE_LAND',
        payload,
        `Land Appraisal: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        workflowForwardPayload
      );
      Alert.alert(
        'Queued for Office Sync',
        'Your land inspection changes are safely saved locally on this phone. It will automatically upload and transfer to the next step when you return to the office.',
        [{ text: 'OK', onPress: () => navigation.goBack() }]
      );
      return;
    }

    try {
      setSaving(true);
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
            console.warn('Forward workflow warning:', fwdErr);
          }
        }
        Alert.alert('Saved to Server', 'Land appraisal updated and transferred successfully.', [
          { text: 'OK', onPress: () => navigation.goBack() }
        ]);
      } else {
        throw new Error(res?.message || 'Server error');
      }
    } catch (err) {
      // Offline fallback: Queue automatically
      await queueOfflineUpdate(
        'UPDATE_LAND',
        payload,
        `Land Appraisal: ${trackingNo || arpNo} (${ownerName || 'Owner'})`,
        workflowForwardPayload
      );
      Alert.alert(
        'Saved Offline',
        'Network disconnected. Your changes and next-step transfer are safely queued on this phone. Tap Sync All when you return to the office.',
        [{ text: 'OK', onPress: () => navigation.goBack() }]
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={styles.container}>
      <HeaderBar title="LAND APPRAISAL" subtitle={`Tracking No: ${trackingNo || 'N/A'}`} navigation={navigation} />

      {/* Offline Status Ribbon */}
      <View style={[styles.ribbon, isDownloaded ? styles.ribbonDownloaded : styles.ribbonStandard]}>
        <Ionicons name={isDownloaded ? "shield-checkmark" : "cloud-outline"} size={16} color={isDownloaded ? "#15803d" : COLORS.primary} />
        <Text style={[styles.ribbonText, isDownloaded && { color: '#15803d' }]}>
          {isDownloaded ? "Downloaded Form • 100% Offline Field Ready" : "Live Online Form"}
        </Text>
      </View>

      {/* Form Navigation Tabs */}
      <View style={styles.sectionTabs}>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 0 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(0)}
        >
          <Text style={[styles.sectionTabText, activeTab === 0 && styles.sectionTabTextActive]}>1. Identification</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 1 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(1)}
        >
          <Text style={[styles.sectionTabText, activeTab === 1 && styles.sectionTabTextActive]}>2. Boundaries</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 2 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(2)}
        >
          <Text style={[styles.sectionTabText, activeTab === 2 && styles.sectionTabTextActive]}>3. Appraisal</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.sectionTabBtn, activeTab === 3 && styles.sectionTabBtnActive]}
          onPress={() => setActiveTab(3)}
        >
          <Text style={[styles.sectionTabText, activeTab === 3 && styles.sectionTabTextActive]}>4. Transfer</Text>
        </TouchableOpacity>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        {loading ? (
          <ActivityIndicator color={COLORS.primary} size="large" style={{ marginVertical: 30 }} />
        ) : notDownloadedError ? (
          <View style={styles.notDownloadedBox}>
            <Ionicons name="cloud-offline-outline" size={56} color="#d97706" />
            <Text style={styles.notDownloadedTitle}>Form Not Stored on Phone</Text>
            <Text style={styles.notDownloadedText}>
              This land appraisal data has not been downloaded to your device storage. To view and inspect this form in offline mode, please connect to Wi-Fi/Mobile Data and tap "Download" in the transactions list.
            </Text>
            <TouchableOpacity style={styles.backBtn} onPress={() => navigation.goBack()}>
              <Ionicons name="arrow-back" size={18} color="#ffffff" />
              <Text style={styles.backBtnText}>Return to Transactions</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            {/* TAB 0: PROPERTY IDENTIFICATION & OWNERSHIP */}
            {activeTab === 0 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Property Numbers & Cadastral Reference</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>ARP No.</Text>
                      <TextInput
                        style={styles.input}
                        value={arpNo}
                        onChangeText={setArpNo}
                        placeholder="02-0002-XXXX"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>PIN</Text>
                      <TextInput
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
                        style={styles.input}
                        value={declarationNo}
                        onChangeText={setDeclarationNo}
                        placeholder="Declaration No."
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Transaction Code</Text>
                      <TextInput
                        style={styles.input}
                        value={transactionCode}
                        onChangeText={setTransactionCode}
                        placeholder="e.g. TR-LAND"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>OCT/TCT/CLOA No.</Text>
                      <TextInput
                        style={styles.input}
                        value={tctNo}
                        onChangeText={setTctNo}
                        placeholder="Title No."
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Title Date</Text>
                      <TextInput
                        style={styles.input}
                        value={tctDate}
                        onChangeText={setTctDate}
                        placeholder="YYYY-MM-DD"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Cadastral Survey No.</Text>
                      <TextInput
                        style={styles.input}
                        value={cadastralSurveyNo}
                        onChangeText={setCadastralSurveyNo}
                        placeholder="Survey No."
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Lot No.</Text>
                      <TextInput
                        style={styles.input}
                        value={lotNo}
                        onChangeText={setLotNo}
                        placeholder="Lot No."
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Section No.</Text>
                      <TextInput
                        style={styles.input}
                        value={sectionNo}
                        onChangeText={setSectionNo}
                        placeholder="Section"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Parcel No.</Text>
                      <TextInput
                        style={styles.input}
                        value={parcelNo}
                        onChangeText={setParcelNo}
                        placeholder="Parcel"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Property Location / Street</Text>
                    <TextInput
                      style={styles.input}
                      value={streetLocation}
                      onChangeText={setStreetLocation}
                      placeholder="Street, Sitio, or Purok"
                    />
                  </View>
                </View>

                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Owner & Administrator Details</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Owner / Declared Name</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerName}
                      onChangeText={setOwnerName}
                      placeholder="Owner Name"
                    />
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Owner Mailing Address</Text>
                    <TextInput
                      style={styles.input}
                      value={ownerAddress}
                      onChangeText={setOwnerAddress}
                      placeholder="Mailing Address"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Owner TIN</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerTin}
                        onChangeText={setOwnerTin}
                        placeholder="TIN No."
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Contact No.</Text>
                      <TextInput
                        style={styles.input}
                        value={ownerContact}
                        onChangeText={setOwnerContact}
                        placeholder="Phone / Mobile"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Administrator / Representative Name</Text>
                    <TextInput
                      style={styles.input}
                      value={adminName}
                      onChangeText={setAdminName}
                      placeholder="Admin Name"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>Administrator Address</Text>
                      <TextInput
                        style={styles.input}
                        value={adminAddress}
                        onChangeText={setAdminAddress}
                        placeholder="Admin Address"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Admin Contact</Text>
                      <TextInput
                        style={styles.input}
                        value={adminContact}
                        onChangeText={setAdminContact}
                        placeholder="Contact"
                      />
                    </View>
                  </View>
                </View>
              </View>
            )}

            {/* TAB 1: BOUNDARIES */}
            {activeTab === 1 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Parcel Boundaries & Adjoining Lots</Text>

                  {/* North */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>NORTH BOUNDARY</Text>
                    <TextInput
                      style={styles.input}
                      value={northBoundary}
                      onChangeText={setNorthBoundary}
                      placeholder="North adjacent owner / Lot No."
                    />
                    <TextInput
                      style={[styles.input, { marginTop: 4 }]}
                      value={nRemarks}
                      onChangeText={setNRemarks}
                      placeholder="North boundary notes..."
                    />
                  </View>

                  {/* South */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>SOUTH BOUNDARY</Text>
                    <TextInput
                      style={styles.input}
                      value={southBoundary}
                      onChangeText={setSouthBoundary}
                      placeholder="South adjacent owner / Lot No."
                    />
                    <TextInput
                      style={[styles.input, { marginTop: 4 }]}
                      value={sRemarks}
                      onChangeText={setSRemarks}
                      placeholder="South boundary notes..."
                    />
                  </View>

                  {/* East */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>EAST BOUNDARY</Text>
                    <TextInput
                      style={styles.input}
                      value={eastBoundary}
                      onChangeText={setEastBoundary}
                      placeholder="East adjacent owner / Road / Lot No."
                    />
                    <TextInput
                      style={[styles.input, { marginTop: 4 }]}
                      value={eRemarks}
                      onChangeText={setERemarks}
                      placeholder="East boundary notes..."
                    />
                  </View>

                  {/* West */}
                  <View style={styles.boundarySection}>
                    <Text style={styles.boundaryTitle}>WEST BOUNDARY</Text>
                    <TextInput
                      style={styles.input}
                      value={westBoundary}
                      onChangeText={setWestBoundary}
                      placeholder="West adjacent owner / Creek / Lot No."
                    />
                    <TextInput
                      style={[styles.input, { marginTop: 4 }]}
                      value={wRemarks}
                      onChangeText={setWRemarks}
                      placeholder="West boundary notes..."
                    />
                  </View>
                </View>
              </View>
            )}

            {/* TAB 2: APPRAISAL & VALUATION */}
            {activeTab === 2 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Classification & Dimensions</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Classification</Text>
                      <TextInput
                        style={styles.input}
                        value={classification}
                        onChangeText={setClassification}
                        placeholder="e.g. Residential / Commercial"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Actual Use</Text>
                      <TextInput
                        style={styles.input}
                        value={actualUse}
                        onChangeText={setActualUse}
                        placeholder="e.g. Residential"
                      />
                    </View>
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>Land Area</Text>
                      <TextInput
                        style={styles.input}
                        value={area}
                        onChangeText={setArea}
                        keyboardType="numeric"
                        placeholder="Area"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Unit</Text>
                      <TextInput
                        style={styles.input}
                        value={unit}
                        onChangeText={setUnit}
                        autoCapitalize="characters"
                        placeholder="SQM / HA"
                      />
                    </View>
                  </View>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Base Market Value / Rate (PHP per Unit)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={baseMarketValue}
                      onChangeText={setBaseMarketValue}
                      keyboardType="numeric"
                      placeholder="Base unit rate"
                    />
                  </View>
                </View>

                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Market Value & Assessed Value</Text>

                  <View style={styles.inputGroup}>
                    <Text style={styles.inputLabel}>Total Market Value (PHP)</Text>
                    <TextInput
                      style={[styles.input, styles.valuationInput]}
                      value={marketValue}
                      onChangeText={setMarketValue}
                      keyboardType="numeric"
                    />
                  </View>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Assessment Level (%)</Text>
                      <TextInput
                        style={styles.input}
                        value={assessmentLevel}
                        onChangeText={setAssessmentLevel}
                        keyboardType="numeric"
                        placeholder="e.g. 20"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 2 }]}>
                      <Text style={styles.inputLabel}>Total Assessed Value (PHP)</Text>
                      <TextInput
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
                      trackColor={{ false: '#cbd5e1', true: COLORS.primary }}
                    />
                  </View>
                </View>
              </View>
            )}

            {/* TAB 3: EFFECTIVITY, MEMORANDA & WORKFLOW */}
            {activeTab === 3 && (
              <View style={styles.tabContentBlock}>
                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Tax Effectivity & Memoranda</Text>

                  <View style={styles.rowInputs}>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Year Effectivity</Text>
                      <TextInput
                        style={styles.input}
                        value={yearEffectivity}
                        onChangeText={setYearEffectivity}
                        keyboardType="numeric"
                      />
                    </View>
                    <View style={[styles.inputGroup, { flex: 1 }]}>
                      <Text style={styles.inputLabel}>Quarter Effectivity (1-4)</Text>
                      <TextInput
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
                      style={[styles.input, { height: 60, textAlignVertical: 'top' }]}
                      value={remarks}
                      onChangeText={setRemarks}
                      multiline
                      placeholder="Field inspection observations..."
                    />
                  </View>
                </View>

                <View style={[styles.card, SHADOWS.card]}>
                  <Text style={styles.cardHeaderTitle}>Office Workflow Transfer</Text>

                  <View style={styles.switchRow}>
                    <View style={{ flex: 1 }}>
                      <Text style={styles.switchTitle}>Transfer to Next Step on Sync</Text>
                      <Text style={styles.switchSubtitle}>Automatically forwards workflow after office sync</Text>
                    </View>
                    <Switch
                      value={autoForward}
                      onValueChange={setAutoForward}
                      trackColor={{ false: '#cbd5e1', true: COLORS.primary }}
                    />
                  </View>

                  {autoForward && (
                    <View style={styles.nextStepBox}>
                      <Text style={styles.nextStepLabel}>Target Next Stage:</Text>
                      <View style={styles.nextStepBadge}>
                        <Ionicons name="checkmark-circle-outline" size={16} color={COLORS.primary} />
                        <Text style={styles.nextStepBadgeText}>Inspection Checked / Recommend Approval</Text>
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
                    <Text style={styles.saveBtnText}>Save Appraisal & Stage Transfer</Text>
                  </>
                )}
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.saveBtn, styles.saveBtnSecondary]}
                onPress={() => handleSave(true)}
              >
                <Ionicons name="save-outline" size={18} color={COLORS.primary} />
                <Text style={styles.saveBtnSecondaryText}>Save to Offline Queue Only</Text>
              </TouchableOpacity>
            </View>
          </>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.bgLight
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
    backgroundColor: '#dcfce7',
    borderBottomColor: '#bbf7d0'
  },
  ribbonStandard: {
    backgroundColor: '#e0f2fe',
    borderBottomColor: '#bae6fd'
  },
  ribbonText: {
    fontSize: 12,
    fontWeight: '800',
    color: COLORS.primaryDark
  },
  sectionTabs: {
    flexDirection: 'row',
    backgroundColor: '#ffffff',
    borderBottomWidth: 1,
    borderBottomColor: COLORS.border,
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
    backgroundColor: COLORS.primaryLight
  },
  sectionTabText: {
    fontSize: 11,
    fontWeight: '700',
    color: COLORS.textMuted
  },
  sectionTabTextActive: {
    color: COLORS.primaryDark,
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
    backgroundColor: '#ffffff',
    borderRadius: 16,
    padding: 16,
    borderWidth: 1,
    borderColor: COLORS.border,
    gap: 12
  },
  cardHeaderTitle: {
    fontSize: 14,
    fontWeight: '800',
    color: COLORS.primaryDark,
    borderBottomWidth: 1,
    borderBottomColor: '#f1f5f9',
    paddingBottom: 8
  },
  boundarySection: {
    backgroundColor: '#f8fafc',
    padding: 10,
    borderRadius: 10,
    gap: 4
  },
  boundaryTitle: {
    fontSize: 11,
    fontWeight: '800',
    color: COLORS.primaryDark,
    letterSpacing: 0.5
  },
  inputGroup: {
    gap: 4
  },
  inputLabel: {
    fontSize: 12,
    fontWeight: '700',
    color: COLORS.textDark
  },
  input: {
    borderWidth: 1,
    borderColor: COLORS.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 13,
    color: COLORS.textDark,
    backgroundColor: '#f8fafc'
  },
  valuationInput: {
    fontWeight: '800',
    color: COLORS.primaryDark,
    backgroundColor: '#f0fdf4'
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
    color: COLORS.textDark
  },
  switchSubtitle: {
    fontSize: 11,
    color: COLORS.textMuted,
    marginTop: 2
  },
  nextStepBox: {
    backgroundColor: '#f8fafc',
    padding: 10,
    borderRadius: 10,
    gap: 4
  },
  nextStepLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: COLORS.textMuted
  },
  nextStepBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  nextStepBadgeText: {
    fontSize: 12,
    fontWeight: '800',
    color: COLORS.primary
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
    backgroundColor: COLORS.primary
  },
  saveBtnText: {
    color: '#ffffff',
    fontSize: 14,
    fontWeight: '800'
  },
  saveBtnSecondary: {
    backgroundColor: '#ffffff',
    borderWidth: 1,
    borderColor: COLORS.primary
  },
  saveBtnSecondaryText: {
    color: COLORS.primary,
    fontSize: 13,
    fontWeight: '800'
  },
  notDownloadedBox: {
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#ffffff',
    borderRadius: 18,
    padding: 24,
    marginTop: 20,
    borderWidth: 1,
    borderColor: COLORS.border
  },
  notDownloadedTitle: {
    fontSize: 17,
    fontWeight: '800',
    color: COLORS.textDark,
    marginTop: 12,
    marginBottom: 8
  },
  notDownloadedText: {
    fontSize: 13,
    color: COLORS.textMuted,
    textAlign: 'center',
    lineHeight: 20,
    marginBottom: 20
  },
  backBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: COLORS.primary,
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
