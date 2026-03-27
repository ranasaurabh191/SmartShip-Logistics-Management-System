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