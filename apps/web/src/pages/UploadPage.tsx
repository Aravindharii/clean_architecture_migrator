import React, { useState } from 'react'
import { UploadCloud, ShieldCheck, CheckCircle2, AlertCircle, ArrowRight } from 'lucide-react'
import type { ProjectResponse } from '../types'
import { api } from '../api/client'

interface UploadPageProps {
  projects: ProjectResponse[]
  activeProjectId: string | null
  onUploadSuccess: (jobId: string, projectId: string) => void
}

export const UploadPage: React.FC<UploadPageProps> = ({
  projects,
  activeProjectId,
  onUploadSuccess
}) => {
  const [selectedProjectId, setSelectedProjectId] = useState<string>(activeProjectId || projects[0]?.id || '')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [dragActive, setDragActive] = useState(false)

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === 'dragenter' || e.type === 'dragover') setDragActive(true)
    else if (e.type === 'dragleave') setDragActive(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFileSelected(e.dataTransfer.files[0])
    }
  }

  const handleFileSelected = (file: File) => {
    setUploadError(null)
    if (!file.name.toLowerCase().endsWith('.zip')) {
      setUploadError('Only .zip archive files are accepted.')
      return
    }
    if (file.size > 50 * 1024 * 1024) {
      setUploadError('Archive exceeds 50MB maximum upload limit.')
      return
    }
    setSelectedFile(file)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedFile) {
      setUploadError('Please select a .zip archive to upload.')
      return
    }

    let projId = selectedProjectId
    if (!projId) {
      // Auto-create a project with archive base name
      const defaultName = selectedFile.name.replace(/\.zip$/i, '')
      const newProj = await api.createProject(defaultName, 'Automated migration run')
      projId = newProj.id
    }

    setIsUploading(true)
    setUploadError(null)

    try {
      const res = await api.uploadArchive(projId, selectedFile)
      onUploadSuccess(res.jobId, projId)
    } catch (err: any) {
      setUploadError(err.message || 'Upload failed.')
      setIsUploading(false)
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-8">
      <div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
          Upload Monolith Archive
        </h1>
        <p className="text-sm text-slate-400 mt-1">
          Upload an existing .NET or Java layered solution as a ZIP file. The original archive is preserved as read-only.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Project Target */}
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-5">
          <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
            Target Project Workspace
          </label>
          <select
            value={selectedProjectId}
            onChange={(e) => setSelectedProjectId(e.target.value)}
            className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-sky-500"
          >
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
            <option value="">-- Create New Project Automatically --</option>
          </select>
        </div>

        {/* Drag and Drop Zone */}
        <div
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
          className={`border-2 border-dashed rounded-3xl p-10 text-center transition-all ${
            dragActive
              ? 'border-sky-400 bg-sky-500/10'
              : selectedFile
              ? 'border-emerald-500/50 bg-emerald-500/5'
              : 'border-slate-800 bg-slate-900/60 hover:border-slate-700'
          }`}
        >
          <input
            type="file"
            id="archive-upload"
            accept=".zip"
            className="hidden"
            onChange={(e) => e.target.files?.[0] && handleFileSelected(e.target.files[0])}
          />

          <label htmlFor="archive-upload" className="cursor-pointer flex flex-col items-center">
            {selectedFile ? (
              <div className="w-16 h-16 rounded-2xl bg-emerald-500/15 flex items-center justify-center text-emerald-400 mb-4 shadow-lg shadow-emerald-500/20">
                <CheckCircle2 className="w-8 h-8" />
              </div>
            ) : (
              <div className="w-16 h-16 rounded-2xl bg-sky-500/10 flex items-center justify-center text-sky-400 mb-4 shadow-lg shadow-sky-500/20">
                <UploadCloud className="w-8 h-8" />
              </div>
            )}

            <h3 className="text-base font-bold text-white">
              {selectedFile ? selectedFile.name : 'Choose or drop monolith ZIP archive'}
            </h3>

            <p className="text-xs text-slate-400 mt-1 max-w-sm">
              {selectedFile
                ? `${(selectedFile.size / (1024 * 1024)).toFixed(2)} MB • Ready for analysis`
                : 'Supports .NET solutions (.sln, .csproj) and Java projects (Maven pom.xml, Gradle)'}
            </p>

            <span className="mt-4 px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-slate-200 transition-all">
              {selectedFile ? 'Change File' : 'Browse Local Files'}
            </span>
          </label>
        </div>

        {uploadError && (
          <div className="p-4 bg-rose-500/10 border border-rose-500/30 rounded-2xl flex items-center space-x-3 text-rose-300 text-sm">
            <AlertCircle className="w-5 h-5 text-rose-400 shrink-0" />
            <span>{uploadError}</span>
          </div>
        )}

        {/* Security checklist */}
        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-5">
          <div className="flex items-center space-x-2 text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            <span>Automated Security Enforcements</span>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs text-slate-300">
            <div className="flex items-center space-x-2">
              <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
              <span>Zip Slip & Path Traversal Prevention</span>
            </div>
            <div className="flex items-center space-x-2">
              <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
              <span>Secret Redaction prior to  inspection</span>
            </div>
            <div className="flex items-center space-x-2">
              <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
              <span>Symlink & Hardlink Entry Prohibition</span>
            </div>
            <div className="flex items-center space-x-2">
              <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
              <span>Immutable Original Source Preservation</span>
            </div>
          </div>
        </div>

        {/* Submit */}
        <button
          type="submit"
          disabled={!selectedFile || isUploading}
          className={`w-full py-4 rounded-2xl font-bold text-sm flex items-center justify-center space-x-2 shadow-xl transition-all ${
            !selectedFile || isUploading
              ? 'bg-slate-800 text-slate-500 cursor-not-allowed'
              : 'bg-gradient-to-r from-sky-500 via-indigo-500 to-purple-600 hover:opacity-95 text-slate-950 font-extrabold shadow-sky-500/25'
          }`}
        >
          {isUploading ? (
            <span>Extracting & Initiating Static Analysis...</span>
          ) : (
            <>
              <span>Begin Analysis & Clean Architecture Planning</span>
              <ArrowRight className="w-4 h-4" />
            </>
          )}
        </button>
      </form>
    </div>
  )
}
