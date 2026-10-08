#!/usr/bin/env bash
set -euo pipefail

SUMMARY_JSON="${1:-coverage/Summary.json}"
GLOBAL_THRESHOLD="${GLOBAL_COVERAGE_THRESHOLD:-80}"
DOMAIN_APP_THRESHOLD="${DOMAIN_APP_COVERAGE_THRESHOLD:-90}"

if [ ! -f "$SUMMARY_JSON" ]; then
  echo "No coverage summary found at $SUMMARY_JSON yet (no instrumented code) — skipping threshold check."
  exit 0
fi

global_coverage=$(jq '.summary.linecoverage // 0' "$SUMMARY_JSON")
domain_app_covered=$(jq '[.coverage.assemblies[] | select(.name == "ReveilMusical.Domain" or .name == "ReveilMusical.Application") | .coveredlines] | add // 0' "$SUMMARY_JSON")
domain_app_coverable=$(jq '[.coverage.assemblies[] | select(.name == "ReveilMusical.Domain" or .name == "ReveilMusical.Application") | .coverablelines] | add // 0' "$SUMMARY_JSON")

if [ "$domain_app_coverable" -gt 0 ]; then
  domain_app_coverage=$(echo "scale=2; 100 * $domain_app_covered / $domain_app_coverable" | bc)
else
  domain_app_coverage=0
  echo "Domain/Application assemblies not found in coverage report yet — skipping their threshold."
fi

echo "Global line coverage: ${global_coverage}% (threshold: ${GLOBAL_THRESHOLD}%)"
echo "Domain+Application line coverage: ${domain_app_coverage}% (threshold: ${DOMAIN_APP_THRESHOLD}%)"

failed=0

if (( $(echo "$global_coverage < $GLOBAL_THRESHOLD" | bc -l) )); then
  echo "::error::Global coverage ${global_coverage}% is below the ${GLOBAL_THRESHOLD}% threshold."
  failed=1
fi

if [ "$domain_app_coverable" -gt 0 ] && (( $(echo "$domain_app_coverage < $DOMAIN_APP_THRESHOLD" | bc -l) )); then
  echo "::error::Domain+Application coverage ${domain_app_coverage}% is below the ${DOMAIN_APP_THRESHOLD}% threshold."
  failed=1
fi

exit $failed
