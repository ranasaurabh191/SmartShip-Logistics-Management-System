USE SmartShip_IdentityDB;

INSERT INTO Users (Name, Email, Phone, PasswordHash, Role, IsActive, CreatedAt) VALUES
('Saurabh Rana',   'saurabh@smartship.com',  '9876543210', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'CUSTOMER', 1, '2026-01-15'),
('Rahul Sharma',   'rahul@smartship.com',    '9812345678', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'CUSTOMER', 1, '2026-02-01'),
('Priya Singh',    'priya@smartship.com',    '9856781234', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'CUSTOMER', 1, '2026-02-15');

SELECT * FROM Users;
