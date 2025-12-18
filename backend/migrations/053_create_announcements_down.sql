-- Migration: 053_create_announcements_down
-- Description: Drops announcements tables
-- Date: 2025-12-17

DROP TABLE IF EXISTS announcement_reads;
DROP TABLE IF EXISTS announcements;
