namespace Appsdemo.TravelCrm.Core.Security;

public static class Permissions
{
    public static class Dashboard { public const string View = "dashboard.view"; }

    public static class Leads
    {
        public const string View   = "leads.view";
        public const string Create = "leads.create";
        public const string Edit   = "leads.edit";
        public const string Delete = "leads.delete";
        public const string Assign = "leads.assign";
        public const string Export = "leads.export";
    }

    public static class Quotes
    {
        public const string View    = "quotes.view";
        public const string Create  = "quotes.create";
        public const string Edit    = "quotes.edit";
        public const string Delete  = "quotes.delete";
        public const string Approve = "quotes.approve";
        public const string Send    = "quotes.send";
        public const string Export  = "quotes.export";
    }

    public static class Bookings
    {
        public const string View   = "bookings.view";
        public const string Create = "bookings.create";
        public const string Edit   = "bookings.edit";
        public const string Cancel = "bookings.cancel";
        public const string Export = "bookings.export";
    }

    public static class Vouchers
    {
        public const string View   = "vouchers.view";
        public const string Create = "vouchers.create";
        public const string Edit   = "vouchers.edit";
        public const string Send   = "vouchers.send";
    }

    public static class Invoices
    {
        public const string View   = "invoices.view";
        public const string Create = "invoices.create";
        public const string Edit   = "invoices.edit";
        public const string Delete = "invoices.delete";
        public const string Send   = "invoices.send";
        public const string Export = "invoices.export";
    }

    public static class Payments
    {
        public const string View   = "payments.view";
        public const string Create = "payments.create";
        public const string Edit   = "payments.edit";
        public const string Delete = "payments.delete";
    }

    public static class Masters
    {
        public const string View   = "masters.view";
        public const string Manage = "masters.manage";
    }

    public static class Users
    {
        public const string View          = "users.view";
        public const string Create        = "users.create";
        public const string Edit          = "users.edit";
        public const string Delete        = "users.delete";
        public const string ResetPassword = "users.reset_password";
    }

    public static class Roles
    {
        public const string View   = "roles.view";
        public const string Manage = "roles.manage";
    }

    public static class Branches
    {
        public const string View   = "branches.view";
        public const string Manage = "branches.manage";
    }

    public static class Settings
    {
        public const string View   = "settings.view";
        public const string Manage = "settings.manage";
    }

    public static class Reports
    {
        public const string View   = "reports.view";
        public const string Export = "reports.export";
    }

    public static class Audit
    {
        public const string View = "audit.view";
    }

    public static IEnumerable<(string Key, string Module, string Action, string Display)> All()
    {
        yield return (Dashboard.View, "dashboard", "view",  "View Dashboard");

        yield return (Leads.View,   "leads", "view",   "View Leads");
        yield return (Leads.Create, "leads", "create", "Create Leads");
        yield return (Leads.Edit,   "leads", "edit",   "Edit Leads");
        yield return (Leads.Delete, "leads", "delete", "Delete Leads");
        yield return (Leads.Assign, "leads", "assign", "Assign Leads");
        yield return (Leads.Export, "leads", "export", "Export Leads");

        yield return (Quotes.View,    "quotes", "view",    "View Quotes");
        yield return (Quotes.Create,  "quotes", "create",  "Create Quotes");
        yield return (Quotes.Edit,    "quotes", "edit",    "Edit Quotes");
        yield return (Quotes.Delete,  "quotes", "delete",  "Delete Quotes");
        yield return (Quotes.Approve, "quotes", "approve", "Approve Quotes");
        yield return (Quotes.Send,    "quotes", "send",    "Send Quotes");
        yield return (Quotes.Export,  "quotes", "export",  "Export Quotes");

        yield return (Bookings.View,   "bookings", "view",   "View Bookings");
        yield return (Bookings.Create, "bookings", "create", "Create Bookings");
        yield return (Bookings.Edit,   "bookings", "edit",   "Edit Bookings");
        yield return (Bookings.Cancel, "bookings", "cancel", "Cancel Bookings");
        yield return (Bookings.Export, "bookings", "export", "Export Bookings");

        yield return (Vouchers.View,   "vouchers", "view",   "View Vouchers");
        yield return (Vouchers.Create, "vouchers", "create", "Create Vouchers");
        yield return (Vouchers.Edit,   "vouchers", "edit",   "Edit Vouchers");
        yield return (Vouchers.Send,   "vouchers", "send",   "Send Vouchers");

        yield return (Invoices.View,   "invoices", "view",   "View Invoices");
        yield return (Invoices.Create, "invoices", "create", "Create Invoices");
        yield return (Invoices.Edit,   "invoices", "edit",   "Edit Invoices");
        yield return (Invoices.Delete, "invoices", "delete", "Delete Invoices");
        yield return (Invoices.Send,   "invoices", "send",   "Send Invoices");
        yield return (Invoices.Export, "invoices", "export", "Export Invoices");

        yield return (Payments.View,   "payments", "view",   "View Payments");
        yield return (Payments.Create, "payments", "create", "Record Payments");
        yield return (Payments.Edit,   "payments", "edit",   "Edit Payments");
        yield return (Payments.Delete, "payments", "delete", "Delete Payments");

        yield return (Masters.View,   "masters", "view",   "View Master Data");
        yield return (Masters.Manage, "masters", "manage", "Manage Master Data");

        yield return (Users.View,          "users", "view",           "View Users");
        yield return (Users.Create,        "users", "create",         "Create Users");
        yield return (Users.Edit,          "users", "edit",           "Edit Users");
        yield return (Users.Delete,        "users", "delete",         "Delete Users");
        yield return (Users.ResetPassword, "users", "reset_password", "Reset User Password");

        yield return (Roles.View,   "roles", "view",   "View Roles");
        yield return (Roles.Manage, "roles", "manage", "Manage Roles");

        yield return (Branches.View,   "branches", "view",   "View Branches");
        yield return (Branches.Manage, "branches", "manage", "Manage Branches");

        yield return (Settings.View,   "settings", "view",   "View Settings");
        yield return (Settings.Manage, "settings", "manage", "Manage Settings");

        yield return (Reports.View,   "reports", "view",   "View Reports");
        yield return (Reports.Export, "reports", "export", "Export Reports");

        yield return (Audit.View, "audit", "view", "View Audit Log");
    }
}

public static class Features
{
    public const string ModuleCrm        = "module.crm";
    public const string ModuleQuotes     = "module.quotes";
    public const string ModuleBookings   = "module.bookings";
    public const string ModuleVouchers   = "module.vouchers";
    public const string ModuleInvoices   = "module.invoices";
    public const string ModulePayments   = "module.payments";
    public const string ModuleAccounting = "module.accounting";
    public const string ModuleReports    = "module.reports";
    public const string ModuleApi        = "module.api";
    public const string LimitMaxUsers    = "limit.max_users";
    public const string LimitMaxBranches = "limit.max_branches";
}

public static class SystemRoles
{
    public const string TenantAdmin = "TenantAdmin";
    public const string Manager     = "Manager";
    public const string Sales       = "Sales";
    public const string Ops         = "Ops";
    public const string Accounts    = "Accounts";
    public const string ReadOnly    = "ReadOnly";
}
