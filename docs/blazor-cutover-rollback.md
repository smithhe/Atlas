# Phase 8 rollback

React is removed in a **standalone commit** so `git revert <delete-sha>` restores `src/atlas.ui`. Hosting/CI/Docker changes live in earlier Phase 8 commits and can be reverted independently if a React image must be rebuilt.

SHAs below are on the **rebased umbrella history** that merges to `main`. The pre-rebase delete `57e85657a551c364d3cc7ded33778bc38940aea1` is **not** an ancestor of this branch — do not revert it.

## Recorded SHAs

| What | SHA | Notes |
| --- | --- | --- |
| Last React-only `main` | `7f189ed60a37350b4de162f116296f47804822e3` | `Bump the npm_and_yarn group across 1 directory with 2 updates` |
| Last umbrella commit that still contains React (pre–Phase 8) | `4034942e6e7adbc146e8f7387cd271b7de688641` | `Updated Blazor UI to have the same look and feel as the react ui` |
| Standalone `src/atlas.ui` source delete | `ef3285e447ff7683bfea9234bbb369c989ba299c` | Restores the React tree except `package.json` / lockfile (those were left behind) |
| Residual npm manifests | `de80286629fc66c9454a59fb9d07b4c82b98ca78` | Completes the hard delete of `src/atlas.ui` |

There is no published container registry for Atlas UI images. The last React `Dockerfile.ui` was Node 22 Vite → `nginx:1.27-alpine`. To rebuild that image, check out the last React-only `main` SHA (or revert the delete commits + `Dockerfile.ui`) and run `docker compose build ui`.

## Restore React hosting

1. `git revert de80286629fc66c9454a59fb9d07b4c82b98ca78` — restores leftover `package.json` / lockfile.
2. `git revert ef3285e447ff7683bfea9234bbb369c989ba299c` — restores the React source tree.
3. Revert Phase 8 `Dockerfile.ui`, `docker-compose.yml` (`API_BASE_URL` → `VITE_API_BASE_URL`), `docker/nginx/default.conf`, and frontend CI if nginx must serve the Vite `dist/` again.
4. Playwright after a React restore would still live under `tests/e2e/` (Blazor `webServer`); point `webServer.command` back at Vite preview if you need React e2e.
