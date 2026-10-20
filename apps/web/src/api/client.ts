import type {
  ProjectResponse,
  JobStatusResponse,
  ProjectAnalysisResult,
  MigrationPlan,
  ApprovalRecord,
  ValidationReport,
  ArtifactMetadata
} from '../types'

const BASE_URL = '/api'

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let errorMessage = `HTTP error ${res.status}`
    try {
      const err = await res.json()
      if (err.error) errorMessage = err.error
    } catch {
      // fallback
    }
    throw new Error(errorMessage)
  }
  return res.json()
}

export const api = {
  async getProjects(): Promise<ProjectResponse[]> {
    const res = await fetch(`${BASE_URL}/projects`)
    return handleResponse<ProjectResponse[]>(res)
  },

  async getProject(id: string): Promise<ProjectResponse> {
    const res = await fetch(`${BASE_URL}/projects/${id}`)
    return handleResponse<ProjectResponse>(res)
  },

  async createProject(name: string, description?: string): Promise<ProjectResponse> {
    const res = await fetch(`${BASE_URL}/projects`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, description })
    })
    return handleResponse<ProjectResponse>(res)
  },

  async uploadArchive(projectId: string, file: File): Promise<{ jobId: string; sourceHash: string; status: string }> {
    const formData = new FormData()
    formData.append('file', file)

    const res = await fetch(`${BASE_URL}/projects/${projectId}/uploads`, {
      method: 'POST',
      body: formData
    })
    return handleResponse(res)
  },

  async getJobStatus(jobId: string): Promise<JobStatusResponse> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}`)
    return handleResponse<JobStatusResponse>(res)
  },

  async getAnalysis(jobId: string): Promise<ProjectAnalysisResult> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/analysis`)
    return handleResponse<ProjectAnalysisResult>(res)
  },

  async regeneratePlan(jobId: string, customInstructions?: string): Promise<{ message: string }> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/plans`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ customInstructions })
    })
    return handleResponse(res)
  },

  async getPlan(jobId: string, planId: string): Promise<MigrationPlan> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/plans/${planId}`)
    return handleResponse<MigrationPlan>(res)
  },

  async updatePlan(jobId: string, planId: string, payload: Partial<MigrationPlan>): Promise<MigrationPlan> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/plans/${planId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    return handleResponse<MigrationPlan>(res)
  },

  async approvePlan(jobId: string, planId: string, payload: { sourceHash: string; planVersion: number; approvedBy: string; notes: string }): Promise<ApprovalRecord> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/plans/${planId}/approve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    return handleResponse<ApprovalRecord>(res)
  },

  async applyTransformations(jobId: string): Promise<{ message: string }> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/transformations`, {
      method: 'POST'
    })
    return handleResponse(res)
  },

  async getValidation(jobId: string): Promise<ValidationReport> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/validation`)
    return handleResponse<ValidationReport>(res)
  },

  async getArtifacts(jobId: string): Promise<ArtifactMetadata[]> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/artifacts`)
    return handleResponse<ArtifactMetadata[]>(res)
  },

  async cancelJob(jobId: string): Promise<{ message: string }> {
    const res = await fetch(`${BASE_URL}/jobs/${jobId}/cancel`, {
      method: 'POST'
    })
    return handleResponse(res)
  }
}
