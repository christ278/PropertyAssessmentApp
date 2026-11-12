// Official LGU Barangays (City of Koronadal, South Cotabato)
// Exactly aligned with Web Portal (OfficialKoronadalBarangays)
export const BARANGAY_OPTIONS = [
  'BRGY. ASSUMPTION',
  'BRGY. AVANCENA',
  'BRGY. CACUB',
  'BRGY. CALOOCAN',
  'BRGY. CARPENTER HILL',
  'BRGY. CONCEPCION',
  'BRGY. ESPERANZA',
  'BRGY. GEN. P. SANTOS',
  'BRGY. MABINI',
  'BRGY. MAGSAYSAY',
  'BRGY. MAMBUCAL',
  'BRGY. MORALES',
  'BRGY. NAMNAMA',
  'BRGY. NEW PANGASINAN',
  'BRGY. PARAISO',
  'BRGY. ROTONDA',
  'BRGY. SAN ISIDRO',
  'BRGY. SAN JOSE',
  'BRGY. SAN ROQUE',
  'BRGY. SARAVIA',
  'BRGY. STA. CRUZ',
  'BRGY. STO. NIÑO',
  'BRGY. TOPLAND',
  'BRGY. ZONE I',
  'BRGY. ZONE II',
  'BRGY. ZONE III',
  'BRGY. ZONE IV'
];

export const normalizeBarangay = (raw) => {
  if (!raw || typeof raw !== 'string') return '';
  const clean = raw.trim();
  const direct = BARANGAY_OPTIONS.find(b => b.toLowerCase() === clean.toLowerCase());
  if (direct) return direct;

  const strip = (s) =>
    s
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/^(brgy\.?|barangay)\s*/i, '')
      .replace(/\s*\(poblacion\)\s*/i, '')
      .replace(/[^a-zA-Z0-9]/g, '')
      .trim()
      .toLowerCase();

  const stripped = strip(clean);

  // Map Roman numerals vs Arabic numerals for Zone 1-4 (e.g., Zone 1 -> Zone I)
  const zoneMap = {
    'zone1': 'zonei',
    'zone2': 'zoneii',
    'zone3': 'zoneiii',
    'zone4': 'zoneiv'
  };
  const normalizedStripped = zoneMap[stripped] || stripped;

  const match = BARANGAY_OPTIONS.find(b => {
    const bStripped = strip(b);
    return bStripped === normalizedStripped || bStripped.includes(normalizedStripped) || normalizedStripped.includes(bStripped);
  });

  return match || clean;
};
