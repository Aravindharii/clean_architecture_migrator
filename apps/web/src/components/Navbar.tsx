import React from 'react'
import { Layers, ShieldCheck, Cpu, GitBranch, Play, CheckCircle2, FileArchive, UploadCloud } from 'lucide-react'
import type { JobStatus } from '../types'

interface NavbarProps {
  currentTab: string
  setCurrentTab: (tab: string) => void
  activeJobId: string | null
  activeJobStatus?: JobStatus
}

export const Navbar: React.FC<NavbarProps> = ({
  currentTab,
  setCurrentTab,
  activeJobId,
  activeJobStatus
}) => {
  const navItems = [
    { id: 'dashboard', label: 'Projects', icon: Layers },
    { id: 'upload', label: 'Upload Archive', icon: UploadCloud },
    { id: 'analysis', label: 'Analysis & Graph', icon: GitBranch, disabled: !activeJobId },
    { id: 'plan', label: 'Plan Review', icon: ShieldCheck, disabled: !activeJobId },
    { id: 'transform', label: 'Pipeline Execution', icon: Play, disabled: !activeJobId },
    { id: 'validation', label: 'Validation Report', icon: CheckCircle2, disabled: !activeJobId },
    { id: 'artifacts', label: 'Export & ZIP', icon: FileArchive, disabled: !activeJobId },
  ]

  return (
    <header className="border-b border-slate-800 bg-slate-950/80 backdrop-blur-md sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <div className="flex items-center space-x-3 cursor-pointer" onClick={() => setCurrentTab('dashboard')}>
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-sky-500 via-indigo-500 to-purple-600 flex items-center justify-center shadow-lg shadow-sky-500/20">
              <Cpu className="w-6 h-6 text-white" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-bold text-lg text-white tracking-tight">CleanArch</span>
                <span className="px-2 py-0.5 text-xs font-semibold uppercase bg-sky-500/10 text-sky-400 border border-sky-500/30 rounded-full"></span>
              </div>
              <p className="text-xs text-slate-400">Deterministic .NET & Java Modernization Platform</p>
            </div>
          </div>

          {/* Nav Links */}
          <nav className="flex space-x-1 sm:space-x-2">
            {navItems.map((item) => {
              const Icon = item.icon
              const isActive = currentTab === item.id
              const isDisabled = item.disabled

              return (
                <button
                  key={item.id}
                  disabled={isDisabled}
                  onClick={() => setCurrentTab(item.id)}
                  className={`flex items-center space-x-2 px-3 py-1.5 text-sm font-medium rounded-lg transition-all ${
                    isActive
                      ? 'bg-sky-500/15 text-sky-300 border border-sky-500/40 shadow-sm'
                      : isDisabled
                      ? 'text-slate-600 cursor-not-allowed opacity-50'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  <span className="hidden md:inline">{item.label}</span>
                </button>
              )
            })}
          </nav>

          {/* Active Status Badge */}
          {activeJobId && (
            <div className="hidden lg:flex items-center space-x-3 bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5">
              <span className="text-xs text-slate-400">Job: <span className="font-mono text-slate-300">{activeJobId.substring(0, 8)}...</span></span>
              {activeJobStatus && (
                <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${
                  activeJobStatus === 'Completed' ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40' :
                  activeJobStatus === 'AwaitingApproval' ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 animate-pulse' :
                  activeJobStatus === 'Failed' ? 'bg-rose-500/20 text-rose-400 border border-rose-500/40' :
                  'bg-sky-500/20 text-sky-400 border border-sky-500/40 animate-pulse'
                }`}>
                  {activeJobStatus}
                </span>
              )}
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
