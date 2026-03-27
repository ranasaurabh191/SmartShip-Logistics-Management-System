USE SmartShip_ShipmentDB;

DELETE FROM Shipments;
DELETE FROM Packages;
DELETE FROM Addresses;

DBCC CHECKIDENT ('Shipments', RESEED, 0);
DBCC CHECKIDENT ('Packages', RESEED, 0);
DBCC CHECKIDENT ('Addresses', RESEED, 0);

SELECT * FROM Addresses;


SELECT * FROM Packages;


SELECT * FROM Shipments;

