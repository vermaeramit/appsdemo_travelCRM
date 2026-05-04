-- Add notes column to bookings for internal notes
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS notes TEXT;
