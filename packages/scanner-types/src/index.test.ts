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
        resolutions: [150, 200, 300, 600],
        colorModes: ["color", "grayscale", "black-white"],
        sources: ["flatbed", "feeder"],
        formats: ["pdf", "png", "jpeg"],
        duplex: true
      }
    };

    expect(device.provider).toBe("mock");
  });
});
