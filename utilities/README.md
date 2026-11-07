# Fixture Utilities

Manual tools for refreshing the cached MyPurdue API responses that
`src/Tests` replays offline. **Run these by hand only** - they query the live
class search service. Their request volume deliberately mirrors what production
`CatalogSync` does; do not wire them into cron or CI.

## `fetch-section-dumps.sh` - section search pages

Refreshes the paginated `searchResults` fixtures:

```
utilities/fetch-section-dumps.sh [TERM] [SUBJECT] [OUT_DIR]
# defaults: 202720 CS src/Tests/Resources
```

Replays the exact sequence from `MyPurdueConnection.GetSectionsAsync`
(session POST with a fresh `uniqueSessionId` -> `resetDataForm` -> paged GETs)
and writes `<TERM>-<SUBJECT>-section-dump-<pageOffset>-500.json` files - the
raw service response bytes, matching the committed fixture format. Requires
`curl`, `jq`, and `uuidgen`.

Keep every page of a term/subject from one run: pages cross-check against each
other's `totalCount`, and mixing an old page 0 with a new page 2 makes
replay inconsistent.

## `record-linked-sections/` - fetchLinkedSections responses

Refreshes `<TERM>-<SUBJECT>-linked-sections.json`, the fixture that
`FakeMyPurdueHandler` serves for `GroupLinkedSectionsAsync`:

```
cd utilities/record-linked-sections
dotnet run -- [TERM] [SUBJECT] [OUT_FILE]
# defaults: 202720 CS src/Tests/Resources/<TERM>-<SUBJECT>-linked-sections.json
```

Rather than synthesizing responses (link identifiers are per-component-type
buckets - they can never join a lecture to its labs), this runs the **real**
`MyPurdueConnection.GetSectionsAsync` + `GroupLinkedSectionsAsync` against the
live service behind a recording handler. The recorded set is therefore exactly
the queries production's heuristic makes (about one per class plus a few
lecture expansions - not one per section).

The tool prints a coverage report; every linked section in the fetched pages
should be covered (as a queried CRN or a member of some recorded response).
`FakeMyPurdueHandler` 404s on CRNs missing from the fixture, so if
`GroupLinkedSectionsAsync`'s query pattern ever changes, the offline tests
fail with "re-record the linked-sections fixture" - run this tool again and
re-run `dotnet test` to regenerate.
