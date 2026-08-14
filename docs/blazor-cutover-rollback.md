# Phase 8 rollback

React is removed in a **standalone commit** so `git revert <delete-sha>` restores `src/atlas.ui`. Hosting/CI/Docker changes live in earlier Phase 8 commits and can be reverted independently if a React image must be rebuilt.

## Recorded SHAs

| What | SHA | Notes |
| --- | --- | --- |
| Last React-only `main` | `7f189ed60a37350b4de162f116296f47804822e3` | `Bump the npm_and_yarn group across 1 directory with 2 updates` |
| Last umbrella commit that still contains React (pre–Phase 8) | `cd1b4aea751a16460579f8be671808348299e309` | `Updated Blazor UI to have the same look and feel as the react ui` |
| Standalone `src/atlas.ui` delete | `57e85657a551c364d3cc7ded33778bc38940aea1` | `git revert 57e85657a551c364d3cc7ded33778bc38940aea1` restores the React tree |

There is no published container registry for Atlas UI images. The last React `Dockerfile.ui` was Node 22 Vite → `nginx:1.27-alpine`. To rebuild that image, check out the last React-only `main` SHA (or revert the delete + Dockerfile.ui) and run `docker compose build ui`.

## Restore React hosting

1. `git revert <standalone-delete-sha>` — restores `src/atlas.ui`.
2. Revert Phase 8 `Dockerfile.ui`, `docker-compose.yml` (`API_BASE_URL` → `VITE_API_BASE_URL`), `docker/nginx/default.conf`, and frontend CI if nginx must serve the Vite `dist/` again.
3. Playwright after a React restore would still live under `tests/e2e/` (Blazor `webServer`); point `webServer.command` back at Vite preview if you need React e2e.
