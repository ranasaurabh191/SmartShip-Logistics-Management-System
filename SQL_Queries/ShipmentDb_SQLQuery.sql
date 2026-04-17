
--DELETE FROM Packages;
--DELETE FROM Addresses;
--delete from shipments
--delete from ShipmentOrderSagas;
--DBCC CHECKIDENT ('Shipments', RESEED, 0);
--DBCC CHECKIDENT ('Packages', RESEED, 0);
--DBCC CHECKIDENT ('Addresses', RESEED, 0);

SELECT * FROM ShipmentOrderSagas;
	
--SELECT * FROM Addresses;

--SELECT * FROM Packages;

SELECT * FROM Shipments;




