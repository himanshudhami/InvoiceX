-- Migration: 055_create_employee_documents_down
-- Description: Drops employee documents tables
-- Date: 2025-12-17

DROP TABLE IF EXISTS document_requests;
DROP TABLE IF EXISTS employee_documents;
