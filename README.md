# Gotcha

A two-player adversarial word-guessing game. One player picks the word; the other tries to guess it. The twist: the setter can swap the word once mid-game — but only if they respect every letter the guesser has already uncovered.

---

## How It Works

### Roles

| Role | What they do |
|---|---|
| **Setter** | Creates the game, picks a 5-letter target word, watches the guesses come in, and may trigger one word swap. |
| **Guesser** | Receives a link, submits guesses Wordle-style, and tries to crack the word before running out of chances. |

### Standard Rules (Hard Mode Wordle)

- All words are exactly 5 letters and must be valid English words.
- Each guess is scored per letter:
  - 🟩 **Green** — correct letter, correct position
  - 🟨 **Yellow** — correct letter, wrong position
  - ⬜ **Gray** — letter not in the word
- Hard mode is enforced: every green letter must reappear in the same position in later guesses; every yellow letter must appear somewhere.
- The guesser gets **6 guesses** by default.

### The Swap

After any guess (but before the next one is submitted), the setter may swap the target word **once**. The swap is constrained:

- Every **green** letter from any previous guess must remain green in the new word (same letter, same position).
- Every **yellow** letter from any previous guess must appear somewhere in the new word.
- The new word must be a valid 5-letter word from the word list.

When a swap occurs:

- All previous guesses are **re-evaluated** against the new word and tile colours update accordingly.
- The guesser is notified that the word changed and receives **bonus guesses** (configurable by the setter, default 3).
- The setter's swap button is permanently disabled afterwards.

### Winning and Losing

- **Guesser wins** — guesses the exact word within the allowed attempts.
- **Setter wins** — the guesser exhausts all guesses without success.
- Once the game ends (either outcome), both URLs become read-only: they show the result and reveal the answer but cannot start a new game.

---

## Architecture

```
Browser (Setter)          Browser (Guesser)
      │                          │
      │  HTTPS                   │  HTTPS
      ▼                          ▼
┌─────────────────────────────────────┐
│         nginx  (host, furman87.com) │  ← TLS termination, port 443
└──────────────┬──────────────────────┘
               │ http://127.0.0.1:8079
               ▼
┌──────────────────────────────────────┐
│  Docker network: gotcha_net          │
│                                      │
│  ┌─────────────────────────────┐     │
│  │  web  (nginx:alpine)        │     │  ← port 8079 bound to 127.0.0.1
│  │  Serves Blazor WASM files   │     │
│  │  Proxies /api/* → api:8080  │     │
│  └──────────────┬──────────────┘     │
│                 │ http://api:8080     │
│  ┌──────────────▼──────────────┐     │
│  │  api  (.NET 10 ASP.NET Core)│     │  ← internal only
│  │  Minimal API endpoints      │     │
│  └──────────────┬──────────────┘     │
│                 │                    │
│  ┌──────────────▼──────────────┐     │
│  │  db  (PostgreSQL 16)        │     │  ← internal only
│  └─────────────────────────────┘     │
└──────────────────────────────────────┘
```

### Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 Minimal APIs |
| Frontend | Blazor WebAssembly (.NET 10) |
| Database | PostgreSQL 16 with Dapper |
| Styling | Tailwind CSS |
| Container runtime | Docker Compose |
| Reverse proxy | nginx (host) + nginx:alpine (container) |

### URL Design

No authentication. Access is controlled by knowing the correct URL:

| URL | Who uses it |
|---|---|
| `/game/{id}/set` | Setter — enter word, watch guesses, trigger swap |
| `/game/{id}/guess` | Guesser — submit guesses, see tile feedback |

The setter shares the `/guess` URL with the other player out-of-band.

### API Endpoints

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/games` | Create a new game; returns setter and guesser URLs |
| `GET` | `/api/games/{id}` | Get game state (`?setter=true` reveals target word) |
| `POST` | `/api/games/{id}/word` | Setter submits the initial word and bonus guess count |
| `POST` | `/api/games/{id}/guess` | Guesser submits a guess |
| `POST` | `/api/games/{id}/swap` | Setter swaps the target word |

### Database Schema

```sql
games (
  id UUID PRIMARY KEY,
  word TEXT,
  previous_word TEXT,          -- populated after swap
  status TEXT,                 -- waiting | active | won | lost
  guesses_allowed INT,
  bonus_guesses INT,
  swap_used BOOLEAN,
  swap_occurred_after_guess INT,
  created_at TIMESTAMPTZ,
  ended_at TIMESTAMPTZ
)

guesses (
  id UUID PRIMARY KEY,
  game_id UUID REFERENCES games(id),
  guess_number INT,
  word TEXT,
  result JSONB,                -- [{letter, status: green|yellow|gray}, ...]
  submitted_at TIMESTAMPTZ
)
```

---

## Project Structure

```
Gotcha.slnx
├── Gotcha.Api/                  ← ASP.NET Core backend
│   ├── Endpoints/
│   │   └── GameEndpoints.cs     — all 5 minimal API routes
│   ├── Services/
│   │   ├── GameService.cs       — hard-mode & swap validation, state builder
│   │   ├── GuessEvaluationService.cs  — two-pass green/yellow/gray scoring
│   │   └── WordValidationService.cs  — answer list + guess list validation
│   ├── Data/
│   │   └── GameRepository.cs    — Dapper queries against PostgreSQL
│   ├── Models/
│   │   └── DTOs.cs              — request/response records
│   └── wwwroot/wordlists/
│       ├── answers.txt          — ~2 300 valid target words
│       └── guesses.txt          — ~10 000 valid guess words
│
├── Gotcha.Client/               ← Blazor WebAssembly frontend
│   ├── Pages/
│   │   ├── Home.razor           — game creation page
│   │   ├── SetterView.razor     — /game/{id}/set
│   │   └── GuesserView.razor    — /game/{id}/guess
│   ├── Components/
│   │   ├── TileGrid.razor       — guess history with colour-coded tiles
│   │   ├── Keyboard.razor       — on-screen keyboard with letter state
│   │   ├── SwapPanel.razor      — word-swap form with constraint preview
│   │   └── GuessBanner.razor    — "word changed" notification banner
│   └── Services/
│       └── GameApiService.cs    — typed HTTP client wrapper
│
├── nginx/
│   ├── app.conf                 — Docker web container nginx config
│   └── gotcha.furman87.com.conf — host nginx config (copy to sites-available)
├── db/
│   └── init.sql                 — creates tables on first DB start
├── Dockerfile                   — multi-stage: build → api + web targets
├── docker-compose.yml
└── .env.example                 — copy to .env and set POSTGRES_PASSWORD
```

---

## Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL running locally (or via Docker)

### 1. Database

Create the database and tables:

```bash
psql -U postgres -c "CREATE DATABASE gotcha;"
psql -U postgres -d gotcha -f db/init.sql
```

Or if you have Docker:

```bash
docker run -d --name gotcha-db \
  -e POSTGRES_DB=gotcha \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16-alpine
docker exec -i gotcha-db psql -U postgres -d gotcha < db/init.sql
```

### 2. API

```bash
cd Gotcha.Api
dotnet run
# Listening on http://localhost:5004
```

Connection string and other settings live in `appsettings.json`. Override locally via `appsettings.Development.json` or user secrets.

### 3. Client

```bash
cd Gotcha.Client
dotnet run
# Listening on http://localhost:5213
```

The client reads `ApiBaseUrl` from `wwwroot/appsettings.Development.json` (`http://localhost:5004` by default). Both views poll the API every 2 seconds — open the setter in one tab and the guesser in another.

---

## Production Deployment

### Prerequisites on the server

- Docker + Docker Compose
- nginx installed on the host (not in Docker)
- certbot for Let's Encrypt

### 1. Clone and configure

```bash
git clone <repo-url> /opt/gotcha
cd /opt/gotcha
cp .env.example .env
nano .env   # set a strong POSTGRES_PASSWORD
```

### 2. TLS certificate

```bash
sudo certbot --nginx -d gotcha.furman87.com
```

### 3. Host nginx

```bash
sudo cp nginx/gotcha.furman87.com.conf /etc/nginx/sites-available/gotcha.furman87.com
sudo ln -s /etc/nginx/sites-available/gotcha.furman87.com /etc/nginx/sites-enabled/
sudo nginx -t && sudo nginx -s reload
```

### 4. Start the app

```bash
docker compose up -d --build
```

The `web` container binds to `127.0.0.1:8079`. Host nginx proxies `gotcha.furman87.com` → `127.0.0.1:8079`. The `api` and `db` containers are only reachable inside the `gotcha_net` Docker network.

### Subsequent deploys

```bash
git pull
docker compose up -d --build
```

---

## Configuration

### API (`appsettings.json`)

| Key | Default | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;...` | PostgreSQL connection string |
| `ClientBaseUrl` | `http://localhost:5213` | Base URL used to build game links returned by `POST /api/games` |
| `AllowedOrigins` | localhost variants | CORS allowed origins |
| `GameSettings:DefaultGuessesAllowed` | `6` | Guesses per game |
| `GameSettings:DefaultBonusGuessesOnSwap` | `3` | Extra guesses added when setter swaps |
| `GameSettings:WordLength` | `5` | Letter count (informational; logic assumes 5) |

In production these are set via environment variables in `docker-compose.yml` (using `__` as the path separator, e.g. `ConnectionStrings__DefaultConnection`).

### Setter-configurable (per game)

When setting the initial word, the setter can choose how many bonus guesses to award the guesser if a swap is used (0–9, default 3).
