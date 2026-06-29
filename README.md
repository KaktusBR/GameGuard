# GameGuard

A Windows parental game-blocker that runs as a SYSTEM service and is hardened against tampering by standard (child) user accounts.

> **Built with Claude.** GameGuard was designed and implemented end-to-end by
> [Claude](https://claude.com/claude-code) (Anthropic's Claude Code, model Claude Opus 4.8) — from
> brainstorming and the design spec through a test-driven, reviewed implementation.
> See [`docs/design.md`](docs/design.md) and [`docs/implementation-plan.md`](docs/implementation-plan.md).
> Open source under the [MIT License](LICENSE) — fork it, change it, ship it.

## What It Does

GameGuard prevents children from launching configured games, game launchers, or accessing browser-based gaming sites. It:

- Kills blocked processes as soon as they appear (process scanning loop).
- Injects DNS-level blocks into the Windows `hosts` file for browser games and game CDN domains.
- Locks the system in the blocked state by default — even after a reboot the child cannot play until a parent unlocks it.
- Shows the child a tray icon / dialog explaining they cannot play, and allows them to request a timed unlock by entering the parent code.

---

## Honest Limits

GameGuard is designed to stop a casual child, not a determined attacker. Know the following:

- **Safe Mode / Recovery Console** — Windows Safe Mode does not run third-party services. A child who boots into Safe Mode can launch blocked games. Mitigate by setting a BIOS/UEFI password and disabling alternative boot devices.
- **USB-boot / second OS** — Booting from a USB drive or a second OS installation completely bypasses GameGuard (it lives on the primary Windows install). Lock down the BIOS boot order and set a BIOS password to close this gap.
- **Second admin account** — Any Windows administrator can stop the service (`sc stop GameGuard`) or delete the `hosts` entries. GameGuard is not a substitute for limiting which accounts have administrator rights on the machine.

---

## Components

| Component | Role |
|---|---|
| **GameGuard.Service** | SYSTEM Windows Service — enforces process blocking and `hosts` injection; exposes a hardened named pipe (`GameGuardPipe`) for inter-process communication. |
| **GameGuard.Agent** | Per-session tray application — shows the "blocked" notification and the unlock dialog in the child's interactive desktop session. |
| **GameGuard.Admin** | UAC-elevated Admin tool — lets the parent set/change the PBKDF2-hashed unlock code and edit the game/folder/domain blocklist stored in `C:\ProgramData\GameGuard\config.json`. |

---

## Build Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (includes the `dotnet` CLI)
- Windows 10 / Windows 11 (x64)

To verify your SDK:

```powershell
dotnet --version   # should print 8.x.x
```

---

## Installation

1. **Open an elevated PowerShell** (right-click PowerShell → "Run as Administrator").

2. **Run the install script** from the repo root:

   ```powershell
   .\install\Install-GameGuard.ps1
   ```

   This will:
   - Publish all three projects to `.\publish\` as self-contained win-x64 executables.
   - Register `GameGuard` as an auto-start Windows Service running as `LocalSystem`, with automatic restart-on-crash recovery.
   - Register a scheduled task (`GameGuardAgent`) that launches the tray Agent at logon for all standard users (`BUILTIN\Users`).
   - Start the service immediately.

3. **Set the parent unlock code** — launch the Admin tool (path printed by the script) with elevation and enter a strong code. Until a code is set the service blocks but no unlock is possible.

   ```powershell
   # Example path — adjust to your actual publish output
   & ".\publish\admin\GameGuard.Admin.exe"
   ```

---

## How Unlock Works

1. The child clicks the tray icon or the "GameGuard" block notification.
2. An unlock dialog appears asking for the **parent code**.
3. The parent enters the code and selects a duration (e.g. 1 hour, 2 hours).
4. The service verifies the code against the stored PBKDF2 hash, lifts the process block and removes the `hosts` entries for the selected period.
5. When the timer expires the service **automatically re-locks** — the `hosts` block is re-injected and process scanning resumes. No manual re-lock is needed.

### Brute-Force Lockout

After **5 consecutive wrong code attempts** the service enforces an escalating backoff lockout:

- 1st lockout: 30 seconds
- Each subsequent lockout doubles the wait (60 s → 120 s → … capped at **1 hour**)

During a lockout, unlock attempts are rejected immediately without checking the code. **Parents must also wait out any active lockout** — there is no override shortcut. If you forget your code, uninstall and reinstall the service from an admin account.

---

## Adding New Games, Folders, or Domains

Launch the Admin tool (elevated) and edit the blocklist:

- **Executables / paths** — add the full path to the game's `.exe` or the folder containing it. Any process whose image path starts with a blocked folder path will be killed.
- **Domains** — add domain names (e.g. `miniclip.com`, `poki.com`). The service writes `0.0.0.0 <domain>` blocks into the Windows `hosts` file, redirecting DNS lookups to the loopback address so browsers cannot reach the site.

Changes take effect immediately — the service picks up the new config within its next scan cycle (default: 5 seconds).

---

## Uninstallation

Run the uninstall script from an elevated PowerShell:

```powershell
.\install\Uninstall-GameGuard.ps1
```

This stops and removes the Windows Service, unregisters the logon scheduled task, and strips the `# BEGIN GameGuard … # END GameGuard` block from the `hosts` file. Configuration and logs under `C:\ProgramData\GameGuard` are left in place — delete that folder manually if you no longer need the history.

---

## Running Tests

```powershell
dotnet test
```

All 33 unit tests should pass. The test suite covers code hashing, config serialisation, lock manager state, process scanning, hosts-file editing, pipe protocol, and unlock handler logic.

---

## License

[MIT](LICENSE) — © 2026 KaktusBR. You are free to use, copy, modify, merge, publish,
distribute, sublicense, and sell copies, including commercially. Just keep the copyright
and license notice. Forks and pull requests welcome.

## Authorship

This project was created with [Claude Code](https://claude.com/claude-code) (Anthropic's
Claude Opus 4.8). Claude drove the full workflow — requirements brainstorming, the design
spec, the bite-sized implementation plan, and a test-driven implementation executed and
reviewed task-by-task (including an automated security pass that added the brute-force
lockout and named-pipe hardening). Commits are co-authored accordingly.
