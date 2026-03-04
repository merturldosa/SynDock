#!/bin/bash
set -euo pipefail

# ─── SynDock Shop - Backup Verification Script ───────────────────
# Usage: ./verify-backup.sh [backup_dir]
# Exit codes: 0 = OK, 1 = WARNING, 2 = CRITICAL
# Designed for cron/monitoring integration
# ──────────────────────────────────────────────────────────────────

BACKUP_DIR="${1:-${BACKUP_DIR:-/backups}}"
MAX_AGE_HOURS="${MAX_AGE_HOURS:-24}"
MIN_SIZE_BYTES="${MIN_SIZE_BYTES:-1024}"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

EXIT_CODE=0
CHECKS_PASSED=0
CHECKS_TOTAL=0

check_pass() { echo -e "  ${GREEN}[PASS]${NC} $1"; CHECKS_PASSED=$((CHECKS_PASSED + 1)); CHECKS_TOTAL=$((CHECKS_TOTAL + 1)); }
check_warn() { echo -e "  ${YELLOW}[WARN]${NC} $1"; CHECKS_TOTAL=$((CHECKS_TOTAL + 1)); [ "$EXIT_CODE" -lt 1 ] && EXIT_CODE=1; }
check_fail() { echo -e "  ${RED}[FAIL]${NC} $1"; CHECKS_TOTAL=$((CHECKS_TOTAL + 1)); EXIT_CODE=2; }

echo ""
echo "═══════════════════════════════════════════════════"
echo "  SynDock Shop - Backup Verification"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"
echo "═══════════════════════════════════════════════════"
echo ""

# ─── Check 1: Backup directory exists ────────────────────────────
if [ -d "$BACKUP_DIR" ]; then
    check_pass "Backup directory exists: $BACKUP_DIR"
else
    check_fail "Backup directory not found: $BACKUP_DIR"
    echo ""
    echo "Result: $CHECKS_PASSED/$CHECKS_TOTAL checks passed"
    exit $EXIT_CODE
fi

# ─── Check 2: Backup files exist ────────────────────────────────
BACKUP_COUNT=$(find "$BACKUP_DIR" -name '*.dump' -type f 2>/dev/null | wc -l)
if [ "$BACKUP_COUNT" -gt 0 ]; then
    check_pass "Found $BACKUP_COUNT backup file(s)"
else
    check_fail "No .dump files found in $BACKUP_DIR"
    echo ""
    echo "Result: $CHECKS_PASSED/$CHECKS_TOTAL checks passed"
    exit $EXIT_CODE
fi

# ─── Find latest backup ─────────────────────────────────────────
LATEST_BACKUP=$(find "$BACKUP_DIR" -name '*.dump' -type f -printf '%T@ %p\n' 2>/dev/null \
    | sort -rn | head -1 | awk '{print $2}')
LATEST_NAME=$(basename "$LATEST_BACKUP")

echo ""
echo "  Latest backup: $LATEST_NAME"
echo ""

# ─── Check 3: File size ─────────────────────────────────────────
FILE_SIZE=$(stat -c '%s' "$LATEST_BACKUP" 2>/dev/null || stat -f '%z' "$LATEST_BACKUP" 2>/dev/null || echo "0")
FILE_SIZE_HUMAN=$(du -h "$LATEST_BACKUP" | awk '{print $1}')

if [ "$FILE_SIZE" -eq 0 ]; then
    check_fail "Latest backup is 0 bytes!"
elif [ "$FILE_SIZE" -lt "$MIN_SIZE_BYTES" ]; then
    check_warn "Latest backup is suspiciously small: $FILE_SIZE_HUMAN (< $(numfmt --to=iec $MIN_SIZE_BYTES 2>/dev/null || echo "${MIN_SIZE_BYTES}B"))"
else
    check_pass "Backup size: $FILE_SIZE_HUMAN"
fi

# ─── Check 4: Backup age ────────────────────────────────────────
FILE_MTIME=$(stat -c '%Y' "$LATEST_BACKUP" 2>/dev/null || stat -f '%m' "$LATEST_BACKUP" 2>/dev/null || echo "0")
CURRENT_TIME=$(date +%s)
AGE_SECONDS=$((CURRENT_TIME - FILE_MTIME))
AGE_HOURS=$((AGE_SECONDS / 3600))
MAX_AGE_SECONDS=$((MAX_AGE_HOURS * 3600))

if [ "$AGE_SECONDS" -gt "$MAX_AGE_SECONDS" ]; then
    check_warn "Latest backup is ${AGE_HOURS}h old (threshold: ${MAX_AGE_HOURS}h)"
else
    check_pass "Backup age: ${AGE_HOURS}h (within ${MAX_AGE_HOURS}h threshold)"
fi

# ─── Check 5: Integrity (pg_restore --list) ─────────────────────
if command -v pg_restore &>/dev/null; then
    if pg_restore --list "$LATEST_BACKUP" >/dev/null 2>&1; then
        TOC_COUNT=$(pg_restore --list "$LATEST_BACKUP" 2>/dev/null | wc -l)
        check_pass "Backup integrity OK ($TOC_COUNT TOC entries)"
    else
        check_fail "Backup integrity check FAILED (pg_restore --list returned error)"
    fi
else
    check_warn "pg_restore not available, skipping integrity check"
fi

# ─── Check 6: Retention (old backups cleaned) ───────────────────
OLD_BACKUPS=$(find "$BACKUP_DIR" -name '*.dump' -type f -mtime +7 2>/dev/null | wc -l)
if [ "$OLD_BACKUPS" -gt 0 ]; then
    check_warn "$OLD_BACKUPS backup(s) older than 7 days (retention policy may not be running)"
else
    check_pass "No backups older than 7 days (retention OK)"
fi

# ─── Summary ─────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════"
if [ "$EXIT_CODE" -eq 0 ]; then
    echo -e "  Result: ${GREEN}ALL CHECKS PASSED${NC} ($CHECKS_PASSED/$CHECKS_TOTAL)"
elif [ "$EXIT_CODE" -eq 1 ]; then
    echo -e "  Result: ${YELLOW}WARNING${NC} ($CHECKS_PASSED/$CHECKS_TOTAL passed)"
else
    echo -e "  Result: ${RED}CRITICAL${NC} ($CHECKS_PASSED/$CHECKS_TOTAL passed)"
fi
echo "═══════════════════════════════════════════════════"
echo ""

exit $EXIT_CODE
