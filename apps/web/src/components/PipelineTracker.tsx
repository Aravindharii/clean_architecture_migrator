import React from 'react'
import { CheckCircle2, Circle, AlertCircle, Clock, XCircle } from 'lucide-react'
import type { JobStatus } from '../types'

interface PipelineTrackerProps {
  status: JobStatus
  progressPercent: number
  currentStep?: string
  errorMessage?: string
  onCancel?: () => void
}

const PIPELINE_STEPS: { id: JobStatus; label: string }[] = [
  { id: 'Uploaded', label: 'Uploaded' },
  { id: 'Extracted', label: 'Extracted' },
  { id: 'Analyzing', label: 'Analyzing' },
  { id: 'AnalysisReady', label: 'Analysis Ready' },
  { id: 'Planning', label: 'AI Planning' },
  { id: 'AwaitingApproval', label: 'Review & Approval' },
  { id: 'Approved', label: 'Approved' },
  { id: 'Transforming', label: 'Transforming' },
  { id: 'Validating', label: 'Validating' },
  { id: 'Completed', label: 'Completed' },
]

export const PipelineTracker: React.FC<PipelineTrackerProps> = ({
  status,
  progressPercent,
  currentStep,
  errorMessage,
  onCancel
}) => {
  const currentIndex = PIPELINE_STEPS.findIndex((s) => s.id === status)

  return (
    <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-6 shadow-xl backdrop-blur-md mb-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between pb-4 border-b border-slate-800 gap-4">
        <div>
          <div className="flex items-center space-x-3">
            <span className="text-sm font-semibold uppercase tracking-wider text-sky-400">Pipeline State Machine</span>
            <span className="text-xs px-2.5 py-0.5 rounded-full font-mono bg-slate-800 text-slate-300">
              {progressPercent}% Complete
            </span>
          </div>
          <p className="text-sm text-slate-300 mt-1">
            {currentStep || 'Job is processing through isolated verification stages...'}
          </p>
        </div>

        {status !== 'Completed' && status !== 'Failed' && status !== 'Cancelled' && onCancel && (
          <button
            onClick={onCancel}
            className="self-start sm:self-auto flex items-center space-x-1.5 px-3 py-1.5 text-xs font-medium text-rose-300 bg-rose-500/10 border border-rose-500/30 rounded-lg hover:bg-rose-500/20 transition-all"
          >
            <XCircle className="w-4 h-4" />
            <span>Cancel Job</span>
          </button>
        )}
      </div>

      {/* Progress Bar */}
      <div className="w-full bg-slate-800 h-2 rounded-full mt-5 overflow-hidden">
        <div
          className={`h-full transition-all duration-500 ease-out ${
            status === 'Failed'
              ? 'bg-rose-500'
              : status === 'Completed'
              ? 'bg-gradient-to-r from-sky-400 via-indigo-400 to-emerald-400'
              : 'bg-gradient-to-r from-sky-400 to-indigo-500 animate-pulse'
          }`}
          style={{ width: `${Math.max(5, progressPercent)}%` }}
        />
      </div>

      {/* Steps visualization */}
      <div className="grid grid-cols-2 sm:grid-cols-5 lg:grid-cols-10 gap-2 mt-6">
        {PIPELINE_STEPS.map((step, idx) => {
          const isDone = currentIndex > idx || status === 'Completed'
          const isCurrent = step.id === status
          const isFailed = status === 'Failed' && isCurrent

          return (
            <div
              key={step.id}
              className={`flex flex-col items-center p-2 rounded-xl border text-center transition-all ${
                isCurrent
                  ? isFailed
                    ? 'bg-rose-500/10 border-rose-500/50 text-rose-300'
                    : 'bg-sky-500/10 border-sky-500/50 text-sky-300 shadow-md shadow-sky-500/10'
                  : isDone
                  ? 'bg-emerald-500/5 border-emerald-500/20 text-emerald-400'
                  : 'bg-slate-950/40 border-slate-800/80 text-slate-500'
              }`}
            >
              <div className="mb-1.5">
                {isFailed ? (
                  <AlertCircle className="w-5 h-5 text-rose-400 animate-bounce" />
                ) : isCurrent ? (
                  <Clock className="w-5 h-5 text-sky-400 animate-spin" />
                ) : isDone ? (
                  <CheckCircle2 className="w-5 h-5 text-emerald-400" />
                ) : (
                  <Circle className="w-5 h-5 text-slate-600" />
                )}
              </div>
              <span className="text-[11px] font-medium leading-tight">{step.label}</span>
            </div>
          )
        })}
      </div>

      {errorMessage && (
        <div className="mt-4 p-3 bg-rose-500/10 border border-rose-500/30 rounded-xl flex items-center space-x-3 text-rose-300 text-sm">
          <AlertCircle className="w-5 h-5 text-rose-400 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}
    </div>
  )
}
