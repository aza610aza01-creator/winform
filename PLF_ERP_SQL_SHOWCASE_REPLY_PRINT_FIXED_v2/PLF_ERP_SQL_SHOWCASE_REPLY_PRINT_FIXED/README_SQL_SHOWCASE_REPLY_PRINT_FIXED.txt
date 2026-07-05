PLF ERP - SQL Database Showcase + Customer Reply Slip Print Fix

Fixed issues:
1. SQL Database Showcase
   - Sidebar SQL Database and utility Showcase SQL Database button now open the fixed read-only SQL showcase page.
   - The page lists tables with row counts.
   - Show Details displays DESCRIBE table structure and first 200 rows.
   - Uses safe table-name validation.

2. Customer Reply Slip printing
   - Print Content on ReplySlips prints ALL reply-slip rows as Customer Reply Slip form pages.
   - Print Selected Rows on ReplySlips prints selected reply-slip rows as Customer Reply Slip form pages.
   - Customer Signature section appears on both print actions.
   - If SignatureRef points to a missing image, the print form falls back to sig_customer_reply_slip.png.

3. Retained fixes
   - Utility panel: Check SQL Connection, Showcase SQL Database, Debug Check, Logout.
   - ProductImages/1.png to ProductImages/6.png included.
   - sig_customer_reply_slip.png included.
   - Products.ImagePath SQL fix retained.
