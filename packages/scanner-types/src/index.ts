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
  resolutions: number[];
  colorModes: ScanColorMode[];
  sources: ScanSource[];
  formats: ScanFormat[];
  duplex: boolean;
}

export interface ScannerDevice {
  id: string;
  name: string;
  provider: ScannerProviderType;
  status: ScannerStatus;
  capabilities: ScannerCapabilities;
  manufacturer?: string;
  model?: string;
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
