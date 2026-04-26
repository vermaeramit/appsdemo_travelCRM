-- ============================================================================
-- Tenant DB: CRM, quotes, bookings, vouchers, invoices, payments
-- ============================================================================

CREATE TABLE leads (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_no             VARCHAR(40) NOT NULL UNIQUE,
    source              VARCHAR(50),
    customer_name       VARCHAR(200) NOT NULL,
    email               VARCHAR(150),
    phone               VARCHAR(30),
    destination_id      UUID REFERENCES destinations(id),
    travel_date         DATE,
    travel_nights       INT,
    pax_adults          INT NOT NULL DEFAULT 0,
    pax_children        INT NOT NULL DEFAULT 0,
    budget              NUMERIC(12,2),
    currency_code       CHAR(3) NOT NULL DEFAULT 'INR',
    status              VARCHAR(30) NOT NULL DEFAULT 'New',
    lost_reason         VARCHAR(200),
    assigned_to         UUID REFERENCES users(id),
    branch_id           UUID REFERENCES branches(id),
    notes               TEXT,
    is_deleted          BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID
);
CREATE INDEX ix_leads_status   ON leads(status);
CREATE INDEX ix_leads_assigned ON leads(assigned_to);
CREATE INDEX ix_leads_branch   ON leads(branch_id);

CREATE TABLE lead_followups (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_id             UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    followup_date       TIMESTAMPTZ NOT NULL,
    mode                VARCHAR(30),
    notes               TEXT,
    next_followup_date  TIMESTAMPTZ,
    done_by             UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE lead_activities (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_id       UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    activity_type VARCHAR(40) NOT NULL,
    description   TEXT,
    occurred_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    by_user_id    UUID REFERENCES users(id)
);

CREATE TABLE lead_documents (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_id      UUID NOT NULL REFERENCES leads(id) ON DELETE CASCADE,
    file_name    VARCHAR(300) NOT NULL,
    file_path    VARCHAR(500) NOT NULL,
    uploaded_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    uploaded_by  UUID REFERENCES users(id)
);

CREATE TABLE quotes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_no        VARCHAR(40) NOT NULL UNIQUE,
    lead_id         UUID REFERENCES leads(id),
    version         INT NOT NULL DEFAULT 1,
    valid_till      DATE,
    status          VARCHAR(30) NOT NULL DEFAULT 'Draft',
    total_cost      NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_sale      NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_tax       NUMERIC(12,2) NOT NULL DEFAULT 0,
    grand_total     NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency_code   CHAR(3) NOT NULL DEFAULT 'INR',
    branch_id       UUID REFERENCES branches(id),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID
);

CREATE TABLE quote_items (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_id      UUID NOT NULL REFERENCES quotes(id) ON DELETE CASCADE,
    day_no        INT,
    item_date     DATE,
    item_type     VARCHAR(30),
    reference_id  UUID,
    description   TEXT,
    qty           NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_cost     NUMERIC(12,2) NOT NULL DEFAULT 0,
    unit_sale     NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_rate      NUMERIC(5,2)  NOT NULL DEFAULT 0,
    line_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
    sort_order    INT NOT NULL DEFAULT 0
);

CREATE TABLE quote_terms (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_id    UUID NOT NULL REFERENCES quotes(id) ON DELETE CASCADE,
    terms_text  TEXT NOT NULL,
    sort_order  INT NOT NULL DEFAULT 0
);

CREATE TABLE quote_versions (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_id      UUID NOT NULL REFERENCES quotes(id) ON DELETE CASCADE,
    version_no    INT NOT NULL,
    snapshot_json JSONB NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by    UUID REFERENCES users(id)
);

CREATE TABLE bookings (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_no      VARCHAR(40) NOT NULL UNIQUE,
    quote_id        UUID REFERENCES quotes(id),
    lead_id         UUID REFERENCES leads(id),
    customer_name   VARCHAR(200) NOT NULL,
    travel_start    DATE,
    travel_end      DATE,
    status          VARCHAR(30) NOT NULL DEFAULT 'Confirmed',
    total_amount    NUMERIC(12,2) NOT NULL DEFAULT 0,
    paid_amount     NUMERIC(12,2) NOT NULL DEFAULT 0,
    balance_amount  NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency_code   CHAR(3) NOT NULL DEFAULT 'INR',
    branch_id       UUID REFERENCES branches(id),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID
);
CREATE INDEX ix_bookings_status ON bookings(status);

CREATE TABLE booking_items (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id    UUID NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    day_no        INT,
    item_date     DATE,
    item_type     VARCHAR(30),
    reference_id  UUID,
    description   TEXT,
    qty           NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_cost     NUMERIC(12,2) NOT NULL DEFAULT 0,
    unit_sale     NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_rate      NUMERIC(5,2)  NOT NULL DEFAULT 0,
    line_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
    sort_order    INT NOT NULL DEFAULT 0
);

CREATE TABLE vouchers (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    voucher_no        VARCHAR(40) NOT NULL UNIQUE,
    booking_id        UUID NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    supplier_id       UUID REFERENCES suppliers(id),
    voucher_type      VARCHAR(30) NOT NULL,
    confirmation_no   VARCHAR(100),
    status            VARCHAR(30) NOT NULL DEFAULT 'Draft',
    amount            NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency_code     CHAR(3) NOT NULL DEFAULT 'INR',
    notes             TEXT,
    is_deleted        BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at        TIMESTAMPTZ,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by        UUID
);

CREATE TABLE voucher_items (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    voucher_id   UUID NOT NULL REFERENCES vouchers(id) ON DELETE CASCADE,
    description  TEXT,
    qty          NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_amount  NUMERIC(12,2) NOT NULL DEFAULT 0,
    line_total   NUMERIC(12,2) NOT NULL DEFAULT 0
);

CREATE TABLE invoices (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_no      VARCHAR(40) NOT NULL UNIQUE,
    booking_id      UUID REFERENCES bookings(id),
    customer_name   VARCHAR(200) NOT NULL,
    customer_gstin  VARCHAR(20),
    invoice_date    DATE NOT NULL DEFAULT CURRENT_DATE,
    due_date        DATE,
    sub_total       NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_total       NUMERIC(12,2) NOT NULL DEFAULT 0,
    grand_total     NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency_code   CHAR(3) NOT NULL DEFAULT 'INR',
    status          VARCHAR(30) NOT NULL DEFAULT 'Draft',
    branch_id       UUID REFERENCES branches(id),
    notes           TEXT,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID
);
CREATE INDEX ix_invoices_status ON invoices(status);

CREATE TABLE invoice_items (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id    UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    description   TEXT NOT NULL,
    hsn_sac       VARCHAR(20),
    qty           NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_price    NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_rate      NUMERIC(5,2)  NOT NULL DEFAULT 0,
    line_total    NUMERIC(12,2) NOT NULL DEFAULT 0,
    sort_order    INT NOT NULL DEFAULT 0
);

CREATE TABLE payments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_no      VARCHAR(40) NOT NULL UNIQUE,
    payment_date    DATE NOT NULL DEFAULT CURRENT_DATE,
    mode            VARCHAR(30) NOT NULL,
    reference_no    VARCHAR(100),
    amount          NUMERIC(12,2) NOT NULL,
    currency_code   CHAR(3) NOT NULL DEFAULT 'INR',
    notes           TEXT,
    received_by     UUID REFERENCES users(id),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID
);

CREATE TABLE payment_allocations (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id   UUID NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    invoice_id   UUID NOT NULL REFERENCES invoices(id),
    amount       NUMERIC(12,2) NOT NULL
);
