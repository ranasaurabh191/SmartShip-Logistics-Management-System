USE SmartShip_IdentityDB;

INSERT INTO Users (Name, Email, Phone, PasswordHash, Role, IsActive, CreatedAt) VALUES
('Saurabh Rana',   'saurabh@smartship.com',  '9876543210', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'CUSTOMER', 1, '2026-01-15'),
('Rahul Sharma',   'rahul@smartship.com',    '9812345678', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'CUSTOMER', 1, '2026-02-01'),
('Priya Singh',    'priya@smartship.com',    '9856781234', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'CUSTOMER', 1, '2026-02-15');

SELECT * FROM Users;
---------------------------------------
USE SmartShip_AdminDB;

DELETE FROM Hubs;
DBCC CHECKIDENT ('Hubs', RESEED, 0);

INSERT INTO Hubs (Name, City, State, Country, ContactPhone, IsActive, CreatedAt) VALUES
('Delhi Central Hub',   'New Delhi', 'Delhi',         'India', '9800000001', 1, '2026-01-01'),
('Mumbai West Hub',     'Mumbai',    'Maharashtra',   'India', '9800000002', 1, '2026-01-01'),
('Bangalore Tech Hub',  'Bangalore', 'Karnataka',     'India', '9800000003', 1, '2026-01-15'),
('Amritsar North Hub',  'Amritsar',  'Punjab',        'India', '9800000004', 1, '2026-02-01'),
('Chennai South Hub',   'Chennai',   'Tamil Nadu',    'India', '9800000005', 0, '2026-02-15');

SELECT * FROM Hubs;
----------------------------------------
USE SmartShip_ShipmentDB;

DELETE FROM Shipments;
DELETE FROM Packages;
DELETE FROM Addresses;

DBCC CHECKIDENT ('Shipments', RESEED, 0);
DBCC CHECKIDENT ('Packages', RESEED, 0);
DBCC CHECKIDENT ('Addresses', RESEED, 0);

INSERT INTO Addresses (FullName, Phone, Street, City, State, PostalCode, Country) VALUES
('Saurabh Rana',  '9876543210', '12 Lawrence Road',    'Amritsar',   'Punjab',       '143001', 'India'), -- 1
('Rahul Sharma',  '9812345678', '45 MG Road',          'Mumbai',     'Maharashtra',  '400001', 'India'), -- 2
('Priya Singh',   '9856781234', '78 Connaught Place',  'New Delhi',  'Delhi',        '110001', 'India'), -- 3
('Rahul Sharma',  '9812345678', '22 Brigade Road',     'Bangalore',  'Karnataka',    '560001', 'India'), -- 4
('Priya Singh',   '9856781234', '33 Anna Salai',       'Chennai',    'Tamil Nadu',   '600001', 'India')-- 5
INSERT INTO Addresses (FullName, Phone, Street, City, State, PostalCode, Country) VALUES
('Sahil Rana',  '98765432104', '56 Mall Road', 'Amritsar', 'Punjab', '143001', 'India'); -- 6
SELECT * FROM Addresses;


INSERT INTO Packages (WeightKg, LengthCm, WidthCm, HeightCm, Description, DeclaredValue) VALUES
(2.5,  30, 20, 15, 'Electronics - Laptop',      45000), -- 1
(0.5,  15, 10, 10, 'Documents - Legal Papers',  500),   -- 2
(5.0,  40, 30, 25, 'Clothing - Winter Jackets', 3500),  -- 3
(1.2,  20, 15, 12, 'Books - Study Material',    1200),  -- 4
(8.0,  50, 40, 35, 'Machine Parts - Spare',     12000), -- 5
(0.3,  10,  8,  5, 'Jewelry - Gold Ring',       25000); -- 6

SELECT * FROM Packages;

INSERT INTO Shipments 
(TrackingNumber, CustomerId, ShipmentType, Status, ShippingRate, SenderAddressId, ReceiverAddressId, PackageId, PickupScheduledAt, DeliveredAt, Notes, CreatedAt)
VALUES
('SS202601001', 2, 0, 5, 375.00,  1, 2, 1, '2026-01-10', '2026-01-13', NULL,               '2026-01-09'),
('SS202601002', 3, 1, 1, 750.00,  3, 4, 2, '2026-02-05', NULL,         NULL,               '2026-02-04'),
('SS202601003', 1, 2, 3, 1500.00, 5, 6, 3, '2026-02-20', NULL,         NULL,               '2026-02-18'),
('SS202601004', 2, 3, 6, 400.00,  1, 3, 4, '2026-03-01', NULL,         'Delayed at hub',   '2026-02-28'),
('SS202601005', 3, 0, 2, 250.00,  2, 5, 5, '2026-03-10', NULL,         NULL,               '2026-03-09'),
('SS202601006', 1, 1, 5, 900.00,  4, 1, 6, '2026-03-15', '2026-03-18', 'Handle with care', '2026-03-14');

SELECT * FROM Shipments;


---------------------------------

USE SmartShip_TrackingDB;

DELETE FROM TrackingEvents;
DBCC CHECKIDENT ('TrackingEvents', RESEED, 0);

INSERT INTO TrackingEvents (ShipmentId, TrackingNumber, Status, Location, Description, EventTime, UpdatedBy) VALUES
(1, 'SS202601001', 'Booked',      'Amritsar Hub',   'Shipment booked successfully',    '2026-01-09 10:00', 'system'),
(1, 'SS202601001', 'PickedUp',    'Amritsar Hub',   'Package picked up from sender',   '2026-01-10 09:00', 'agent1'),
(1, 'SS202601001', 'InTransit',   'Delhi Hub',      'Package arrived at Delhi hub',    '2026-01-11 14:00', 'agent2'),
(1, 'SS202601001', 'Delivered',   'Mumbai',         'Package delivered to receiver',   '2026-01-13 11:00', 'agent3'),
(2, 'SS202601002', 'Booked',      'Delhi Hub',      'Shipment booked successfully',    '2026-02-04 12:00', 'system'),
(3, 'SS202601003', 'Booked',      'Chennai Hub',    'Shipment booked successfully',    '2026-02-18 09:00', 'system'),
(3, 'SS202601003', 'PickedUp',    'Chennai Hub',    'Package picked up',               '2026-02-20 10:00', 'agent1'),
(3, 'SS202601003', 'InTransit',   'Bangalore Hub',  'In transit to destination',       '2026-02-22 16:00', 'agent2'),
(4, 'SS202601004', 'Booked',      'Amritsar Hub',   'Shipment booked',                 '2026-02-28 08:00', 'system'),
(4, 'SS202601004', 'Delayed',     'Delhi Hub',      'Delayed due to weather',          '2026-03-01 18:00', 'agent1'),
(5, 'SS202601005', 'Booked',      'Mumbai Hub',     'Shipment booked',                 '2026-03-09 11:00', 'system'),
(5, 'SS202601005', 'PickedUp',    'Mumbai Hub',     'Package picked up from sender',   '2026-03-10 09:00', 'agent2'),
(6, 'SS202601006', 'Booked',      'Bangalore Hub',  'Shipment booked',                 '2026-03-14 10:00', 'system'),
(6, 'SS202601006', 'Delivered',   'Amritsar',       'Delivered successfully',          '2026-03-18 14:00', 'agent3');

-- Delivery Proofs for delivered shipments
DELETE FROM DeliveryProofs;
DBCC CHECKIDENT ('DeliveryProofs', RESEED, 0);

INSERT INTO DeliveryProofs (ShipmentId, TrackingNumber, ReceiverName, Notes, DeliveredBy, DeliveredAt) VALUES
(1, 'SS202601001', 'Rahul Sharma', 'Received in good condition', 'agent3', '2026-01-13 11:00'),
(6, 'SS202601006', 'Saurabh Rana', 'Handle with care - done',   'agent3', '2026-03-18 14:00');

SELECT * FROM TrackingEvents;
SELECT * FROM DeliveryProofs;