-- Migration: 054_create_support_tickets_down
-- Description: Drops support ticket tables
-- Date: 2025-12-17

DROP TRIGGER IF EXISTS trg_generate_ticket_number ON support_tickets;
DROP FUNCTION IF EXISTS generate_ticket_number();
DROP SEQUENCE IF EXISTS ticket_number_seq;
DROP TABLE IF EXISTS faq_items;
DROP TABLE IF EXISTS support_ticket_messages;
DROP TABLE IF EXISTS support_tickets;
