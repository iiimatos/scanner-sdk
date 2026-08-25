export type ScannerStatus = "ready" | "busy" | "offline" | "unknown";

export type ScannerProviderType =
  | "mock"
  | "wia"
  | "twain";

export type ScanColorMode =
  | "color"
  | "grayscale"
  | "black-white";

export type ScanSource =
  | "flatbed"
  | "feeder";

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
  provider: ScannerProviderType;
  manufacturer?: string;
  model?: string;
}

export interface ScannerCapabilities {
  resolutions: number[];
  colorModes: ScanColorMode[];
  sources: ScanSource[];
  duplex: boolean;
}

export interface ScanOptions {
  deviceId: string;
  dpi: number;
  colorMode: ScanColorMode;
  source: ScanSource;
  duplex: boolean;
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
