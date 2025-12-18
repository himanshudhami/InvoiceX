-- Migration: 058_create_asset_requests_down.sql
-- Description: Drops the asset_requests table
-- Date: 2024-12-18

DROP TRIGGER IF EXISTS trg_asset_requests_updated_at ON asset_requests;
DROP FUNCTION IF EXISTS update_asset_requests_updated_at();
DROP TABLE IF EXISTS asset_requests;
