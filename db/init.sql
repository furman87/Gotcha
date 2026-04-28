CREATE TABLE IF NOT EXISTS games (
    id UUID PRIMARY KEY,
    word TEXT NOT NULL,
    previous_word TEXT,
    status TEXT NOT NULL DEFAULT 'waiting',
    guesses_allowed INT NOT NULL DEFAULT 6,
    bonus_guesses INT NOT NULL DEFAULT 3,
    swap_used BOOLEAN NOT NULL DEFAULT FALSE,
    swap_occurred_after_guess INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS guesses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_id UUID NOT NULL REFERENCES games(id),
    guess_number INT NOT NULL,
    word TEXT NOT NULL,
    result JSONB NOT NULL,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
