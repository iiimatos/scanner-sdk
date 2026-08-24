import { describe, expect, it } from "vitest";
import type { ScannerDevice } from "./index";

describe("scanner types", () => {
  it("supports a strongly typed scanner device", () => {
    const device: ScannerDevice = {
      id: "mock-scanner-001",
      name: "Scanner SDK Virtual Scanner",
      provider: "mock",
      status: "ready",
      capabilities: {
        supportsAdf: false,
        supportsDuplex: false,
        colorModes: ["color", "grayscale", "black-and-white"],
        formats: ["pdf", "png", "jpeg"],
        minDpi: 75,
        maxDpi: 600
      }
    };

    expect(device.provider).toBe("mock");
  });
});
