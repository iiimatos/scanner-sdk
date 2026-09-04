import { describe, expect, it } from "vitest";
import type { ScanOptions, ScannerDevice } from "./index";

describe("scanner types", () => {
  it("supports a strongly typed scanner device", () => {
    const device: ScannerDevice = {
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
    };

    expect(device.provider).toBe("mock");
  });

  it("supports selecting scan output mode", () => {
    const options: ScanOptions = {
      deviceId: "mock-scanner-1",
      dpi: 300,
      colorMode: "color",
      source: "flatbed",
      duplex: false,
      format: "pdf",
      outputMode: "url"
    };

    expect(options.outputMode).toBe("url");
  });
});
