# Development

Install dependencies:

```bash
pnpm install
dotnet restore ScannerSdk.sln
```

Run the Scanner Agent:

```bash
pnpm agent:dev
```

Run the TypeScript workspace:

```bash
pnpm dev
```

Run checks:

```bash
pnpm typecheck
pnpm build
pnpm test
pnpm agent:build
pnpm agent:test
```

The playground must call the agent through `@scanner-sdk/client`; it should not call `fetch` directly for scanner operations.
