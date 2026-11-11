// Official LGU Barangays (General Santos City, Koronadal City & Regional Centers)
export const BARANGAY_OPTIONS = [
  // General Santos City
  'Brgy. Central',
  'Brgy. Dadiangas East',
  'Brgy. Dadiangas North',
  'Brgy. Dadiangas South',
  'Brgy. Dadiangas West',
  'Brgy. Lagao',
  'Brgy. San Isidro',
  'Brgy. Calumpang',
  'Brgy. City Heights',
  'Brgy. Labangal',
  'Brgy. Apopong',
  'Brgy. Baluan',
  'Brgy. Batomelong',
  'Brgy. Buayan',
  'Brgy. Bula',
  'Brgy. Conel',
  'Brgy. Fatima',
  'Brgy. Katangawan',
  'Brgy. Ligaya',
  'Brgy. Mabuhay',
  'Brgy. San Jose',
  'Brgy. Sinawal',
  'Brgy. Tambler',
  'Brgy. Tinagacan',
  'Brgy. Upper Labay',
  'Brgy. Olympog',

  // Koronadal City
  'Brgy. Zone I (Poblacion)',
  'Brgy. Zone II (Poblacion)',
  'Brgy. Zone III (Poblacion)',
  'Brgy. Zone IV (Poblacion)',
  'Brgy. Assumption',
  'Brgy. Avanceña',
  'Brgy. Cacub',
  'Brgy. Caloocan',
  'Brgy. Carpenter Hill',
  'Brgy. Concepcion',
  'Brgy. Esperanza',
  'Brgy. Gen. P. Santos',
  'Brgy. Mabini',
  'Brgy. Magsaysay',
  'Brgy. Mambucal',
  'Brgy. Morales',
  'Brgy. Namnama',
  'Brgy. New Pangasinan',
  'Brgy. Paraiso',
  'Brgy. Rotonda',
  'Brgy. San Roque',
  'Brgy. Saravia',
  'Brgy. Sta. Cruz',
  'Brgy. Sto. Niño',
  'Brgy. Topland'
];

export const normalizeBarangay = (raw) => {
  if (!raw || typeof raw !== 'string') return '';
  const clean = raw.trim();
  const direct = BARANGAY_OPTIONS.find(b => b.toLowerCase() === clean.toLowerCase());
  if (direct) return direct;

  // Partial match without "Brgy." or "Barangay"
  const stripped = clean.replace(/^(brgy\.?|barangay)\s*/i, '').trim().toLowerCase();
  const partial = BARANGAY_OPTIONS.find(b => {
    const bStripped = b.replace(/^(brgy\.?|barangay)\s*/i, '').trim().toLowerCase();
    return bStripped === stripped || bStripped.includes(stripped) || stripped.includes(bStripped);
  });
  return partial || clean;
};

