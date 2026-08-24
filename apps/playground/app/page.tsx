"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { ScannerClient, type ScanResult, type ScannerDevice } from "@scanner-sdk/client";

export default function Home() {
  const scanner = useMemo(() => new ScannerClient(), []);
  const [connected, setConnected] = useState(false);
  const [devices, setDevices] = useState<ScannerDevice[]>([]);
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isScanning, setIsScanning] = useState(false);

  const refresh = useCallback(async () => {
    setError(null);
    const isAvailable = await scanner.isAvailable();
    setConnected(isAvailable);

    if (!isAvailable) {
      setDevices([]);
      return;
    }

    try {
      setDevices(await scanner.getDevices());
    } catch (caught) {
      const message = caught instanceof Error ? caught.message : "Unable to load scanners";
      setError(message);
    }
  }, [scanner]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  async function handleScan() {
    const device = devices[0];

    if (!device) {
      return;
    }

    setIsScanning(true);
    setError(null);
    setScanResult(null);

    try {
      const scanOptions = {
        deviceId: device.id,
        dpi: 300,
        colorMode: "color",
        format: "pdf"
      } as const;

      setScanResult(await scanner.scan(scanOptions));
    } catch (caught) {
      const message = caught instanceof Error ? caught.message : "Scan failed";
      setError(message);
    } finally {
      setIsScanning(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col gap-8 px-6 py-10">
      <header>
        <h1 className="text-3xl font-semibold tracking-normal">Scanner SDK Playground</h1>
      </header>

      <section className="rounded border border-slate-200 bg-white p-5">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-lg font-medium">Agent Status</h2>
            <p className={connected ? "text-emerald-700" : "text-rose-700"}>
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
        <h2 className="text-lg font-medium">Available scanners</h2>
        <div className="mt-4 space-y-3">
          {devices.length === 0 ? (
            <p className="text-sm text-slate-600">No scanners found.</p>
          ) : (
            devices.map((device) => (
              <div className="flex items-center justify-between rounded border border-slate-200 p-3" key={device.id}>
                <div>
                  <p className="font-medium">{device.name}</p>
                  <p className="text-sm text-slate-600">{device.provider}</p>
                </div>
                <button
                  className="rounded bg-slate-900 px-4 py-2 text-sm text-white disabled:cursor-not-allowed disabled:bg-slate-400"
                  disabled={isScanning}
                  onClick={() => void handleScan()}
                  type="button"
                >
                  {isScanning ? "Scanning" : "Scan"}
                </button>
              </div>
            ))
          )}
        </div>
      </section>

      {scanResult ? (
        <section className="rounded border border-emerald-200 bg-emerald-50 p-5 text-emerald-900">
          <h2 className="text-lg font-medium">Scan result</h2>
          <p className="mt-2 text-sm">{scanResult.message ?? `${scanResult.fileName} completed.`}</p>
        </section>
      ) : null}

      {error ? (
        <section className="rounded border border-rose-200 bg-rose-50 p-5 text-rose-900">
          <p className="text-sm">{error}</p>
        </section>
      ) : null}
    </main>
  );
}
