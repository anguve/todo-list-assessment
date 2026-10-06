# Fieldbook

A private task list. You sign in, see only your own tasks, add items, edit them, and delete them.

The API is ASP.NET Core on .NET 10. The client is Angular 22. Users and tasks are stored in memory, so there is no database to install.

## Prerequisites

- .NET 10 SDK
- Node.js 22 or newer
- npm

## Run the API

HTTPS is required, including in development. Trust the local development certificate once:

```bash
dotnet dev-certs https --trust
```

The signing key is not in the repository. Set it with user secrets (Development only):

```bash
cd backend/Tasks.Api
dotnet user-secrets set "Jwt:Key" "replace-with-at-least-32-random-characters"
dotnet run
```

You can use the `Jwt__Key` environment variable instead of user secrets. The value must be at least 32 characters. Do not put it in `appsettings.json`.

The API listens on `https://localhost:5001`.

## Run the Angular app

```bash
cd frontend
npm install
npm start
```

Open `http://localhost:4200`. The dev server proxies `/api` to the API, so the browser does not call the API origin directly.

## Create an account

Use **Create an account** in the app. A valid password has at least 8 characters and includes letters and numbers. Registration signs you in and opens your task list.

You can also call the API:

```bash
curl -k -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"ada@example.com","password":"password1","displayName":"Ada"}'
```

## Development user

When `ASPNETCORE_ENVIRONMENT` is `Development`, the API seeds one user if it is not already there:

| Email | Password |
| --- | --- |
| `demo@fieldbook.test` | `Demo1234` |

That user is not created while running the automated API tests.

## In-memory data

Users and tasks live in an EF Core InMemory store. Restarting the API removes every account and every task, including the development user, which is seeded again on the next Development start.

## Tests

API integration tests:

```bash
dotnet test
```

Angular unit tests:

```bash
cd frontend
npm test
```

End-to-end test (register, sign in, add a task, edit it, delete it, sign out). Configure `Jwt:Key` first, the same way you do to run the API. Playwright starts the API and the Angular dev server if they are not already running:

```bash
cd frontend
npx playwright install chromium
npm run e2e
```

## Field rules

Every text field is cleaned, then checked for length and a pattern. The Angular forms and the API use the same limits.

| Field | Length | Allowed after cleaning |
| --- | --- | --- |
| Name | 2–80 | Letters, numbers, spaces, apostrophes, hyphens |
| Email | 6–254 | A normal email address, stored in lowercase |
| Password | 8–64 | Letters, numbers, and a short list of punctuation. No spaces. Must include a letter and a number |
| Task title | 2–120 | Letters, numbers, and simple punctuation |
| Task description | 2–400 | Letters, numbers, and simple punctuation |

Cleaning removes control characters and HTML tags. A password is rejected rather than rewritten, so the stored secret matches what was typed. A task title such as `Buy <b>milk</b>` is stored as `Buy milk`. The description is cleaned the same way.

## Security decisions

Identity stores the users and hashes passwords with `PasswordHasher<T>` (PBKDF2 and a salt). The API does not use a hand-rolled hash, and it never stores or logs a plaintext password.

The endpoints are custom instead of `MapIdentityApi`. The required contract is specific: `POST /api/auth/register` returns 201, a duplicate email returns 409, invalid input returns 400 with ProblemDetails, and `POST /api/auth/login` returns a bearer token or 401 with the same message for a bad password and an unknown email (`Email or password is incorrect`). `MapIdentityApi` uses different routes and status codes. JWT is still short-lived (30 minutes) and is checked for issuer, audience, and expiry.

The signing key comes from user secrets or `Jwt__Key`. It is not in `appsettings.json` or in git.

The Angular client keeps the access token in `localStorage`. That is acceptable for this exercise and is vulnerable to XSS: any script that runs in the page can read it. In production I would keep the session in an `HttpOnly`, `Secure`, `SameSite` cookie so browser script cannot read it.

Sign out calls `POST /api/auth/logout`. The API remembers that token's id in memory until the token would have expired, and later requests with the same token return 401. A new sign-in issues a new token. Restarting the API clears that list together with the rest of the in-memory data, so a token that was never revoked still works until it expires. The log line records the user id and the request facts, not the token.

Every API response sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy` with camera, microphone, and location disabled, `Cache-Control: no-store`, and a content security policy of `default-src 'none'`. The Angular dev server sends the same framing and referrer headers. Its content security policy allows the scripts and the live-reload socket that `npm start` needs, including `unsafe-eval`. A production host should drop `unsafe-eval`.

The user id is the `sub` claim on the token. The client never sends it. `GET /api/todos` is filtered by that id. Deleting another person's task returns 404, not 403, so the response does not reveal that the task exists. HTTPS redirection is on outside the test host.

## Request throttle

Every request is limited per IP address, using a one-minute window:

| Route | Limit |
| --- | --- |
| `POST /api/auth/login` and `POST /api/auth/register` | 8 requests |
| Every other request | 120 requests |

Login and registration count against both limits, so the stricter one applies. A rejected request returns 429 with ProblemDetails (`Wait a moment and try again.`) and writes a throttle line to the log. The limits are `Throttle:AuthPermitLimit` and `Throttle:ApiPermitLimit` in `appsettings.json`. Automated tests raise them so the suite does not trip the limit.

## Logs

The API writes three kinds of lines, both to the console and to `backend/Tasks.Api/logs/fieldbook.log`. Each request line records the client IP, HTTP method, path, user agent, and trace id. None of them include a password, a token, a request body, a query string, or an `Authorization` header. A failed sign-in does not record the email.

- **Errors.** Every response with status 400 or higher is logged with the status and the request facts above. An unhandled exception is logged with its stack on the server and returned to the client as a generic 500 ProblemDetails message, without the exception text.
- **Sign-ins.** A successful sign-in records the user id and the request facts. A failed sign-in records the same request facts and only that it failed, with the same line whether the email exists or not. Sign-out records the user id and the request facts after the token is revoked.
- **Transactions.** Creating an account, creating a task, editing a task, and deleting a task each write a transaction line with the user id, the request facts, and, for a task, the task id. Seeding the development user has no request, so that line records only the user id. An edit or delete that does not match the signed-in user is logged as a missed transaction and returned as 404.

The in-memory store does not support database transactions. Each write is a single `SaveChanges`, and the log line above is the record of that change.

In production I would replace the in-memory store with a real database, rotate refresh tokens, move the session to the cookie described above, and add email confirmation and password reset. OAuth, roles, and 2FA are out of scope for this test.

## Project layout

- `backend/Tasks.Api` — API, Identity, JWT, task repository
- `backend/Tasks.Api.Tests` — `WebApplicationFactory` integration tests
- `frontend` — Angular app, unit tests, and the Playwright flow
