-- Practice 4 setup for isolation anomalies (MySQL 8+)
-- Run this script once before scenario scripts.

DROP DATABASE IF EXISTS isolation_lab;
CREATE DATABASE isolation_lab;
USE isolation_lab;

DROP TABLE IF EXISTS accounts;
CREATE TABLE accounts (
    id INT PRIMARY KEY,
    owner_name VARCHAR(50) NOT NULL,
    balance INT NOT NULL
);

INSERT INTO accounts (id, owner_name, balance) VALUES
(1, 'Alice', 1000),
(2, 'Bob', 500);


