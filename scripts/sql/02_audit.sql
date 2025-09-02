-- Tabela e função de auditoria (esqueleto)
CREATE TABLE IF NOT EXISTS audit_log(
  id BIGSERIAL PRIMARY KEY,
  tabela text,
  operacao text,
  user_id int,
  empresa_id int,
  momento timestamptz default now(),
  txid bigint,
  old_data jsonb,
  new_data jsonb
);

CREATE OR REPLACE FUNCTION fn_audit() RETURNS trigger AS $$
BEGIN
  INSERT INTO audit_log(tabela, operacao, user_id, empresa_id, txid, old_data, new_data)
  VALUES (TG_TABLE_NAME, TG_OP,
          NULLIF(current_setting('app.user_id', true), '')::int,
          NULLIF(current_setting('app.empresa_id', true), '')::int,
          txid_current(),
          CASE WHEN TG_OP IN ('UPDATE','DELETE') THEN to_jsonb(OLD) END,
          CASE WHEN TG_OP IN ('UPDATE','INSERT') THEN to_jsonb(NEW) END);
  RETURN NEW;
END; $$ LANGUAGE plpgsql;
