import React, { useState } from 'react'
import { Check, ChevronDown, ChevronRight, ShieldAlert, ArrowRight, Sparkles } from 'lucide-react'
import type { PlanOperation, RiskLevel, OperationType } from '../types'
import { DiffViewer } from './DiffViewer'

interface OperationsListProps {
  operations: PlanOperation[]
  onToggleApprove: (operationId: string) => void
  onUpdateOperation: (operationId: string, updated: Partial<PlanOperation>) => void
  isReadOnly?: boolean
}

const RISK_BADGES: Record<RiskLevel, { bg: string; text: string; border: string }> = {
  Low: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/30' },
  Medium: { bg: 'bg-amber-500/10', text: 'text-amber-400', border: 'border-amber-500/30' },
  High: { bg: 'bg-rose-500/10', text: 'text-rose-400', border: 'border-rose-500/30' },
}

const OP_TYPE_BADGES: Record<OperationType, { label: string; color: string }> = {
  CreateProjectOrModule: { label: 'Create Project', color: 'bg-sky-500/15 text-sky-400' },
  MoveFile: { label: 'Move File', color: 'bg-indigo-500/15 text-indigo-400' },
  RenameNamespaceOrPackage: { label: 'Rename Namespace', color: 'bg-purple-500/15 text-purple-400' },
  UpdateProjectReferences: { label: 'Project References', color: 'bg-blue-500/15 text-blue-400' },
  ExtractInterface: { label: 'Extract Interface', color: 'bg-emerald-500/15 text-emerald-400' },
  UpdateDependencyInjection: { label: 'Update DI', color: 'bg-teal-500/15 text-teal-400' },
  CreateApplicationUseCase: { label: 'Create UseCase', color: 'bg-pink-500/15 text-pink-400' },
  ProposeRepositoryAbstraction: { label: 'Repo Abstraction', color: 'bg-amber-500/15 text-amber-400' },
  FlagForManualRefactoring: { label: 'Manual Refactor', color: 'bg-rose-500/15 text-rose-400' },
  AddArchitectureValidationRules: { label: 'Arch Rules', color: 'bg-violet-500/15 text-violet-400' },
}

export const OperationsList: React.FC<OperationsListProps> = ({
  operations,
  onToggleApprove,
  onUpdateOperation,
  isReadOnly = false
}) => {
  const [expandedId, setExpandedId] = useState<string | null>(null)

  return (
    <div className="space-y-3">
      {operations.map((op) => {
        const isExpanded = expandedId === op.operationId
        const riskStyle = RISK_BADGES[op.risk] || RISK_BADGES.Medium
        const typeInfo = OP_TYPE_BADGES[op.type] || { label: op.type, color: 'bg-slate-800 text-slate-300' }

        return (
          <div
            key={op.operationId}
            className={`border rounded-2xl transition-all overflow-hidden ${
              op.isApproved
                ? 'bg-slate-900/90 border-slate-800 hover:border-slate-700'
                : 'bg-slate-950/60 border-rose-900/30 opacity-70'
            }`}
          >
            {/* Header row */}
            <div className="p-4 flex flex-col md:flex-row md:items-center justify-between gap-3">
              <div className="flex items-center space-x-3">
                {/* Approval toggle checkbox */}
                {!isReadOnly ? (
                  <button
                    onClick={() => onToggleApprove(op.operationId)}
                    className={`w-6 h-6 rounded-lg flex items-center justify-center transition-all ${
                      op.isApproved
                        ? 'bg-sky-500 text-slate-950 shadow-sm shadow-sky-500/40'
                        : 'border border-slate-700 bg-slate-950 text-transparent hover:border-slate-500'
                    }`}
                  >
                    <Check className="w-4 h-4 stroke-[3]" />
                  </button>
                ) : (
                  <span className={`w-5 h-5 rounded-full flex items-center justify-center ${op.isApproved ? 'text-emerald-400' : 'text-slate-600'}`}>
                    <Check className="w-4 h-4" />
                  </span>
                )}

                <span className="w-6 h-6 rounded-md bg-slate-800 flex items-center justify-center text-xs font-mono text-slate-400 font-bold shrink-0">
                  {op.sequence}
                </span>

                <span className={`px-2.5 py-0.5 rounded-lg text-xs font-semibold ${typeInfo.color}`}>
                  {typeInfo.label}
                </span>

                <span className="text-sm font-medium text-slate-200">
                  {op.description}
                </span>
              </div>

              <div className="flex items-center space-x-2 self-end md:self-auto">
                {op.manualReviewRequired && (
                  <span className="flex items-center space-x-1 px-2 py-0.5 bg-amber-500/10 text-amber-400 border border-amber-500/30 rounded-md text-xs font-medium">
                    <ShieldAlert className="w-3 h-3" />
                    <span>Review Required</span>
                  </span>
                )}

                <span className={`px-2 py-0.5 rounded-md text-xs font-medium border ${riskStyle.bg} ${riskStyle.text} ${riskStyle.border}`}>
                  {op.risk} Risk
                </span>

                <span className="text-xs font-mono px-2 py-0.5 bg-slate-800 rounded text-slate-400">
                  {(op.confidence * 100).toFixed(0)}% Conf.
                </span>

                <button
                  onClick={() => setExpandedId(isExpanded ? null : op.operationId)}
                  className="p-1 text-slate-400 hover:text-slate-200 transition-colors"
                >
                  {isExpanded ? <ChevronDown className="w-5 h-5" /> : <ChevronRight className="w-5 h-5" />}
                </button>
              </div>
            </div>

            {/* Path preview if move or transform */}
            {(op.sourcePath || op.destinationPath) && (
              <div className="px-4 pb-3 pt-0 flex items-center space-x-2 text-xs font-mono text-slate-400">
                {op.sourcePath && <span className="truncate max-w-xs">{op.sourcePath}</span>}
                {op.sourcePath && op.destinationPath && <ArrowRight className="w-3.5 h-3.5 text-sky-400 shrink-0" />}
                {op.destinationPath && <span className="text-sky-300 font-semibold truncate max-w-md">{op.destinationPath}</span>}
              </div>
            )}

            {/* Expanded Accordion Details */}
            {isExpanded && (
              <div className="border-t border-slate-800 bg-slate-950/80 p-5 space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* AI Reason & Architectural Rationale */}
                  <div>
                    <h4 className="text-xs uppercase font-semibold text-sky-400 tracking-wider flex items-center space-x-1.5">
                      <Sparkles className="w-3.5 h-3.5" />
                      <span>Architectural Rationale</span>
                    </h4>
                    <p className="text-xs text-slate-300 mt-1 leading-relaxed">{op.reason}</p>
                    {op.evidence.length > 0 && (
                      <div className="mt-2 space-y-1">
                        <span className="text-[11px] font-semibold text-slate-500">Supporting Evidence:</span>
                        {op.evidence.map((ev, i) => (
                          <div key={i} className="text-xs text-slate-400 flex items-start space-x-1.5">
                            <span className="text-sky-400">•</span>
                            <span>{ev}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Configurable Parameters */}
                  <div>
                    <h4 className="text-xs uppercase font-semibold text-slate-400 tracking-wider">
                      Transformation Parameters
                    </h4>
                    {Object.keys(op.parameters).length > 0 ? (
                      <div className="mt-2 space-y-2">
                        {Object.entries(op.parameters).map(([key, val]) => (
                          <div key={key} className="flex items-center space-x-2 text-xs">
                            <span className="text-slate-400 font-mono w-28 shrink-0">{key}:</span>
                            {!isReadOnly ? (
                              <input
                                type="text"
                                value={val}
                                onChange={(e) => {
                                  const updated = { ...op.parameters, [key]: e.target.value }
                                  onUpdateOperation(op.operationId, { parameters: updated })
                                }}
                                className="flex-1 bg-slate-900 border border-slate-800 rounded px-2 py-1 text-slate-200 font-mono text-xs focus:outline-none focus:border-sky-500"
                              />
                            ) : (
                              <span className="font-mono text-slate-200">{val}</span>
                            )}
                          </div>
                        ))}
                      </div>
                    ) : (
                      <p className="text-xs text-slate-500 mt-1">No custom parameters required.</p>
                    )}
                  </div>
                </div>

                {/* Diff Preview */}
                {op.codeDiffPreview && (
                  <div className="mt-3">
                    <DiffViewer
                      diffText={op.codeDiffPreview}
                      title={`Code Modification: ${op.destinationPath || op.sourcePath}`}
                    />
                  </div>
                )}
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
