import React, { useState } from 'react'
import { Plus, FolderGit2, ArrowRight, CheckCircle2, Layers, Clock, Cpu, UploadCloud } from 'lucide-react'
import type { ProjectResponse } from '../types'
import { api } from '../api/client'

interface DashboardPageProps {
  projects: ProjectResponse[]
  onSelectProject: (projectId: string, jobId?: string) => void
  onNavigateToUpload: () => void
  onRefresh: () => void
}

export const DashboardPage: React.FC<DashboardPageProps> = ({
  projects,
  onSelectProject,
  onNavigateToUpload,
  onRefresh
}) => {
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [newProjectName, setNewProjectName] = useState('')
  const [newProjectDesc, setNewProjectDesc] = useState('')
  const [isCreating, setIsCreating] = useState(false)

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!newProjectName.trim()) return

    setIsCreating(true)
    try {
      const created = await api.createProject(newProjectName.trim(), newProjectDesc.trim())
      setShowCreateModal(false)
      setNewProjectName('')
      setNewProjectDesc('')
      onRefresh()
      onSelectProject(created.id)
    } catch (err: any) {
      alert(`Failed to create project: ${err.message}`)
    } finally {
      setIsCreating(false)
    }
  }

  return (
    <div className="space-y-8">
      {/* Hero Banner */}
      <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-slate-900 via-indigo-950/40 to-slate-900 border border-slate-800 p-8 shadow-2xl">
        <div className="relative z-10 max-w-3xl">
          <div className="inline-flex items-center space-x-2 px-3 py-1 rounded-full bg-sky-500/10 border border-sky-500/30 text-sky-400 text-xs font-semibold mb-4">
            <Cpu className="w-3.5 h-3.5" />
            <span>Deterministic Clean Architecture Engine</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight leading-tight">
            Modernize Layered .NET & Java Monoliths with AI-Assisted Precision
          </h1>
          <p className="mt-3 text-slate-300 text-base leading-relaxed">
            Upload your existing codebase archive. We detect architectural violations, generate versioned migration plans with strict JSON schemas, require explicit cryptographic approvals, and execute deterministic AST transformations in an isolated sandbox.
          </p>

          <div className="mt-6 flex flex-wrap items-center gap-4">
            <button
              onClick={onNavigateToUpload}
              className="flex items-center space-x-2 px-5 py-3 rounded-xl bg-gradient-to-r from-sky-500 to-indigo-600 hover:from-sky-400 hover:to-indigo-500 text-slate-950 font-bold text-sm shadow-lg shadow-sky-500/25 transition-all"
            >
              <UploadCloud className="w-4 h-4" />
              <span>Upload Monolith ZIP</span>
            </button>
            <button
              onClick={() => setShowCreateModal(true)}
              className="flex items-center space-x-2 px-4 py-3 rounded-xl bg-slate-900 hover:bg-slate-800 text-slate-200 border border-slate-700 font-medium text-sm transition-all"
            >
              <Plus className="w-4 h-4" />
              <span>New Migration Workspace</span>
            </button>
          </div>
        </div>
      </div>

      {/* Metrics Row */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-5">
        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-5 backdrop-blur-md">
          <div className="flex items-center justify-between text-slate-400 text-xs uppercase font-semibold">
            <span>Target Architectures</span>
            <Layers className="w-4 h-4 text-sky-400" />
          </div>
          <p className="text-2xl font-bold text-white mt-2">.NET 10 & Java 17/21</p>
          <p className="text-xs text-slate-400 mt-1">Domain, Application, Infrastructure, API</p>
        </div>

        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-5 backdrop-blur-md">
          <div className="flex items-center justify-between text-slate-400 text-xs uppercase font-semibold">
            <span>Transformation Safety</span>
            <CheckCircle2 className="w-4 h-4 text-emerald-400" />
          </div>
          <p className="text-2xl font-bold text-emerald-400 mt-2">100% Deterministic</p>
          <p className="text-xs text-slate-400 mt-1">Roslyn AST & JavaParser • No hallucinated shell commands</p>
        </div>

        <div className="bg-slate-900/80 border border-slate-800 rounded-2xl p-5 backdrop-blur-md">
          <div className="flex items-center justify-between text-slate-400 text-xs uppercase font-semibold">
            <span>Approval Enforcement</span>
            <Clock className="w-4 h-4 text-amber-400" />
          </div>
          <p className="text-2xl font-bold text-white mt-2">Cryptographic Binding</p>
          <p className="text-xs text-slate-400 mt-1">Bound to SHA-256(Source) + PlanVersion</p>
        </div>
      </div>

      {/* Projects List */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-bold text-white tracking-tight flex items-center space-x-2">
            <FolderGit2 className="w-5 h-5 text-sky-400" />
            <span>Migration Projects</span>
          </h2>
          <button
            onClick={() => setShowCreateModal(true)}
            className="text-xs text-sky-400 hover:text-sky-300 font-semibold flex items-center space-x-1"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Create Project</span>
          </button>
        </div>

        {projects.length === 0 ? (
          <div className="bg-slate-900/40 border border-slate-800 border-dashed rounded-3xl p-12 text-center">
            <FolderGit2 className="w-12 h-12 text-slate-600 mx-auto mb-3" />
            <h3 className="text-base font-semibold text-slate-300">No migration projects yet</h3>
            <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
              Get started by uploading your first layered .NET or Java project archive.
            </p>
            <button
              onClick={onNavigateToUpload}
              className="mt-5 px-4 py-2 rounded-xl bg-sky-500 hover:bg-sky-400 text-slate-950 font-semibold text-xs transition-all"
            >
              Upload Project Archive
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            {projects.map((proj) => (
              <div
                key={proj.id}
                onClick={() => onSelectProject(proj.id, proj.latestJobId)}
                className="group bg-slate-900/90 border border-slate-800 hover:border-sky-500/50 rounded-2xl p-5 shadow-lg transition-all cursor-pointer flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-slate-500 font-mono">
                      {new Date(proj.createdAtUtc).toLocaleDateString()}
                    </span>
                    {proj.latestJobStatus && (
                      <span className={`px-2 py-0.5 rounded-full text-xs font-semibold ${
                        proj.latestJobStatus === 'Completed' ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30' :
                        proj.latestJobStatus === 'AwaitingApproval' ? 'bg-amber-500/10 text-amber-300 border border-amber-500/30 animate-pulse' :
                        'bg-sky-500/10 text-sky-400 border border-sky-500/30'
                      }`}>
                        {proj.latestJobStatus}
                      </span>
                    )}
                  </div>

                  <h3 className="text-base font-bold text-white mt-2 group-hover:text-sky-300 transition-colors">
                    {proj.name}
                  </h3>
                  <p className="text-xs text-slate-400 mt-1 line-clamp-2">
                    {proj.description || 'Clean Architecture migration workspace.'}
                  </p>
                </div>

                <div className="mt-5 pt-3 border-t border-slate-800/80 flex items-center justify-between text-xs text-sky-400 font-semibold">
                  <span>Open Workspace</span>
                  <ArrowRight className="w-4 h-4 transform group-hover:translate-x-1 transition-transform" />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Create Project Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-3xl p-6 max-w-md w-full shadow-2xl">
            <h3 className="text-lg font-bold text-white">New Migration Project</h3>
            <p className="text-xs text-slate-400 mt-1">Create a container for your application migration runs.</p>

            <form onSubmit={handleCreate} className="mt-5 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1">
                  Project Name
                </label>
                <input
                  type="text"
                  required
                  placeholder="e.g. LegacyShop Modernization"
                  value={newProjectName}
                  onChange={(e) => setNewProjectName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:border-sky-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-1">
                  Description
                </label>
                <textarea
                  placeholder="Optional context about the system..."
                  value={newProjectDesc}
                  onChange={(e) => setNewProjectDesc(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:border-sky-500 h-20"
                />
              </div>

              <div className="flex items-center justify-end space-x-3 pt-3">
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="px-4 py-2 text-xs font-semibold text-slate-400 hover:text-slate-200"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isCreating}
                  className="px-5 py-2 rounded-xl bg-sky-500 hover:bg-sky-400 text-slate-950 font-bold text-xs"
                >
                  {isCreating ? 'Creating...' : 'Create Project'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
