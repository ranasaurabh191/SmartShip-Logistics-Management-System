
USE SmartShip_TrackingDB;
DELETE FROM DeliveryProofs;
DELETE FROM TrackingEvents;
DBCC CHECKIDENT ('TrackingEvents', RESEED, 0);
DBCC CHECKIDENT ('DeliveryProofs', RESEED, 0);

INSERT INTO TrackingEvents (ShipmentId, TrackingNumber, Status, Location, Description, EventTime, UpdatedBy) VALUES
(1, 'SS202601001', 'Booked',    'Amritsar Hub', 'Shipment booked',              '2026-01-09 10:00', 'system'),
(1, 'SS202601001', 'PickedUp',  'Amritsar Hub', 'Package picked up',            '2026-01-10 09:00', 'agent1'),
(1, 'SS202601001', 'Delivered', 'Mumbai',       'Delivered to receiver',        '2026-01-13 11:00', 'agent2'),
(2, 'SS202601002', 'Booked',    'Mumbai Hub',   'Shipment booked',              '2026-02-04 12:00', 'system'),
(3, 'SS202601003', 'Booked',    'Amritsar Hub', 'Shipment booked',              '2026-02-28 08:00', 'system'),
(3, 'SS202601003', 'Delayed',   'Delhi Hub',    'Delayed due to weather',       '2026-03-01 18:00', 'agent1');

-- Delivery Proofs for delivered shipments
INSERT INTO DeliveryProofs (ShipmentId, TrackingNumber, ReceiverName, Notes, DeliveredBy, DeliveredAt) VALUES
(1, 'SS202601001', 'Sonia', 'Received in good condition', 'agent2', '2026-01-13 11:00');


SELECT * FROM TrackingEvents;
SELECT * FROM DeliveryProofs;

delete from TrackingEvents where Description = 'status updated to intransit';