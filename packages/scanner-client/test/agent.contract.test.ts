import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import { once } from "node:events";
import { dirname, resolve } from "node:path";
import { setTimeout as delay } from "node:timers/promises";
import { fileURLToPath } from "node:url";
import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { ScannerClient } from "../src/index";

const agentUrl = "http://127.0.0.1:17890";
const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const agentAssembly = resolve(
  repoRoot,
  "apps/scanner-agent/bin/Debug/net10.0/ScannerAgent.dll"
);

let agentProcess: ChildProcessWithoutNullStreams | undefined;
let startedAgent = false;

describe("Scanner Agent contract", () => {
  beforeAll(async () => {
    if (await isAgentAvailable()) {
      return;
    }

    agentProcess = spawn(
      "dotnet",
      [agentAssembly],
      {
        cwd: repoRoot,
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: "Testing",
        },
      }
    );
    startedAgent = true;

    let output = "";

    agentProcess.stdout.on("data", (chunk: Buffer) => {
      output += chunk.toString();
    });
    agentProcess.stderr.on("data", (chunk: Buffer) => {
      output += chunk.toString();
    });

    await waitForAgent(() => output);
  }, 45_000);

  afterAll(async () => {
    if (!startedAgent || !agentProcess || agentProcess.exitCode !== null) {
      return;
    }

    agentProcess.kill("SIGTERM");
    await Promise.race([
      once(agentProcess, "exit"),
      delay(5_000).then(() => agentProcess?.kill("SIGKILL")),
    ]);
  });

  it("matches the TypeScript client contract for the happy path", async () => {
    const scanner = new ScannerClient({ baseUrl: agentUrl });

    await expect(scanner.isAvailable()).resolves.toBe(true);

    const devices = await scanner.getDevices();

    expect(devices).toEqual([
      {
        id: "mock-scanner-001",
        name: "Scanner SDK Virtual Scanner",
        provider: "mock",
        status: "ready",
        capabilities: {
          resolutions: [150, 200, 300, 600],
          colorModes: ["color", "grayscale", "black-white"],
          sources: ["flatbed", "feeder"],
          formats: ["pdf", "png", "jpeg"],
          duplex: true,
        },
      },
    ]);

    await expect(scanner.getCapabilities("mock-scanner-001")).resolves.toEqual(
      devices[0]?.capabilities
    );

    await expect(
      scanner.scan({
        deviceId: "mock-scanner-001",
        dpi: 300,
        colorMode: "color",
        source: "flatbed",
        duplex: false,
        format: "pdf",
      })
    ).resolves.toMatchObject({
      deviceId: "mock-scanner-001",
      status: "completed",
      format: "pdf",
      mimeType: "application/pdf",
      fileName: "mock-scan.pdf",
    });
  });

  it("surfaces scanner agent domain errors through the client", async () => {
    const scanner = new ScannerClient({ baseUrl: agentUrl });

    await expect(
      scanner.scan({
        deviceId: "mock-scanner-001",
        dpi: 75,
        colorMode: "color",
        source: "flatbed",
        duplex: false,
        format: "pdf",
      })
    ).rejects.toThrow(/UNSUPPORTED_CAPABILITY/);

    await expect(scanner.getCapabilities("missing-scanner")).rejects.toThrow(
      /SCANNER_DEVICE_NOT_FOUND/
    );
  });
});

async function isAgentAvailable(): Promise<boolean> {
  return new ScannerClient({ baseUrl: agentUrl }).isAvailable();
}

async function waitForAgent(getOutput: () => string): Promise<void> {
  const deadline = Date.now() + 30_000;

  while (Date.now() < deadline) {
    if (await isAgentAvailable()) {
      return;
    }

    if (agentProcess?.exitCode !== null) {
      throw new Error(`Scanner Agent exited early.\n${getOutput()}`);
    }

    await delay(250);
  }

  throw new Error(`Scanner Agent did not become ready.\n${getOutput()}`);
}
