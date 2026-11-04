# Step 11 — supplied-host integration profile

TARGET: uploaded Executor/GameAI reference, where ClientCard.ActionIndex is scalar and ActionActivateIndex is a description-to-index dictionary. This is not a universal adaptation for all WindBot forks.

Fixed prior incorrect array-style ActionIndex accesses in candidate generation and legal logs. Set/reposition now record scalar indices; phase transitions retain -1. Included CardValueEngine and GameStateAdapter, stripped code fences and removed standalone evaluator Program demo entrypoint. Included GameAI.cs built from supplied reference with observation hooks. Executor.reference.cs.txt is comparison only; your Executor must retain TryGetMainPhaseAction.

Install: replace existing RyzealExecutor, candidate, logger, profile, adapter, evaluator and observer classes; do not leave old duplicate copies. Add new files to project compile items as required by the host project. If your local GameAI has changed since upload, apply GameAI.integration.patch rather than overwrite it. If unchanged, supplied GameAI.cs is the integrated version. Do not compile both GameAI.cs and another GameAI definition. Likewise reference .txt files are not compile units.

Known limitations: no actual host C# build/assembly validation; enums/material prompt behavior and runtime game state must be checked locally. Node cost checks are heuristic and not a full Lua cost simulator. Replacement yes/no policy remains unfinished. Visibility filtering and chain-event correlation remain unfinished. Legacy policy is still gated behind candidate fallback and may be delayed when candidates exist. No guarantee of optimal play or full effect coverage.

NEXT REQUIRED CHECK: build in the actual WindBot solution before running example duels. If compiler errors occur, provide the error output and relevant ClientCard/MainPhase/HintMsg definitions. No replay needed yet.
