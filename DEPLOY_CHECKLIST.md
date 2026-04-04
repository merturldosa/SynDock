# SynDock 프로덕션 배포 체크리스트

## 사전 준비

### 서버 요구사항
- [ ] Ubuntu 22.04+ 또는 CentOS 8+ 서버
- [ ] CPU: 4코어 이상, RAM: 8GB 이상, SSD: 100GB 이상
- [ ] Docker + Docker Compose 설치
- [ ] 도메인 DNS 설정 완료

### DNS 설정
- [ ] `syndock.co.kr` → 서버 IP (회사 홈페이지)
- [ ] `admin.syndock.co.kr` → 서버 IP (플랫폼 관리)
- [ ] `*.shop.syndock.co.kr` → 서버 IP (테넌트 쇼핑몰 서브도메인)
- [ ] `mohyun.com` → 서버 IP (모현 커스텀 도메인)
- [ ] `catholia.co.kr` → 서버 IP (카톨리아 커스텀 도메인)

---

## 배포 순서

### 1. 환경변수 설정
```bash
cd SynDock.Shop/docker
cp .env.example .env
```

**.env 필수 설정:**
```
# DB
POSTGRES_PASSWORD=강력한_비밀번호
POSTGRES_DB=syndock_shop

# JWT (반드시 변경!)
JWT_SECRET=최소64자_랜덤_문자열

# CORS
CORS_ORIGIN_0=https://catholia.co.kr
CORS_ORIGIN_1=https://mohyun.com
CORS_ORIGIN_2=https://admin.syndock.co.kr

# 결제 (TossPayments)
# 각 테넌트 관리자가 /admin/settings에서 개별 설정

# AI (각 테넌트 관리자가 /admin/settings에서 개별 설정)

# 이메일 (SMTP)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=이메일
SMTP_PASSWORD=앱_비밀번호
SMTP_FROM=noreply@syndock.co.kr

# 암호화 키 (반드시 변경!)
ENCRYPTION_KEY=Base64_인코딩된_32바이트_키
```

### 2. SSL 인증서 발급
```bash
cd docker
./scripts/init-ssl.sh
```

### 3. 서비스 시작
```bash
docker compose -f docker-compose.prod.yml up -d
```

### 4. DB 초기화 확인
```bash
# 로그 확인 (시드 데이터 생성)
docker logs shop-api --tail 50

# 테넌트 확인
docker exec shop-db psql -U shop_admin -d syndock_shop \
  -c 'SELECT "Id", "Name", "Slug" FROM "SP_Tenants";'
```

### 5. 서비스 확인
```bash
# API 헬스체크
curl https://syndock.co.kr/api/health/ready

# 쇼핑몰 접속
curl -I https://mohyun.com
curl -I https://catholia.co.kr

# 관리자 포털
curl -I https://admin.syndock.co.kr
```

---

## 배포 후 확인

### 기본 기능 테스트
- [ ] 홈페이지 접속 (각 테넌트 도메인)
- [ ] 회원가입 + 로그인
- [ ] 상품 목록 + 상세 페이지
- [ ] 장바구니 + 주문
- [ ] 관리자 로그인 + 대시보드
- [ ] 상품 등록/수정
- [ ] 이미지 업로드
- [ ] 회사 소개 페이지 (/about)

### 결제 테스트
- [ ] TossPayments 테스트 모드 결제
- [ ] 결제 완료 → 주문 확인
- [ ] 환불 처리

### 모니터링
- [ ] Grafana 대시보드 접속 (http://서버IP:3001)
- [ ] Prometheus 메트릭 확인 (http://서버IP:9090)
- [ ] API 로그 확인: `docker logs -f shop-api`

---

## 새 테넌트 분양 절차

```bash
# 1. 시드 데이터 파일 작성 (Shop.Infrastructure/Data/NewClientSeedData.cs)
# 2. InitialDataSeeder에 등록
# 3. API 재배포
docker compose -f docker-compose.prod.yml up -d --build shop-api

# 4. DNS 설정
#    newclient.shop.syndock.co.kr → 서버 IP
#    또는 커스텀 도메인: newclient.com → 서버 IP

# 5. nginx.conf에 도메인 추가 (필요시)
#    server_name에 newclient.com 추가

# 6. SSL 인증서 갱신
docker compose -f docker-compose.prod.yml restart shop-nginx
```

---

## 트러블슈팅

| 증상 | 원인 | 해결 |
|------|------|------|
| 502 Bad Gateway | API 미시작 | `docker logs shop-api` 확인 |
| SSL 에러 | 인증서 미발급 | `./scripts/init-ssl.sh` 재실행 |
| DB 연결 실패 | 비밀번호 불일치 | `.env` 확인 |
| 이미지 404 | wwwroot 마운트 | `volumes` 설정 확인 |
| SignalR 연결 실패 | WebSocket 미전달 | nginx `proxy_set_header Upgrade` 확인 |

---

## 관리 명령어

```bash
# 서비스 상태 확인
docker compose -f docker-compose.prod.yml ps

# 로그 확인
docker logs -f shop-api --tail 100

# DB 백업 (수동)
docker exec shop-db pg_dump -U shop_admin syndock_shop > backup.sql

# API 재시작 (코드 변경 없이)
docker compose -f docker-compose.prod.yml restart shop-api

# 전체 재배포
docker compose -f docker-compose.prod.yml up -d --build

# CLI 사용
docker exec shop-api dotnet /app/syndock.dll tenant list
docker exec shop-api dotnet /app/syndock.dll health
```
