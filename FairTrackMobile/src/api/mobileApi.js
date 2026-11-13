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

  ping: async (timeoutMs = 3500) => {
    const response = await apiClient.get('/api/auth/ping', { timeout: timeoutMs });
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
  downloadTransactionDetails: async (formWorkflowId, trackingNo = '', partitionIndex = null) => {
    const params = { formWorkflowId, trackingNo };
    if (partitionIndex !== null && partitionIndex !== undefined) {
      params.partitionIndex = partitionIndex;
    }
    const response = await apiClient.get('/api/mobile/transaction/download-details', {
      params
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
