-- ContaNexo - Base de datos MySQL
-- Ejecutar: mysql -u root -p < database/schema.sql

CREATE DATABASE IF NOT EXISTS calcas_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE calcas_db;

-- Nota: La API crea las tablas automaticamente con Entity Framework (EnsureCreated)
-- y carga datos iniciales via DbSeeder al iniciar.

-- Usuario admin por defecto (creado por la API):
-- Email: admin@contanexo.com
-- Password: Admin123!
