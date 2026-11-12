// General Santos City / CAST Standard Land Schedules & Valuation Constants
// Used for offline-first calculation and auto-lookup in Land Appraisal Form Tab 4

export const LAND_CLASSIFICATIONS = [
  { id: 1, code: 'RES', name: 'RESIDENTIAL' },
  { id: 4, code: 'COM', name: 'COMMERCIAL' },
  { id: 6, code: 'IND', name: 'INDUSTRIAL' },
  { id: 48, code: 'AGR', name: 'AGRICULTURAL' },
  { id: 10, code: 'CUL', name: 'SPECIAL - CULTURAL' },
  { id: 11, code: 'HOS', name: 'SPECIAL - HOSPITAL' },
  { id: 12, code: 'SCI', name: 'SPECIAL - SCIENTIFIC' },
  { id: 49, code: 'MIN', name: 'MINERAL' },
  { id: 50, code: 'TIM', name: 'TIMBERLAND' }
];

export const LAND_SUB_CLASSIFICATIONS = [
  // Residential (General Santos / CAST Schedule of Unit Values per sq.m.)
  { id: 1, classificationId: 1, subClass: '1st Class Residential', unitValue: 520.00 },
  { id: 2, classificationId: 1, subClass: '2nd Class Residential', unitValue: 468.00 },
  { id: 3, classificationId: 1, subClass: '3rd Class Residential', unitValue: 410.80 },
  { id: 4, classificationId: 1, subClass: '4th Class Residential', unitValue: 351.00 },
  { id: 5, classificationId: 1, subClass: '5th Class Residential', unitValue: 286.00 },
  { id: 6, classificationId: 1, subClass: '6th Class Residential', unitValue: 234.00 },
  { id: 7, classificationId: 1, subClass: '7th Class Residential', unitValue: 163.80 },
  { id: 8, classificationId: 1, subClass: '8th Class Residential', unitValue: 117.00 },

  // Commercial
  { id: 9, classificationId: 4, subClass: '1st Class Commercial', unitValue: 3120.00 },
  { id: 10, classificationId: 4, subClass: '2nd Class Commercial', unitValue: 2522.00 },
  { id: 11, classificationId: 4, subClass: '3rd Class Commercial', unitValue: 1924.00 },
  { id: 12, classificationId: 4, subClass: '4th Class Commercial', unitValue: 1378.00 },
  { id: 13, classificationId: 4, subClass: '5th Class Commercial', unitValue: 1222.00 },
  { id: 14, classificationId: 4, subClass: '6th Class Commercial', unitValue: 988.00 },

  // Industrial
  { id: 15, classificationId: 6, subClass: '1st Class Industrial', unitValue: 910.00 },
  { id: 16, classificationId: 6, subClass: '2nd Class Industrial', unitValue: 806.00 },
  { id: 17, classificationId: 6, subClass: '3rd Class Industrial', unitValue: 676.00 },
  { id: 18, classificationId: 6, subClass: '4th Class Industrial', unitValue: 546.00 },
  { id: 19, classificationId: 6, subClass: '5th Class Industrial', unitValue: 416.00 },

  // Agricultural
  { id: 20, classificationId: 48, subClass: 'Riceland Irrigated (1st Class)', unitValue: 150000.00 },
  { id: 21, classificationId: 48, subClass: 'Riceland Unirrigated (2nd Class)', unitValue: 130000.00 },
  { id: 22, classificationId: 48, subClass: 'Cornland (1st Class)', unitValue: 110000.00 },
  { id: 23, classificationId: 48, subClass: 'Coconut Land', unitValue: 100000.00 },
  { id: 24, classificationId: 48, subClass: 'Banana Plantation', unitValue: 120000.00 },
  { id: 25, classificationId: 48, subClass: 'Fishpond / Aquaculture', unitValue: 160000.00 }
];

export const LAND_ACTUAL_USES = [
  'RESIDENTIAL',
  'AGRICULTURAL',
  'COMMERCIAL',
  'INDUSTRIAL',
  'MINERAL',
  'SPECIAL - CULTURAL',
  'SPECIAL - HOSPITAL',
  'SPECIAL - SCIENTIFIC',
  'GOVERNMENT',
  'RELIGIOUS',
  'CHARITABLE',
  'EDUCATIONAL'
];

export const LAND_ASSESSMENT_LEVELS = {
  'RESIDENTIAL': 20.00,
  'AGRICULTURAL': 40.00,
  'COMMERCIAL': 50.00,
  'INDUSTRIAL': 50.00,
  'MINERAL': 50.00,
  'SPECIAL - CULTURAL': 10.00,
  'SPECIAL - HOSPITAL': 10.00,
  'SPECIAL - SCIENTIFIC': 10.00,
  'GOVERNMENT': 0.00,
  'RELIGIOUS': 0.00,
  'CHARITABLE': 0.00,
  'EDUCATIONAL': 0.00
};

export const LAND_ADJUSTMENT_FACTORS = [
  { id: 1, description: 'Corner Influence (Residential)', percent: 10.00 },
  { id: 2, description: 'Corner Influence (Commercial)', percent: 25.00 },
  { id: 3, description: 'Provincial & National Highway Frontage', percent: 0.00 },
  { id: 4, description: 'All-Weather Gravel Road', percent: -2.00 },
  { id: 5, description: 'Dirt Road', percent: -6.00 },
  { id: 6, description: 'Interior Lot / No Road Outlet', percent: -10.00 },
  { id: 7, description: 'Topography: 16-30 degrees slope', percent: -10.00 },
  { id: 8, description: 'Topography: 31-45 degrees slope', percent: -20.00 },
  { id: 9, description: 'Sunken / Lowland / Subject to Inundation', percent: -30.00 },
  { id: 10, description: 'Cost of Filling / Stripping (20m depth)', percent: -20.00 },
  { id: 11, description: 'Slightly Eroded Land', percent: -10.00 }
];

export const LAND_IMPROVEMENT_TYPES = [
  { id: 1, code: 'CCT', description: 'COCONUT TREE', kind: 'FRUIT BEARING', baseValue: 120.00, assessmentLevel: 40.00 },
  { id: 2, code: 'MGO', description: 'MANGO TREE', kind: 'FRUIT BEARING', baseValue: 350.00, assessmentLevel: 40.00 },
  { id: 3, code: 'BNN', description: 'BANANA CLUMPS', kind: 'FRUIT BEARING', baseValue: 35.00, assessmentLevel: 40.00 },
  { id: 4, code: 'BMO', description: 'BAMBOO CLUMPS', kind: 'PLANT', baseValue: 45.00, assessmentLevel: 40.00 },
  { id: 5, code: 'AVD', description: 'AVOCADO TREE', kind: 'FRUIT BEARING', baseValue: 150.00, assessmentLevel: 40.00 },
  { id: 6, code: 'COFT', description: 'COFFEE TREE', kind: 'FRUIT BEARING', baseValue: 50.00, assessmentLevel: 40.00 },
  { id: 7, code: 'CAT', description: 'CACAO TREE', kind: 'FRUIT BEARING', baseValue: 60.00, assessmentLevel: 40.00 },
  { id: 8, code: 'DB', description: 'DURIAN (BEARING)', kind: 'FRUIT BEARING', baseValue: 400.00, assessmentLevel: 40.00 },
  { id: 9, code: 'GF', description: 'GEMILINA TREE', kind: 'FOREST TREE', baseValue: 180.00, assessmentLevel: 40.00 },
  { id: 10, code: 'MB', description: 'MAHOGANY (BIG)', kind: 'FOREST TREE', baseValue: 250.00, assessmentLevel: 40.00 },
  { id: 11, code: 'NR', description: 'NARRA TREE', kind: 'HARDWOOD', baseValue: 500.00, assessmentLevel: 40.00 },
  { id: 12, code: 'FENCE', description: 'CONCRETE FENCE / PERIMETER WALL', kind: 'IMPROVEMENT', baseValue: 650.00, assessmentLevel: 40.00 },
  { id: 13, code: 'PAVE', description: 'CONCRETE PAVEMENT / DRIVEWAY', kind: 'IMPROVEMENT', baseValue: 250.00, assessmentLevel: 40.00 }
];

export const getAssessmentLevelForActualUse = (actualUse) => {
  if (!actualUse) return 20.00;
  const key = actualUse.trim().toUpperCase();
  return LAND_ASSESSMENT_LEVELS[key] !== undefined ? LAND_ASSESSMENT_LEVELS[key] : 20.00;
};

