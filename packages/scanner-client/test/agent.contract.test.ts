import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import { once } from "node:events";
import { dirname, resolve } from "node:path";
import { setTimeout as delay } from "node:timers/promises";
import { fileURLToPath } from "node:url";
import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { ScannerClient } from "../src/index";

const agentUrl =
  process.env.SCANNER_AGENT_CONTRACT_URL ?? "http://127.0.0.1:17891";
const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const agentTargetFramework =
  process.env.SCANNER_AGENT_TFM ??
  (process.platform === "win32" ? "net10.0-windows" : "net10.0");
const agentAssembly = resolve(
  repoRoot,
  `apps/scanner-agent/bin/Debug/${agentTargetFramework}/ScannerAgent.dll`
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
          Scanner__UseMock: "true",
          ScannerAgent__Url: agentUrl,
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
        id: "mock-scanner-1",
        name: "Development Scanner",
        provider: "mock",
        status: "ready",
        capabilities: {
          resolutions: [200, 300],
          colorModes: ["color", "grayscale", "black-white"],
          sources: ["flatbed"],
          formats: ["pdf", "png", "jpeg"],
          duplex: false,
        },
      },
    ]);

    await expect(scanner.getCapabilities("mock-scanner-1")).resolves.toEqual(
      devices[0]?.capabilities
    );

    const base64Result = await scanner.scan({
      deviceId: "mock-scanner-1",
      dpi: 300,
      colorMode: "color",
      source: "flatbed",
      duplex: false,
      format: "pdf",
      outputMode: "base64",
    });

    expect(base64Result).toMatchObject({
      deviceId: "mock-scanner-1",
      status: "completed",
      format: "pdf",
      mimeType: "application/pdf",
      fileName: "mock-scan.pdf",
    });
    expect(base64Result.dataBase64).toBeTruthy();
    expect(base64Result.downloadUrl).toBeUndefined();

    const urlResult = await scanner.scan({
      deviceId: "mock-scanner-1",
      dpi: 300,
      colorMode: "color",
      source: "flatbed",
      duplex: false,
      format: "pdf",
      outputMode: "url",
    });

    expect(urlResult.dataBase64).toBeUndefined();
    expect(urlResult.downloadUrl).toMatch(/\/scans\/.+\/file$/);

    const scanFile = await scanner.getScanFile(urlResult.downloadUrl!);

    expect(scanFile.size).toBeGreaterThan(0);
    expect(scanFile.type).toBe(urlResult.mimeType);
  });

  it("surfaces scanner agent domain errors through the client", async () => {
    const scanner = new ScannerClient({ baseUrl: agentUrl });

    await expect(
      scanner.scan({
        deviceId: "mock-scanner-1",
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
