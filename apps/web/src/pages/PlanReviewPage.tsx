import React, { useState } from 'react'
import { ShieldCheck, RefreshCw, AlertTriangle, Play, Sparkles, Layers, Lock } from 'lucide-react'
import type { MigrationPlan, PlanOperation, JobStatusResponse } from '../types'
import { OperationsList } from '../components/OperationsList'
import { api } from '../api/client'

interface PlanReviewPageProps {
  plan: MigrationPlan | null
  job: JobStatusResponse | null
  onPlanApproved: () => void
  onExecuteTransformation: () => void
  onRefreshPlan: () => void
}

export const PlanReviewPage: React.FC<PlanReviewPageProps> = ({
  plan,
  job,
  onPlanApproved,
  onExecuteTransformation,
  onRefreshPlan
}) => {
  const [operations, setOperations] = useState<PlanOperation[]>(plan?.operations || [])
  const [showApprovalModal, setShowApprovalModal] = useState(false)
  const [approvedByName, setApprovedByName] = useState('Senior Software Architect')
  const [approvalNotes, setApprovalNotes] = useState('Reviewed proposed Clean Architecture boundaries, interfaces, and file relocations.')
  const [isSubmittingApproval, setIsSubmittingApproval] = useState(false)
  const [customPrompt, setCustomPrompt] = useState('')
  const [isRegenerating, setIsRegenerating] = useState(false)

  // Keep local operations in sync with plan
  React.useEffect(() => {
    if (plan?.operations) {
      setOperations(plan.operations)
    }
  }, [plan])

  if (!plan || !job) {
    return (
      <div className="text-center py-24 bg-slate-900 border border-slate-800 rounded-3xl">
        <Sparkles className="w-12 h-12 text-slate-600 mx-auto mb-3 animate-pulse" />
        <h3 className="text-base font-semibold text-slate-300">Generating Migration Plan</h3>
        <p className="text-xs text-slate-500 mt-1">Consulting Clean Architecture rules and synthesizing migration recipe...</p>
      </div>
    )
  }

  const isApproved = job.hasApproval || job.status === 'Approved' || job.status === 'Transforming' || job.status === 'Validating' || job.status === 'Completed'

  const handleToggleApprove = async (operationId: string) => {
    const updated = operations.map((op) =>
      op.operationId === operationId ? { ...op, isApproved: !op.isApproved } : op
    )
    setOperations(updated)
    await api.updatePlan(job.jobId, plan.planId, { operations: updated })
  }

  const handleUpdateOperation = async (operationId: string, diff: Partial<PlanOperation>) => {
    const updated = operations.map((op) =>
      op.operationId === operationId ? { ...op, ...diff } : op
    )
    setOperations(updated)
    await api.updatePlan(job.jobId, plan.planId, { operations: updated })
  }

  const handleRegenerate = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsRegenerating(true)
    try {
      await api.regeneratePlan(job.jobId, customPrompt)
      setCustomPrompt('')
      onRefreshPlan()
    } catch (err: any) {
      alert(`Regeneration failed: ${err.message}`)
    } finally {
      setIsRegenerating(false)
    }
  }

  const handleConfirmApproval = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSubmittingApproval(true)
    try {
      await api.approvePlan(job.jobId, plan.planId, {
        sourceHash: job.sourceHash,
        planVersion: plan.planVersion,
        approvedBy: approvedByName,
        notes: approvalNotes
      })
      setShowApprovalModal(false)
      onPlanApproved()
    } catch (err: any) {
      alert(`Approval error: ${err.message}`)
    } finally {
      setIsSubmittingApproval(false)
    }
  }

  return (
    <div className="space-y-8">
      {/* Plan Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-slate-900 border border-slate-800 rounded-3xl p-6">
        <div>
          <div className="flex items-center space-x-2">
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-sky-500/10 text-sky-400 border border-sky-500/30">
              {plan.targetArchitecture}
            </span>
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-purple-500/10 text-purple-400 border border-purple-500/30">
              Plan Version v{plan.planVersion}
            </span>
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${
              plan.overallRisk === 'High' ? 'bg-rose-500/10 text-rose-400 border border-rose-500/30' :
              plan.overallRisk === 'Medium' ? 'bg-amber-500/10 text-amber-400 border border-amber-500/30' :
              'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
            }`}>
              {plan.overallRisk} Overall Risk
            </span>
          </div>

          <h1 className="text-2xl font-extrabold text-white tracking-tight mt-2">
            Clean Architecture Migration Plan
          </h1>
          <p className="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
            {plan.summary}
          </p>
        </div>

        <div className="flex items-center space-x-3 self-end md:self-auto">
          {!isApproved ? (
            <button
              onClick={() => setShowApprovalModal(true)}
              className="flex items-center space-x-2 px-6 py-3 rounded-xl bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-slate-950 font-extrabold text-xs shadow-lg shadow-sky-500/25 transition-all"
            >
              <ShieldCheck className="w-4 h-4" />
              <span>Review & Approve Plan</span>
            </button>
          ) : (
            <button
              onClick={onExecuteTransformation}
              className="flex items-center space-x-2 px-6 py-3 rounded-xl bg-gradient-to-r from-emerald-500 to-teal-500 hover:from-emerald-400 hover:to-teal-400 text-slate-950 font-extrabold text-xs shadow-lg shadow-emerald-500/25 transition-all"
            >
              <Play className="w-4 h-4 fill-slate-950" />
              <span>Execute Transformations</span>
            </button>
          )}
        </div>
      </div>

      {/* Target Structure Blueprint */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <h3 className="text-sm font-bold text-white mb-3 flex items-center space-x-2">
          <Layers className="w-4 h-4 text-sky-400" />
          <span>Target Clean Architecture Topology</span>
        </h3>
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-4 text-xs">
          <div className="p-3 bg-slate-950 rounded-xl border border-sky-500/30">
            <span className="font-bold text-sky-300">1. Presentation / API</span>
            <p className="text-slate-400 mt-1">HTTP Controllers, Middleware, Filters, Composition Root</p>
          </div>
          <div className="p-3 bg-slate-950 rounded-xl border border-purple-500/30">
            <span className="font-bold text-purple-300">2. Application</span>
            <p className="text-slate-400 mt-1">Use Cases, DTOs, Repository Interfaces, Application Services</p>
          </div>
          <div className="p-3 bg-slate-950 rounded-xl border border-emerald-500/30">
            <span className="font-bold text-emerald-300">3. Domain (Core)</span>
            <p className="text-slate-400 mt-1">Entities, Value Objects, Domain Exceptions (Zero outward deps)</p>
          </div>
          <div className="p-3 bg-slate-950 rounded-xl border border-amber-500/30">
            <span className="font-bold text-amber-300">4. Infrastructure</span>
            <p className="text-slate-400 mt-1">EF Core / JPA Repositories, Database Drivers, External APIs</p>
          </div>
        </div>
      </div>

      {/* Operations List with diffs & edits */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-base font-bold text-white">
            Proposed Transformation Operations ({operations.length})
          </h2>
          <span className="text-xs text-slate-400">
            {operations.filter((o) => o.isApproved).length} of {operations.length} operations approved
          </span>
        </div>

        <OperationsList
          operations={operations}
          onToggleApprove={handleToggleApprove}
          onUpdateOperation={handleUpdateOperation}
          isReadOnly={isApproved}
        />
      </div>

      {/* Plan Regeneration section */}
      {!isApproved && (
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
          <h3 className="text-sm font-bold text-white mb-2 flex items-center space-x-2">
            <RefreshCw className="w-4 h-4 text-sky-400" />
            <span>Regenerate Plan with Custom Guidance</span>
          </h3>
          <p className="text-xs text-slate-400 mb-4">
            Provide additional architectural instructions (e.g. "Do not extract use cases for read-only controllers", "Keep existing DTO names unchanged").
          </p>

          <form onSubmit={handleRegenerate} className="flex gap-3">
            <input
              type="text"
              placeholder="e.g. Place shared utilities in Project.Domain.Common..."
              value={customPrompt}
              onChange={(e) => setCustomPrompt(e.target.value)}
              className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none focus:border-sky-500"
            />
            <button
              type="submit"
              disabled={isRegenerating}
              className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-xs transition-all flex items-center space-x-1.5"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isRegenerating ? 'animate-spin' : ''}`} />
              <span>{isRegenerating ? 'Regenerating...' : 'Regenerate Plan'}</span>
            </button>
          </form>
        </div>
      )}

      {/* Cryptographic Approval Modal */}
      {showApprovalModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 max-w-lg w-full shadow-2xl space-y-5">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 rounded-xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400">
                <Lock className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-white">Cryptographic Developer Sign-Off</h3>
                <p className="text-xs text-slate-400">Explicit approval bound to source hash & plan version</p>
              </div>
            </div>

            <div className="p-4 bg-slate-950 rounded-2xl border border-slate-800 space-y-2 text-xs font-mono">
              <div className="flex justify-between text-slate-400">
                <span>Source SHA-256:</span>
                <span className="text-slate-200 truncate max-w-xs">{job.sourceHash}</span>
              </div>
              <div className="flex justify-between text-slate-400">
                <span>Migration Plan:</span>
                <span className="text-sky-400">Version {plan.planVersion}</span>
              </div>
              <div className="flex justify-between text-slate-400">
                <span>Approved Operations:</span>
                <span className="text-emerald-400">{operations.filter((o) => o.isApproved).length} operations</span>
              </div>
            </div>

            <form onSubmit={handleConfirmApproval} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1">
                  Developer / Architect Signature
                </label>
                <input
                  type="text"
                  required
                  value={approvedByName}
                  onChange={(e) => setApprovedByName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-sky-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1">
                  Approval Notes / Rationale
                </label>
                <textarea
                  value={approvalNotes}
                  onChange={(e) => setApprovalNotes(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-sky-500 h-20"
                />
              </div>

              <div className="p-3 bg-amber-500/10 border border-amber-500/30 rounded-xl flex items-start space-x-2 text-amber-300 text-xs">
                <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5" />
                <span>
                  By signing off, you authorize the isolated transformation sandbox to restructure project files according to the approved operations.
                </span>
              </div>

              <div className="flex items-center justify-end space-x-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowApprovalModal(false)}
                  className="px-4 py-2 text-xs font-semibold text-slate-400 hover:text-slate-200"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmittingApproval}
                  className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-sky-500 to-indigo-600 hover:opacity-95 text-slate-950 font-bold text-xs"
                >
                  {isSubmittingApproval ? 'Verifying...' : 'Sign & Authorize Pipeline'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
