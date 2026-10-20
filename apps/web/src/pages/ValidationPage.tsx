import React from 'react'
import { CheckCircle2, AlertTriangle, ShieldCheck, Terminal, Cpu, FileArchive } from 'lucide-react'
import type { ValidationReport } from '../types'

interface ValidationPageProps {
  validation: ValidationReport | null
  onDownloadArtifacts: () => void
}

export const ValidationPage: React.FC<ValidationPageProps> = ({
  validation,
  onDownloadArtifacts
}) => {
  if (!validation) {
    return (
      <div className="text-center py-24 bg-slate-900 border border-slate-800 rounded-3xl">
        <Cpu className="w-12 h-12 text-slate-600 mx-auto mb-3 animate-pulse" />
        <h3 className="text-base font-semibold text-slate-300">Validation Not Available Yet</h3>
        <p className="text-xs text-slate-500 mt-1">Validation runs automatically after transformations are applied.</p>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-2">
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${
              validation.overallSuccess
                ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                : 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
            }`}>
              {validation.overallSuccess ? 'Overall Success: Passed' : 'Issues Observed'}
            </span>
            <span className="text-xs text-slate-400">
              Validated on {new Date(validation.validatedAtUtc).toLocaleTimeString()}
            </span>
          </div>

          <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight mt-2">
            Validation & Architecture Verification Report
          </h1>
          <p className="text-xs text-slate-300 mt-1">{validation.summary}</p>
        </div>

        <button
          onClick={onDownloadArtifacts}
          className="flex items-center space-x-2 px-6 py-3 rounded-xl bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-slate-950 font-extrabold text-xs shadow-lg shadow-sky-500/20 transition-all self-start sm:self-auto"
        >
          <FileArchive className="w-4 h-4" />
          <span>Download Output ZIP</span>
        </button>
      </div>

      {/* Summary Stat Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-5">
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
          <div className="flex items-center justify-between text-xs font-semibold uppercase text-slate-400">
            <span>Compiler Build Status</span>
            {validation.build.success ? (
              <CheckCircle2 className="w-4 h-4 text-emerald-400" />
            ) : (
              <AlertTriangle className="w-4 h-4 text-rose-400" />
            )}
          </div>
          <p className="text-2xl font-bold text-white mt-2">
            {validation.build.success ? 'Build Succeeded' : 'Build Failed'}
          </p>
          <p className="text-xs text-slate-400 mt-1">
            {validation.build.errorsCount} errors • {validation.build.warningsCount} warnings
          </p>
        </div>

        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
          <div className="flex items-center justify-between text-xs font-semibold uppercase text-slate-400">
            <span>Automated Test Suite</span>
            {validation.tests.success ? (
              <CheckCircle2 className="w-4 h-4 text-emerald-400" />
            ) : (
              <AlertTriangle className="w-4 h-4 text-rose-400" />
            )}
          </div>
          <p className="text-2xl font-bold text-emerald-400 mt-2">
            {validation.tests.passedTests} / {validation.tests.totalTests} Passed
          </p>
          <p className="text-xs text-slate-400 mt-1">
            {validation.tests.failedTests} failures • {validation.tests.duration}
          </p>
        </div>

        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
          <div className="flex items-center justify-between text-xs font-semibold uppercase text-slate-400">
            <span>Clean Architecture Rules</span>
            <ShieldCheck className="w-4 h-4 text-sky-400" />
          </div>
          <p className="text-2xl font-bold text-sky-400 mt-2">
            {validation.architectureRules.filter((r) => r.passed).length} / {validation.architectureRules.length} Compliant
          </p>
          <p className="text-xs text-slate-400 mt-1">Dependency Inversion & Boundaries Verified</p>
        </div>
      </div>

      {/* Architecture Rule Compliance Matrix */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <h3 className="text-base font-bold text-white mb-4 flex items-center space-x-2">
          <ShieldCheck className="w-5 h-5 text-sky-400" />
          <span>Clean Architecture Rule Evaluation Matrix</span>
        </h3>

        <div className="space-y-3">
          {validation.architectureRules.map((rule, idx) => (
            <div
              key={idx}
              className={`p-4 rounded-xl border flex flex-col md:flex-row md:items-center justify-between gap-3 text-xs ${
                rule.passed
                  ? 'bg-slate-950 border-slate-800'
                  : 'bg-rose-500/10 border-rose-500/30'
              }`}
            >
              <div className="flex items-start space-x-3">
                {rule.passed ? (
                  <CheckCircle2 className="w-5 h-5 text-emerald-400 mt-0.5 shrink-0" />
                ) : (
                  <AlertTriangle className="w-5 h-5 text-rose-400 mt-0.5 shrink-0" />
                )}
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="font-mono font-bold text-slate-300">{rule.ruleId}</span>
                    <span className="font-semibold text-white">{rule.ruleName}</span>
                  </div>
                  <p className="text-slate-400 mt-1">{rule.message}</p>
                </div>
              </div>

              <span className={`px-2.5 py-1 rounded-full font-bold uppercase text-[10px] self-start md:self-auto ${
                rule.passed
                  ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                  : 'bg-rose-500/20 text-rose-400 border border-rose-500/40'
              }`}>
                {rule.passed ? 'Compliant' : 'Violation'}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Raw Build Output Terminal */}
      <div className="bg-slate-950 border border-slate-800 rounded-2xl overflow-hidden font-mono text-xs">
        <div className="p-3 bg-slate-900 border-b border-slate-800 flex items-center space-x-2 text-slate-400">
          <Terminal className="w-4 h-4 text-sky-400" />
          <span className="font-bold text-slate-300">Sandbox Compiler & Test Output</span>
        </div>
        <pre className="p-4 text-slate-300 max-h-60 overflow-y-auto whitespace-pre-wrap leading-relaxed select-text">
          {validation.build.rawOutput || 'Clean build output.'}
        </pre>
      </div>
    </div>
  )
}
