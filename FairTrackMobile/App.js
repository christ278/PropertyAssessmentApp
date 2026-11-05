import React, { useContext } from 'react';
import { View, ActivityIndicator } from 'react-native';
import { NavigationContainer, DefaultTheme, DarkTheme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { StatusBar } from 'expo-status-bar';
import { Ionicons } from '@expo/vector-icons';
import { SafeAreaProvider, useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemeProvider, useTheme } from './src/context/ThemeContext';
import { AuthProvider, AuthContext } from './src/context/AuthContext';
import { SyncProvider, SyncContext } from './src/context/SyncContext';

import { LoginScreen } from './src/screens/LoginScreen';
import { DashboardScreen } from './src/screens/DashboardScreen';
import { TransactionsScreen } from './src/screens/TransactionsScreen';
import { BuildingFormScreen } from './src/screens/BuildingFormScreen';
import { LandFormScreen } from './src/screens/LandFormScreen';
import { MachineryFormScreen } from './src/screens/MachineryFormScreen';
import { SyncQueueScreen } from './src/screens/SyncQueueScreen';
import { ProfileScreen } from './src/screens/ProfileScreen';

const Stack = createNativeStackNavigator();
const Tab = createBottomTabNavigator();

const MainTabs = () => {
  const { pendingCount } = useContext(SyncContext);
  const { colors, isDarkMode } = useTheme();
  const insets = useSafeAreaInsets();

  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textMuted,
        tabBarStyle: {
          backgroundColor: colors.cardBg,
          borderTopWidth: 1,
          borderTopColor: colors.border,
          height: 60 + (insets.bottom > 0 ? insets.bottom - 4 : 0),
          paddingBottom: insets.bottom > 0 ? insets.bottom : 8,
          paddingTop: 6
        },
        tabBarLabelStyle: {
          fontSize: 11,
          fontWeight: '700'
        },
        tabBarIcon: ({ focused, color, size }) => {
          let iconName = 'home';
          if (route.name === 'Home') {
            iconName = focused ? 'home' : 'home-outline';
          } else if (route.name === 'Feed') {
            iconName = focused ? 'list' : 'list-outline';
          } else if (route.name === 'SyncCenter') {
            iconName = focused ? 'cloud-upload' : 'cloud-upload-outline';
          } else if (route.name === 'ProfileTab') {
            iconName = focused ? 'person' : 'person-outline';
          }
          return <Ionicons name={iconName} size={22} color={color} />;
        }
      })}
    >
      <Tab.Screen name="Home" component={DashboardScreen} options={{ tabBarLabel: 'Dashboard' }} />
      <Tab.Screen name="Feed" component={TransactionsScreen} options={{ tabBarLabel: 'Transactions' }} />
      <Tab.Screen
        name="SyncCenter"
        component={SyncQueueScreen}
        options={{
          tabBarLabel: 'Office Sync',
          tabBarBadge: pendingCount > 0 ? pendingCount : null
        }}
      />
      <Tab.Screen
        name="ProfileTab"
        component={ProfileScreen}
        options={{ tabBarLabel: 'Profile' }}
      />
    </Tab.Navigator>
  );
};

const RootNavigator = () => {
  const { isAuthenticated, isLoading } = useContext(AuthContext);
  const { colors } = useTheme();

  if (isLoading) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.bgLight }}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      {!isAuthenticated ? (
        <Stack.Screen name="Login" component={LoginScreen} />
      ) : (
        <>
          <Stack.Screen name="Main" component={MainTabs} />
          <Stack.Screen name="Profile" component={ProfileScreen} />
          <Stack.Screen name="Transactions" component={TransactionsScreen} />
          <Stack.Screen name="BuildingForm" component={BuildingFormScreen} />
          <Stack.Screen name="LandForm" component={LandFormScreen} />
          <Stack.Screen name="MachineryForm" component={MachineryFormScreen} />
          <Stack.Screen name="SyncQueue" component={SyncQueueScreen} />
        </>
      )}
    </Stack.Navigator>
  );
};

const ThemedApp = () => {
  const { isDarkMode, colors } = useTheme();

  const navigationTheme = isDarkMode
    ? {
        ...DarkTheme,
        colors: {
          ...DarkTheme.colors,
          primary: colors.primary,
          background: colors.bgLight,
          card: colors.cardBg,
          text: colors.textDark,
          border: colors.border
        }
      }
    : {
        ...DefaultTheme,
        colors: {
          ...DefaultTheme.colors,
          primary: colors.primary,
          background: colors.bgLight,
          card: colors.cardBg,
          text: colors.textDark,
          border: colors.border
        }
      };

  return (
    <NavigationContainer theme={navigationTheme}>
      <StatusBar
        style={isDarkMode ? 'light' : 'dark'}
        backgroundColor={colors.cardBg}
      />
      <RootNavigator />
    </NavigationContainer>
  );
};

export default function App() {
  return (
    <SafeAreaProvider>
      <ThemeProvider>
        <AuthProvider>
          <SyncProvider>
            <ThemedApp />
          </SyncProvider>
        </AuthProvider>
      </ThemeProvider>
    </SafeAreaProvider>
  );
}

