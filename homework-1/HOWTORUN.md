# How to Run AiCraft.Banking API

---

## 1. Prerequisites

**.NET 9 SDK** must be installed. Verify with:

```bash
dotnet --version
```

Expected output: `9.0.x` or higher.

### Installing .NET 9 SDK

**Windows**

Option 1 — Installer (recommended):
1. Go to [https://dot.net/download](https://dot.net/download) and select **.NET 9 → SDK → Windows x64**.
2. Download and run the `.exe` installer.
3. Open a new terminal and run `dotnet --version` to confirm.

Option 2 — winget:
```powershell
winget install Microsoft.DotNet.SDK.9
```

Option 3 — Chocolatey:
```powershell
choco install dotnet-sdk --version=9.0.0
```

---

**macOS**

Option 1 — Installer (recommended):
1. Go to [https://dot.net/download](https://dot.net/download) and select **.NET 9 → SDK → macOS**.
2. Choose **Arm64** (Apple Silicon) or **x64** (Intel) to match your Mac.
3. Download and run the `.pkg` installer.
4. Open a new terminal and run `dotnet --version` to confirm.

Option 2 — Homebrew:
```bash
brew install --cask dotnet-sdk
```

> If Homebrew installs a different version, use the tap directly:
> ```bash
> brew tap isen-ng/dotnet-sdk-versions
> brew install --cask dotnet-sdk9
> ```

After installation on macOS, if `dotnet` is not found, add it to your PATH:
```bash
export PATH="$PATH:/usr/local/share/dotnet"
```

Add that line to `~/.zshrc` (or `~/.bash_profile`) to make it permanent.

---

## 2. Running the Application

From the repository root, run:

```bash
cd homework-1/src/AiCraft.Banking
dotnet run
```

Or using the provided script from `homework-1/demo/`:

```bat
demo\run.bat
```

Expected console output:

```
info: AiCraft.Banking.Services.DataSeeder[0]
      Seeded 10 transactions from sample-data.json.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5092
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

The API is ready when you see `Now listening on: http://localhost:5092`.

---

## 3. Swagger UI

Open in a browser:

```
http://localhost:5092/swagger
```

**To test an endpoint:**

1. Click on any endpoint row (e.g. `GET /transactions`) to expand it.
2. Click **Try it out** in the top-right of the expanded panel.
3. Fill in any parameters (all are optional for `GET /transactions`).
4. Click **Execute**.
5. The response body, status code, and headers appear below.

> **Seed data is preloaded.** `GET /transactions` returns 10 transactions immediately
> without needing to create any first. The seed data is loaded from
> `demo/sample-data.json` at startup via `DataSeeder`.

---

## 4. Testing Endpoints Manually

### Option A — VS Code REST Client

Open `demo/sample-requests.http` in VS Code with the
[REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) installed.
Click **Send Request** above any request block.

### Option B — curl

**Create a transaction (Transfer):**

```bash
curl -X POST http://localhost:5092/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "fromAccount": "ACC-10001",
    "toAccount":   "ACC-20002",
    "amount":      250.00,
    "currency":    "USD",
    "type":        "Transfer"
  }'
```

**Create a transaction (Deposit, no fromAccount required):**

```bash
curl -X POST http://localhost:5092/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "toAccount": "ACC-10001",
    "amount":    500.00,
    "currency":  "EUR",
    "type":      "Deposit"
  }'
```

**List all transactions:**

```bash
curl http://localhost:5092/transactions
```

**Filter by account:**

```bash
curl "http://localhost:5092/transactions?accountId=ACC-10001"
```

**Filter by type:**

```bash
curl "http://localhost:5092/transactions?type=Transfer"
```

**Filter by date range:**

```bash
curl "http://localhost:5092/transactions?from=2026-01-01&to=2026-04-30"
```

**Combine filters:**

```bash
curl "http://localhost:5092/transactions?accountId=ACC-10001&type=Transfer&from=2026-01-01"
```

**Get transaction by ID** (replace with a real ID from the list response):

```bash
curl http://localhost:5092/transactions/00000000-0000-0000-0000-000000000001
```

**Get account balance:**

```bash
curl http://localhost:5092/accounts/ACC-10001/balance
```

**Get account summary:**

```bash
curl http://localhost:5092/accounts/ACC-10001/summary
```

**Trigger a validation error** (negative amount, unsupported currency, transfer to self):

```bash
curl -X POST http://localhost:5092/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "fromAccount": "ACC-10001",
    "toAccount":   "ACC-10001",
    "amount":      -10,
    "currency":    "XYZ",
    "type":        "Transfer"
  }'
```

Expected: `400 Bad Request` with field-level error details.

---

## 5. Running the Build

```bash
cd homework-1/src/AiCraft.Banking
dotnet build
```

Clean output looks like:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 6. Common Issues

### Port already in use

```
Failed to bind to address http://localhost:5092: address already in use.
```

**Find and kill the process using the port:**

```bash
# Linux / macOS
lsof -ti :5092 | xargs kill -9

# Windows (PowerShell)
Get-NetTCPConnection -LocalPort 5092 | Select-Object -ExpandProperty OwningProcess | ForEach-Object { Stop-Process -Id $_ -Force }
```

**Or change the port** in [src/AiCraft.Banking/Properties/launchSettings.json](src/AiCraft.Banking/Properties/launchSettings.json):

```json
"applicationUrl": "http://localhost:5099"
```

Update the port in `demo/sample-requests.http` to match.
