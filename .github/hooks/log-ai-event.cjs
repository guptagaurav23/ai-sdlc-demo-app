const fs = require("node:fs");
const path = require("node:path");

const event = JSON.parse(fs.readFileSync(0, "utf8"));

const logFile = path.join(
  __dirname,
  "../../docs/ai/AI-HOOK-LOG.jsonl"
);

const record = {
  timestamp: new Date().toISOString(),
  event: event.hook_event_name || "unknown",
  sessionId: event.sessionId || event.session_id || null,
  agentId: event.agentId || event.agent_id || null
};

if (event.prompt) {
  record.prompt = event.prompt;
}

if (event.toolName || event.tool_name) {
  record.tool = event.toolName || event.tool_name;
}

fs.mkdirSync(path.dirname(logFile), { recursive: true });

fs.appendFileSync(
  logFile,
  JSON.stringify(record) + "\n"
);

process.stdout.write("{}");