# TrackerSimulator

[🇧🇷 Português](#-português) | [🇺🇸 English](#-english)

---

## 🇧🇷 Português

### Sobre o Projeto

**TrackerSimulator** é uma solução em **.NET 11** que simula o tráfego de telemetria de uma frota de rastreadores GPS e exibe a movimentação em tempo real em um dashboard web com mapa interativo.

O sistema é composto por dois serviços principais desacoplados:

1. **`TrackerSimulator.Worker`**: um *Worker Service* (`BackgroundService`) que simula múltiplos rastreadores concorrentes com estado individual (posição geográfica contínua por velocidade e direção, ignição, bateria e odômetro) e envia pacotes JSON via `HTTP POST`.
2. **`TrackerSimulator.Web`**: um backend **ASP.NET Core + Blazor Web App** que recebe os pacotes em `POST /api/telemetry`, mantém o estado atual e o rastro recente em memória, retransmite as atualizações em tempo real via **SignalR** (`/hubs/telemetry`) e renderiza os dispositivos no mapa com **Leaflet + OpenStreetMap**.

### Arquitetura

```text
TrackerSimulator.Worker  ──(HTTP POST /api/telemetry)──▶  TrackerSimulator.Web
                                                                │
                                                  (SignalR /hubs/telemetry)
                                                                ▼
                                                     Dashboard Blazor + Leaflet
```

### Stack Utilizada

- **.NET 11 / C#**
- **Worker Service** (`Microsoft.Extensions.Hosting`, `BackgroundService`, `IHttpClientFactory`)
- **ASP.NET Core** (Minimal APIs, `IOptions` com validação por `DataAnnotations`)
- **Blazor Web App** (`InteractiveServer`)
- **SignalR** (atualizações em tempo real)
- **Leaflet.js + OpenStreetMap** (visualização de mapa e trajetos)
- **xUnit** (testes unitários)

### Estrutura da Solução

- `src/TrackerSimulator.Contracts`: contrato compartilhado (`TrackerTelemetry`).
- `src/TrackerSimulator.Worker`: simulador de frota e publicador HTTP.
- `src/TrackerSimulator.Web`: API de ingestão, store em memória, Hub SignalR e dashboard Blazor.
- `tests/TrackerSimulator.Tests`: testes unitários do simulador, publicador HTTP e store em memória.

### Configuração (`appsettings.json`)

As configurações do simulador ficam em `src/TrackerSimulator.Worker/appsettings.json` na seção `Simulation`:

- `DeviceCount`: quantidade de rastreadores simulados (padrão: `10`).
- `IntervalMs` / `JitterMs`: intervalo de envio e variação aleatória entre pacotes.
- `MaxConcurrentRequests`: limite de requisições HTTP simultâneas.
- `EndpointUrl`: URL de destino da API (padrão: `http://localhost:5100/api/telemetry`).
- `DryRun`: quando `true`, registra apenas nos logs sem enviar requisições HTTP.
- `BaseLatitude` / `BaseLongitude`: coordenadas iniciais da frota.

### Como Executar

No diretório raiz do projeto, abra dois terminais:

**Terminal 1 — Backend + Dashboard Blazor:**
```powershell
dotnet run --project src/TrackerSimulator.Web
```
Acesse `http://localhost:5100` no navegador.

**Terminal 2 — Simulador de Rastreadores:**
```powershell
dotnet run --project src/TrackerSimulator.Worker
```

**Executar testes:**
```powershell
dotnet test
```

---

## 🇺🇸 English

### About the Project

**TrackerSimulator** is a **.NET 11** solution that simulates GPS tracker fleet telemetry traffic and displays live movement on an interactive web map dashboard.

The solution consists of two decoupled services:

1. **`TrackerSimulator.Worker`**: a .NET *Worker Service* (`BackgroundService`) that simulates multiple concurrent trackers with per-device state (continuous geographic movement based on speed and heading, ignition state, battery level, and odometer) and dispatches JSON packets via `HTTP POST`.
2. **`TrackerSimulator.Web`**: an **ASP.NET Core + Blazor Web App** backend that ingests packets at `POST /api/telemetry`, stores the latest state and recent trail in memory, broadcasts real-time updates over **SignalR** (`/hubs/telemetry`), and renders live markers and trails on a **Leaflet + OpenStreetMap** map.

### Architecture

```text
TrackerSimulator.Worker  ──(HTTP POST /api/telemetry)──▶  TrackerSimulator.Web
                                                                │
                                                  (SignalR /hubs/telemetry)
                                                                ▼
                                                     Blazor + Leaflet Dashboard
```

### Tech Stack

- **.NET 11 / C#**
- **Worker Service** (`Microsoft.Extensions.Hosting`, `BackgroundService`, `IHttpClientFactory`)
- **ASP.NET Core** (Minimal APIs, `IOptions` with `DataAnnotations` validation)
- **Blazor Web App** (`InteractiveServer`)
- **SignalR** (real-time telemetry broadcasting)
- **Leaflet.js + OpenStreetMap** (interactive map and device trails)
- **xUnit** (unit testing)

### Solution Structure

- `src/TrackerSimulator.Contracts`: shared telemetry contract (`TrackerTelemetry`).
- `src/TrackerSimulator.Worker`: fleet simulator and HTTP telemetry publisher.
- `src/TrackerSimulator.Web`: ingestion API, in-memory store, SignalR Hub, and Blazor dashboard.
- `tests/TrackerSimulator.Tests`: unit tests for the simulator, HTTP publisher, and telemetry store.

### Configuration (`appsettings.json`)

Simulation settings are located in `src/TrackerSimulator.Worker/appsettings.json` under `Simulation`:

- `DeviceCount`: number of simulated trackers (default: `10`).
- `IntervalMs` / `JitterMs`: dispatch interval and random per-tick jitter.
- `MaxConcurrentRequests`: maximum concurrent HTTP requests.
- `EndpointUrl`: target API endpoint (default: `http://localhost:5100/api/telemetry`).
- `DryRun`: when `true`, logs telemetry payloads without making HTTP requests.
- `BaseLatitude` / `BaseLongitude`: initial fleet coordinates.

### How to Run

From the repository root, open two terminals:

**Terminal 1 — Backend + Blazor Dashboard:**
```powershell
dotnet run --project src/TrackerSimulator.Web
```
Open `http://localhost:5100` in your browser.

**Terminal 2 — Tracker Simulator:**
```powershell
dotnet run --project src/TrackerSimulator.Worker
```

**Run tests:**
```powershell
dotnet test
```

---

## Autor / Author

**Rui De Moraes**  
GitHub: [@ruiapmoraes](https://github.com/ruiapmoraes)
