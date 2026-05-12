CREATE TABLE IF NOT EXISTS players (
    id SERIAL PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS words (
    id SERIAL PRIMARY KEY,
    word_value VARCHAR(5) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS games (
    id SERIAL PRIMARY KEY,
    player_id INT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    hidden_word VARCHAR(5) NOT NULL,
    attempts_used INT NOT NULL,
    score INT NOT NULL DEFAULT 0,
    is_win BOOLEAN NOT NULL DEFAULT FALSE,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    finished_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS game_moves (
    id SERIAL PRIMARY KEY,
    game_id INT NOT NULL REFERENCES games(id) ON DELETE CASCADE,
    attempt_number INT NOT NULL,
    guess_word VARCHAR(5) NOT NULL,
    feedback VARCHAR(5) NOT NULL
);

INSERT INTO words (word_value)
VALUES
    ('APPLE'), ('MANGO'), ('GRAPE'), ('TRAIN'), ('PLANT'),
    ('BRAIN'), ('CLOUD'), ('STONE'), ('RIVER'), ('OCEAN'),
    ('FLAME'), ('BEACH'), ('STORM'), ('DANCE'), ('MUSIC'),
    ('HOUSE'), ('HEART'), ('PEACE'), ('SMILE'), ('LUCKY')
ON CONFLICT (word_value) DO NOTHING;