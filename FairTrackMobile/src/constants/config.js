/**
 * Centralized API & Server Configuration
 * Single source of truth for all API base URLs and environment endpoints.
 */

export const API_CONFIG = {
  // Primary default base URL for FairTrack Mobile
  BASE_URL: 'https://fairtrack.sinsilyopos.com',

  // Recognized domain for validation checks
  DOMAIN: 'fairtrack.sinsilyopos.com',

  // Local development port
  DEFAULT_PORT: 5156,

  // Quick preset options for the Server Settings modal
  PRESET_URLS: [
    {
      id: 'cloud',
      label: 'Production Cloud (fairtrack.sinsilyopos.com)',
      url: 'https://fairtrack.sinsilyopos.com'
    },
    {
      id: 'lan',
      label: 'LAN Wi-Fi (192.168.1.243:5156)',
      url: 'http://192.168.1.243:5156'
    },
    {
      id: 'emulator',
      label: 'Android Emulator (10.0.2.2:5156)',
      url: 'http://10.0.2.2:5156'
    }
  ]
};

export const DEFAULT_BASE_URL = API_CONFIG.BASE_URL;
