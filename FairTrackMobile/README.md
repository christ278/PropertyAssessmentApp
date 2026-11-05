# FAIRTRACK Mobile &bull; Field Assessment & Office Sync Application

React Native (Expo) Android mobile application designed for City Assessor field appraisers and inspectors in Koronadal City.

---

## Key Features

1. **JWT Security Layer**:
   - Secure staff sign-in with Bearer token authentication against `/api/auth/login`.
   - Automatic session persistence via `@react-native-async-storage/async-storage`.

2. **Offline-First Field Assessment**:
   - Work anywhere with 0% cellular/Wi-Fi coverage.
   - Inspect and modify:
     - **Building FAAS Form**: Storey counts, Land Area, Total Floor Area, Market Value, Assessed Value, and Permit details.
     - **Land Appraisal Form**: Parcel area, unit, valuation, and field descriptions.
     - **Machinery FAAS Form**: Acquisition cost, depreciation, and market value.
   - Saves all field edits to a local offline queue.

3. **One-Tap Office Sync Engine**:
   - When returning to the office, tap **"Sync All to Server"** in the **Office Sync Center**.
   - Batches and pushes all local offline modifications to `/api/mobile/form/update-building`, `/api/mobile/form/update-land`, `/api/mobile/form/update-machinery`, and `/api/mobile/workflow/forward`.
   - Displays real-time sync status badges, progress indicators, and audit logs.

4. **Dynamic Server IP Switcher**:
   - Easily configure server endpoints (e.g. `http://10.0.2.2:56018` for Android Emulator or `http://192.168.1.X:56018` for physical Android phones on Wi-Fi).

---

## Project Structure

```
FairTrackMobile/
├── App.js                          # Root Navigator, Safe Area & Global Context Providers
├── app.json                        # Expo Android app configuration & permissions
├── package.json                    # Dependencies (React Native, React Navigation, Axios)
├── src/
│   ├── api/
│   │   ├── apiClient.js            # Axios client with JWT auto-injection
│   │   └── mobileApi.js            # REST API service endpoints
│   ├── context/
│   │   ├── AuthContext.js          # JWT login/logout state manager
│   │   └── SyncContext.js          # Offline queue & batch sync engine
│   ├── screens/
│   │   ├── LoginScreen.js          # Staff sign-in & Server IP configurator
│   │   ├── DashboardScreen.js      # Inspector overview & pending metrics
│   │   ├── TransactionsScreen.js   # Filterable transaction feed & search
│   │   ├── BuildingFormScreen.js   # Building inspection & FAAS editor
│   │   ├── LandFormScreen.js       # Land parcel appraisal editor
│   │   ├── MachineryFormScreen.js  # Machinery & equipment editor
│   │   └── SyncQueueScreen.js      # Office Sync Center with batch sync
│   ├── components/
│   │   ├── HeaderBar.js            # Header with sync badge
│   │   └── StatusBadge.js          # Color-coded workflow stage badges
│   └── constants/
│       └── theme.js                # Official FairTrack color system
```

---

## How to Run on Android

### Prerequisites
- Node.js (v18+)
- Expo CLI or Expo Go on Android device

### Steps:
1. Open terminal in the `FairTrackMobile` folder of this project:
   ```bash
   cd FairTrackMobile
   npm install
   ```

2. Start the development server:
   ```bash
   npx expo start
   ```

3. Run on Android:
   - **On Android Emulator**: Press `a` in the terminal to launch on the emulator.
   - **On Physical Android Phone**: Install **Expo Go** from Google Play Store and scan the QR code displayed in the terminal.
