
const fs = require("node:fs");
const path = require("node:path");

let raw = "";

process.stdin.setEncoding("utf8");

process.stdin.on("data", chunk => {
  raw += chunk;
});

process.stdin.on("end", () => {
  try {
    const event = JSON.parse(raw);

    let eventName = event.hook_event_name || event.event || "unknown";

    if (eventName === "unknown") {
      if (event.prompt) {
        eventName = "userPromptSubmitted";
      } else if (event.tool_name || event.toolName) {
        eventName = "toolUse";
      }
    }

    const record = {
      timestamp: event.timestamp || new Date().toISOString(),
      event: eventName,
      sessionId: event.session_id || event.sessionId || null,
      agentId: event.agent_id || event.agentId || null,
      prompt: event.prompt || undefined,
      tool: event.tool_name || event.toolName || undefined
    };

    const logFile = path.join(
      __dirname,
      "../../docs/ai/AI-HOOK-LOG.jsonl"
    );

    fs.mkdirSync(path.dirname(logFile), { recursive: true });

    fs.appendFileSync(
      logFile,
      JSON.stringify(record) + "\n"
    );

    process.stdout.write("{}");
  } catch (error) {
    process.stderr.write("AI audit hook error: " + error.message);
    process.stdout.write("{}");
  }
});
