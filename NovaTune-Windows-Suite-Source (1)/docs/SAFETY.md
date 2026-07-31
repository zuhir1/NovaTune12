# Safety contract

NovaTune treats system optimization as a transaction, not a collection of registry hacks.

- No issue is fixed during scanning.
- Every change has an explanation, estimated impact, exact plan, and confirmation.
- A restore point is mandatory for a cleanup/fix batch. Failure aborts the batch.
- Files are moved to quarantine with a manifest; undo checks destination conflicts.
- Registry and configuration changes must export original values before writes.
- Security findings are advisory and are never deleted automatically.
- Registry cleaners, RAM "boosters", Prefetch deletion, service disabling, driver download scraping, DLL mass-registration, and blanket permission resets are intentionally excluded because they are commonly harmful or misleading.
- CHKDSK `/f`, Winsock reset, DISM repair, and similar disruptive commands require a dedicated confirmation that states restart/network impact.

Before commercial distribution: threat-model privileged IPC, sign all binaries, enable MSIX/AppInstaller signing, add WDAC/SmartScreen testing, fuzz parsers, run Windows HLK-style compatibility testing, and complete an independent security review.
