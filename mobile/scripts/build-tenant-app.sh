#!/bin/bash
# White-label tenant app builder
# Usage: ./build-tenant-app.sh <tenant-slug> <platform>
# Example: ./build-tenant-app.sh mohyun android
#          ./build-tenant-app.sh catholia ios

set -euo pipefail

TENANT_SLUG="${1:?Usage: $0 <tenant-slug> <platform: android|ios>}"
PLATFORM="${2:-android}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/.."
API_BASE="${API_BASE:-https://syndock.com}"

echo "=== SynDock White-Label App Builder ==="
echo "Tenant: ${TENANT_SLUG}"
echo "Platform: ${PLATFORM}"
echo ""

# 1. Fetch tenant config from API
echo "[1/5] Fetching tenant config..."
TENANT_CONFIG=$(curl -s "${API_BASE}/api/platform/tenants/${TENANT_SLUG}" \
    -H "X-Tenant-Id: ${TENANT_SLUG}")

TENANT_NAME=$(echo "${TENANT_CONFIG}" | jq -r '.name // "SynDock Shop"')
CONFIG_JSON=$(echo "${TENANT_CONFIG}" | jq -r '.configJson // "{}"')
PRIMARY_COLOR=$(echo "${CONFIG_JSON}" | jq -r '.theme.primaryColor // "#3B82F6"')
LOGO_URL=$(echo "${CONFIG_JSON}" | jq -r '.theme.logoUrl // ""')

echo "  Name: ${TENANT_NAME}"
echo "  Color: ${PRIMARY_COLOR}"

# 2. Generate tenant-specific app config
echo "[2/5] Generating app config..."
BUNDLE_ID="com.syndock.shop.${TENANT_SLUG}"

cat > "${PROJECT_DIR}/tenant-config.json" << EOF
{
  "tenantSlug": "${TENANT_SLUG}",
  "tenantName": "${TENANT_NAME}",
  "apiBaseUrl": "${API_BASE}",
  "primaryColor": "${PRIMARY_COLOR}",
  "bundleId": "${BUNDLE_ID}",
  "logoUrl": "${LOGO_URL}"
}
EOF

# 3. Update app.json with tenant branding
echo "[3/5] Updating app.json..."
ORIGINAL_APP_JSON="${PROJECT_DIR}/app.json"
BACKUP_APP_JSON="${PROJECT_DIR}/app.json.bak"
cp "${ORIGINAL_APP_JSON}" "${BACKUP_APP_JSON}"

# Update app name, slug, bundle identifier
jq --arg name "${TENANT_NAME}" \
   --arg slug "${TENANT_SLUG}" \
   --arg bundle "${BUNDLE_ID}" \
   --arg color "${PRIMARY_COLOR}" \
   '.expo.name = $name |
    .expo.slug = $slug |
    .expo.ios.bundleIdentifier = $bundle |
    .expo.android.package = $bundle |
    .expo.primaryColor = $color |
    .expo.splash.backgroundColor = $color |
    .expo.android.adaptiveIcon.backgroundColor = $color' \
   "${BACKUP_APP_JSON}" > "${ORIGINAL_APP_JSON}"

# 4. Set environment variables
echo "[4/5] Setting environment..."
export EXPO_PUBLIC_TENANT_SLUG="${TENANT_SLUG}"
export EXPO_PUBLIC_API_URL="${API_BASE}"

# 5. Build with EAS
echo "[5/5] Building ${PLATFORM} app..."
cd "${PROJECT_DIR}"

if command -v eas &> /dev/null; then
    eas build --platform "${PLATFORM}" --profile production --non-interactive
else
    echo "EAS CLI not found. Building with expo..."
    npx expo export --platform "${PLATFORM}"
fi

# Restore original app.json
mv "${BACKUP_APP_JSON}" "${ORIGINAL_APP_JSON}"

echo ""
echo "=== Build Complete ==="
echo "App: ${TENANT_NAME} (${TENANT_SLUG})"
echo "Platform: ${PLATFORM}"
echo "Bundle ID: ${BUNDLE_ID}"
