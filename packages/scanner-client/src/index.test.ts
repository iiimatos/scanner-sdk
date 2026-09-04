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
        id: "mock-scanner-1",
        name: "Development Scanner",
        provider: "mock",
        status: "ready",
        capabilities: {
          resolutions: [200, 300],
          colorModes: ["color", "grayscale", "black-white"],
          sources: ["flatbed"],
          formats: ["pdf", "png", "jpeg"],
          duplex: false
        }
      }
    ]));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.getDevices()).resolves.toHaveLength(1);
  });

  it("posts scan options", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      id: "scan_mock-scanner-1_001",
      deviceId: "mock-scanner-1",
      status: "completed",
      format: "pdf",
      mimeType: "application/pdf",
      fileName: "mock-scan.pdf",
      dataBase64: "JVBERi0xLjQ="
    }));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.scan({
      deviceId: "mock-scanner-1",
      dpi: 300,
      colorMode: "color",
      source: "flatbed",
      duplex: false,
      format: "pdf",
      outputMode: "base64"
    })).resolves.toMatchObject({
      status: "completed",
      dataBase64: "JVBERi0xLjQ="
    });
    expect(fetchFn).toHaveBeenCalledWith("http://127.0.0.1:17890/scan", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        deviceId: "mock-scanner-1",
        dpi: 300,
        colorMode: "color",
        source: "flatbed",
        duplex: false,
        format: "pdf",
        outputMode: "base64",
      }),
    });
  });

  it("accepts scan results with a download URL instead of base64", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      id: "scan_mock-scanner-1_001",
      deviceId: "mock-scanner-1",
      status: "completed",
      format: "pdf",
      mimeType: "application/pdf",
      fileName: "mock-scan.pdf",
      downloadUrl: "http://127.0.0.1:17890/scans/scan_mock-scanner-1_001/file"
    }));
    const scanner = new ScannerClient({ fetchFn });

    const result = await scanner.scan({
      deviceId: "mock-scanner-1",
      dpi: 300,
      colorMode: "color",
      source: "flatbed",
      duplex: false,
      format: "pdf",
      outputMode: "url"
    });

    expect(result.status).toBe("completed");
    expect(result.dataBase64).toBeUndefined();
    expect(result.downloadUrl).toBe(
      "http://127.0.0.1:17890/scans/scan_mock-scanner-1_001/file"
    );
  });

  it("rejects scan results that include base64 and download URL together", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      id: "scan_mock-scanner-1_001",
      deviceId: "mock-scanner-1",
      status: "completed",
      format: "pdf",
      mimeType: "application/pdf",
      fileName: "mock-scan.pdf",
      dataBase64: "JVBERi0xLjQ=",
      downloadUrl: "http://127.0.0.1:17890/scans/scan_mock-scanner-1_001/file"
    }));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.scan({
      deviceId: "mock-scanner-1",
      dpi: 300,
      colorMode: "color",
      source: "flatbed",
      duplex: false,
      format: "pdf"
    })).rejects.toThrow(/dataBase64 and downloadUrl/);
  });

  it("downloads a scan file", async () => {
    const scanFile = new Blob(["mock scan"], {
      type: "application/pdf"
    });
    const fetchFn = vi.fn(async () => new Response(scanFile));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.getScanFile("/scans/scan-1/file")).resolves.toMatchObject({
      size: scanFile.size,
      type: "application/pdf"
    });
    expect(fetchFn).toHaveBeenCalledWith(
      "http://127.0.0.1:17890/scans/scan-1/file"
    );
  });

  it("loads capabilities with the configured fetch implementation", async () => {
    const fetchFn = vi.fn(async () => Response.json({
      resolutions: [200, 300],
      colorModes: ["color", "grayscale", "black-white"],
      sources: ["flatbed"],
      formats: ["pdf", "png", "jpeg"],
      duplex: false
    }));
    const scanner = new ScannerClient({ fetchFn });

    await expect(scanner.getCapabilities("mock scanner/id")).resolves.toMatchObject({
      resolutions: [200, 300],
      duplex: false
    });
    expect(fetchFn).toHaveBeenCalledWith(
      "http://127.0.0.1:17890/devices/mock%20scanner%2Fid/capabilities"
    );
  });
});
