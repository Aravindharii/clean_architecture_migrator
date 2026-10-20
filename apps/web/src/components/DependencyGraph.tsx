import React, { useState } from 'react'
import { AlertTriangle, Layers, Info, CheckCircle2, FileCode } from 'lucide-react'
import type { DependencyGraphData, SymbolInfo, ArchitectureViolation } from '../types'

interface DependencyGraphProps {
  graphData: DependencyGraphData
  symbols: SymbolInfo[]
  violations: ArchitectureViolation[]
}

const LAYER_COLORS: Record<string, { bg: string; border: string; text: string; glow: string }> = {
  Presentation: { bg: 'bg-sky-500/10', border: 'border-sky-500/50', text: 'text-sky-300', glow: '#38bdf8' },
  Application: { bg: 'bg-purple-500/10', border: 'border-purple-500/50', text: 'text-purple-300', glow: '#c084fc' },
  Domain: { bg: 'bg-emerald-500/10', border: 'border-emerald-500/50', text: 'text-emerald-300', glow: '#34d399' },
  Infrastructure: { bg: 'bg-amber-500/10', border: 'border-amber-500/50', text: 'text-amber-300', glow: '#fbbf24' },
  Shared: { bg: 'bg-slate-500/10', border: 'border-slate-500/50', text: 'text-slate-300', glow: '#94a3b8' },
}

export const DependencyGraph: React.FC<DependencyGraphProps> = ({
  graphData,
  symbols,
  violations
}) => {
  const [filterLayer, setFilterLayer] = useState<string>('All')
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null)
  const [violationsOnly, setViolationsOnly] = useState<boolean>(false)

  const selectedSymbol = symbols.find((s) => s.fullName === selectedNodeId || s.name === selectedNodeId)

  // Filter nodes
  const filteredNodes = graphData.nodes.filter((node) => {
    if (violationsOnly) {
      const hasOutgoingViolation = graphData.edges.some((e) => e.source === node.id && e.isViolation)
      const hasIncomingViolation = graphData.edges.some((e) => e.target === node.id && e.isViolation)
      return hasOutgoingViolation || hasIncomingViolation
    }
    if (filterLayer === 'All') return true
    return node.layer === filterLayer
  })

  const visibleNodeIds = new Set(filteredNodes.map((n) => n.id))

  // Filter edges where both source and target are visible
  const filteredEdges = graphData.edges.filter(
    (edge) => visibleNodeIds.has(edge.source) && visibleNodeIds.has(edge.target)
  )

  // Compute layout positions by layer
  const layers = ['Presentation', 'Application', 'Domain', 'Infrastructure']
  const nodesByLayer: Record<string, typeof filteredNodes> = {}
  layers.forEach((l) => (nodesByLayer[l] = []))

  filteredNodes.forEach((node) => {
    const l = layers.includes(node.layer) ? node.layer : 'Application'
    nodesByLayer[l].push(node)
  })

  const nodePositions: Record<string, { x: number; y: number }> = {}
  const layerYOffsets: Record<string, number> = {
    Presentation: 80,
    Application: 230,
    Domain: 380,
    Infrastructure: 530,
  }

  layers.forEach((layer) => {
    const layerNodes = nodesByLayer[layer]
    const count = layerNodes.length
    const spacing = 860 / Math.max(1, count + 1)
    layerNodes.forEach((node, idx) => {
      nodePositions[node.id] = {
        x: 60 + (idx + 1) * spacing,
        y: layerYOffsets[layer] || 250,
      }
    })
  })

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-2xl">
      {/* Graph Toolbar */}
      <div className="p-4 border-b border-slate-800 bg-slate-950/60 flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center space-x-2">
          <Layers className="w-5 h-5 text-sky-400" />
          <span className="font-semibold text-slate-200">Architectural Dependency Graph</span>
          <span className="text-xs px-2 py-0.5 rounded-full bg-slate-800 text-slate-400">
            {filteredNodes.length} symbols • {filteredEdges.length} edges
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Layer Filter buttons */}
          <div className="flex items-center bg-slate-900 p-1 rounded-xl border border-slate-800 text-xs">
            {['All', ...layers].map((layer) => (
              <button
                key={layer}
                onClick={() => {
                  setFilterLayer(layer)
                  setViolationsOnly(false)
                }}
                className={`px-2.5 py-1 rounded-lg font-medium transition-all ${
                  filterLayer === layer && !violationsOnly
                    ? 'bg-sky-500 text-slate-950 font-semibold'
                    : 'text-slate-400 hover:text-slate-200'
                }`}
              >
                {layer}
              </button>
            ))}
          </div>

          <button
            onClick={() => setViolationsOnly(!violationsOnly)}
            className={`flex items-center space-x-1.5 px-3 py-1.5 rounded-xl border text-xs font-semibold transition-all ${
              violationsOnly
                ? 'bg-rose-500/20 text-rose-300 border-rose-500/50'
                : 'bg-slate-900 text-slate-400 border-slate-800 hover:text-slate-200'
            }`}
          >
            <AlertTriangle className="w-3.5 h-3.5 text-rose-400" />
            <span>Violations Only ({violations.length})</span>
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 divide-y lg:divide-y-0 lg:divide-x divide-slate-800 min-h-[580px]">
        {/* SVG Canvas Area */}
        <div className="lg:col-span-2 relative bg-slate-950 p-4 overflow-auto">
          {/* Layer Swimlane Labels */}
          <div className="absolute left-4 top-0 bottom-0 flex flex-col justify-around pointer-events-none opacity-30 select-none text-[11px] font-bold uppercase tracking-wider text-slate-400">
            <div>Presentation Layer</div>
            <div>Application Layer</div>
            <div>Domain Layer</div>
            <div>Infrastructure Layer</div>
          </div>

          <svg viewBox="0 0 950 620" className="w-full h-[580px]">
            <defs>
              <marker id="arrow" viewBox="0 0 10 10" refX="22" refY="5" markerWidth="6" markerHeight="6" orient="auto">
                <path d="M 0 0 L 10 5 L 0 10 z" fill="#64748b" />
              </marker>
              <marker id="arrow-violation" viewBox="0 0 10 10" refX="22" refY="5" markerWidth="7" markerHeight="7" orient="auto">
                <path d="M 0 0 L 10 5 L 0 10 z" fill="#f43f5e" />
              </marker>
            </defs>

            {/* Render Edges */}
            {filteredEdges.map((edge, idx) => {
              const srcPos = nodePositions[edge.source]
              const tgtPos = nodePositions[edge.target]
              if (!srcPos || !tgtPos) return null

              const isViolation = edge.isViolation || edge.violationReason != null

              return (
                <g key={`edge-${idx}`}>
                  <line
                    x1={srcPos.x}
                    y1={srcPos.y}
                    x2={tgtPos.x}
                    y2={tgtPos.y}
                    stroke={isViolation ? '#f43f5e' : '#475569'}
                    strokeWidth={isViolation ? '2.5' : '1.5'}
                    strokeDasharray={isViolation ? '5,5' : 'none'}
                    markerEnd={isViolation ? 'url(#arrow-violation)' : 'url(#arrow)'}
                    className={isViolation ? 'animate-pulse' : ''}
                  />
                  {isViolation && (
                    <circle
                      cx={(srcPos.x + tgtPos.x) / 2}
                      cy={(srcPos.y + tgtPos.y) / 2}
                      r="4"
                      fill="#f43f5e"
                    />
                  )}
                </g>
              )
            })}

            {/* Render Nodes */}
            {filteredNodes.map((node) => {
              const pos = nodePositions[node.id] || { x: 100, y: 100 }
              const isSelected = selectedNodeId === node.id
              const hasViolation = graphData.edges.some((e) => (e.source === node.id || e.target === node.id) && e.isViolation)
              const colorInfo = LAYER_COLORS[node.layer] || LAYER_COLORS.Shared

              return (
                <g
                  key={node.id}
                  transform={`translate(${pos.x}, ${pos.y})`}
                  onClick={() => setSelectedNodeId(node.id)}
                  className="cursor-pointer group"
                >
                  {/* Halo glow when selected */}
                  {isSelected && (
                    <rect
                      x="-85"
                      y="-28"
                      width="170"
                      height="56"
                      rx="14"
                      fill={colorInfo.glow}
                      opacity="0.15"
                      stroke={colorInfo.glow}
                      strokeWidth="2"
                    />
                  )}

                  {/* Node Rect */}
                  <rect
                    x="-80"
                    y="-24"
                    width="160"
                    height="48"
                    rx="10"
                    fill="#111827"
                    stroke={hasViolation ? '#f43f5e' : isSelected ? colorInfo.glow : '#334155'}
                    strokeWidth={hasViolation ? '2' : isSelected ? '2' : '1'}
                    className="transition-all group-hover:stroke-sky-400"
                  />

                  {/* Role indicator pill */}
                  <text
                    x="-70"
                    y="-8"
                    fontSize="9"
                    fontWeight="bold"
                    fill={colorInfo.glow}
                    className="select-none uppercase"
                  >
                    {node.role} • {node.layer}
                  </text>

                  {/* Symbol Label */}
                  <text
                    x="-70"
                    y="12"
                    fontSize="12"
                    fontWeight="600"
                    fill="#f8fafc"
                    className="select-none"
                  >
                    {node.label.length > 18 ? node.label.substring(0, 16) + '..' : node.label}
                  </text>

                  {/* Violation badge */}
                  {hasViolation && (
                    <circle cx="70" cy="-14" r="6" fill="#f43f5e" />
                  )}
                </g>
              )
            })}
          </svg>
        </div>

        {/* Selected Node Details Drawer */}
        <div className="p-6 bg-slate-900/60 overflow-y-auto max-h-[580px]">
          {selectedSymbol ? (
            <div>
              <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                <span className="text-xs uppercase font-mono tracking-wider text-sky-400">Symbol Inspector</span>
                <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${LAYER_COLORS[selectedSymbol.currentLayer]?.bg} ${LAYER_COLORS[selectedSymbol.currentLayer]?.text}`}>
                  {selectedSymbol.currentLayer} Layer
                </span>
              </div>

              <div className="mt-4">
                <h3 className="text-lg font-bold text-white tracking-tight">{selectedSymbol.name}</h3>
                <p className="text-xs font-mono text-slate-400 mt-0.5 break-all">{selectedSymbol.fullName}</p>
                <div className="flex items-center space-x-2 mt-2">
                  <span className="text-xs px-2 py-0.5 bg-slate-800 rounded text-slate-300 font-mono">
                    {selectedSymbol.kind}
                  </span>
                  <span className="text-xs px-2 py-0.5 bg-sky-500/10 text-sky-400 rounded">
                    Confidence: {(selectedSymbol.roleConfidence * 100).toFixed(0)}%
                  </span>
                </div>
              </div>

              {/* File Path */}
              <div className="mt-4 p-2.5 bg-slate-950 rounded-xl border border-slate-800 flex items-center space-x-2 text-xs text-slate-300 font-mono">
                <FileCode className="w-4 h-4 text-slate-400 shrink-0" />
                <span className="truncate">{selectedSymbol.relativeFilePath}</span>
              </div>

              {/* Evidence */}
              {selectedSymbol.roleEvidence.length > 0 && (
                <div className="mt-4">
                  <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Classification Evidence</span>
                  <ul className="mt-2 space-y-1 text-xs text-slate-300">
                    {selectedSymbol.roleEvidence.map((ev, i) => (
                      <li key={i} className="flex items-start space-x-2">
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 mt-0.5 shrink-0" />
                        <span>{ev}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Injected Dependencies */}
              <div className="mt-4">
                <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                  Dependencies ({selectedSymbol.dependencies.length})
                </span>
                {selectedSymbol.dependencies.length > 0 ? (
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    {selectedSymbol.dependencies.map((dep, i) => (
                      <span key={i} className="text-xs px-2 py-1 bg-slate-950 border border-slate-800 rounded font-mono text-slate-300">
                        {dep}
                      </span>
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-slate-500 mt-1">No outward dependencies detected (Pure POCO).</p>
                )}
              </div>

              {/* Methods */}
              {selectedSymbol.methods.length > 0 && (
                <div className="mt-4">
                  <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                    Methods ({selectedSymbol.methods.length})
                  </span>
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    {selectedSymbol.methods.map((m, i) => (
                      <span key={i} className="text-xs px-2 py-0.5 bg-slate-800/80 rounded font-mono text-slate-300">
                        {m}()
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="h-full flex flex-col items-center justify-center text-center p-6 text-slate-500">
              <Info className="w-8 h-8 mb-2 opacity-40" />
              <p className="text-sm font-medium">Select a node to inspect</p>
              <p className="text-xs mt-1">Click any class or interface in the graph to view role evidence, dependencies, and Clean Architecture layering.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
