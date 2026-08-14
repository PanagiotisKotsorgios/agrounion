# Coolify deployment

This repository includes `docker-compose.coolify.yml` for production deployment. It runs the ASP.NET Core application and a private MySQL service in one Coolify resource. Database data, application logs, uploaded release assets, and ASP.NET Core Data Protection keys use persistent named volumes.

## First deployment

1. Point the domain's `A` record (and `AAAA`, if used) to the Coolify server.
2. In Coolify, open **Sources**, add a GitHub App, and grant it access to `PanagiotisKotsorgios/agrounion`.
3. Create a resource using **Private Repository (with GitHub App)** and select this repository.
4. Use these build settings:
   - Branch: `main`
   - Build Pack: `Docker Compose`
   - Base Directory: `/`
   - Docker Compose Location: `/docker-compose.coolify.yml`
5. After Coolify loads the Compose file, assign only the `app` service a domain. Because the container listens on port 8080, enter it as `https://example.com:8080`. Do not assign a domain to `mysql`.
6. Review the generated environment variables. Coolify generates the MySQL, JWT, admin, and demo secrets referenced by the `SERVICE_*` variables. Set `ADMIN_EMAIL` and the optional `SMTP_*` variables if email delivery is required.
7. Click **Deploy**, then check the application URL and the deployment logs.

The application exposes `/health`. Coolify uses it to confirm that both the web process and MySQL connection are ready before routing traffic.

To retrieve the initially seeded login passwords, reveal the generated values in the resource's environment variables and add the shown prefix:

- Admin password: `Admin1!` + `SERVICE_HEX_64_ADMIN`
- Demo-user password: `Demo1!` + `SERVICE_HEX_64_DEMO`

The seeded emails are documented in `README.md`. `SeedData__PasswordVersion` makes password rotation idempotent: a new version resets the built-in seeded accounts once, while ordinary redeployments do not overwrite passwords changed by users.

If the resource was created before these hex-based passwords were introduced, remove the now-unused `SERVICE_PASSWORDWITHSYMBOLS_64_ADMIN` and `SERVICE_PASSWORDWITHSYMBOLS_64_DEMO` entries from Coolify after a successful deployment. This removes Docker Compose warnings caused by `$` characters in the old generated values.

## Automatic deployments

GitHub App resources normally enable auto-deploy automatically. In the resource's **Advanced** settings, confirm that **Auto Deploy** is enabled. Each push to `main` will then build and deploy the new commit.

The normal workflow from this workspace is:

```powershell
git status
git add -A
git commit -m "Describe the change"
git push origin main
```

Do not commit `.env` files or paste production secrets into the Compose files. Manage production values in Coolify.

## Backups

Configure scheduled Coolify backups for the MySQL volume/database before storing production data. The `agro_releases` volume should also be included in server-level backups if uploaded release assets are important.
