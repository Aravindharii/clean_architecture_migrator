import { useState, useEffect } from 'react'
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import { Navbar } from './components/Navbar'
import { DashboardPage } from './pages/DashboardPage'
import { UploadPage } from './pages/UploadPage'
import { AnalysisPage } from './pages/AnalysisPage'
import { PlanReviewPage } from './pages/PlanReviewPage'
import { TransformationPage } from './pages/TransformationPage'
import { ValidationPage } from './pages/ValidationPage'
import { ArtifactsPage } from './pages/ArtifactsPage'
import { api } from './api/client'
import type {
  ProjectResponse,
  JobStatusResponse,
  ProjectAnalysisResult,
  MigrationPlan,
  ValidationReport,
  ArtifactMetadata
} from './types'

const queryClient = new QueryClient()

function MigrationPlatformApp() {
  const [currentTab, setCurrentTab] = useState<string>('dashboard')
  const [activeProjectId, setActiveProjectId] = useState<string | null>(null)
  const [activeJobId, setActiveJobId] = useState<string | null>(null)

  // Fetch all projects
  const { data: projects = [], refetch: refetchProjects } = useQuery<ProjectResponse[]>({
    queryKey: ['projects'],
    queryFn: () => api.getProjects(),
  })

  // Poll active job status
  const { data: jobStatus, refetch: refetchJob } = useQuery<JobStatusResponse | null>({
    queryKey: ['job', activeJobId],
    queryFn: () => (activeJobId ? api.getJobStatus(activeJobId) : null),
    enabled: !!activeJobId,
    refetchInterval: (query) => {
      const data = query.state.data
      if (!data) return 1500
      if (data.status === 'Completed' || data.status === 'Failed' || data.status === 'Cancelled') {
        return false // stop polling
      }
      return 1500 // poll every 1.5s
    },
  })

  // Fetch analysis if job is past AnalysisReady
  const { data: analysis } = useQuery<ProjectAnalysisResult | null>({
    queryKey: ['analysis', activeJobId],
    queryFn: () => (activeJobId ? api.getAnalysis(activeJobId) : null),
    enabled:
      !!activeJobId &&
      jobStatus != null &&
      jobStatus.status !== 'Uploaded' &&
      jobStatus.status !== 'Extracted' &&
      jobStatus.status !== 'Analyzing',
  })

  // Fetch plan if available
  const { data: plan, refetch: refetchPlan } = useQuery<MigrationPlan | null>({
    queryKey: ['plan', activeJobId, jobStatus?.planId],
    queryFn: () =>
      activeJobId && jobStatus?.planId ? api.getPlan(activeJobId, jobStatus.planId) : null,
    enabled: !!activeJobId && !!jobStatus?.planId,
  })

  // Fetch validation if completed or validating
  const { data: validation } = useQuery<ValidationReport | null>({
    queryKey: ['validation', activeJobId],
    queryFn: () => (activeJobId ? api.getValidation(activeJobId) : null),
    enabled: !!activeJobId && (jobStatus?.status === 'Validating' || jobStatus?.status === 'Completed'),
  })

  // Fetch artifacts if completed
  const { data: artifacts = [] } = useQuery<ArtifactMetadata[]>({
    queryKey: ['artifacts', activeJobId],
    queryFn: () => (activeJobId ? api.getArtifacts(activeJobId) : []),
    enabled: !!activeJobId && jobStatus?.status === 'Completed',
  })

  // Automatically advance tabs based on state progression
  useEffect(() => {
    if (!jobStatus) return

    if (jobStatus.status === 'AwaitingApproval' && currentTab === 'upload') {
      setCurrentTab('analysis')
    } else if (jobStatus.status === 'Transforming' && currentTab === 'plan') {
      setCurrentTab('transform')
    }
  }, [jobStatus?.status, currentTab])

  const handleSelectProject = (projectId: string, latestJobId?: string) => {
    setActiveProjectId(projectId)
    if (latestJobId) {
      setActiveJobId(latestJobId)
      setCurrentTab('analysis')
    } else {
      setCurrentTab('upload')
    }
  }

  const handleUploadSuccess = (jobId: string, projectId: string) => {
    setActiveJobId(jobId)
    setActiveProjectId(projectId)
    refetchProjects()
    setCurrentTab('analysis')
  }

  const handleExecuteTransformation = async () => {
    if (!activeJobId) return
    try {
      await api.applyTransformations(activeJobId)
      refetchJob()
      setCurrentTab('transform')
    } catch (err: any) {
      alert(`Transformation start failed: ${err.message}`)
    }
  }

  const handleCancelJob = async () => {
    if (!activeJobId) return
    if (confirm('Are you sure you want to cancel this migration job?')) {
      try {
        await api.cancelJob(activeJobId)
        refetchJob()
      } catch (err: any) {
        alert(`Cancel failed: ${err.message}`)
      }
    }
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-sky-500/30 selection:text-sky-200">
      <Navbar
        currentTab={currentTab}
        setCurrentTab={setCurrentTab}
        activeJobId={activeJobId}
        activeJobStatus={jobStatus?.status}
      />

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {currentTab === 'dashboard' && (
          <DashboardPage
            projects={projects}
            onSelectProject={handleSelectProject}
            onNavigateToUpload={() => setCurrentTab('upload')}
            onRefresh={refetchProjects}
          />
        )}

        {currentTab === 'upload' && (
          <UploadPage
            projects={projects}
            activeProjectId={activeProjectId}
            onUploadSuccess={handleUploadSuccess}
          />
        )}

        {currentTab === 'analysis' && (
          <AnalysisPage
            analysis={analysis || null}
            job={jobStatus || null}
            onProceedToPlan={() => setCurrentTab('plan')}
          />
        )}

        {currentTab === 'plan' && (
          <PlanReviewPage
            plan={plan || null}
            job={jobStatus || null}
            onPlanApproved={() => {
              refetchJob()
              refetchPlan()
            }}
            onExecuteTransformation={handleExecuteTransformation}
            onRefreshPlan={refetchPlan}
          />
        )}

        {currentTab === 'transform' && (
          <TransformationPage
            job={jobStatus || null}
            validation={validation || null}
            onProceedToValidation={() => setCurrentTab('validation')}
            onCancelJob={handleCancelJob}
          />
        )}

        {currentTab === 'validation' && (
          <ValidationPage
            validation={validation || null}
            onDownloadArtifacts={() => setCurrentTab('artifacts')}
          />
        )}

        {currentTab === 'artifacts' && (
          <ArtifactsPage
            artifacts={artifacts}
            validation={validation || null}
            job={jobStatus || null}
            onBackToDashboard={() => setCurrentTab('dashboard')}
          />
        )}
      </main>

      <footer className="border-t border-slate-900 bg-slate-950/60 py-6 text-center text-xs text-slate-500">
        <p>
          ArchMorph Clean Architecture Migration Platform • Roslyn & OpenRewrite Deterministic Analysis • v1.0.0
        </p>
      </footer>
    </div>
  )
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MigrationPlatformApp />
    </QueryClientProvider>
  )
}
