# Scanner SDK

Scanner SDK is a hybrid monorepo for web applications that need to communicate with physical scanners connected to a local Windows machine.

## Architecture

```text
Next.js / Web App
        ↓
@scanner-sdk/client
        ↓
HTTP
        ↓
Scanner Agent (.NET)
        ↓
IScannerProvider
        ↓
Mock / WIA / TWAIN
```

## Requirements

- Node.js
- pnpm
- .NET SDK
- Windows will be required for future WIA/TWAIN providers. The current mock provider runs cross-platform.

## Installation

```bash
pnpm install
dotnet restore ScannerSdk.sln
```

## Commands

```bash
pnpm dev
pnpm build
pnpm test
pnpm typecheck
pnpm agent:dev
pnpm agent:build
pnpm agent:test
```

## Running Locally

Start the Scanner Agent:

```bash
pnpm agent:dev
```

The agent listens only on:

```text
http://127.0.0.1:17890
```

Start the TypeScript workspace and playground:

```bash
pnpm dev
```

The playground uses `@scanner-sdk/client` to call the local agent.

## Structure

```text
apps/
  playground/
  scanner-agent/
  scanner-agent.Tests/
packages/
  scanner-client/
  scanner-types/
  config-typescript/
docs/
tests/
ScannerSdk.sln
```

## Initial Decisions

- `@scanner-sdk/client` is framework independent and uses `fetch`.
- `@scanner-sdk/types` owns the public TypeScript contracts.
- The .NET agent exposes Minimal API endpoints over HTTP.
- Endpoints depend on `IScannerProvider`, not concrete scanner implementations.
- `MockScannerProvider` enables development without scanner hardware.
- CORS allows only local playground origins during development.
- OpenAPI is exposed by the agent only in development for future generated clients.
