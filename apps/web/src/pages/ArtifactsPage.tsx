import React from 'react'
import { FileArchive, Download, ShieldCheck, FileText, Hash, ArrowLeft } from 'lucide-react'
import type { ArtifactMetadata, ValidationReport, JobStatusResponse } from '../types'

interface ArtifactsPageProps {
  artifacts: ArtifactMetadata[]
  validation: ValidationReport | null
  job: JobStatusResponse | null
  onBackToDashboard: () => void
}

export const ArtifactsPage: React.FC<ArtifactsPageProps> = ({
  artifacts,
  validation,
  job,
  onBackToDashboard
}) => {
  const downloadReportJson = () => {
    if (!validation) return
    const blob = new Blob([JSON.stringify(validation, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `migration-validation-report-${job?.jobId || 'job'}.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="max-w-4xl mx-auto space-y-8">
      <div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
          Migration Artifacts & Output ZIP
        </h1>
        <p className="text-sm text-slate-400 mt-1">
          Download your verified, transformed Clean Architecture solution archive and audit reports.
        </p>
      </div>

      {/* Main Download Card */}
      <div className="bg-gradient-to-br from-slate-900 via-indigo-950/40 to-slate-900 border border-slate-800 rounded-3xl p-8 shadow-2xl relative overflow-hidden">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-6 relative z-10">
          <div className="flex items-start space-x-4">
            <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-sky-500 to-indigo-600 flex items-center justify-center text-slate-950 shadow-lg shadow-sky-500/25 shrink-0">
              <FileArchive className="w-8 h-8" />
            </div>
            <div>
              <span className="text-xs uppercase font-mono font-semibold text-sky-400 tracking-wider">
                Production-Ready Clean Architecture
              </span>
              <h2 className="text-xl font-bold text-white mt-1">
                migrated-clean-architecture.zip
              </h2>
              <p className="text-xs text-slate-300 mt-1 max-w-md">
                Contains complete restructured solution with Domain, Application, Infrastructure, and API projects, extracted interfaces, and DI composition.
              </p>
            </div>
          </div>

          <a
            href={`/api/jobs/${job?.jobId}/artifacts/output.zip`}
            download
            className="flex items-center justify-center space-x-2 px-6 py-3.5 rounded-2xl bg-gradient-to-r from-sky-400 to-indigo-500 hover:from-sky-300 hover:to-indigo-400 text-slate-950 font-extrabold text-sm shadow-xl shadow-sky-500/25 transition-all shrink-0"
          >
            <Download className="w-5 h-5 stroke-[2.5]" />
            <span>Download Output ZIP</span>
          </a>
        </div>
      </div>

      {/* Artifacts Metadata Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6">
        <h3 className="text-base font-bold text-white mb-4 flex items-center space-x-2">
          <Hash className="w-4 h-4 text-sky-400" />
          <span>Artifact Hashes & Verification</span>
        </h3>

        <div className="space-y-3">
          {artifacts.map((art) => (
            <div
              key={art.artifactId}
              className="p-4 bg-slate-950 rounded-xl border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-3 text-xs"
            >
              <div>
                <span className="font-bold text-slate-200">{art.fileName}</span>
                <span className="ml-2 text-slate-500">({(art.sizeBytes / 1024).toFixed(1)} KB)</span>
                <p className="text-[11px] font-mono text-slate-400 mt-1">
                  SHA-256: <span className="text-slate-300">{art.sha256}</span>
                </p>
              </div>

              <a
                href={art.downloadUrl}
                download
                className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold self-start md:self-auto transition-all"
              >
                <Download className="w-3.5 h-3.5" />
                <span>Download</span>
              </a>
            </div>
          ))}

          {/* Validation report JSON export */}
          <div className="p-4 bg-slate-950 rounded-xl border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-3 text-xs">
            <div>
              <span className="font-bold text-slate-200">validation-audit-report.json</span>
              <p className="text-[11px] text-slate-400 mt-0.5">
                Structured JSON schema report containing compiler outputs, test metrics, and architectural rules.
              </p>
            </div>

            <button
              onClick={downloadReportJson}
              className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold self-start md:self-auto transition-all"
            >
              <FileText className="w-3.5 h-3.5" />
              <span>Export JSON Report</span>
            </button>
          </div>
        </div>
      </div>

      {/* Safety Notice */}
      <div className="p-5 bg-slate-900/60 border border-slate-800 rounded-2xl flex items-center space-x-3 text-xs text-slate-400">
        <ShieldCheck className="w-5 h-5 text-emerald-400 shrink-0" />
        <span>
          Original source archive was protected and preserved as read-only throughout all pipeline operations.
        </span>
      </div>

      {/* Back button */}
      <div>
        <button
          onClick={onBackToDashboard}
          className="flex items-center space-x-2 text-xs text-sky-400 hover:text-sky-300 font-semibold"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Return to Workspace Dashboard</span>
        </button>
      </div>
    </div>
  )
}
