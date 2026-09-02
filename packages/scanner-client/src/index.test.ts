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
          resolutions: [150, 200, 300, 600],
          colorModes: ["color", "grayscale", "black-white"],
          sources: ["flatbed", "feeder"],
          formats: ["pdf", "png", "jpeg"],
          duplex: true
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
      source: "flatbed",
      duplex: false,
      format: "pdf"
    })).resolves.toMatchObject({ status: "completed" });
  });

  it("loads capabilities with the configured fetch implementation", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      resolutions: [150, 200, 300, 600],
      colorModes: ["color", "grayscale", "black-white"],
      sources: ["flatbed", "feeder"],
      formats: ["pdf", "png", "jpeg"],
      duplex: true
    }));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.getCapabilities("mock scanner/id")).resolves.toMatchObject({
      resolutions: [150, 200, 300, 600],
      duplex: true
    });
    expect(fetchFn).toHaveBeenCalledWith(
      "http://127.0.0.1:17890/devices/mock%20scanner%2Fid/capabilities"
    );
  });
});
