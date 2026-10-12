#!/bin/sh
# Seed a Roivo database with the demo client book.
#
#   ROIVO_DB='Host=...;Database=roivo;Username=...;Password=...' \
#   ROIVO_SEED_EMAIL=you@example.com \
#   ROIVO_SEED_PASSWORD='...' \
#   scripts/seed-database.sh
#
# For staging, ROIVO_DB is the Neon connection string held in the Render
# environment (Dashboard -> the service -> Environment). It is deliberately not
# in this repository and must not be committed to it.
#
# Writes a user, an accountant tenant, six client businesses and a year of
# invoices, transactions, matches, forecasts and obligations. It is additive and
# not idempotent: running it twice against the same database gives you two of
# everything.

set -eu

root=$(cd "$(dirname "$0")/.." && pwd)

fail() {
    echo "$1" >&2
    exit 1
}

[ -n "${ROIVO_DB:-}" ] || fail "ROIVO_DB is not set. Give it the target connection string."
[ -n "${ROIVO_SEED_EMAIL:-}" ] || fail "ROIVO_SEED_EMAIL is not set."
[ -n "${ROIVO_SEED_PASSWORD:-}" ] || fail "ROIVO_SEED_PASSWORD is not set."

# Name the target before touching it. The whole reason this script exists is
# that the connection string is pasted in by hand, and a pasted connection
# string is exactly the kind of thing that turns out to point somewhere else.
# Host and database only — never the password.
host=$(printf '%s' "$ROIVO_DB" | sed -n 's/.*[Hh]ost=\([^;]*\).*/\1/p')
database=$(printf '%s' "$ROIVO_DB" | sed -n 's/.*[Dd]atabase=\([^;]*\).*/\1/p')

echo "About to seed:"
echo "  host      ${host:-<unparsed>}"
echo "  database  ${database:-<unparsed>}"
echo "  user      $ROIVO_SEED_EMAIL"
echo

if [ -z "${ROIVO_SEED_YES:-}" ]; then
    printf 'Type the database name to continue: '
    read -r confirmation
    [ "$confirmation" = "$database" ] || fail "Did not match. Nothing was written."
fi

echo "Seeding..."
cd "$root"
dotnet run --project tools/seeder/Seeder.csproj -c Release
