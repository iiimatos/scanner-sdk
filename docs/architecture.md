# Architecture

Scanner SDK starts with a small hybrid architecture:

```text
Web Application
      ↓
@scanner-sdk/client
      ↓
HTTP
      ↓
localhost Scanner Agent
      ↓
IScannerProvider
      ↓
Mock / WIA / TWAIN
      ↓
Physical Scanner
```

The TypeScript client has no Windows-specific code. It only knows how to call the local agent.

The Scanner Agent owns scanner discovery and scan execution. API endpoints use `IScannerProvider`, so the mock provider can later be replaced by WIA or TWAIN without changing endpoint code.

The agent binds to `127.0.0.1:17890` to avoid exposing scanner control on external interfaces.

OpenAPI is enabled during development. The current SDK is handwritten, but the project is prepared for a later flow from C# contracts to OpenAPI to generated TypeScript types or clients.
