namespace PremiumLivingFurnitureWinForms;

public static class Security
{
    static bool IsAccountAdminOnlyTable(string table) => table is "Users" or "UserRoles";

    public static bool CanAccess(string role, string table)
    {
        if (IsAccountAdminOnlyTable(table)) return role == "Admin";
        if (role == "Admin" || role == "Manager") return true;
        if (role == "Sales Staff") return table is "Customers" or "Products" or "Orders" or "OrderItems" or "Complaints" or "DeliveryNotes" or "ReplySlips";
        if (role == "Warehouse Clerk") return table is "Products" or "Stock" or "StockMovements" or "DeliveryNotes" or "Suppliers" or "SupplierItems" or "PurchaseOrders";
        if (role == "Finance Staff") return table is "Customers" or "Orders" or "Invoices" or "Payments" or "PurchaseOrders" or "Suppliers";
        if (role == "Customer Service") return table is "Customers" or "Orders" or "Complaints" or "ReplySlips";
        return true;
    }

    public static bool CanAdd(string role, string table)
    {
        if (IsAccountAdminOnlyTable(table)) return role == "Admin";
        if (role == "Admin" || role == "Manager") return true;
        if (role == "Sales Staff") return table is "Customers" or "Orders" or "Complaints";
        if (role == "Warehouse Clerk") return table is "Stock" or "StockMovements" or "PurchaseOrders" or "Suppliers" or "SupplierItems";
        if (role == "Finance Staff") return table is "Invoices" or "Payments";
        if (role == "Customer Service") return table is "Complaints" or "ReplySlips";
        return false;
    }

    public static bool CanDelete(string role, string table)
    {
        if (IsAccountAdminOnlyTable(table)) return role == "Admin";
        return role == "Admin" || role == "Manager";
    }

    public static bool CanShowcaseDatabase(string role)
    {
        return role is "Admin" or "Manager" or "Finance Staff" or "Warehouse Clerk";
    }

    public static bool CanDebugCheck(string role)
    {
        return role is "Admin" or "Manager";
    }
}
