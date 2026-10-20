import React from 'react'
import { FileCode, Plus, Minus } from 'lucide-react'

interface DiffViewerProps {
  diffText?: string
  filePath?: string
  title?: string
}

export const DiffViewer: React.FC<DiffViewerProps> = ({ diffText, filePath, title }) => {
  if (!diffText) {
    return (
      <div className="p-4 bg-slate-950 rounded-xl border border-slate-800 text-xs text-slate-500 italic">
        No code diff preview required for this structural operation.
      </div>
    )
  }

  const lines = diffText.split('\n')

  return (
    <div className="bg-slate-950 border border-slate-800 rounded-xl overflow-hidden font-mono text-xs">
      <div className="bg-slate-900/80 px-4 py-2 border-b border-slate-800 flex items-center justify-between">
        <div className="flex items-center space-x-2 text-slate-300">
          <FileCode className="w-3.5 h-3.5 text-sky-400" />
          <span className="font-semibold">{title || filePath || 'Proposed Code Modification'}</span>
        </div>
        <span className="text-[11px] text-slate-500">Unified Diff</span>
      </div>

      <div className="p-2 overflow-x-auto max-h-64 divide-y divide-slate-900">
        {lines.map((line, idx) => {
          const isAdd = line.startsWith('+')
          const isDel = line.startsWith('-')

          return (
            <div
              key={idx}
              className={`flex items-start px-2 py-0.5 font-mono ${
                isAdd
                  ? 'bg-emerald-500/10 text-emerald-300'
                  : isDel
                  ? 'bg-rose-500/10 text-rose-300'
                  : 'text-slate-400'
              }`}
            >
              <span className="w-6 shrink-0 select-none text-slate-600 text-[10px] text-right pr-2">
                {idx + 1}
              </span>
              <span className="w-4 shrink-0 select-none">
                {isAdd ? <Plus className="w-3 h-3 text-emerald-400 inline" /> : isDel ? <Minus className="w-3 h-3 text-rose-400 inline" /> : ' '}
              </span>
              <pre className="whitespace-pre-wrap break-all flex-1 font-mono">{line.substring(isAdd || isDel ? 1 : 0)}</pre>
            </div>
          )
        })}
      </div>
    </div>
  )
}
