DROP DATABASE IF EXISTS plf_system;
CREATE DATABASE plf_system CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE plf_system;

CREATE TABLE UserRoles (Id INT AUTO_INCREMENT PRIMARY KEY, RoleName VARCHAR(50) NOT NULL UNIQUE, PermissionLevel VARCHAR(50), CanManageOrders BOOLEAN, CanManageStock BOOLEAN, CanManageFinance BOOLEAN, CanManageUsers BOOLEAN, Description VARCHAR(255)) ENGINE=InnoDB;
CREATE TABLE Users (Id INT AUTO_INCREMENT PRIMARY KEY, Username VARCHAR(50) UNIQUE, Password VARCHAR(100), RoleId INT, FullName VARCHAR(120), Email VARCHAR(120), Status VARCHAR(50), FOREIGN KEY(RoleId) REFERENCES UserRoles(Id)) ENGINE=InnoDB;
CREATE TABLE Customers (Id INT AUTO_INCREMENT PRIMARY KEY, CustomerCode VARCHAR(50) UNIQUE, CustomerName VARCHAR(150), CustomerType VARCHAR(50), Phone VARCHAR(50), Email VARCHAR(120), Address VARCHAR(255), Status VARCHAR(50)) ENGINE=InnoDB;
CREATE TABLE Suppliers (Id INT AUTO_INCREMENT PRIMARY KEY, SupplierCode VARCHAR(50) UNIQUE, SupplierName VARCHAR(150), ContactPerson VARCHAR(120), Phone VARCHAR(50), Email VARCHAR(120), Country VARCHAR(80), Status VARCHAR(50)) ENGINE=InnoDB;
CREATE TABLE Products (Id INT AUTO_INCREMENT PRIMARY KEY, ProductCode VARCHAR(50) UNIQUE, ProductName VARCHAR(150), Category VARCHAR(80), UnitPrice DECIMAL(12,2), ImagePath VARCHAR(500) NULL, Status VARCHAR(50)) ENGINE=InnoDB;
CREATE TABLE SupplierItems (Id INT AUTO_INCREMENT PRIMARY KEY, SupplierId INT NOT NULL, ProductId INT NULL, ItemType VARCHAR(80), SupplyDescription VARCHAR(150), DefaultUnitCost DECIMAL(12,2), Status VARCHAR(50), FOREIGN KEY(SupplierId) REFERENCES Suppliers(Id), FOREIGN KEY(ProductId) REFERENCES Products(Id)) ENGINE=InnoDB;
CREATE TABLE Orders (Id INT AUTO_INCREMENT PRIMARY KEY, OrderNo VARCHAR(50) UNIQUE, CustomerId INT, ProductId INT, Quantity INT, Status VARCHAR(50), OrderDate DATE, Priority VARCHAR(50), SalesUserId INT, TotalAmount DECIMAL(12,2), FOREIGN KEY(CustomerId) REFERENCES Customers(Id), FOREIGN KEY(ProductId) REFERENCES Products(Id), FOREIGN KEY(SalesUserId) REFERENCES Users(Id)) ENGINE=InnoDB;
CREATE TABLE OrderItems (Id INT AUTO_INCREMENT PRIMARY KEY, OrderId INT, ProductId INT, Description VARCHAR(150), Quantity INT, UnitPrice DECIMAL(12,2), Discount DECIMAL(12,2), LineTotal DECIMAL(12,2), FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE, FOREIGN KEY(ProductId) REFERENCES Products(Id)) ENGINE=InnoDB;
CREATE TABLE PurchaseOrders (Id INT AUTO_INCREMENT PRIMARY KEY, PONo VARCHAR(50) UNIQUE, SupplierId INT, SupplierItemId INT NULL, RelatedOrderId INT NULL, Item VARCHAR(150), Quantity INT, UnitCost DECIMAL(12,2), Status VARCHAR(50), OrderDate DATE, ExpectedDate DATE, FOREIGN KEY(SupplierId) REFERENCES Suppliers(Id), FOREIGN KEY(SupplierItemId) REFERENCES SupplierItems(Id), FOREIGN KEY(RelatedOrderId) REFERENCES Orders(Id)) ENGINE=InnoDB;
CREATE TABLE Complaints (Id INT AUTO_INCREMENT PRIMARY KEY, ComplaintNo VARCHAR(50) UNIQUE, CustomerId INT, RelatedOrderId INT NULL, IssueType VARCHAR(100), Priority VARCHAR(50), Status VARCHAR(50), AssignedUserId INT NULL, CreatedDate DATE, Notes VARCHAR(255), FOREIGN KEY(CustomerId) REFERENCES Customers(Id), FOREIGN KEY(RelatedOrderId) REFERENCES Orders(Id), FOREIGN KEY(AssignedUserId) REFERENCES Users(Id)) ENGINE=InnoDB;
CREATE TABLE Stock (Id INT AUTO_INCREMENT PRIMARY KEY, ItemCode VARCHAR(50) UNIQUE, ProductId INT NULL, ItemName VARCHAR(150), ItemType VARCHAR(80), Warehouse VARCHAR(50), Quantity INT, ReorderPoint INT, Status VARCHAR(50), LastUpdated DATE, FOREIGN KEY(ProductId) REFERENCES Products(Id)) ENGINE=InnoDB;
CREATE TABLE StockMovements (Id INT AUTO_INCREMENT PRIMARY KEY, MovementNo VARCHAR(50) UNIQUE, StockId INT, MovementType VARCHAR(50), Quantity INT, Warehouse VARCHAR(50), MovementDate DATE, Reason VARCHAR(255), FOREIGN KEY(StockId) REFERENCES Stock(Id)) ENGINE=InnoDB;
CREATE TABLE DeliveryNotes (Id INT AUTO_INCREMENT PRIMARY KEY, DeliveryNoteNo VARCHAR(50) UNIQUE, OrderId INT, Warehouse VARCHAR(50), DeliveryMethod VARCHAR(100), DriverOrCourier VARCHAR(100), Status VARCHAR(50), DispatchDate DATE, FOREIGN KEY(OrderId) REFERENCES Orders(Id)) ENGINE=InnoDB;
CREATE TABLE ReplySlips (Id INT AUTO_INCREMENT PRIMARY KEY, ReplySlipNo VARCHAR(50) UNIQUE, DeliveryNoteId INT, CustomerId INT, ContactPerson VARCHAR(150), ResponseType VARCHAR(100), SatisfactionRating VARCHAR(50), FollowUpRequired VARCHAR(10), ReceivedBy VARCHAR(150), SignatureRef VARCHAR(150), Status VARCHAR(50), ReturnedDate DATE, Remarks VARCHAR(255), FOREIGN KEY(DeliveryNoteId) REFERENCES DeliveryNotes(Id), FOREIGN KEY(CustomerId) REFERENCES Customers(Id)) ENGINE=InnoDB;
CREATE TABLE Invoices (Id INT AUTO_INCREMENT PRIMARY KEY, InvoiceNo VARCHAR(50) UNIQUE, OrderId INT, CustomerId INT, Amount DECIMAL(12,2), Currency VARCHAR(20), PaymentStatus VARCHAR(50), InvoiceDate DATE, DueDate DATE, FOREIGN KEY(OrderId) REFERENCES Orders(Id), FOREIGN KEY(CustomerId) REFERENCES Customers(Id)) ENGINE=InnoDB;
CREATE TABLE Payments (Id INT AUTO_INCREMENT PRIMARY KEY, PaymentNo VARCHAR(50) UNIQUE, InvoiceId INT, Amount DECIMAL(12,2), PaymentMethod VARCHAR(50), PaymentDate DATE, ReferenceNo VARCHAR(100), FOREIGN KEY(InvoiceId) REFERENCES Invoices(Id)) ENGINE=InnoDB;

INSERT INTO UserRoles VALUES (1,'Admin','Full Access',1,1,1,1,'System administrator'),(2,'Manager','Approval Access',1,1,1,0,'Manager'),(3,'Sales Staff','Sales Access',1,0,0,0,'Sales'),(4,'Logistics Clerk','Delivery Access',1,1,0,0,'Delivery'),(5,'Warehouse Clerk','Stock Access',0,1,0,0,'Inventory'),(6,'Finance Staff','Finance Access',0,0,1,0,'Finance'),(7,'Customer Service','Complaint Access',1,0,0,0,'Service');
INSERT INTO Users VALUES (1,'admin','1234',1,'System Administrator','admin@plf.local','Active'),(2,'manager','1234',2,'Olivia Chan','olivia@plf.local','Active'),(3,'sales01','1234',3,'Jason Lee','sales@plf.local','Active'),(4,'logistics01','1234',4,'Grace Lam','logistics@plf.local','Active'),(5,'warehouse01','1234',5,'Henry Wong','warehouse@plf.local','Active'),(6,'finance01','1234',6,'Mandy Ho','finance@plf.local','Active'),(7,'cs01','1234',7,'Crystal Ng','cs@plf.local','Active');
INSERT INTO Customers VALUES (1,'C0001','Harbour View Hotel','B2B','2888-1001','purchase@harbourview.example','Tsim Sha Tsui','Active'),(2,'C0002','Lau Family','B2C','2888-1002','lau@example.com','Lam Tin','Active'),(3,'C0003','Urban Loft Studio','B2B','2888-1003','studio@urbanloft.example','Kwun Tong','Active');
INSERT INTO Suppliers VALUES (1,'SUP-001','Guangdong Oak Timber Co.','Mr. Wong','2333-2001','sales@oak-timber.example','China','Active'),(2,'SUP-002','Hong Kong Furniture Hardware Ltd.','Ms. Lee','2333-2002','orders@hk-hardware.example','Hong Kong','Active'),(3,'SUP-003','Vietnam Premium Fabric Supplier','Mr. Pham','2333-2003','fabric@vn-premium.example','Vietnam','Active');
INSERT INTO Products (Id,ProductCode,ProductName,Category,UnitPrice,ImagePath,Status) VALUES
(1,'P001','Custom L-shape Sofa','Living Room',6800,'ProductImages/3.png','Active'),
(2,'P002','Oak Dining Chair','Dining Room',780,'ProductImages/1.png','Active'),
(3,'P003','Marble Dining Table','Dining Room',5200,'ProductImages/2.png','Active'),
(4,'P004','Ergonomic Office Desk','Office',2600,'ProductImages/5.png','Active'),
(5,'P005','Hotel Queen Bed Frame','Bedroom',4300,'ProductImages/6.png','Active');
INSERT INTO SupplierItems VALUES (1,1,2,'Raw Material','Oak timber and chair frame components',350,'Active'),(2,2,2,'Accessory','Chair hardware set and installation parts',42,'Active'),(3,3,1,'Fabric','Premium sofa fabric and upholstery material',180,'Active'),(4,1,5,'Raw Material','Hotel bed frame timber package',980,'Active');
INSERT INTO Orders VALUES (1,'SO-2026-0001',1,1,5,'Confirmed','2026-06-03','VIP',3,14600),(2,'SO-2026-0002',2,3,1,'Processing','2026-06-05','Normal',3,5200);
INSERT INTO OrderItems VALUES (1,1,1,'Custom L-shape Sofa',1,6800,0,6800),(2,1,2,'Oak Dining Chair',10,780,0,7800),(3,2,3,'Marble Dining Table',1,5200,0,5200);
INSERT INTO PurchaseOrders VALUES (1,'PO-0001',1,1,1,'Oak frame materials for SO-2026-0001',20,350,'Supplier Confirmed','2026-06-04','2026-06-20'),(2,'PO-0002',2,2,1,'Chair hardware set',50,42,'Approved','2026-06-05','2026-06-18'),(3,'PO-0003',3,3,2,'Premium sofa fabric',15,180,'Request','2026-06-07','2026-06-25');
INSERT INTO Complaints VALUES (1,'FB-2026-0001',1,1,'Late delivery','Medium','In Progress',7,'2026-06-10','Customer asked for revised delivery schedule'),(2,'FB-2026-0002',2,2,'Damage','High','Open',7,'2026-06-12','Small scratch reported on table corner');
INSERT INTO Stock VALUES (1,'STK-0001',1,'Custom L-shape Sofa','Finished Product','WH-HK-01',8,3,'Normal','2026-06-10'),(2,'STK-0002',2,'Oak Dining Chair','Finished Product','WH-HK-01',18,20,'LOW STOCK','2026-06-10'),(3,'STK-0003',3,'Marble Dining Table','Finished Product','WH-CN-01',5,2,'Normal','2026-06-10'),(4,'STK-0004',NULL,'Oak Timber Board','Raw Material','WH-CN-01',120,40,'Normal','2026-06-10');
INSERT INTO StockMovements VALUES (1,'SM-0001',2,'Issue',10,'WH-HK-01','2026-06-06','Allocated to SO-2026-0001'),(2,'SM-0002',4,'Receipt',50,'WH-CN-01','2026-06-08','Supplier replenishment'),(3,'SM-0003',1,'Adjustment',1,'WH-HK-01','2026-06-09','Cycle count correction');
INSERT INTO DeliveryNotes VALUES (1,'DN-2026-0001',1,'WH-HK-01','Company Truck','Driver Chan','Closed','2026-06-08'),(2,'DN-2026-0002',2,'WH-HK-01','Courier','SF Express','Dispatched','2026-06-12');

-- 已將 ReplySlips, Invoices, Payments 預設資料中的 -2026- 年份移除
INSERT INTO ReplySlips VALUES (1,'RS-0001',1,1,'Kelly Yu','Delivery Acknowledgement','5 - Very Satisfied','No','Kelly Yu','Signatures/sig_rs_0001.png','Closed','2026-06-08','Received in good condition'),(2,'RS-0002',2,2,'Mrs. Lau','Customer Feedback','4 - Satisfied','Yes','Mrs. Lau','Signatures/sig_rs_0002.png','Follow-up Required','2026-06-12','Customer asks for clearer delivery time');
INSERT INTO Invoices VALUES (1,'INV-0001',1,1,14600,'HKD','Partially Paid','2026-06-04','2026-07-04'),(2,'INV-0002',2,2,5200,'HKD','Unpaid','2026-06-06','2026-07-06');
INSERT INTO Payments VALUES (1,'PAY-0001',1,3000,'FPS','2026-06-07','FPS-0001');

ALTER TABLE DeliveryNotes
    ADD COLUMN IF NOT EXISTS FromAddress VARCHAR(255) NULL AFTER Warehouse,
    ADD COLUMN IF NOT EXISTS ToAddress VARCHAR(255) NULL AFTER FromAddress,
    ADD COLUMN IF NOT EXISTS RouteNotes VARCHAR(255) NULL AFTER DispatchDate;

UPDATE DeliveryNotes dn
JOIN Orders o ON dn.OrderId = o.Id
JOIN Customers c ON o.CustomerId = c.Id
SET dn.FromAddress = COALESCE(NULLIF(dn.FromAddress,''),
    CASE dn.Warehouse
        WHEN 'WH-HK-01' THEN 'Premium Living Furniture HK Warehouse, Kwun Tong, Hong Kong'
        WHEN 'WH-CN-01' THEN 'Premium Living Furniture China Warehouse, Guangdong'
        WHEN 'WH-VN-01' THEN 'Premium Living Furniture Vietnam Warehouse, Ho Chi Minh City'
        ELSE dn.Warehouse
    END),
    dn.ToAddress = COALESCE(NULLIF(dn.ToAddress,''), c.Address),
    dn.RouteNotes = COALESCE(NULLIF(dn.RouteNotes,''), CONCAT(dn.Warehouse, ' -> ', c.Address));

-- Product image paths and customer reply-slip signature image
UPDATE Products SET ImagePath = 'ProductImages/3.png' WHERE ProductCode IN ('P001','F003');
UPDATE Products SET ImagePath = 'ProductImages/1.png' WHERE ProductCode IN ('P002','F001');
UPDATE Products SET ImagePath = 'ProductImages/2.png' WHERE ProductCode IN ('P003','F002');
UPDATE Products SET ImagePath = 'ProductImages/5.png' WHERE ProductCode IN ('P004','F005');
UPDATE Products SET ImagePath = 'ProductImages/6.png' WHERE ProductCode IN ('P005','F006');
UPDATE Products SET ImagePath = 'ProductImages/4.png' WHERE ProductCode = 'F004';
UPDATE ReplySlips SET SignatureRef = 'sig_customer_reply_slip.png' WHERE SignatureRef IS NULL OR SignatureRef = '' OR SignatureRef LIKE 'sig_rs_%';
