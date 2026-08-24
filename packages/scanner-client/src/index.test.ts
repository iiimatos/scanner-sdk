import { describe, expect, it, vi } from "vitest";
import { ScannerClient } from "./index";

describe("ScannerClient", () => {
  it("checks agent availability", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      status: "ready",
      service: "scanner-agent",
      version: "0.1.0"
    }));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.isAvailable()).resolves.toBe(true);
    expect(fetchFn).toHaveBeenCalledWith("http://127.0.0.1:17890/health");
  });

  it("loads devices from the agent", async () => {
    const fetchFn = vi.fn(async () => Response.json([
      {
        id: "mock-scanner-001",
        name: "Scanner SDK Virtual Scanner",
        provider: "mock",
        status: "ready",
        capabilities: {
          supportsAdf: false,
          supportsDuplex: false,
          colorModes: ["color"],
          formats: ["pdf"],
          minDpi: 75,
          maxDpi: 600
        }
      }
    ]));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.getDevices()).resolves.toHaveLength(1);
  });

  it("posts scan options", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      id: "scan_mock-scanner-001_001",
      deviceId: "mock-scanner-001",
      status: "completed",
      format: "pdf",
      mimeType: "application/pdf",
      fileName: "mock-scan.pdf"
    }));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.scan({
      deviceId: "mock-scanner-001",
      dpi: 300,
      colorMode: "color",
      format: "pdf"
    })).resolves.toMatchObject({ status: "completed" });
  });
});
