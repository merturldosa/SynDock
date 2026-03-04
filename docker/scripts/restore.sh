#!/bin/bash
set -euo pipefail

# ─── SynDock Shop - Database Restore Script ───────────────────────
# Usage: ./restore.sh [dump_file] [--force]
#   dump_file: Path to .dump file (default: latest backup)
#   --force:   Skip confirmation prompt
# ──────────────────────────────────────────────────────────────────

BACKUP_DIR="${BACKUP_DIR:-/backups}"
PGHOST="${PGHOST:-shop-db}"
PGPORT="${PGPORT:-5432}"
PGDATABASE="${PGDATABASE:-syndock_shop}"
PGUSER="${PGUSER:-postgres}"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC}  $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

FORCE=false
DUMP_FILE=""

# Parse arguments
for arg in "$@"; do
    case $arg in
        --force) FORCE=true ;;
        *)       DUMP_FILE="$arg" ;;
    esac
done

# ─── Select dump file ────────────────────────────────────────────
if [ -z "$DUMP_FILE" ]; then
    log_info "No dump file specified, searching for latest backup..."
    DUMP_FILE=$(find "$BACKUP_DIR" -name '*.dump' -type f -printf '%T@ %p\n' 2>/dev/null \
        | sort -rn | head -1 | awk '{print $2}')

    if [ -z "$DUMP_FILE" ]; then
        log_error "No backup files found in $BACKUP_DIR"
        exit 1
    fi
    log_info "Latest backup: $DUMP_FILE"
fi

if [ ! -f "$DUMP_FILE" ]; then
    log_error "Dump file not found: $DUMP_FILE"
    exit 1
fi

# ─── Show dump info ──────────────────────────────────────────────
DUMP_SIZE=$(du -h "$DUMP_FILE" | awk '{print $1}')
DUMP_DATE=$(stat -c '%y' "$DUMP_FILE" 2>/dev/null || stat -f '%Sm' "$DUMP_FILE" 2>/dev/null || echo "unknown")

echo ""
echo "═══════════════════════════════════════════════════"
echo "  SynDock Shop - Database Restore"
echo "═══════════════════════════════════════════════════"
echo "  Source:   $DUMP_FILE"
echo "  Size:     $DUMP_SIZE"
echo "  Date:     $DUMP_DATE"
echo "  Target:   $PGHOST:$PGPORT/$PGDATABASE"
echo "═══════════════════════════════════════════════════"
echo ""

# ─── Confirmation ────────────────────────────────────────────────
if [ "$FORCE" = false ]; then
    log_warn "This will REPLACE the current database with the backup."
    read -p "Are you sure? (y/N): " -r REPLY
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_info "Restore cancelled."
        exit 0
    fi
fi

# ─── Pre-restore safety snapshot ─────────────────────────────────
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
PRE_RESTORE_FILE="$BACKUP_DIR/pre_restore_${TIMESTAMP}.dump"

log_info "Creating pre-restore safety snapshot..."
if pg_dump -Fc -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -f "$PRE_RESTORE_FILE" 2>/dev/null; then
    PRE_SIZE=$(du -h "$PRE_RESTORE_FILE" | awk '{print $1}')
    log_info "Safety snapshot saved: $PRE_RESTORE_FILE ($PRE_SIZE)"
else
    log_warn "Could not create safety snapshot (database may not exist yet). Continuing..."
    rm -f "$PRE_RESTORE_FILE"
fi

# ─── Restore ─────────────────────────────────────────────────────
log_info "Restoring database from backup..."
if pg_restore --clean --if-exists -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" "$DUMP_FILE" 2>&1; then
    log_info "Database restore completed successfully."
else
    # pg_restore returns non-zero even for warnings, check if it actually worked
    log_warn "pg_restore completed with warnings (this is often normal for --clean --if-exists)."
fi

# ─── Post-restore verification ───────────────────────────────────
log_info "Verifying restored database..."
echo ""

TABLE_COUNT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -t -c \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';" \
    2>/dev/null | tr -d ' ')

echo "  Tables found: $TABLE_COUNT"

# Check key tables
KEY_TABLES=("SP_Users" "SP_Products" "SP_Orders" "SP_Tenants" "SP_Categories")
for table in "${KEY_TABLES[@]}"; do
    ROW_COUNT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -t -c \
        "SELECT count(*) FROM \"$table\";" 2>/dev/null | tr -d ' ' || echo "N/A")
    printf "  %-20s %s rows\n" "$table:" "$ROW_COUNT"
done

echo ""

if [ "$TABLE_COUNT" -gt 0 ] 2>/dev/null; then
    log_info "Restore verification PASSED ($TABLE_COUNT tables)"
else
    log_error "Restore verification FAILED (0 tables found)"
    if [ -f "$PRE_RESTORE_FILE" ]; then
        log_warn "You can roll back using: $0 $PRE_RESTORE_FILE --force"
    fi
    exit 1
fi

echo ""
log_info "Done. Pre-restore snapshot available at: $PRE_RESTORE_FILE"
