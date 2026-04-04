#!/bin/bash
set -euo pipefail

# SynDock.Shop - Database Migration Management Script
# Usage:
#   ./migrate.sh                    # Apply pending migrations
#   ./migrate.sh status             # Show migration status
#   ./migrate.sh rollback [name]    # Rollback to specific migration
#   ./migrate.sh --dry-run          # Generate SQL script only
#   ./migrate.sh add [name]         # Add new migration

PROJECT_DIR="$(cd "$(dirname "$0")/../../api/src/Shop.API" && pwd)"
STARTUP_PROJECT="$PROJECT_DIR"
CONTEXT_PROJECT="$(cd "$(dirname "$0")/../../api/src/Shop.Infrastructure" && pwd)"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

show_status() {
    log_info "Current migration status:"
    dotnet ef migrations list \
        --startup-project "$STARTUP_PROJECT" \
        --project "$CONTEXT_PROJECT" \
        --context ShopDbContext
}

apply_migrations() {
    log_info "Applying pending migrations..."
    dotnet ef database update \
        --startup-project "$STARTUP_PROJECT" \
        --project "$CONTEXT_PROJECT" \
        --context ShopDbContext
    log_info "Migrations applied successfully."
}

dry_run() {
    log_info "Generating SQL script for pending migrations..."
    dotnet ef migrations script \
        --startup-project "$STARTUP_PROJECT" \
        --project "$CONTEXT_PROJECT" \
        --context ShopDbContext \
        --idempotent \
        --output "$(dirname "$0")/migration_$(date +%Y%m%d_%H%M%S).sql"
    log_info "SQL script generated."
}

rollback() {
    local target="${1:-0}"
    log_warn "Rolling back to migration: $target"
    dotnet ef database update "$target" \
        --startup-project "$STARTUP_PROJECT" \
        --project "$CONTEXT_PROJECT" \
        --context ShopDbContext
    log_info "Rollback completed."
}

add_migration() {
    local name="${1:?Migration name is required}"
    log_info "Adding migration: $name"
    dotnet ef migrations add "$name" \
        --startup-project "$STARTUP_PROJECT" \
        --project "$CONTEXT_PROJECT" \
        --context ShopDbContext
    log_info "Migration '$name' added."
}

case "${1:-apply}" in
    status)
        show_status
        ;;
    rollback)
        rollback "${2:-0}"
        ;;
    --dry-run|dry-run)
        dry_run
        ;;
    add)
        add_migration "${2:-}"
        ;;
    apply|"")
        show_status
        apply_migrations
        ;;
    *)
        echo "Usage: $0 {apply|status|rollback [migration]|--dry-run|add [name]}"
        exit 1
        ;;
esac
