# SynDock.Shop - k6 Load Tests

## Install k6

```bash
# Windows (Chocolatey)
choco install k6

# Windows (Scoop)
scoop install k6

# macOS
brew install k6

# Docker
docker pull grafana/k6
```

## Test Scenarios

| Script | Description | Target |
|--------|-------------|--------|
| `auth.js` | Login/Register flow | Auth endpoints |
| `browse.js` | Product browsing, search, categories | Public APIs |
| `checkout.js` | Full cart → order flow | Cart + Order APIs |
| `admin.js` | Admin dashboard, analytics | Admin APIs |
| `mixed.js` | Multi-scenario realistic traffic | All APIs |

## Run Tests

```bash
# Prerequisites: Start Shop API at port 5100
cd SynDock.Shop/api && dotnet run --project src/Shop.API

# Smoke test (5 VUs, 2 min)
k6 run -e STAGE=smoke load-tests/browse.js

# Load test (20-50 VUs, 10 min)
k6 run -e STAGE=load load-tests/browse.js

# Stress test (50-200 VUs, 10 min)
k6 run -e STAGE=stress load-tests/browse.js

# Spike test (300 VUs spike, 3 min)
k6 run -e STAGE=spike load-tests/browse.js

# Mixed scenario (55 total VUs, 5 min)
k6 run load-tests/mixed.js

# Admin load test (requires admin credentials)
k6 run -e ADMIN_EMAIL=admin@syndock.com -e ADMIN_PASSWORD=Admin123! load-tests/admin.js

# Custom tenant
k6 run -e TENANT_ID=mohyun -e STAGE=load load-tests/browse.js

# Docker
docker run --rm -i --network=host grafana/k6 run - < load-tests/browse.js
```

## Thresholds

| Metric | Threshold |
|--------|-----------|
| p95 response time | < 500ms |
| p99 response time | < 1000ms |
| Error rate | < 5% |
| Throughput | > 10 req/s |

## Output Formats

```bash
# JSON output
k6 run --out json=results.json load-tests/browse.js

# CSV output
k6 run --out csv=results.csv load-tests/browse.js

# InfluxDB (for Grafana dashboard)
k6 run --out influxdb=http://localhost:8086/k6 load-tests/browse.js
```
