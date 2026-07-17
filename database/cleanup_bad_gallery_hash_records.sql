START TRANSACTION;

DELETE FROM catalogproducts
WHERE Title REGEXP '^[0-9A-Fa-f]{24,}$'
   OR (Price = 0 AND ProductLine IN ('protectores-tanque', 'calcas-motos', 'calcas-rines'))
   OR Slug LIKE 'img-%'
   OR Slug LIKE 'ocr-%'
   OR Description LIKE '%Reconstruido%'
   OR Description LIKE '%recuperada%'
   OR Description LIKE '%fisica%';

COMMIT;
