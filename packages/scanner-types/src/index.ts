export type ScannerStatus = "ready" | "busy" | "offline" | "unknown";

export type ScanColorMode = "color" | "grayscale" | "black-and-white";

export type ScanFormat = "pdf" | "png" | "jpeg";

export interface ScannerCapabilities {
  supportsDuplex: boolean;
  supportsAdf: boolean;
  colorModes: ScanColorMode[];
  formats: ScanFormat[];
  minDpi: number;
  maxDpi: number;
}

export interface ScannerDevice {
  id: string;
  name: string;
  provider: string;
  status: ScannerStatus;
  capabilities: ScannerCapabilities;
}

export interface ScanOptions {
  deviceId: string;
  dpi: number;
  colorMode: ScanColorMode;
  format: ScanFormat;
}

export interface ScanResult {
  id: string;
  deviceId: string;
  status: "completed" | "failed";
  format: ScanFormat;
  mimeType: string;
  fileName?: string;
  message?: string;
}
