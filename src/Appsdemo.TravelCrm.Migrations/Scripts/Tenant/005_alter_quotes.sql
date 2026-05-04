-- Add customer_name and notes to quotes for standalone quotes and general notes
ALTER TABLE quotes
    ADD COLUMN IF NOT EXISTS customer_name VARCHAR(200),
    ADD COLUMN IF NOT EXISTS notes TEXT;
