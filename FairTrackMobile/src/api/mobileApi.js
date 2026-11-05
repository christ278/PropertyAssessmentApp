import apiClient from './apiClient';

export const MobileApi = {
  // Authentication
  login: async (username, password) => {
    const response = await apiClient.post('/api/auth/login', { username, password });
    return response.data;
  },

  logout: async () => {
    try {
      const response = await apiClient.post('/api/auth/logout');
      return response.data;
    } catch (e) {
      // Offline fallback
      return { success: true };
    }
  },

  ping: async () => {
    const response = await apiClient.get('/api/auth/ping');
    return response.data;
  },

  getProfile: async () => {
    const response = await apiClient.get('/api/auth/profile');
    return response.data;
  },

  // Dashboard Overview
  getDashboard: async () => {
    const response = await apiClient.get('/api/mobile/dashboard');
    return response.data;
  },

  // Transactions Feed (My Transactions)
  getTransactions: async (propertyTypeId = null, trackingNo = '', page = 1, pageSize = 50) => {
    const response = await apiClient.get('/api/mobile/transactions', {
      params: { propertyTypeId, trackingNo, page, pageSize }
    });
    return response.data;
  },

  // Download Full Form Assessment Details for Field Inspection
  downloadTransactionDetails: async (formWorkflowId, trackingNo = '') => {
    const response = await apiClient.get('/api/mobile/transaction/download-details', {
      params: { formWorkflowId, trackingNo }
    });
    return response.data;
  },

  // Fetch Form Details (Land, Building, Machinery)
  getBuildingForm: async (tdnId, castTdnBuildingId = null) => {
    const response = await apiClient.get('/api/mobile/form/building', {
      params: { tdnId, castTdnBuildingId }
    });
    return response.data;
  },

  getLandForm: async (tdnId, tdnLandId = null) => {
    const response = await apiClient.get('/api/mobile/form/land', {
      params: { tdnId, tdnLandId }
    });
    return response.data;
  },

  getMachineryForm: async (tdnId, tdnMachineryId = null) => {
    const response = await apiClient.get('/api/mobile/form/machinery', {
      params: { tdnId, tdnMachineryId }
    });
    return response.data;
  },

  // Update Form Details
  updateBuildingForm: async (dto) => {
    const response = await apiClient.post('/api/mobile/form/update-building', dto);
    return response.data;
  },

  updateLandForm: async (dto) => {
    const response = await apiClient.post('/api/mobile/form/update-land', dto);
    return response.data;
  },

  updateMachineryForm: async (dto) => {
    const response = await apiClient.post('/api/mobile/form/update-machinery', dto);
    return response.data;
  },

  // Advance Workflow
  forwardWorkflow: async (formWorkflowId, nextStatusId, remarks = '') => {
    const response = await apiClient.post('/api/mobile/workflow/forward', {
      formWorkflowId,
      nextStatusId,
      remarks
    });
    return response.data;
  }
};
