import { z } from "zod";
import type {
  ScanOptions,
  ScannerCapabilities,
  ScannerDevice,
  ScanResult,
} from "@scanner-sdk/types";

export type {
  ScanColorMode,
  ScanFormat,
  ScanOutputMode,
  ScanOptions,
  ScanResult,
  ScanSource,
  ScannerCapabilities,
  ScannerDevice,
  ScannerStatus,
} from "@scanner-sdk/types";

export interface ScannerClientOptions {
  baseUrl?: string;
  fetchFn?: FetchFn;
}

type FetchFn = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

const scannerCapabilitiesSchema = z.object({
  resolutions: z.array(z.number()),
  colorModes: z.array(
    z.enum(["color", "grayscale", "black-white"])
  ),
  sources: z.array(
    z.enum(["flatbed", "feeder"])
  ),
  formats: z.array(
    z.enum(["pdf", "png", "jpeg"])
  ),
  duplex: z.boolean(),
});

const scannerDeviceSchema = z.object({
  id: z.string(),
  name: z.string(),
  provider: z.enum(["mock", "wia", "twain"]),
  status: z.enum([
    "ready",
    "busy",
    "offline",
    "unknown",
  ]),
  capabilities: scannerCapabilitiesSchema,
});

const scanResultSchema = z.object({
  id: z.string(),
  deviceId: z.string(),
  status: z.enum(["completed", "failed"]),
  format: z.enum(["pdf", "png", "jpeg"]),
  mimeType: z.string(),
  fileName: z.string().optional(),
  message: z.string().optional(),
  dataBase64: z.string().optional(),
  downloadUrl: z.string().optional(),
}).refine(
  (result) => !(result.dataBase64 && result.downloadUrl),
  "Scan result cannot include both dataBase64 and downloadUrl"
);

const healthSchema = z.object({
  status: z.string(),
  service: z.string(),
  version: z.string(),
});

export class ScannerClient {
  private readonly baseUrl: string;
  private readonly fetchFn: FetchFn;

  constructor(options: ScannerClientOptions = {}) {
    this.baseUrl = (options.baseUrl ?? "http://127.0.0.1:17890").replace(/\/+$/, "");
    this.fetchFn = options.fetchFn ?? ((input, init) => fetch(input, init));
  }

  async isAvailable(): Promise<boolean> {
    try {
      const response = await this.fetchFn(`${this.baseUrl}/health`);

      if (!response.ok) {
        return false;
      }

      const health = healthSchema.parse(await response.json());
      return health.status === "ready" && health.service === "scanner-agent";
    } catch {
      return false;
    }
  }

  async getDevices(): Promise<ScannerDevice[]> {
    const response = await this.fetchFn(`${this.baseUrl}/devices`);
    await ensureOk(response, "Unable to fetch scanner devices");

    return z.array(scannerDeviceSchema).parse(await response.json());
  }

  async scan(options: ScanOptions): Promise<ScanResult> {
    const response = await this.fetchFn(`${this.baseUrl}/scan`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(options),
    });
    await ensureOk(response, "Unable to start scan");

    return scanResultSchema.parse(await response.json());
  }

  async getScanFile(downloadUrl: string): Promise<Blob> {
    const response = await this.fetchFn(
      this.toAgentUrl(downloadUrl)
    );
    await ensureOk(response, "Unable to fetch scan file");

    return response.blob();
  }

  async downloadScan(scanResult: ScanResult): Promise<void> {
    if (!scanResult.downloadUrl) {
      throw new Error("Scan result does not include a downloadUrl");
    }

    if (typeof document === "undefined") {
      throw new Error("downloadScan is only available in browser environments");
    }

    const file = await this.getScanFile(scanResult.downloadUrl);
    const fileUrl = URL.createObjectURL(file);
    const link = document.createElement("a");

    link.href = fileUrl;
    link.download = scanResult.fileName ?? `scan.${scanResult.format}`;
    document.body.append(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(fileUrl);
  }

  async getCapabilities(
    deviceId: string
  ): Promise<ScannerCapabilities> {
    const response = await this.fetchFn(
      `${this.baseUrl}/devices/${encodeURIComponent(
        deviceId
      )}/capabilities`
    );
    await ensureOk(response, "Unable to load scanner capabilities");

    return scannerCapabilitiesSchema.parse(await response.json());
  }

  private toAgentUrl(url: string): string {
    if (/^https?:\/\//i.test(url)) {
      return url;
    }

    return `${this.baseUrl}${url.startsWith("/") ? "" : "/"}${url}`;
  }
}

async function ensureOk(response: Response, message: string): Promise<void> {
  if (!response.ok) {
    const body = await response.text().catch(() => "");
    throw new Error(
      body
        ? `${message}: ${response.status} ${body}`
        : `${message}: ${response.status}`
    );
  }
}
