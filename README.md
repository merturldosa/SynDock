# SynDock.Shop

Multi-tenant SaaS e-commerce platform (MakeShop-style) built on .NET 8 Clean Architecture and Next.js. Supports multiple independent storefronts on a shared database with row-level security, a full commerce stack from cart through payment settlement, and AI-powered merchandising features.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Tech Stack](#tech-stack)
3. [Architecture](#architecture)
4. [Features](#features)
5. [Prerequisites](#prerequisites)
6. [Quick Start (Docker)](#quick-start-docker)
7. [Manual Setup](#manual-setup)
8. [Environment Variables](#environment-variables)
9. [Port Assignments](#port-assignments)
10. [Project Structure](#project-structure)
11. [Testing](#testing)
12. [Deployment](#deployment)
13. [API Documentation](#api-documentation)
14. [Tenants](#tenants)
15. [License](#license)

---

## Project Overview

SynDock.Shop is a multi-tenant commerce platform where each tenant (storefront) is isolated at the row level within a single shared PostgreSQL database. Tenant resolution happens at request time via HTTP header, custom domain, or subdomain. A `PlatformAdmin` role governs the platform; each store is managed by a `TenantAdmin`.

Key characteristics:

- Shared-DB + Row-Level Security via EF Core Global Query Filters on `ITenantEntity`
- RBAC with four roles: `Member`, `TenantAdmin`, `Admin`, `PlatformAdmin`
- Full commerce flow: catalog, cart, checkout, TossPayments integration, settlement
- AI features: Claude chatbot, Holt-Winters demand forecast, product recommendations, image generation
- MES (Manufacturing Execution System) bridge for inventory reservation and production planning
- Internationalization: 5 languages (ko, en, ja, zh-CN, vi), GeoIP auto-detection, multi-currency (KRW/USD/EUR/JPY/CNY/VND)

---

## Tech Stack

### Backend

| Layer          | Technology                                     |
|----------------|------------------------------------------------|
| Runtime        | .NET 8                                         |
| Framework      | ASP.NET Core Web API                           |
| ORM            | Entity Framework Core 8 + Npgsql               |
| CQRS           | MediatR 12                                     |
| Validation     | FluentValidation 11                            |
| Authentication | JWT Bearer + BCrypt + TOTP (2FA)               |
| OAuth          | Google, Kakao, Naver                           |
| Caching        | Redis (StackExchange.Redis)                    |
| Messaging      | SignalR (real-time admin dashboard)            |
| Logging        | Serilog (file + console, PII masking)          |
| Monitoring     | Prometheus + Grafana, Sentry error tracking    |
| PDF            | QuestPDF                                       |
| Image          | SixLabors.ImageSharp (WebP conversion)         |
| GeoIP          | MaxMind GeoIP2                                 |
| Email          | MailKit                                        |
| Encryption     | AES-256 (EncryptedStringConverter on PII fields) |

### Frontend (Web)

| Technology     | Version  | Purpose                              |
|----------------|----------|--------------------------------------|
| Next.js        | 16       | App router, SSR/SSG, standalone output |
| React          | 19       | UI framework                         |
| TypeScript     | 5.9      | Type safety                          |
| Tailwind CSS   | 4        | Styling                              |
| next-intl      | 4        | i18n routing and translations        |
| Zustand        | 5        | Global state (auth, cart, tenant)    |
| SignalR client | 10       | Real-time admin notifications        |
| Framer Motion  | 12       | Animations                           |
| Axios          | 1        | HTTP client                          |
| React Hook Form + Zod | - | Forms and validation             |

### Mobile

| Technology          | Version | Purpose                |
|---------------------|---------|------------------------|
| React Native (Expo) | SDK 53  | iOS and Android        |
| Expo Router         | 5       | File-based navigation  |
| Zustand             | 5       | Auth, cart, tenant state |
| Axios + SecureStore | -       | HTTP + JWT storage     |
| React Query         | 5       | Server state management |

### Infrastructure

| Component    | Version       |
|--------------|---------------|
| PostgreSQL   | 16-alpine     |
| Redis        | 7-alpine      |
| Nginx        | 1.27-alpine   |
| Docker       | Compose v3.8  |
| CI/CD        | GitHub Actions |
| Registry     | GHCR          |

---

## Architecture

The backend follows Clean Architecture with four projects:

```
Shop.Domain          -- Entities, value objects, domain interfaces. No external deps.
Shop.Application     -- CQRS commands/queries (MediatR), validators, application interfaces.
Shop.Infrastructure  -- EF Core DbContext, Redis, email, PDF, image, GeoIP, MES client.
Shop.API             -- Controllers, middleware, SignalR hubs, Swagger, DI composition.
```

All entities that are tenant-scoped implement `ITenantEntity` (TenantId column). A Global Query Filter on `ShopDbContext` appends `WHERE TenantId = @current` to every query. `TenantMiddleware` resolves the current tenant from:

1. `X-Tenant-Id` request header
2. Custom domain lookup
3. Subdomain (e.g., `catholia.syndock.co.kr`)

Table prefix for all Shop entities: `SP_`

---

## Features

### Commerce Core
- Multi-tenant catalog: categories, sub-categories, products, variants, images
- Shopping cart with quantity management
- Checkout flow (4 steps: cart, address, payment, confirmation)
- TossPayments integration (confirm, cancel, refund)
- Order lifecycle management
- PDF order receipts (QuestPDF)

### Platform Management
- Commission and settlement system with automated payout scheduling
- Tenant provisioning with `SeedTenantDataCommand` (auto-creates categories, products, config)
- Billing and usage quota tracking
- Platform-wide admin via SynDock.Portal (Next.js, port 3300)

### Member Features
- JWT authentication + refresh tokens
- 2FA TOTP (enable / verify / disable)
- OAuth login: Google, Kakao, Naver
- Email verification, password reset
- Profile and password management
- Address book, wishlist, reviews with photo upload
- Q&A, loyalty points, coupons (with auto-issue on signup/birthday)
- Web push notifications

### Marketing and Automation
- Campaign engine with A/B variant testing and performance metrics
- Kakao AlimTalk order notifications (confirmation, dispatch, delivery)
- SNS auto-posting (Instagram, Facebook) with AI caption generation
- Automated coupon issuance (welcome, birthday)

### AI Features
- Claude-powered customer chatbot
- Holt-Winters demand forecasting with accuracy tracking
- Product recommendation engine
- AI image generation endpoint
- MES production plan suggestions from sales trends

### MES Integration
- Inventory reservation and release on order create/cancel
- MesOrderId/MesOrderNo tracking on orders
- Production plan proposal and approval, then transmission to MES
- Webhook receiver for MES status updates

### Internationalization
- 5 languages: Korean, English, Japanese, Simplified Chinese, Vietnamese
- GeoIP auto-detection via MaxMind + CDN headers
- Multi-currency display: KRW, USD, EUR, JPY, CNY, VND
- Accept-Language header support

### SEO and Performance
- Dynamic sitemap.xml and robots.txt per tenant
- JSON-LD structured data (Product, Organization, BreadcrumbList)
- Response caching with ETag and Cache-Control headers
- WebP/AVIF image conversion and resizing

### Observability
- Prometheus metrics endpoint (`/metrics`)
- Grafana dashboards (preconfigured)
- Serilog structured logging with PII masking and redaction
- Sentry error monitoring (optional DSN)
- Health check endpoints: `/health/live`, `/health/ready`

### Security
- AES-256 encryption for PII fields (phone, address)
- PII masking in logs via `PiiMaskingMiddleware` and `PiiDestructurePolicy`
- Security headers middleware (CSP, HSTS, X-Frame-Options)
- Rate limiting
- HTTPS enforced in production via Nginx

---

## Prerequisites

| Requirement       | Minimum Version | Notes                              |
|-------------------|-----------------|------------------------------------|
| .NET SDK          | 8.0             | `dotnet --version`                 |
| Node.js           | 20              | LTS recommended                    |
| PostgreSQL        | 16              | Or use the Docker container        |
| Redis             | 7               | Or use the Docker container        |
| Docker            | 24+             | Required for compose workflow      |
| Docker Compose    | 2.20+           | Plugin form (`docker compose`)     |

---

## Quick Start (Docker)

The fastest path to a running local environment (infrastructure only — API runs via `dotnet run`):

```bash
# 1. Start PostgreSQL and Redis
docker compose -f docker/docker-compose.dev.yml up -d

# 2. Run the API (from the api/ directory)
cd api
dotnet run --project src/Shop.API

# 3. Run the web frontend (from the web/ directory, in a separate terminal)
cd web
npm install
npm run dev
```

The API will be available at `http://localhost:5100`.
The web frontend will be available at `http://localhost:3200`.
Swagger UI is at `http://localhost:5100/swagger`.

Default dev credentials (from `appsettings.json`):

```
DB host:    localhost:5434
DB name:    syndock_shop
DB user:    shop_admin
DB pass:    Shop@2026!
Redis:      localhost:6381
```

---

## Manual Setup

### 1. Database migration

```bash
cd api
dotnet ef database update --project src/Shop.Infrastructure --startup-project src/Shop.API
```

### 2. Backend (API)

```bash
cd api
dotnet restore
dotnet build
dotnet run --project src/Shop.API
```

Configuration is loaded from `src/Shop.API/appsettings.json` and `appsettings.Development.json`. Override any value with environment variables using `__` as the path separator (e.g., `ConnectionStrings__ShopDb`).

### 3. Frontend (Web)

```bash
cd web
npm install
npm run dev        # development server on port 3200
npm run build      # production build
npm run start      # serve production build
```

The dev server proxies `/api/*` to `http://127.0.0.1:5100` via `next.config.ts` rewrites. Do not use `localhost` — use `127.0.0.1` to avoid Turbopack proxy issues on Windows.

### 4. Mobile (React Native Expo)

```bash
cd mobile
npm install
npx expo start        # interactive launcher
npx expo start --android
npx expo start --ios
```

Bundle identifier: `com.syndock.shop`

---

## Environment Variables

| Variable                        | Required | Description                             |
|---------------------------------|----------|-----------------------------------------|
| `ConnectionStrings__ShopDb`     | Yes      | PostgreSQL connection string            |
| `ConnectionStrings__Redis`      | Yes      | Redis connection string                 |
| `Jwt__Secret`                   | Yes      | JWT signing key (min. 32 chars)         |
| `Jwt__Issuer`                   | Yes      | JWT issuer claim                        |
| `Jwt__Audience`                 | Yes      | JWT audience claim                      |
| `Encryption__Key`               | Yes      | AES-256 key (Base64, 32-byte raw)       |
| `Email__SmtpHost`               | No       | SMTP server hostname                    |
| `Email__SmtpPort`               | No       | SMTP port (default: 587)                |
| `Email__Username`               | No       | SMTP username                           |
| `Email__Password`               | No       | SMTP password                           |
| `Email__FromAddress`            | No       | Sender address                          |
| `Sentry__Dsn`                   | No       | Sentry DSN for error monitoring         |
| `AI__Claude__ApiKey`            | No       | Anthropic API key for chatbot           |
| `AI__OpenAI__ApiKey`            | No       | OpenAI API key for image generation     |
| `Mes__Enabled`                  | No       | Enable MES integration (default: false) |
| `Mes__BaseUrl`                  | No       | MES API base URL                        |
| `KakaoAlimtalk__ApiKey`         | No       | Kakao AlimTalk API key                  |
| `WebPush__PublicKey`            | No       | VAPID public key                        |
| `WebPush__PrivateKey`           | No       | VAPID private key                       |
| `NEXT_PUBLIC_API_URL`           | Yes (FE) | API base URL for the frontend           |

---

## Port Assignments

| Service         | Host Port | Container Port | Description                |
|-----------------|-----------|----------------|----------------------------|
| PostgreSQL      | 5434      | 5432           | Shared tenant database     |
| Redis           | 6381      | 6379           | Cache and session store     |
| Shop API        | 5100      | 5100           | .NET 8 Web API             |
| Shop Web        | 3200      | 3200           | Tenant storefront (Next.js) |
| Portal          | 3300      | 3300           | PlatformAdmin dashboard     |
| Grafana         | 3001      | 3001           | Metrics dashboard          |
| Nginx (HTTP)    | 80        | 80             | Reverse proxy              |
| Nginx (HTTPS)   | 443       | 443            | TLS termination            |

---

## Project Structure

```
SynDock.Shop/
├── api/                          # .NET 8 backend
│   ├── Shop.sln
│   ├── src/
│   │   ├── Shop.Domain/          # Entities (SP_ prefix), domain interfaces
│   │   ├── Shop.Application/     # CQRS handlers, validators, DTOs
│   │   ├── Shop.Infrastructure/  # EF Core, Redis, email, PDF, MES client
│   │   └── Shop.API/             # Controllers, middleware, hubs, Program.cs
│   │       ├── Controllers/      # 27 API controllers
│   │       ├── Hubs/             # SignalR: NotificationHub, AdminHub
│   │       ├── Middleware/       # Tenant, exception, PII masking, security headers
│   │       ├── Monitoring/       # Prometheus business metrics
│   │       └── Resources/        # i18n SharedResource (ko/en/ja/zh-CN/vi)
│   ├── tests/
│   │   ├── Shop.Tests.Unit/      # 29 unit tests (xUnit, Moq, FluentAssertions)
│   │   └── Shop.Tests.Integration/ # 141 integration tests (WebApplicationFactory)
│   └── load-tests/               # k6 load test scripts (5 scenarios)
├── web/                          # Next.js 16 frontend
│   ├── src/
│   │   ├── app/                  # App router pages
│   │   ├── components/           # UI components
│   │   ├── i18n/                 # next-intl config and messages
│   │   └── stores/               # Zustand stores (auth, cart, tenant)
│   ├── e2e/                      # Playwright E2E tests (7 spec files, 59 tests)
│   ├── next.config.ts
│   └── package.json
├── mobile/                       # React Native Expo SDK 53
│   ├── app/                      # Expo Router screens (37 TypeScript files, 24 screens)
│   ├── app.json
│   └── package.json
├── docker/                       # Docker Compose files
│   ├── docker-compose.dev.yml    # Local dev: DB + Redis only
│   ├── docker-compose.yml        # Full stack (API + Web)
│   └── docker-compose.prod.yml   # Production (all services + monitoring + backup)
├── nginx/                        # Nginx reverse proxy config and SSL
├── monitoring/                   # Prometheus config, alert rules, Grafana dashboards
└── .github/
    └── workflows/
        └── ci.yml                # CI/CD: build, test, push to GHCR, deploy
```

---

## Testing

### Unit Tests

```bash
cd api
dotnet test tests/Shop.Tests.Unit
```

Uses xUnit, Moq, FluentAssertions, and EF Core InMemory. Covers domain logic, command handlers, validators.

### Integration Tests

Integration tests use `WebApplicationFactory` with the EF Core InMemory provider.

```bash
cd api
dotnet test tests/Shop.Tests.Integration
```

For tests against a real PostgreSQL instance, set the connection string:

```bash
ConnectionStrings__ShopDb="Host=localhost;Port=5434;Database=syndock_shop_test;Username=shop_admin;Password=TestPassword123!" \
dotnet test tests/Shop.Tests.Integration
```

Total: 170 tests (29 unit + 141 integration).

### E2E Tests (Playwright)

```bash
cd web
npm install
npx playwright install chromium
npm run test:e2e            # headless
npm run test:e2e:ui         # interactive UI mode
npm run test:e2e:report     # view last report
```

7 spec files covering: home, navigation, auth, products, cart, order, admin. 59 tests total, Chromium browser.

### Load Tests (k6)

Load test scripts are located in `api/load-tests/`. Five scenarios are provided: auth, browse, checkout, mixed, and admin. Requires [k6](https://k6.io/docs/getting-started/installation/).

```bash
k6 run api/load-tests/mixed.js
```

---

## Deployment

### Production Docker Compose

The production compose file (`docker/docker-compose.prod.yml`) includes all services:

- PostgreSQL (512 MB memory limit)
- Redis (256 MB, LRU eviction policy)
- Shop API (.NET 8, 1 GB limit)
- Shop Web (Next.js, 512 MB)
- Portal (Platform Admin, 256 MB)
- HomePage (static, Nginx, 64 MB)
- Nginx reverse proxy (HTTP + HTTPS)
- Daily PostgreSQL backup (7-day retention)
- Prometheus + Grafana + exporters

```bash
# Copy and populate the environment file
cp docker/.env.example docker/.env
# Edit docker/.env with production values

# Start all services
docker compose -f docker/docker-compose.prod.yml up -d

# Check health
curl http://localhost/health/live
```

### CI/CD

GitHub Actions workflow (`.github/workflows/ci.yml`) runs on push to `main` and `develop`:

1. **api-test** -- `dotnet restore`, `dotnet build`, unit tests, integration tests
2. **web-build** -- `npm ci`, `npm run build`
3. **docker-push** -- builds and pushes images to GHCR (on `main` only)
4. **deploy** -- SSH deploy to production server, runs health check post-deploy

Required GitHub Secrets:

| Secret             | Description                           |
|--------------------|---------------------------------------|
| `DEPLOY_HOST`      | Production server IP or hostname      |
| `DEPLOY_USER`      | SSH username                          |
| `DEPLOY_SSH_KEY`   | SSH private key                       |

---

## API Documentation

Swagger UI is available at:

```
http://localhost:5100/swagger
```

The API is organized around these controller groups:

| Controller           | Route prefix                   | Description                         |
|----------------------|-------------------------------|-------------------------------------|
| AuthController       | `/api/auth`                   | Login, register, OAuth, 2FA, tokens |
| ProductsController   | `/api/products`               | Catalog, variants, images, slugs    |
| CategoriesController | `/api/categories`             | Category tree, slugs                |
| CartController       | `/api/cart`                   | Cart CRUD                           |
| OrderController      | `/api/order`                  | Order lifecycle, PDF receipt        |
| PaymentController    | `/api/payment`                | TossPayments confirm/cancel/refund  |
| AdminController      | `/api/admin`                  | Tenant admin: orders, products, stats |
| PlatformController   | `/api/platform`               | Platform admin: tenants, settlement  |
| MesIntegrationController | `/api/mes`                | MES reserve/release/status/webhook  |
| ForecastController   | `/api/forecast`               | Demand forecast, accuracy           |
| RecommendationsController | `/api/recommendations`   | Product recommendations             |
| CouponsController    | `/api/coupons`                | Coupon CRUD and redemption          |
| PointsController     | `/api/points`                 | Point earn/use/refund (admin)       |
| ReviewController     | `/api/review`                 | Reviews with photo upload           |
| CurrencyController   | `/api/currency`               | Exchange rates                      |
| PushSubscriptionController | `/api/push`             | Web push subscribe/unsubscribe      |
| LocaleController     | `/api/locale`                 | i18n messages, language switching   |
| UploadController     | `/api/upload`                 | Image upload and optimization       |

Health endpoints:

```
GET /health/live    -- liveness probe (always 200 if process is running)
GET /health/ready   -- readiness probe (checks DB and Redis)
GET /metrics        -- Prometheus metrics
```

---

## Tenants

The platform ships with two pre-configured seed tenants. Run seeding via the platform endpoint:

```
POST /api/platform/{slug}/seed
Authorization: Bearer <platform-admin-token>
```

### Catholia

Catholic religious goods store.

| Property      | Value                                |
|---------------|--------------------------------------|
| Slug          | `catholia`                           |
| Domain        | `catholia.co.kr`                     |
| Theme color   | `#8B4513` (brown) / `#F5F0E8` (cream) |
| Font          | Noto Serif KR                        |
| Categories    | 6 top-level, 24 sub-categories        |
| Products      | 18 initial products                  |
| MES mapping   | `catholia` -> `smartdocking`         |

### MoHyun

Traditional Korean fermented condiments store (Sunchang region, doenjang / gochujang / ganjang / jirihwan).

| Property      | Value                                |
|---------------|--------------------------------------|
| Slug          | `mohyun`                             |
| Domain        | `mohyun.com`                         |
| Theme color   | `#8B4513` (brown) / `#2D5016` (green) |
| Font          | Noto Serif KR                        |
| Categories    | 6 top-level, 21 sub-categories        |
| Products      | 18 initial products                  |

---

## License

Proprietary. All rights reserved. Unauthorized copying, distribution, or use of this software is strictly prohibited.

Copyright (c) 2026 SmartDocking Station. All rights reserved.
