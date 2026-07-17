-- Importacion inicial lista de precios Jhon Calcas
-- Modelo validado:
--   categories -> products -> inventorymovements / inventorylots
--   products.Id -> catalogproducts.InternalProductId
--
-- Reglas:
--   UnitPrice = precio publico / detal
--   UnitCost  = valor real por mayor
--   Stock inicial configurable con @initial_stock
--   La galeria queda relacionada por catalogproducts.InternalProductId
--   ImageFileName queda vacio para que el frontend muestre "Sin imagen"
--
-- IMPORTANTE:
--   La pagina 3 del OCR contiene muchas filas fusionadas en una misma linea.
--   No se carga aqui para evitar registros errados. Debe importarse desde Excel/CSV limpio.

SET @category_name = 'Calcas y protectores';
SET @initial_stock = 1;
SET @import_reference = 'IMPORT-LISTA-PRECIOS-2026';

START TRANSACTION;

CREATE TEMPORARY TABLE tmp_price_import (
    Sku VARCHAR(80) NOT NULL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    ProductLine VARCHAR(50) NOT NULL,
    Brand VARCHAR(50) NOT NULL,
    Model VARCHAR(100) NULL,
    MenuModel VARCHAR(100) NULL,
    DesignRef VARCHAR(100) NULL,
    PublicPrice DECIMAL(18,2) NOT NULL,
    WholesaleCost DECIMAL(18,2) NOT NULL,
    SortOrder INT NOT NULL
) ENGINE=Memory;

INSERT INTO tmp_price_import
(Sku, Name, ProductLine, Brand, Model, MenuModel, DesignRef, PublicPrice, WholesaleCost, SortOrder)
VALUES
-- Pagina 1: protectores / piezas de tanque
('JC-PT-0001','Protector VSTROM SX 250','protectores-tanque','SUZUKI','VSTROM SX 250','VSTROM SX 250',NULL,165000,90000,1),
('JC-PT-0002','Protector GIXXER 150-250','protectores-tanque','SUZUKI','GIXXER 150-250','GIXXER',NULL,175000,100000,2),
('JC-PT-0003','Protector VSTROM 160','protectores-tanque','SUZUKI','VSTROM 160','VSTROM 160',NULL,175000,100000,3),
('JC-PT-0004','Protector HUNK 160 LETRA H','protectores-tanque','HERO','HUNK 160','HUNK 160','LETRA H',160000,80000,4),
('JC-PT-0005','Protector HONDA NX 190','protectores-tanque','HONDA','NX 190','NX 190',NULL,155000,95000,5),
('JC-PT-0006','Protector FZ 250','protectores-tanque','YAMAHA','FZ 250','FZ 250',NULL,150000,85000,6),
('JC-PT-0007','Protector FZ 3.0','protectores-tanque','YAMAHA','FZ 3.0','FZ 3.0',NULL,190000,105000,7),
('JC-PT-0008','Protector XR 190L','protectores-tanque','HONDA','XR 190L','XR 190L',NULL,120000,55000,8),
('JC-PT-0009','Protector PULSAR NS','protectores-tanque','BAJAJ','PULSAR NS','PULSAR NS',NULL,140000,65000,9),
('JC-PT-0010','Piso AEROX','protectores-tanque','YAMAHA','AEROX','AEROX','PISO',85000,55000,10),
('JC-PT-0011','Protector XTZ 250 COMPLETA','protectores-tanque','YAMAHA','XTZ 250','XTZ 250','COMPLETA',160000,85000,11),
('JC-PT-0012','Tapa tanque XTZ 250','protectores-tanque','YAMAHA','XTZ 250','XTZ 250','TAPA TANQUE',80000,45000,12),
('JC-PT-0013','Protector VSTROM XT','protectores-tanque','SUZUKI','VSTROM XT','VSTROM XT',NULL,140000,75000,13),
('JC-PT-0014','Protector MT 03','protectores-tanque','YAMAHA','MT 03','MT 03',NULL,250000,150000,14),
('JC-PT-0015','Protector MT 15','protectores-tanque','YAMAHA','MT 15','MT 15',NULL,150000,70000,15),
('JC-PT-0016','Protector NMAX V2 CONNECTED','protectores-tanque','YAMAHA','NMAX V2 CONNECTED','NMAX','V2 CONNECTED',105000,60000,16),
('JC-PT-0017','Exosto AEROX','protectores-tanque','YAMAHA','AEROX','AEROX','EXOSTO',105000,65000,17),
('JC-PT-0018','Protector NMAX V3','protectores-tanque','YAMAHA','NMAX V3','NMAX','V3',95000,40000,18),
('JC-PT-0019','Protector NMAX V3 COMPLETA','protectores-tanque','YAMAHA','NMAX V3','NMAX','V3 COMPLETA',125000,50000,19),
('JC-PT-0020','Protector NMAX V1','protectores-tanque','YAMAHA','NMAX V1','NMAX','V1',110000,65000,20),
('JC-PT-0021','Protector NMAX V1 COMPLETA','protectores-tanque','YAMAHA','NMAX V1','NMAX','V1 COMPLETA',180000,100000,21),
('JC-PT-0022','Protector PCX 150','protectores-tanque','HONDA','PCX 150','PCX',NULL,150000,85000,22),
('JC-PT-0023','Protector PCX 2026','protectores-tanque','HONDA','PCX 2026','PCX','2026',150000,85000,23),
('JC-PT-0024','Protector VOGE PRO 300','protectores-tanque','VOGE','PRO 300','PRO 300',NULL,150000,85000,24),
('JC-PT-0025','Protector VOGE 300 FULL TANQUE','protectores-tanque','VOGE','300 FULL','300 FULL','TANQUE',230000,130000,25),
('JC-PT-0026','Protector VOGE PRO 300 AC','protectores-tanque','VOGE','PRO 300 AC','PRO 300 AC',NULL,180000,105000,26),
('JC-PT-0027','Protector XTZ 150','protectores-tanque','YAMAHA','XTZ 150','XTZ 150',NULL,95000,50000,27),
('JC-PT-0028','Protector DR 150','protectores-tanque','SUZUKI','DR 150','DR 150',NULL,110000,65000,28),
('JC-PT-0029','Centro tanque NS 200','protectores-tanque','BAJAJ','NS 200','NS 200','CENTRO TANQUE',65000,40000,29),
('JC-PT-0030','Protector PULSAR NS','protectores-tanque','BAJAJ','PULSAR NS','PULSAR NS',NULL,110000,65000,30),
('JC-PT-0031','Victory BET solo centro','protectores-tanque','VICTORY','BET','BET','SOLO CENTRO',105000,65000,31),
('JC-PT-0032','Victory BET full tanque','protectores-tanque','VICTORY','BET','BET','FULL TANQUE',150000,85000,32),
('JC-PT-0033','Protector SYM ADX','protectores-tanque','SYM','ADX','ADX',NULL,125000,50000,33),

-- Pagina 2: protectores / tanque
('JC-PT-0101','Protector APACHE 4V tanque superior','protectores-tanque','TVS','APACHE 4V','APACHE 4V','TANQUE SUPERIOR',130000,45000,101),
('JC-PT-0102','Protector GIXXER tanque superior','protectores-tanque','SUZUKI','GIXXER','GIXXER','TANQUE SUPERIOR',105000,38000,102),
('JC-PT-0103','Protector CR4 200','protectores-tanque','AKT','CR4 200','CR4','200',145000,55000,103),
('JC-PT-0104','Protector CR4 250','protectores-tanque','AKT','CR4 250','CR4','250',145000,55000,104),
('JC-PT-0105','Protector DUKE 200','protectores-tanque','KTM','DUKE 200','DUKE','200',150000,60000,105),
('JC-PT-0106','Full tanque DR 150 MN','protectores-tanque','SUZUKI','DR 150 MN','DR 150','FULL TANQUE',103000,40000,106),
('JC-PT-0107','Protector HUNK 160R 2V','protectores-tanque','HERO','HUNK 160R 2V','HUNK 160R','2V',130000,50000,107),
('JC-PT-0108','Full tanque DR 160','protectores-tanque','SUZUKI','DR 160','DR 160','FULL TANQUE',103000,40000,108),
('JC-PT-0109','Protector DOMINAR','protectores-tanque','BAJAJ','DOMINAR','DOMINAR',NULL,161000,65000,109),
('JC-PT-0110','Protector BOXER','protectores-tanque','BAJAJ','BOXER','BOXER',NULL,115000,40000,110),
('JC-PT-0111','Protector YCZ','protectores-tanque','YAMAHA','YCZ','YCZ',NULL,145000,60000,111),
('JC-PT-0112','Protector NS','protectores-tanque','BAJAJ','NS','NS',NULL,105000,45000,112),
('JC-PT-0113','Protector XRE 300 NUEVO','protectores-tanque','HONDA','XRE 300 NUEVO','XRE 300','NUEVO',130000,55000,113),
('JC-PT-0114','TT DS 200 FULL','protectores-tanque','AKT','TT DS 200','TT DS 200','FULL',130000,55000,114),
('JC-PT-0115','TT DS 200 2025','protectores-tanque','AKT','TT DS 200','TT DS 200','2025',128000,47000,115),
('JC-PT-0116','GIXXER 250 FULL TANQUE','protectores-tanque','SUZUKI','GIXXER 250','GIXXER 250','FULL TANQUE',130000,55000,116),
('JC-PT-0117','APACHE 2V','protectores-tanque','TVS','APACHE 2V','APACHE','2V',110000,40000,117),
('JC-PT-0118','FZS 150','protectores-tanque','YAMAHA','FZS 150','FZS 150',NULL,133000,48000,118),
('JC-PT-0119','Tapa tanque NKD','protectores-tanque','AKT','NKD','NKD','TAPA TANQUE',68000,23000,119),
('JC-PT-0120','SUZUKI GN','protectores-tanque','SUZUKI','GN','GN',NULL,85000,28000,120),
('JC-PT-0121','PULSAR 180 REF 001 y 002','protectores-tanque','BAJAJ','PULSAR 180','PULSAR 180','REF 001 y 002',108000,40000,121),
('JC-PT-0122','XTZ 150 COMPLETO','protectores-tanque','YAMAHA','XTZ 150','XTZ 150','COMPLETO',138000,50000,122),
('JC-PT-0123','TTX TTR FULL','protectores-tanque','AKT','TTX TTR','TTX TTR','FULL',107000,40000,123),
('JC-PT-0124','XTZ 150','protectores-tanque','YAMAHA','XTZ 150','XTZ 150',NULL,80000,35000,124),
('JC-PT-0125','SZR FULL TANQUE','protectores-tanque','YAMAHA','SZR','SZR','FULL TANQUE',93000,33000,125),
('JC-PT-0126','SUZUKI GSX S','protectores-tanque','SUZUKI','GSX S','GSX S',NULL,67000,25000,126),
('JC-PT-0127','VENOM 14','protectores-tanque','VENOM','14','14',NULL,125000,50000,127),
('JC-PT-0128','CB100','protectores-tanque','HONDA','CB100','CB100',NULL,71000,28000,128),
('JC-PT-0129','Protector ARIZONA','protectores-tanque','AKT','ARIZONA','ARIZONA',NULL,155000,60000,129),
('JC-PT-0130','CB160F','protectores-tanque','HONDA','CB160F','CB160F',NULL,148000,55000,130),
('JC-PT-0131','DUKE NG','protectores-tanque','KTM','DUKE NG','DUKE NG',NULL,100000,38000,131),
('JC-PT-0132','VSTROM SX 250','protectores-tanque','SUZUKI','VSTROM SX 250','VSTROM SX 250',NULL,142000,55000,132),
('JC-PT-0133','VSTROM SX 250 2027','protectores-tanque','SUZUKI','VSTROM SX 250','VSTROM SX 250','2027',142000,60000,133),
('JC-PT-0134','FZ 3.0','protectores-tanque','YAMAHA','FZ 3.0','FZ 3.0',NULL,117000,45000,134),
('JC-PT-0135','PULSAR 135','protectores-tanque','BAJAJ','PULSAR 135','PULSAR 135',NULL,102000,35000,135),
('JC-PT-0136','HONDA NAVI','protectores-tanque','HONDA','NAVI','NAVI',NULL,70000,30000,136),
('JC-PT-0137','TT ADVENTOUR','protectores-tanque','AKT','TT ADVENTOUR','TT ADVENTOUR',NULL,115000,40000,137),
('JC-PT-0138','XTZ 250 V2 REF 001','protectores-tanque','YAMAHA','XTZ 250 V2','XTZ 250','REF 001',58000,20000,138),
('JC-PT-0139','XTZ 250 V2 REF 002','protectores-tanque','YAMAHA','XTZ 250 V2','XTZ 250','REF 002',130000,50000,139),
('JC-PT-0140','DISCOVER FULL','protectores-tanque','BAJAJ','DISCOVER','DISCOVER','FULL',115000,40000,140),
('JC-PT-0141','MRX 150','protectores-tanque','AKT','MRX 150','MRX 150',NULL,96000,37000,141),
('JC-PT-0142','RAIDER','protectores-tanque','SUZUKI','RAIDER','RAIDER',NULL,145000,55000,142),
('JC-PT-0143','CB300','protectores-tanque','HONDA','CB300','CB300',NULL,145000,55000,143),
('JC-PT-0144','CB300 TAPA TANQUE','protectores-tanque','HONDA','CB300','CB300','TAPA TANQUE',85000,30000,144),
('JC-PT-0145','BMW 750 EDICION 40 ANOS','protectores-tanque','BMW','750','750','EDICION 40 ANOS',105000,40000,145),
('JC-PT-0146','DISCOVER CENTRO DE TANQUE','protectores-tanque','BAJAJ','DISCOVER','DISCOVER','CENTRO DE TANQUE',45000,18000,146),
('JC-PT-0147','FZ 160','protectores-tanque','YAMAHA','FZ 160','FZ 160',NULL,110000,45000,147),
('JC-PT-0148','PULSAR N160 REF 001','protectores-tanque','BAJAJ','PULSAR N160','PULSAR N160','REF 001',145000,55000,148),
('JC-PT-0149','PULSAR N160 REF 002','protectores-tanque','BAJAJ','PULSAR N160','PULSAR N160','REF 002',140000,53000,149),
('JC-PT-0150','PULSAR N250 REF 001','protectores-tanque','BAJAJ','PULSAR N250','PULSAR N250','REF 001',155000,60000,150),
('JC-PT-0151','PULSAR N250 REF 002','protectores-tanque','BAJAJ','PULSAR N250','PULSAR N250','REF 002',141000,53000,151),
('JC-PT-0152','PULSAR N125','protectores-tanque','BAJAJ','PULSAR N125','PULSAR N125',NULL,148000,60000,152),
('JC-PT-0153','CB125F','protectores-tanque','HONDA','CB125F','CB125F',NULL,148000,55000,153),
('JC-PT-0154','CB110','protectores-tanque','HONDA','CB110','CB110',NULL,133000,55000,154),
('JC-PT-0155','CR4 125-162 REF 001','protectores-tanque','AKT','CR4 125-162','CR4','REF 001',120000,40000,155),
('JC-PT-0156','CR4 125-162 REF 002','protectores-tanque','AKT','CR4 125-162','CR4','REF 002',175000,65000,156),
('JC-PT-0157','CR4 125-162 REF 003','protectores-tanque','AKT','CR4 125-162','CR4','REF 003',155000,60000,157),
('JC-PT-0158','RTX','protectores-tanque','VICTORY','RTX','RTX',NULL,140000,60000,158),
('JC-PT-0159','FZ 250','protectores-tanque','YAMAHA','FZ 250','FZ 250',NULL,138000,50000,159),
('JC-PT-0160','MRX 200','protectores-tanque','AKT','MRX 200','MRX 200',NULL,82000,35000,160),
('JC-PT-0161','MRX 200 REF 002','protectores-tanque','AKT','MRX 200','MRX 200','REF 002',112000,40000,161),
('JC-PT-0162','XBLADE','protectores-tanque','HONDA','XBLADE','XBLADE',NULL,135000,50000,162),
('JC-PT-0163','MRX 125','protectores-tanque','AKT','MRX 125','MRX 125',NULL,100000,38000,163),
('JC-PT-0164','PULSAR P150','protectores-tanque','BAJAJ','PULSAR P150','PULSAR P150',NULL,125000,45000,164),
('JC-PT-0165','SUZUKI GS 500','protectores-tanque','SUZUKI','GS 500','GS 500',NULL,68000,28000,165),
('JC-PT-0166','Protector GSX 750','protectores-tanque','SUZUKI','GSX 750','GSX 750',NULL,160000,63000,166),
('JC-PT-0167','SUZUKI GSX R','protectores-tanque','SUZUKI','GSX R','GSX R',NULL,112000,45000,167),
('JC-PT-0168','MT 03','protectores-tanque','YAMAHA','MT 03','MT 03',NULL,140000,75000,168),
('JC-PT-0169','MT 09','protectores-tanque','YAMAHA','MT 09','MT 09',NULL,140000,60000,169),
('JC-PT-0170','MT 15','protectores-tanque','YAMAHA','MT 15','MT 15',NULL,98000,40000,170),
('JC-PT-0171','Tapa tanque FZS 150 completo','protectores-tanque','YAMAHA','FZS 150','FZS 150','TAPA TANQUE COMPLETO',65000,27000,171),
('JC-PT-0172','CR4 150','protectores-tanque','AKT','CR4 150','CR4','150',140000,58000,172),
('JC-PT-0173','VSTROM 160','protectores-tanque','SUZUKI','VSTROM 160','VSTROM 160',NULL,140000,60000,173),
('JC-PT-0174','NKD FULL TANQUE','protectores-tanque','AKT','NKD','NKD','FULL TANQUE',90000,38000,174),
('JC-PT-0175','GIXXER 150 CARBURADA','protectores-tanque','SUZUKI','GIXXER 150 CARBURADA','GIXXER 150','CARBURADA',125000,45000,175),
('JC-PT-0176','GIXXER 150 CARBURADA COMPLETA','protectores-tanque','SUZUKI','GIXXER 150 CARBURADA','GIXXER 150','CARBURADA COMPLETA',145000,55000,176),
('JC-PT-0177','HUNK 125','protectores-tanque','HERO','HUNK 125','HUNK 125',NULL,140000,60000,177),
('JC-PT-0178','CB190R 2.0','protectores-tanque','HONDA','CB190R 2.0','CB190R','2.0',102000,40000,178),
('JC-PT-0179','HUNK 4V','protectores-tanque','HERO','HUNK 4V','HUNK','4V',135000,55000,179),
('JC-PT-0180','BENELLI 180','protectores-tanque','BENELLI','180','180',NULL,120000,50000,180),
('JC-PT-0181','HONDA NX190','protectores-tanque','HONDA','NX190','NX190',NULL,115000,50000,181),
('JC-PT-0182','XR150L-190L','protectores-tanque','HONDA','XR150L-190L','XR',NULL,100000,40000,182),
('JC-PT-0183','Tapa tanque XR90L XR50L','protectores-tanque','HONDA','XR90L XR50L','XR','TAPA TANQUE',72000,25000,183),
('JC-PT-0184','CR5','protectores-tanque','AKT','CR5','CR5',NULL,105000,43000,184),
('JC-PT-0185','THRILLER','protectores-tanque','AKT','THRILLER','THRILLER',NULL,140000,60000,185),
('JC-PT-0186','NS 400','protectores-tanque','BAJAJ','NS 400','NS 400',NULL,82000,38000,186),
('JC-PT-0187','HUNK 150','protectores-tanque','HERO','HUNK 150','HUNK 150',NULL,107000,40000,187),
('JC-PT-0188','Tapa tanque DOMINAR','protectores-tanque','BAJAJ','DOMINAR','DOMINAR','TAPA TANQUE',68000,28000,188),
('JC-PT-0189','Tapa tanque FZS 150','protectores-tanque','YAMAHA','FZS 150','FZS 150','TAPA TANQUE',57000,22000,189),
('JC-PT-0190','VSTROM 250 modelo anterior','protectores-tanque','SUZUKI','VSTROM 250','VSTROM 250','MODELO ANTERIOR',105000,45000,190),
('JC-PT-0191','CB 190 R V1','protectores-tanque','HONDA','CB 190 R','CB 190 R','V1',115000,45000,191),
('JC-PT-0192','CB 190 R V2','protectores-tanque','HONDA','CB 190 R','CB 190 R','V2',103000,45000,192),
('JC-PT-0193','CB INVICTA','protectores-tanque','HONDA','CB INVICTA','CB INVICTA',NULL,125000,45000,193),
('JC-PT-0194','CBF150','protectores-tanque','HONDA','CBF150','CBF150',NULL,135000,50000,194),
('JC-PT-0195','FZS 150 COMPLETO','protectores-tanque','YAMAHA','FZS 150','FZS 150','COMPLETO',145000,55000,195),
('JC-PT-0196','Protector AX4','protectores-tanque','SUZUKI','AX4','AX4',NULL,55000,25000,196),

-- Pagina 4: calcas / tacometros, centros y cupula
('JC-CM-0401','Tacometro AEROX','calcas-motos','YAMAHA','AEROX','AEROX','TACOMETRO',45000,14000,401),
('JC-CM-0402','Tacometro NEW LIFE','calcas-motos','VICTORY','NEW LIFE','NEW LIFE','TACOMETRO',43000,12000,402),
('JC-CM-0403','Farola y tacometro VSTROM 160','calcas-motos','SUZUKI','VSTROM 160','VSTROM 160','FAROLA Y TACOMETRO',46000,18000,403),
('JC-CM-0404','Farola VSTROM250-GIXXER','calcas-motos','SUZUKI','VSTROM250-GIXXER','VSTROM / GIXXER','FAROLA',47000,16000,404),
('JC-CM-0405','VSTROM SX 250 tacometro','calcas-motos','SUZUKI','VSTROM SX 250','VSTROM SX 250','TACOMETRO',38000,11000,405),
('JC-CM-0406','N125 tacometro','calcas-motos','BAJAJ','N125','N125','TACOMETRO',38000,11000,406),
('JC-CM-0407','Tacometro NS200 UG 2025','calcas-motos','BAJAJ','NS200 UG 2025','NS200','TACOMETRO',38000,11000,407),
('JC-CM-0408','Tacometro FZ 250','calcas-motos','YAMAHA','FZ 250','FZ 250','TACOMETRO',38000,11000,408),
('JC-CM-0409','Tacometro PULSAR N160-250','calcas-motos','BAJAJ','PULSAR N160-250','PULSAR N','TACOMETRO',40000,10000,409),
('JC-CM-0410','Tacometro NMAX V3','calcas-motos','YAMAHA','NMAX V3','NMAX','TACOMETRO V3',43000,12000,410),
('JC-CM-0411','Tacometro RAIDER','calcas-motos','SUZUKI','RAIDER','RAIDER','TACOMETRO',38000,11000,411),
('JC-CM-0412','Tacometro ADX 150','calcas-motos','SYM','ADX 150','ADX 150','TACOMETRO',38000,11000,412),
('JC-CM-0413','Tacometro GIXXER','calcas-motos','SUZUKI','GIXXER','GIXXER','TACOMETRO',40000,10000,413),
('JC-CM-0414','Tacometro DOMINAR','calcas-motos','BAJAJ','DOMINAR','DOMINAR','TACOMETRO',40000,10000,414),
('JC-CM-0415','FZ 3.0','calcas-motos','YAMAHA','FZ 3.0','FZ 3.0',NULL,38000,11000,415),
('JC-CM-0416','DUKE clasica','calcas-motos','KTM','DUKE','DUKE','CLASICA',32000,10000,416),
('JC-CM-0417','HUNK 160 V1','calcas-motos','HERO','HUNK 160','HUNK 160','V1',35000,10000,417),
('JC-CM-0418','DR160','calcas-motos','SUZUKI','DR160','DR160',NULL,35000,10000,418),
('JC-CM-0419','XR 2.0','calcas-motos','HONDA','XR 2.0','XR','2.0',37000,10000,419),
('JC-CM-0420','CB300','calcas-motos','HONDA','CB300','CB300',NULL,37000,10000,420),
('JC-CM-0421','APACHE 4V 2020','calcas-motos','TVS','APACHE 4V','APACHE 4V','2020',35000,10000,421),
('JC-CM-0422','CRYPTON FINN','calcas-motos','YAMAHA','CRYPTON FINN','CRYPTON FINN',NULL,45000,14000,422),
('JC-CM-0423','PCX 2026','calcas-motos','HONDA','PCX 2026','PCX','2026',47000,16000,423),
('JC-CM-0424','Centro de GIXXER','calcas-motos','SUZUKI','GIXXER','GIXXER','CENTRO',67000,20000,424),
('JC-CM-0425','Centro VSTROM 250 SX','calcas-motos','SUZUKI','VSTROM 250 SX','VSTROM 250 SX','CENTRO',65000,20000,425),
('JC-CM-0426','Cupula VSTROM SX','calcas-motos','SUZUKI','VSTROM SX','VSTROM SX','CUPULA',85000,30000,426);

-- Categoria base
INSERT INTO categories (`Name`, `Description`, `IsActive`, `CreatedAt`)
SELECT @category_name, 'Importado desde lista de precios Jhon Calcas', 1, UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE `Name` = @category_name);

SET @category_id = (SELECT Id FROM categories WHERE `Name` = @category_name LIMIT 1);

-- Crear productos faltantes
INSERT INTO products (`Sku`, `Name`, `Description`, `CategoryId`, `UnitCost`, `UnitPrice`, `Stock`, `MinStock`, `Unit`, `IsActive`, `ValuationMethod`, `CreatedAt`, `UpdatedAt`)
SELECT i.Sku, i.Name, CONCAT('Importado desde lista de precios. Linea: ', i.ProductLine), @category_id,
       i.WholesaleCost, i.PublicPrice, @initial_stock, 1, 'UND', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM tmp_price_import i
WHERE NOT EXISTS (SELECT 1 FROM products p WHERE p.Sku = i.Sku);

-- Actualizar precios/costos si el producto ya existe. No se toca stock existente.
UPDATE products p
JOIN tmp_price_import i ON i.Sku = p.Sku
SET p.`Name` = i.Name,
    p.`Description` = CONCAT('Importado desde lista de precios. Linea: ', i.ProductLine),
    p.`CategoryId` = @category_id,
    p.`UnitCost` = i.WholesaleCost,
    p.`UnitPrice` = i.PublicPrice,
    p.`IsActive` = 1,
    p.`UpdatedAt` = UTC_TIMESTAMP();

-- Movimiento inicial solo para productos sin movimientos previos.
INSERT INTO inventorymovements (`ProductId`, `Type`, `Quantity`, `StockBefore`, `StockAfter`, `Reference`, `Notes`, `UserId`, `CreatedAt`)
SELECT p.Id, 0, p.Stock, 0, p.Stock, @import_reference,
       'Inventario inicial generado por importacion lista de precios', NULL, UTC_TIMESTAMP()
FROM products p
JOIN tmp_price_import i ON i.Sku = p.Sku
WHERE p.Stock > 0
  AND NOT EXISTS (SELECT 1 FROM inventorymovements im WHERE im.ProductId = p.Id);

-- Lote inicial solo para productos sin lotes previos.
INSERT INTO inventorylots (`ProductId`, `PurchaseId`, `OriginalQuantity`, `RemainingQuantity`, `UnitCost`, `EntryDate`, `Reference`, `CreatedAt`)
SELECT p.Id, NULL, p.Stock, p.Stock, p.UnitCost, UTC_TIMESTAMP(), @import_reference, UTC_TIMESTAMP()
FROM products p
JOIN tmp_price_import i ON i.Sku = p.Sku
WHERE p.Stock > 0
  AND NOT EXISTS (SELECT 1 FROM inventorylots il WHERE il.ProductId = p.Id);

-- Crear/actualizar galeria relacionada al producto interno.
UPDATE catalogproducts cp
JOIN tmp_price_import i ON cp.Slug = LOWER(i.Sku)
JOIN products p ON p.Sku = i.Sku
SET cp.ProductLine = i.ProductLine,
    cp.Brand = i.Brand,
    cp.Model = i.Model,
    cp.MenuModel = i.MenuModel,
    cp.DesignRef = i.DesignRef,
    cp.Title = i.Name,
    cp.InternalProductId = p.Id,
    cp.Description = CONCAT('Producto importado desde lista de precios. Precio publico: $', FORMAT(i.PublicPrice, 0, 'es_CO')),
    cp.Price = i.PublicPrice,
    cp.SortOrder = i.SortOrder,
    cp.IsActive = 1,
    cp.UpdatedAt = UTC_TIMESTAMP();

INSERT INTO catalogproducts (`Slug`, `ProductLine`, `Brand`, `Model`, `MenuModel`, `DesignRef`, `Color`, `Title`, `InternalProductId`, `Description`, `Price`, `ImageFileName`, `SortOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`)
SELECT LOWER(i.Sku), i.ProductLine, i.Brand, i.Model, i.MenuModel, i.DesignRef, NULL,
       i.Name, p.Id,
       CONCAT('Producto importado desde lista de precios. Precio publico: $', FORMAT(i.PublicPrice, 0, 'es_CO')),
       i.PublicPrice, '', i.SortOrder, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM tmp_price_import i
JOIN products p ON p.Sku = i.Sku
WHERE NOT EXISTS (SELECT 1 FROM catalogproducts cp WHERE cp.Slug = LOWER(i.Sku));

SELECT 'Productos importados/actualizados' AS Resultado, COUNT(*) AS Total FROM tmp_price_import;

COMMIT;
