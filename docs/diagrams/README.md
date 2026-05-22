# Diagrams

C4 diagrams (levels 1-3) + sequence diagrams of the architecture, maintained as Mermaid source (`.mmd`).

| # | File | Type | Description |
|---|---|---|---|
| 1 | [`01-context.mmd`](01-context.mmd) | C4 - Context | External actors + system |
| 2 | [`02-container.mmd`](02-container.mmd) | C4 - Container | Microservices, databases, broker, cache |
| 3 | [`03-component-transactions.mmd`](03-component-transactions.mmd) | C4 - Component | Clean Architecture of the Transactions service |
| 4 | [`04-flow-registration.mmd`](04-flow-registration.mmd) | Sequence | End-to-end transaction registration |
| 5 | [`05-flow-query.mmd`](05-flow-query.mmd) | Sequence | Daily-balance query with cache-aside |

## View

**Option 1 - GitHub** (recommended): GitHub renders Mermaid natively. Just open the `.mmd` file (or the blocks in [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md)).

**Option 2 - Mermaid Live Editor**: paste the contents of any `.mmd` into https://mermaid.live and export PNG/SVG from the interface.

**Option 3 - Export locally** (generates PNGs into `docs/diagrams/exports/`):

```bash
./scripts/export-diagrams.sh
# or
make diagrams
```

Requirement: Node.js >= 18 (downloads `@mermaid-js/mermaid-cli` via `npx` on the first run).

## Conventions

- **Colors and style**: Mermaid theme default (we do not customize to keep portability).
- **Updates**: when changing the architecture, update the `.mmd` AND the corresponding block in `ARCHITECTURE.md` to keep them in sync.
