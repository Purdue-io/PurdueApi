#!/bin/bash
# Refresh the paginated section-search fixtures used by src/Tests.
#
# Usage: utilities/fetch-section-dumps.sh [TERM] [SUBJECT] [OUT_DIR]
#
# Replays the exact request sequence of MyPurdueConnection.GetSectionsAsync
# (session POST -> resetDataForm -> paged searchResults GETs) and writes the
# raw response bytes as <TERM>-<SUBJECT>-section-dump-<offset>-<pageMax>.json.
# Hits the live service - see utilities/README.md.
set -euo pipefail

TERM="${1:-202720}"
SUBJ="${2:-CS}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${3:-${SCRIPT_DIR}/../src/Tests/Resources}"

BASE="https://apps.mypurdue.purdue.edu/StudentRegistrationSsb/ssb"
MAX=500
JAR=$(mktemp)
SID=$(uuidgen)
trap 'rm -f "$JAR"' EXIT

H=( -sS -g -b "$JAR" -c "$JAR" --retry 2
    -H "User-Agent: Mozilla/5.0 (X11; Linux x86_64; rv:157.0) Gecko/20100101 Firefox/157.0"
    -H "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
    -H "Accept-Language: en-US,en;q=0.9"
    -H "Connection: keep-alive" )

echo "term=$TERM subject=$SUBJ -> $OUT_DIR"

# 1) EnsureSession: POST term/search?mode=search (term + our uniqueSessionId)
code=$(curl "${H[@]}" -o /dev/null -w '%{http_code}' -X POST "$BASE/term/search?mode=search" \
  --data-urlencode "term=$TERM" \
  --data-urlencode "studyPath=" \
  --data-urlencode "studyPathText=" \
  --data-urlencode "startDatepicker=" \
  --data-urlencode "endDatepicker=" \
  --data-urlencode "uniqueSessionId=$SID")
echo "term/search -> $code"
case $code in 2*|3*) ;; *) echo "FAIL: session registration"; exit 1;; esac

# 2) ResetSearch: POST classSearch/resetDataForm (empty form body)
code=$(curl "${H[@]}" -o /dev/null -w '%{http_code}' -X POST \
  -H 'Content-Type: application/x-www-form-urlencoded' -d '' \
  "$BASE/classSearch/resetDataForm")
echo "resetDataForm -> $code"
case $code in 2*|3*) ;; *) echo "FAIL: reset"; exit 1;; esac

# 3) Paginated searchResults GETs, same stop conditions as GetSectionsAsync
seen=0
while : ; do
  file="$OUT_DIR/${TERM}-${SUBJ}-section-dump-${seen}-${MAX}.json"
  url="$BASE/searchResults/searchResults?txt_subject=$SUBJ&txt_term=$TERM&startDatepicker=&endDatepicker=&uniqueSessionId=$SID&pageOffset=$seen&pageMaxSize=$MAX&sortColumn=subjectDescription&sortDirection=asc"
  code=$(curl "${H[@]}" -o "$file" -w '%{http_code}' "$url")
  echo "page offset=$seen -> HTTP $code -> $file"
  case $code in 2*) ;; *) echo "FAIL: page at offset $seen"; head -c 300 "$file"; exit 1;; esac

  success=$(jq -r '.success' "$file")
  if [ "$success" != "true" ]; then
      echo "FAIL: success!=true at offset $seen"; head -c 300 "$file"; exit 1
  fi

  # same stale-search guard the scraper does: every row must be the requested subject
  wrong=$(jq -r --arg s "$SUBJ" '[.data[] | select(.subject != $s)] | length' "$file")
  if [ "$wrong" != "0" ]; then
      echo "FAIL: $wrong rows with wrong subject at offset $seen"; exit 1
  fi

  got=$(jq '.data | length' "$file")
  total=$(jq '.totalCount' "$file")
  echo "  rows=$got totalCount=$total"
  seen=$((seen + got))
  if [ "$seen" -ge "$total" ]; then break; fi
  if [ "$got" -eq 0 ]; then
      echo "FAIL: stopped early at $seen of $total (partial scrape)"; exit 1
  fi
  sleep 1
done

echo "DONE: fetched $seen sections across page(s)"
