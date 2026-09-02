"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ScannerClient,
  type ScanColorMode,
  type ScanOptions,
  type ScanResult,
  type ScanSource,
  type ScannerCapabilities,
  type ScannerDevice,
} from "@scanner-sdk/client";

export default function Home() {
  const scanner = useMemo(() => new ScannerClient(), []);
  const [connected, setConnected] = useState(false);
  const [devices, setDevices] = useState<ScannerDevice[]>([]);
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [capabilities, setCapabilities] = useState<ScannerCapabilities | null>(null);
  const [dpi, setDpi] = useState(300);
  const [colorMode, setColorMode] = useState<ScanColorMode>("color");
  const [source, setSource] = useState<ScanSource>("flatbed");
  const [duplex, setDuplex] = useState(false);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setError(null);

    const isAvailable = await scanner.isAvailable();

    setConnected(isAvailable);

    if (!isAvailable) {
      setDevices([]);
      setSelectedDeviceId(null);
      setCapabilities(null);
      setScanResult(null);
      return;
    }

    try {
      const loadedDevices = await scanner.getDevices();

      setDevices(loadedDevices);

      const firstDevice = loadedDevices[0];

      if (!firstDevice) {
        setSelectedDeviceId(null);
        setCapabilities(null);
        setScanResult(null);
        return;
      }

      setSelectedDeviceId(firstDevice.id);

      const loadedCapabilities = await scanner.getCapabilities(firstDevice.id);

      setCapabilities(loadedCapabilities);
      applyCapabilitiesDefaults(loadedCapabilities);
    } catch (caught) {
      const message = caught instanceof Error ? caught.message : "Unable to load scanners";

      setError(message);
      setDevices([]);
      setSelectedDeviceId(null);
      setCapabilities(null);
      setScanResult(null);
    }
  }, [scanner]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  async function handleScan() {
    if (!selectedDeviceId) {
      return;
    }

    setIsScanning(true);
    setError(null);
    setScanResult(null);

    try {
      const scanOptions: ScanOptions = {
        deviceId: selectedDeviceId,
        dpi,
        colorMode,
        source,
        duplex,
        format: "pdf",
      };

      const result = await scanner.scan(scanOptions);

      setScanResult(result);
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "Scan failed"
      );
    } finally {
      setIsScanning(false);
    }
  }

  async function handleDeviceChange(
    deviceId: string
  ) {
    if (!deviceId) {
      setSelectedDeviceId(null);
      setCapabilities(null);
      setScanResult(null);
      setError(null);
      return;
    }

    setSelectedDeviceId(deviceId);
    setCapabilities(null);
    setScanResult(null);
    setError(null);

    try {
      const loadedCapabilities = await scanner.getCapabilities(deviceId);
      setCapabilities(loadedCapabilities);
      applyCapabilitiesDefaults(loadedCapabilities);
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "Unable to load scanner capabilities"
      );
    }
  }

  function applyCapabilitiesDefaults(
    capabilities: ScannerCapabilities
  ) {
    setDpi(capabilities.resolutions.includes(300) ? 300 : capabilities.resolutions[0] ?? 300);
    setColorMode(capabilities.colorModes[0] ?? "color");
    setSource(capabilities.sources[0] ?? "flatbed");
    setDuplex(false);
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col gap-8 px-6 py-10">
      <header>
        <h1 className="text-3xl font-semibold tracking-normal">
          Scanner SDK Playground
        </h1>

        <p className="mt-2 text-sm text-slate-600">
          Test scanner detection, capabilities and document scanning.
        </p>
      </header>
      <section className="rounded border border-slate-200 bg-white p-5">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-lg font-medium">
              Agent Status
            </h2>

            <p
              className={
                connected
                  ? "text-emerald-700"
                  : "text-rose-700"
              }
            >
              {connected ? "Connected" : "Disconnected"}
            </p>
          </div>

          <button
            className="rounded border border-slate-300 px-3 py-2 text-sm hover:bg-slate-50"
            onClick={() => void refresh()}
            type="button"
          >
            Refresh
          </button>
        </div>
      </section>
      <section className="rounded border border-slate-200 bg-white p-5">
        <h2 className="text-lg font-medium">
          Scanner
        </h2>
        <div className="mt-4">
          {devices.length === 0 ? (
            <p className="text-sm text-slate-600">
              No scanners found.
            </p>
          ) : (
            <label className="flex flex-col gap-2">
              <span className="text-sm font-medium text-slate-700">
                Available scanners
              </span>
              <select
                className="w-full rounded border border-slate-300 bg-white px-3 py-2 text-sm"
                value={selectedDeviceId ?? ""}
                onChange={(event) =>
                  void handleDeviceChange(
                    event.target.value
                  )
                }
              >
                {devices.map((device) => (
                  <option
                    key={device.id}
                    value={device.id}
                  >
                    {device.name} ({device.provider})
                  </option>
                ))}
              </select>
            </label>
          )}
        </div>
      </section>
      {capabilities ? (
        <section className="rounded border border-slate-200 bg-white p-5">
          <h2 className="text-lg font-medium">
            Scan configuration
          </h2>
          <p className="mt-1 text-sm text-slate-600">
            Configure the document before starting the scan.
          </p>
          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-2">
              <span className="text-sm font-medium text-slate-700">
                Resolution
              </span>
              <select
                className="rounded border border-slate-300 bg-white px-3 py-2 text-sm"
                value={dpi}
                onChange={(event) =>
                  setDpi(Number(event.target.value))
                }
              >
                {capabilities.resolutions.map(
                  (resolution) => (
                    <option
                      key={resolution}
                      value={resolution}
                    >
                      {resolution} DPI
                    </option>
                  )
                )}
              </select>
            </label>
            <label className="flex flex-col gap-2">
              <span className="text-sm font-medium text-slate-700">
                Color mode
              </span>
              <select
                className="rounded border border-slate-300 bg-white px-3 py-2 text-sm"
                value={colorMode}
                onChange={(event) =>
                  setColorMode(
                    event.target.value as ScanColorMode
                  )
                }
              >
                {capabilities.colorModes.map(
                  (mode) => (
                    <option
                      key={mode}
                      value={mode}
                    >
                      {mode}
                    </option>
                  )
                )}
              </select>
            </label>
            <label className="flex flex-col gap-2">
              <span className="text-sm font-medium text-slate-700">
                Source
              </span>

              <select
                className="rounded border border-slate-300 bg-white px-3 py-2 text-sm"
                value={source}
                onChange={(event) =>
                  setSource(
                    event.target.value as ScanSource
                  )
                }
              >
                {capabilities.sources.map(
                  (item) => (
                    <option
                      key={item}
                      value={item}
                    >
                      {item}
                    </option>
                  )
                )}
              </select>
            </label>
            <label className="flex items-center gap-3 self-end rounded border border-slate-200 px-3 py-2">
              <input
                type="checkbox"
                checked={duplex}
                disabled={!capabilities.duplex}
                onChange={(event) =>
                  setDuplex(event.target.checked)
                }
              />
              <div>
                <span className="text-sm font-medium text-slate-700">
                  Duplex
                </span>
                {!capabilities.duplex ? (
                  <p className="text-xs text-slate-500">
                    Not supported by this scanner.
                  </p>
                ) : null}
              </div>
            </label>
          </div>
          <button
            className="mt-6 w-full rounded bg-slate-900 px-4 py-3 text-sm font-medium text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-400"
            disabled={
              isScanning ||
              !selectedDeviceId ||
              !capabilities
            }
            onClick={() => void handleScan()}
            type="button"
          >
            {isScanning
              ? "Scanning..."
              : "Scan document"}
          </button>
        </section>
      ) : null}
      {scanResult ? (
        <section className="rounded border border-emerald-200 bg-emerald-50 p-5 text-emerald-900">
          <h2 className="text-lg font-medium">
            Scan result
          </h2>
          <p className="mt-2 text-sm">
            {scanResult.message ??
              `${scanResult.fileName} completed.`}
          </p>
        </section>
      ) : null}
      {error ? (
        <section className="rounded border border-rose-200 bg-rose-50 p-5 text-rose-900">
          <h2 className="font-medium">
            Error
          </h2>
          <p className="mt-1 text-sm">
            {error}
          </p>
        </section>
      ) : null}
    </main>
  );
}
