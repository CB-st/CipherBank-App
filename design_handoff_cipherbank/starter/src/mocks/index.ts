export { mockRequest, MockApiError } from './handlers';
export { mockLatency } from './latency';
export { connectMockStream, disconnectMockStream, scheduleSettlement, isMockStreamConnected } from './stream';

export const useMock = () => process.env.EXPO_PUBLIC_USE_MOCK === 'true';
