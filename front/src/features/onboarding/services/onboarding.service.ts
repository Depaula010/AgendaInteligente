import { api } from '@/shared/lib/axios'
import type { CreateServiceRequest, OnboardRequest, OnboardResponse } from '../types/onboarding.types'

export const onboardingService = {
  async onboard(data: OnboardRequest): Promise<OnboardResponse> {
    const res = await api.post<OnboardResponse>('/onboarding', data)
    return res.data
  },

  async createService(data: CreateServiceRequest): Promise<void> {
    await api.post('/services', data)
  },
}
