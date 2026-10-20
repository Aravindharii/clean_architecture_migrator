import React from 'react'
import { GitBranch, AlertTriangle, Database, Cloud, CheckCircle2, ArrowRight } from 'lucide-react'
import type { ProjectAnalysisResult, JobStatusResponse } from '../types'
import { DependencyGraph } from '../components/DependencyGraph'

interface AnalysisPageProps {
  analysis: ProjectAnalysisResult | null
  job: JobStatusResponse | null
  onProceedToPlan: () => void
}

export const AnalysisPage: React.FC<AnalysisPageProps> = ({
  analysis,
  job: _job,
  onProceedToPlan
}) => {
  if (!analysis) {
    return (
      <div className="text-center py-24 bg-slate-900 border border-slate-800 rounded-3xl">
        <GitBranch className="w-12 h-12 text-slate-600 mx-auto mb-3 animate-pulse" />
        <h3 className="text-base font-semibold text-slate-300">Analysis in progress or not loaded</h3>
        <p className="text-xs text-slate-500 mt-1">Waiting for analyzer to parse source syntax and AST...</p>
      </div>
    )
  }

  const criticalViolations = analysis.violations.filter((v) => v.severity === 'Critical')
  const majorViolations = analysis.violations.filter((v) => v.severity === 'Major')

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-2">
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-sky-500/10 text-sky-400 border border-sky-500/30">
              {analysis.language}
            </span>
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-indigo-500/10 text-indigo-400 border border-indigo-500/30">
              {analysis.framework}
            </span>
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-purple-500/10 text-purple-400 border border-purple-500/30">
              {analysis.buildSystem}
            </span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight mt-2">
            Architectural Analysis & Findings
          </h1>
          <p className="text-xs text-slate-400 font-mono mt-1">
            Source SHA-256: {analysis.sourceHash}
          </p>
        </div>

        <button
          onClick={onProceedToPlan}
          className="self-start md:self-auto flex items-center space-x-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-slate-950 font-bold text-xs shadow-lg shadow-sky-500/20 transition-all"
        >
          <span>Review  Migration Plan</span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>

      {/* Overview Stat Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
          <span className="text-xs uppercase font-semibold text-slate-400">Source Files</span>
          <p className="text-2xl font-bold text-white mt-1">{analysis.sourceFiles.length}</p>
          <span className="text-[11px] text-slate-500">Tracked in archive</span>
        </div>

        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
          <span className="text-xs uppercase font-semibold text-slate-400">Discovered Symbols</span>
          <p className="text-2xl font-bold text-sky-400 mt-1">{analysis.symbols.length}</p>
          <span className="text-[11px] text-slate-500">Classes, interfaces, records</span>
        </div>

        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
          <span className="text-xs uppercase font-semibold text-slate-400">Layer Violations</span>
          <p className="text-2xl font-bold text-rose-400 mt-1">{analysis.violations.length}</p>
          <span className="text-[11px] text-rose-500/80 font-medium">
            {criticalViolations.length} critical • {majorViolations.length} major
          </span>
        </div>

        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-4">
          <span className="text-xs uppercase font-semibold text-slate-400">Database Couplings</span>
          <p className="text-2xl font-bold text-amber-400 mt-1">{analysis.databaseDependencies.length}</p>
          <span className="text-[11px] text-slate-500">Persistence touchpoints</span>
        </div>
      </div>

      {/* Interactive Dependency Graph */}
      <div>
        <DependencyGraph
          graphData={analysis.dependencyGraph}
          symbols={analysis.symbols}
          violations={analysis.violations}
        />
      </div>

      {/* Architectural Violations Section */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <div className="flex items-center space-x-2 pb-4 border-b border-slate-800 mb-4">
          <AlertTriangle className="w-5 h-5 text-rose-400" />
          <h3 className="text-base font-bold text-white">Clean Architecture Violations Detected</h3>
          <span className="text-xs px-2 py-0.5 rounded-full bg-rose-500/10 text-rose-400 border border-rose-500/30">
            {analysis.violations.length} Violations
          </span>
        </div>

        {analysis.violations.length === 0 ? (
          <div className="p-4 bg-emerald-500/10 border border-emerald-500/30 rounded-xl flex items-center space-x-2 text-emerald-400 text-xs font-medium">
            <CheckCircle2 className="w-4 h-4" />
            <span>Zero architectural violations observed. Codebase already adheres to Clean Architecture boundaries!</span>
          </div>
        ) : (
          <div className="space-y-3">
            {analysis.violations.map((v, i) => (
              <div
                key={i}
                className="bg-slate-950 p-4 rounded-xl border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-3"
              >
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="font-mono text-xs px-2 py-0.5 bg-slate-800 rounded text-slate-300 font-bold">
                      {v.ruleId}
                    </span>
                    <span className={`text-xs px-2 py-0.5 rounded font-semibold ${
                      v.severity === 'Critical' ? 'bg-rose-500/20 text-rose-400 border border-rose-500/40' :
                      'bg-amber-500/20 text-amber-300 border border-amber-500/40'
                    }`}>
                      {v.severity}
                    </span>
                    <span className="text-xs font-mono text-slate-400 truncate max-w-xs">{v.sourceSymbol}</span>
                  </div>
                  <p className="text-xs text-slate-300 mt-1 font-medium">{v.description}</p>
                  <p className="text-[11px] text-sky-400 mt-1">{v.recommendation}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Database & Integrations */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
          <div className="flex items-center space-x-2 mb-3">
            <Database className="w-4 h-4 text-amber-400" />
            <h3 className="text-sm font-bold text-white">Database & ORM Access</h3>
          </div>
          {analysis.databaseDependencies.length > 0 ? (
            <div className="space-y-2">
              {analysis.databaseDependencies.map((db, i) => (
                <div key={i} className="p-2.5 bg-slate-950 rounded-lg border border-slate-800 text-xs">
                  <div className="flex items-center justify-between text-slate-300 font-semibold">
                    <span>{db.technology}</span>
                    <span className="text-[11px] text-amber-400 font-mono">{db.tableOrEntity}</span>
                  </div>
                  <p className="text-[11px] font-mono text-slate-500 mt-0.5 truncate">{db.declaredInSymbol}</p>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-xs text-slate-500">No concrete database frameworks detected.</p>
          )}
        </div>

        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
          <div className="flex items-center space-x-2 mb-3">
            <Cloud className="w-4 h-4 text-sky-400" />
            <h3 className="text-sm font-bold text-white">External Integrations & Messaging</h3>
          </div>
          {analysis.externalIntegrations.length > 0 ? (
            <div className="space-y-2">
              {analysis.externalIntegrations.map((ext, i) => (
                <div key={i} className="p-2.5 bg-slate-950 rounded-lg border border-slate-800 text-xs">
                  <span className="font-semibold text-slate-300">{ext.serviceType}</span>
                  <p className="text-[11px] font-mono text-slate-500 mt-0.5 truncate">{ext.declaredInSymbol}</p>
                  <p className="text-[11px] text-sky-400 mt-0.5">{ext.details}</p>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-xs text-slate-500">No external integrations detected.</p>
          )}
        </div>
      </div>
    </div>
  )
}
