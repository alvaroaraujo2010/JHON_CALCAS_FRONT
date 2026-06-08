-- ============================================================================
-- Migración 001: Deducciones Art. 387 ET (Ley 2277/2022)
-- Fecha:        2026-06-03
-- Aplica a:     ContaNexo v1
-- Responsable:  Backend (modelo Employee)
-- Reversible:   Sí (ver bloque ROLLBACK al final)
-- Notas:
--   El proyecto usa EnsureCreated() en Program.cs, por lo que este script
--   debe ejecutarse UNA SOLA VEZ contra cada base de datos existente.
--   En bases nuevas, EnsureCreated() creará las columnas automáticamente
--   a partir del modelo C# actualizado.
--
--   Topes (vigentes desde mayo 2023, indexados por UVT del año gravable):
--     * 1 dependiente económico:    10% ingresos,  máx 32  UVT/mes
--     * Intereses de vivienda:                  máx 100 UVT/mes
--     * Medicina prepagada:                     máx 16  UVT/mes
--     * Aportes AFC/FVP:           30% ingresos,  máx 3.800 UVT/año
-- ============================================================================

USE contanexo;

START TRANSACTION;

-- Verificar que la tabla exista (defensa contra BD equivocada)
SELECT COUNT(*) INTO @tbl_exists
FROM information_schema.tables
WHERE table_schema = DATABASE() AND table_name = 'Employees';

SET @msg = IF(@tbl_exists = 0,
    'ERROR: La tabla Employees no existe en esta base de datos. Abortando.',
    'Tabla Employees encontrada. Procediendo con la migración.');
SELECT @msg AS status;

-- Idempotencia: cada ALTER se hace solo si la columna no existe.
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM information_schema.columns
       WHERE table_schema = DATABASE() AND table_name = 'Employees' AND column_name = 'HasDependents') = 0,
    'ALTER TABLE Employees ADD COLUMN HasDependents TINYINT(1) NOT NULL DEFAULT 0 AFTER SolidarityFundOverride',
    'SELECT "HasDependents ya existe, se omite" AS info'
));
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM information_schema.columns
       WHERE table_schema = DATABASE() AND table_name = 'Employees' AND column_name = 'HousingInterestEnabled') = 0,
    'ALTER TABLE Employees ADD COLUMN HousingInterestEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER HasDependents',
    'SELECT "HousingInterestEnabled ya existe, se omite" AS info'
));
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM information_schema.columns
       WHERE table_schema = DATABASE() AND table_name = 'Employees' AND column_name = 'PrepaidHealthEnabled') = 0,
    'ALTER TABLE Employees ADD COLUMN PrepaidHealthEnabled TINYINT(1) NOT NULL DEFAULT 0 AFTER HousingInterestEnabled',
    'SELECT "PrepaidHealthEnabled ya existe, se omite" AS info'
));
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM information_schema.columns
       WHERE table_schema = DATABASE() AND table_name = 'Employees' AND column_name = 'AfcMonthlyAmount') = 0,
    'ALTER TABLE Employees ADD COLUMN AfcMonthlyAmount DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER PrepaidHealthEnabled',
    'SELECT "AfcMonthlyAmount ya existe, se omite" AS info'
));
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

COMMIT;

-- Verificación post-migración
SELECT column_name, column_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'Employees'
  AND column_name IN ('HasDependents','HousingInterestEnabled','PrepaidHealthEnabled','AfcMonthlyAmount')
ORDER BY ordinal_position;

-- ============================================================================
-- ROLLBACK (ejecutar manualmente solo si necesitas revertir):
--
--   USE contanexo;
--   ALTER TABLE Employees DROP COLUMN AfcMonthlyAmount;
--   ALTER TABLE Employees DROP COLUMN PrepaidHealthEnabled;
--   ALTER TABLE Employees DROP COLUMN HousingInterestEnabled;
--   ALTER TABLE Employees DROP COLUMN HasDependents;
-- ============================================================================
