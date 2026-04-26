-- ============================================================================
-- Seed: features catalog + Starter / Pro / Enterprise plans
-- (Super-admin user is seeded by the app on first run, NOT here, because
-- the password hash must be produced by the .NET hasher.)
-- ============================================================================

INSERT INTO features (key, display_name, category, description) VALUES
    ('module.crm',        'CRM',                 'Module', 'Leads, follow-ups, pipeline'),
    ('module.quotes',     'Quotations',          'Module', 'Itinerary cost sheets, PDF, email'),
    ('module.bookings',   'Bookings',            'Module', 'Convert quotes to bookings'),
    ('module.vouchers',   'Supplier Vouchers',   'Module', 'Hotel/transport/sightseeing vouchers'),
    ('module.invoices',   'Invoicing',           'Module', 'Customer invoices with GST'),
    ('module.payments',   'Payments',            'Module', 'Receipts, allocations, ageing'),
    ('module.accounting', 'Accounting',          'Module', 'GL, ledgers, trial balance'),
    ('module.reports',    'Reports',             'Module', 'Advanced reports + exports'),
    ('module.api',        'External API access', 'Module', 'JWT API for integrations'),
    ('limit.max_users',    'Max users',     'Limit', 'Maximum users in the tenant'),
    ('limit.max_branches', 'Max branches',  'Limit', 'Maximum branches in the tenant')
ON CONFLICT (key) DO NOTHING;

INSERT INTO subscription_plans
    (name, description, price_monthly, price_yearly, max_users, max_branches, trial_days, is_active, sort_order)
VALUES
    ('Starter',    'CRM, Quotes and basic reports',                 1499,  14990,  5,  1, 14, TRUE, 1),
    ('Pro',        'Everything in Starter plus full operations',    3999,  39990, 25,  3, 14, TRUE, 2),
    ('Enterprise', 'Unlimited users, multi-branch, API access',     9999,  99990,  0,  0, 14, TRUE, 3)
ON CONFLICT (name) DO NOTHING;

-- Plan features
INSERT INTO plan_features (plan_id, feature_key, feature_value)
SELECT p.id, f.key, f.val
FROM subscription_plans p
JOIN (VALUES
    ('Starter',    'module.crm',        'true'),
    ('Starter',    'module.quotes',     'true'),
    ('Starter',    'module.reports',    'true'),
    ('Starter',    'limit.max_users',    '5'),
    ('Starter',    'limit.max_branches', '1'),

    ('Pro',        'module.crm',        'true'),
    ('Pro',        'module.quotes',     'true'),
    ('Pro',        'module.bookings',   'true'),
    ('Pro',        'module.vouchers',   'true'),
    ('Pro',        'module.invoices',   'true'),
    ('Pro',        'module.payments',   'true'),
    ('Pro',        'module.reports',    'true'),
    ('Pro',        'limit.max_users',    '25'),
    ('Pro',        'limit.max_branches', '3'),

    ('Enterprise', 'module.crm',        'true'),
    ('Enterprise', 'module.quotes',     'true'),
    ('Enterprise', 'module.bookings',   'true'),
    ('Enterprise', 'module.vouchers',   'true'),
    ('Enterprise', 'module.invoices',   'true'),
    ('Enterprise', 'module.payments',   'true'),
    ('Enterprise', 'module.accounting', 'true'),
    ('Enterprise', 'module.reports',    'true'),
    ('Enterprise', 'module.api',        'true'),
    ('Enterprise', 'limit.max_users',    '0'),
    ('Enterprise', 'limit.max_branches', '0')
) AS f(plan, key, val) ON p.name = f.plan
ON CONFLICT (plan_id, feature_key) DO NOTHING;
