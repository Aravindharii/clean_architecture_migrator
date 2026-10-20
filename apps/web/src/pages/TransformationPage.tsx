import React from 'react'
import { CheckCircle2, ArrowRight, FileCheck } from 'lucide-react'
import type { JobStatusResponse, ValidationReport } from '../types'
import { PipelineTracker } from '../components/PipelineTracker'

interface TransformationPageProps {
  job: JobStatusResponse | null
  validation: ValidationReport | null
  onProceedToValidation: () => void
  onCancelJob: () => void
}

export const TransformationPage: React.FC<TransformationPageProps> = ({
  job,
  validation,
  onProceedToValidation,
  onCancelJob
}) => {
  if (!job) return null

  const isCompleted = job.status === 'Completed'

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
          Transformation Execution & Sandbox Sandbox
        </h1>
        <p className="text-sm text-slate-400 mt-1">
          Approved operations are applied in an isolated working copy using deterministic AST tools.
        </p>
      </div>

      {/* State Machine Tracker */}
      <PipelineTracker
        status={job.status}
        progressPercent={job.progressPercent}
        currentStep={job.currentStep}
        errorMessage={job.errorMessage}
        onCancel={onCancelJob}
      />

      {/* Execution logs / operation results */}
      {validation && validation.transformationResults.length > 0 && (
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
          <div className="flex items-center justify-between pb-4 border-b border-slate-800 mb-4">
            <h3 className="text-base font-bold text-white flex items-center space-x-2">
              <FileCheck className="w-5 h-5 text-emerald-400" />
              <span>Applied Transformation Operations ({validation.transformationResults.length})</span>
            </h3>
            {isCompleted && (
              <button
                onClick={onProceedToValidation}
                className="flex items-center space-x-1.5 px-4 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold text-xs transition-all shadow-lg shadow-emerald-500/20"
              >
                <span>View Full Validation Report</span>
                <ArrowRight className="w-4 h-4" />
              </button>
            )}
          </div>

          <div className="space-y-2">
            {validation.transformationResults.map((res, i) => (
              <div
                key={i}
                className="p-3 bg-slate-950 rounded-xl border border-slate-800 flex items-center justify-between text-xs"
              >
                <div className="flex items-center space-x-3">
                  <span className={`w-5 h-5 rounded-full flex items-center justify-center ${res.success ? 'text-emerald-400' : 'text-rose-400'}`}>
                    <CheckCircle2 className="w-4 h-4" />
                  </span>
                  <div>
                    <span className="font-semibold text-slate-200">{res.message}</span>
                    {res.affectedPath && (
                      <p className="text-[11px] font-mono text-slate-500 mt-0.5">{res.affectedPath}</p>
                    )}
                  </div>
                </div>

                <span className="text-[11px] font-mono text-slate-500">{res.duration}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Next Step Call to Action */}
      {isCompleted && (
        <div className="p-6 bg-gradient-to-r from-emerald-500/10 to-teal-500/10 border border-emerald-500/30 rounded-3xl flex flex-col sm:flex-row items-center justify-between gap-4">
          <div>
            <h4 className="text-base font-bold text-emerald-400">Transformations Completed & Verified</h4>
            <p className="text-xs text-slate-300 mt-1">
              Compiler build succeeded and architectural boundaries conform to Clean Architecture.
            </p>
          </div>
          <button
            onClick={onProceedToValidation}
            className="px-6 py-3 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-extrabold text-xs shadow-lg shadow-emerald-500/20 transition-all flex items-center space-x-2"
          >
            <span>Inspect Validation & Download</span>
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      )}
    </div>
  )
}
