-- Dirty read demo (MySQL 8+, InnoDB)
-- Open two SQL sessions: Session A and Session B
USE isolation_lab;

-- Session A
SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
START TRANSACTION;
UPDATE accounts SET balance = balance - 300 WHERE id = 1;
SELECT id, owner_name, balance FROM accounts WHERE id = 1;
-- Keep transaction open, do not COMMIT yet.

-- Session B (run while Session A is still open)
SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
START TRANSACTION;
SELECT id, owner_name, balance FROM accounts WHERE id = 1;
COMMIT;

-- Session A
ROLLBACK;

-- Session B (or any session)
SELECT id, owner_name, balance FROM accounts WHERE id = 1;

-- Expected:
-- Session B first sees 700 (uncommitted value), then after rollback sees 1000.
