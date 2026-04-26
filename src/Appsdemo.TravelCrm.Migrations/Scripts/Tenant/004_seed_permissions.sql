-- ============================================================================
-- Seed: permissions catalog + system roles. The TenantAdmin role gets ALL
-- permissions; other roles are seeded by the app on tenant provisioning so
-- they reflect the latest permission keys defined in code.
-- ============================================================================

INSERT INTO permissions (key, module, action, display_name) VALUES
    ('dashboard.view',      'dashboard', 'view',      'View Dashboard'),

    ('leads.view',          'leads',     'view',      'View Leads'),
    ('leads.create',        'leads',     'create',    'Create Leads'),
    ('leads.edit',          'leads',     'edit',      'Edit Leads'),
    ('leads.delete',        'leads',     'delete',    'Delete Leads'),
    ('leads.assign',        'leads',     'assign',    'Assign Leads'),
    ('leads.export',        'leads',     'export',    'Export Leads'),

    ('quotes.view',         'quotes',    'view',      'View Quotes'),
    ('quotes.create',       'quotes',    'create',    'Create Quotes'),
    ('quotes.edit',         'quotes',    'edit',      'Edit Quotes'),
    ('quotes.delete',       'quotes',    'delete',    'Delete Quotes'),
    ('quotes.approve',      'quotes',    'approve',   'Approve Quotes'),
    ('quotes.send',         'quotes',    'send',      'Send Quotes'),
    ('quotes.export',       'quotes',    'export',    'Export Quotes'),

    ('bookings.view',       'bookings',  'view',      'View Bookings'),
    ('bookings.create',     'bookings',  'create',    'Create Bookings'),
    ('bookings.edit',       'bookings',  'edit',      'Edit Bookings'),
    ('bookings.cancel',     'bookings',  'cancel',    'Cancel Bookings'),
    ('bookings.export',     'bookings',  'export',    'Export Bookings'),

    ('vouchers.view',       'vouchers',  'view',      'View Vouchers'),
    ('vouchers.create',     'vouchers',  'create',    'Create Vouchers'),
    ('vouchers.edit',       'vouchers',  'edit',      'Edit Vouchers'),
    ('vouchers.send',       'vouchers',  'send',      'Send Vouchers'),

    ('invoices.view',       'invoices',  'view',      'View Invoices'),
    ('invoices.create',     'invoices',  'create',    'Create Invoices'),
    ('invoices.edit',       'invoices',  'edit',      'Edit Invoices'),
    ('invoices.delete',     'invoices',  'delete',    'Delete Invoices'),
    ('invoices.send',       'invoices',  'send',      'Send Invoices'),
    ('invoices.export',     'invoices',  'export',    'Export Invoices'),

    ('payments.view',       'payments',  'view',          'View Payments'),
    ('payments.create',     'payments',  'create',        'Record Payments'),
    ('payments.edit',       'payments',  'edit',          'Edit Payments'),
    ('payments.delete',     'payments',  'delete',        'Delete Payments'),

    ('masters.view',        'masters',   'view',      'View Master Data'),
    ('masters.manage',      'masters',   'manage',    'Manage Master Data'),

    ('users.view',          'users',     'view',          'View Users'),
    ('users.create',        'users',     'create',        'Create Users'),
    ('users.edit',          'users',     'edit',          'Edit Users'),
    ('users.delete',        'users',     'delete',        'Delete Users'),
    ('users.reset_password','users',     'reset_password','Reset User Password'),

    ('roles.view',          'roles',     'view',      'View Roles'),
    ('roles.manage',        'roles',     'manage',    'Manage Roles'),

    ('branches.view',       'branches',  'view',      'View Branches'),
    ('branches.manage',     'branches',  'manage',    'Manage Branches'),

    ('settings.view',       'settings',  'view',      'View Settings'),
    ('settings.manage',     'settings',  'manage',    'Manage Settings'),

    ('reports.view',        'reports',   'view',      'View Reports'),
    ('reports.export',      'reports',   'export',    'Export Reports'),

    ('audit.view',          'audit',     'view',      'View Audit Log')
ON CONFLICT (key) DO NOTHING;

-- System roles
INSERT INTO roles (name, description, is_system) VALUES
    ('TenantAdmin', 'Full access — cannot be deleted',          TRUE),
    ('Manager',     'Operations + sales + reports',             TRUE),
    ('Sales',       'Lead-to-quote pipeline',                   TRUE),
    ('Ops',         'Bookings, vouchers, suppliers',            TRUE),
    ('Accounts',    'Invoices, payments, reports',              TRUE),
    ('ReadOnly',    'View-only across modules',                 TRUE)
ON CONFLICT (name) DO NOTHING;

-- TenantAdmin gets every permission
INSERT INTO role_permissions (role_id, permission_key)
SELECT r.id, p.key
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'TenantAdmin'
ON CONFLICT DO NOTHING;
