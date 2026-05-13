-- Non-repeatable read demo
USE isolation_lab;

-- Reset state if needed
UPDATE accounts SET balance = 1000 WHERE id = 1;

-- Session A
SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;
START TRANSACTION;
SELECT balance FROM accounts WHERE id = 1; -- expected 1000

-- Session B
SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;
START TRANSACTION;
UPDATE accounts SET balance = 1300 WHERE id = 1;
COMMIT;

-- Session A (same transaction as first SELECT)
SELECT balance FROM accounts WHERE id = 1; -- expected 1300 (changed)
COMMIT;

-- Expected:
-- In one transaction Session A read the same row twice and got different values.
