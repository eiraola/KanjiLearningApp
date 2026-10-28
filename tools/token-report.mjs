#!/usr/bin/env node
// Summarises Claude Code token usage per agent from the local transcripts of this project.
// Usage: node tools/token-report.mjs [--since 2026-10-22T10:00:00Z] [--dir <transcripts dir>] [--json]

import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const args = process.argv.slice(2);
const opt = (name) => {
  const i = args.indexOf(name);
  return i >= 0 ? args[i + 1] : undefined;
};

const projectDir = process.cwd();
// Claude Code stores transcripts under ~/.claude/projects/<cwd with non-alphanumerics replaced by '-'>
const slug = projectDir.replace(/[^a-zA-Z0-9]/g, "-");
const configDir = process.env.CLAUDE_CONFIG_DIR || path.join(os.homedir(), ".claude");
const dir = opt("--dir") || path.join(configDir, "projects", slug);
const since = opt("--since") ? new Date(opt("--since")) : new Date(0);

if (!fs.existsSync(dir)) {
  console.error(`No transcripts found at ${dir}`);
  process.exit(1);
}

function* walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) yield* walk(p);
    else if (e.name.endsWith(".jsonl")) yield p;
  }
}

function agentNameFor(file, firstLines) {
  const meta = file.replace(/\.jsonl$/, ".meta.json");
  if (fs.existsSync(meta)) {
    try {
      const m = JSON.parse(fs.readFileSync(meta, "utf8"));
      if (m.agentType || m.subagent_type) return m.agentType || m.subagent_type;
    } catch {}
  }
  for (const l of firstLines) {
    if (l.agentType || l.subagent_type) return l.agentType || l.subagent_type;
  }
  return path.basename(file).startsWith("agent-") ? path.basename(file, ".jsonl") : "orquestador";
}

const totals = new Map();
const seen = new Set();

for (const file of walk(dir)) {
  if (fs.statSync(file).mtime < since) continue;
  const lines = fs
    .readFileSync(file, "utf8")
    .split("\n")
    .filter(Boolean)
    .map((l) => {
      try { return JSON.parse(l); } catch { return null; }
    })
    .filter(Boolean);
  const fileAgent = agentNameFor(file, lines.slice(0, 5));

  for (const l of lines) {
    const msg = l.message;
    if (l.type !== "assistant" || !msg?.usage) continue;
    if (l.timestamp && new Date(l.timestamp) < since) continue;
    const key = msg.id || l.uuid;
    if (seen.has(key)) continue;
    seen.add(key);

    // Older transcripts keep subagent turns in the main file, flagged as sidechain.
    const agent = l.isSidechain ? l.agentType || (l.agentId ? `agent-${l.agentId}` : "subagente") : fileAgent;
    const u = msg.usage;
    const t = totals.get(agent) || { turns: 0, input: 0, cacheWrite: 0, cacheRead: 0, output: 0, models: new Set() };
    t.turns++;
    t.input += u.input_tokens || 0;
    t.cacheWrite += u.cache_creation_input_tokens || 0;
    t.cacheRead += u.cache_read_input_tokens || 0;
    t.output += u.output_tokens || 0;
    if (msg.model) t.models.add(msg.model);
    totals.set(agent, t);
  }
}

const rows = [...totals.entries()]
  .map(([agent, t]) => ({ agent, ...t, models: [...t.models].join(","), total: t.input + t.cacheWrite + t.cacheRead + t.output }))
  .sort((a, b) => b.total - a.total);

if (args.includes("--json")) {
  console.log(JSON.stringify(rows, null, 2));
} else {
  const fmt = (n) => n.toLocaleString("es-ES");
  console.log(`Transcripciones: ${dir}\nDesde: ${since.toISOString()}\n`);
  console.log(["Agente", "Turnos", "Input", "Cache write", "Cache read", "Output", "Total", "Modelos"].join("\t"));
  for (const r of rows) {
    console.log([r.agent, r.turns, fmt(r.input), fmt(r.cacheWrite), fmt(r.cacheRead), fmt(r.output), fmt(r.total), r.models].join("\t"));
  }
  const sum = rows.reduce((a, r) => a + r.total, 0);
  console.log(`\nTotal: ${fmt(sum)} tokens (cache read es mucho más barato que input/output; prioriza reducir output y cache write).`);
}
