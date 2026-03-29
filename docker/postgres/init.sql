-- docker/postgres/init.sql
-- Runs ONCE when the Postgres container is first created
-- (Only runs if the data volume is empty — not on every restart)
-- Interview answer: "What is an init script in Postgres Docker?"
-- Lets you set up schemas, extensions, or seed data automatically
-- without manual intervention after docker compose up

-- Enable the uuid-ossp extension for UUID generation
-- (EF Core handles UUIDs in C#, but good practice to have DB support too)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Grant all privileges to our app user
GRANT ALL PRIVILEGES ON DATABASE bookvault_catalog TO bookvault;

-- Log that init ran (visible in docker compose logs)
DO $$
BEGIN
  RAISE NOTICE 'BookVault database initialized successfully';
END $$;