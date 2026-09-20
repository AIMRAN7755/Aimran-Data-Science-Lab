using Microsoft.Data.Sqlite;

namespace AimranDataScienceLab.Engine.Data;

/// <summary>
/// Creates and migrates the SQLite metadata database schema.
/// </summary>
public static class DatabaseInitializer
{
    private const int CurrentSchemaVersion = 5;

    /// <summary>
    /// Ensure the database schema exists and is up to date.
    /// </summary>
    public static void Initialize(SqliteConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var connection = factory.CreateConnection();
        EnsureVersionTable(connection);

        var version = GetSchemaVersion(connection);
        if (version < CurrentSchemaVersion)
        {
            ApplyMigrations(connection, version);
        }
    }

    private static void EnsureVersionTable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static int GetSchemaVersion(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_version;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void ApplyMigrations(SqliteConnection connection, int fromVersion)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            if (fromVersion < 1)
            {
                ApplyV1(connection);
            }

            if (fromVersion < 2)
            {
                ApplyV2(connection);
            }

            if (fromVersion < 3)
            {
                ApplyV3(connection);
            }

            if (fromVersion < 4)
            {
                ApplyV4(connection);
            }

            if (fromVersion < 5)
            {
                ApplyV5(connection);
            }

            SetSchemaVersion(connection, CurrentSchemaVersion);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void SetSchemaVersion(SqliteConnection connection, int version)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM schema_version; INSERT INTO schema_version (version) VALUES ($v);";
        cmd.Parameters.AddWithValue("$v", version);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// V1: Core tables — Projects, Datasets, Experiments, Models, CleaningRules, CleaningPipelines.
    /// </summary>
    private static void ApplyV1(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            -- Projects
            CREATE TABLE IF NOT EXISTS projects (
                id              TEXT PRIMARY KEY,
                name            TEXT NOT NULL,
                description     TEXT,
                local_path      TEXT NOT NULL,
                status          TEXT NOT NULL DEFAULT 'Active',
                created_at      TEXT NOT NULL,
                updated_at      TEXT,
                tags            TEXT,
                azure_subscription_id   TEXT,
                azure_resource_group    TEXT,
                azure_workspace_name    TEXT,
                azure_storage_account   TEXT,
                azure_container_name    TEXT,
                azure_is_connected      INTEGER NOT NULL DEFAULT 0
            );

            -- Datasets
            CREATE TABLE IF NOT EXISTS datasets (
                id              TEXT PRIMARY KEY,
                name            TEXT NOT NULL,
                description     TEXT,
                file_path       TEXT NOT NULL,
                size_bytes      INTEGER NOT NULL DEFAULT 0,
                row_count       INTEGER NOT NULL DEFAULT 0,
                column_count    INTEGER NOT NULL DEFAULT 0,
                format          TEXT NOT NULL DEFAULT 'Csv',
                created_at      TEXT NOT NULL,
                updated_at      TEXT,
                version         INTEGER NOT NULL DEFAULT 1,
                storage_location TEXT NOT NULL DEFAULT 'Local',
                azure_blob_url  TEXT,
                tags            TEXT
            );

            -- Experiments
            CREATE TABLE IF NOT EXISTS experiments (
                id              TEXT PRIMARY KEY,
                name            TEXT NOT NULL,
                description     TEXT,
                project_id      TEXT NOT NULL,
                dataset_id      TEXT NOT NULL,
                status          TEXT NOT NULL DEFAULT 'Pending',
                created_at      TEXT NOT NULL,
                started_at      TEXT,
                completed_at    TEXT,
                compute_target  TEXT NOT NULL DEFAULT 'Local',
                hyperparameters TEXT,
                metrics         TEXT,
                azure_ml_experiment_id TEXT,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
                FOREIGN KEY (dataset_id) REFERENCES datasets(id) ON DELETE CASCADE
            );

            -- Experiment runs
            CREATE TABLE IF NOT EXISTS experiment_runs (
                id              TEXT PRIMARY KEY,
                experiment_id   TEXT NOT NULL,
                run_number      INTEGER NOT NULL,
                started_at      TEXT NOT NULL,
                completed_at    TEXT,
                status          TEXT NOT NULL DEFAULT 'Pending',
                metrics         TEXT,
                log_output      TEXT,
                error_message   TEXT,
                FOREIGN KEY (experiment_id) REFERENCES experiments(id) ON DELETE CASCADE
            );

            -- Models
            CREATE TABLE IF NOT EXISTS models (
                id              TEXT PRIMARY KEY,
                name            TEXT NOT NULL,
                description     TEXT,
                experiment_id   TEXT NOT NULL,
                algorithm       TEXT NOT NULL,
                framework       TEXT NOT NULL,
                version         INTEGER NOT NULL DEFAULT 1,
                file_path       TEXT NOT NULL,
                size_bytes      INTEGER NOT NULL DEFAULT 0,
                created_at      TEXT NOT NULL,
                status          TEXT NOT NULL DEFAULT 'Draft',
                performance_metrics TEXT,
                hyperparameters TEXT,
                azure_ml_model_id TEXT,
                is_deployed     INTEGER NOT NULL DEFAULT 0,
                deployment_endpoint TEXT,
                FOREIGN KEY (experiment_id) REFERENCES experiments(id) ON DELETE CASCADE
            );

            -- Cleaning rules
            CREATE TABLE IF NOT EXISTS cleaning_rules (
                id              TEXT PRIMARY KEY,
                name            TEXT NOT NULL,
                description     TEXT,
                type            TEXT NOT NULL,
                scope           TEXT NOT NULL,
                conditions      TEXT,
                actions         TEXT,
                priority        REAL NOT NULL DEFAULT 1.0,
                is_enabled      INTEGER NOT NULL DEFAULT 1,
                is_learned      INTEGER NOT NULL DEFAULT 0,
                times_applied   INTEGER NOT NULL DEFAULT 0,
                success_rate    REAL NOT NULL DEFAULT 0,
                created_by_user TEXT,
                project_id      TEXT,
                tags            TEXT,
                created_at      TEXT NOT NULL,
                last_applied_at TEXT
            );

            -- Cleaning pipelines
            CREATE TABLE IF NOT EXISTS cleaning_pipelines (
                id              TEXT PRIMARY KEY,
                name            TEXT NOT NULL,
                description     TEXT,
                version         INTEGER NOT NULL DEFAULT 1,
                parent_pipeline_id TEXT,
                category        TEXT NOT NULL,
                steps           TEXT,
                default_params  TEXT,
                aggressiveness  TEXT NOT NULL DEFAULT 'Balanced',
                is_template     INTEGER NOT NULL DEFAULT 0,
                is_public       INTEGER NOT NULL DEFAULT 0,
                industry        TEXT,
                tags            TEXT,
                times_used      INTEGER NOT NULL DEFAULT 0,
                avg_quality_improvement REAL NOT NULL DEFAULT 0,
                created_by_user TEXT,
                created_at      TEXT NOT NULL,
                updated_at      TEXT,
                FOREIGN KEY (parent_pipeline_id) REFERENCES cleaning_pipelines(id)
            );

            -- Indexes for common queries
            CREATE INDEX IF NOT EXISTS idx_datasets_name ON datasets(name);
            CREATE INDEX IF NOT EXISTS idx_experiments_project ON experiments(project_id);
            CREATE INDEX IF NOT EXISTS idx_experiments_dataset ON experiments(dataset_id);
            CREATE INDEX IF NOT EXISTS idx_experiments_status ON experiments(status);
            CREATE INDEX IF NOT EXISTS idx_models_experiment ON models(experiment_id);
            CREATE INDEX IF NOT EXISTS idx_models_status ON models(status);
            CREATE INDEX IF NOT EXISTS idx_runs_experiment ON experiment_runs(experiment_id);
            CREATE INDEX IF NOT EXISTS idx_rules_scope ON cleaning_rules(scope);
            CREATE INDEX IF NOT EXISTS idx_pipelines_category ON cleaning_pipelines(category);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// V2: Audit trail, engine process logs, and API gateway request history.
    /// </summary>
    private static void ApplyV2(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            -- Audit log — tracks all significant user and system actions
            CREATE TABLE IF NOT EXISTS audit_log (
                id              TEXT PRIMARY KEY,
                timestamp       TEXT NOT NULL,
                actor           TEXT NOT NULL DEFAULT 'system',
                action          TEXT NOT NULL,
                entity_type     TEXT NOT NULL,
                entity_id       TEXT,
                details         TEXT,
                severity        TEXT NOT NULL DEFAULT 'Info'
            );

            -- Engine process history — records starts, stops, crashes, restarts
            CREATE TABLE IF NOT EXISTS engine_process_log (
                id              TEXT PRIMARY KEY,
                engine_key      TEXT NOT NULL,
                event_type      TEXT NOT NULL,
                process_id      INTEGER,
                exit_code       INTEGER,
                restart_count   INTEGER NOT NULL DEFAULT 0,
                message         TEXT,
                timestamp       TEXT NOT NULL
            );

            -- API gateway request log — tracks outbound requests with latency and status
            CREATE TABLE IF NOT EXISTS gateway_request_log (
                id              TEXT PRIMARY KEY,
                engine_key      TEXT NOT NULL,
                method          TEXT NOT NULL,
                path            TEXT NOT NULL,
                status_code     INTEGER,
                latency_ms      REAL NOT NULL DEFAULT 0,
                retry_count     INTEGER NOT NULL DEFAULT 0,
                error_message   TEXT,
                timestamp       TEXT NOT NULL
            );

            -- Experiment history snapshots — preserves metric history over time
            CREATE TABLE IF NOT EXISTS experiment_metric_history (
                id              TEXT PRIMARY KEY,
                experiment_id   TEXT NOT NULL,
                run_id          TEXT,
                metric_name     TEXT NOT NULL,
                metric_value    REAL NOT NULL,
                epoch           INTEGER,
                recorded_at     TEXT NOT NULL,
                FOREIGN KEY (experiment_id) REFERENCES experiments(id) ON DELETE CASCADE
            );

            -- Model version history — tracks lineage of model artifacts
            CREATE TABLE IF NOT EXISTS model_version_history (
                id              TEXT PRIMARY KEY,
                model_id        TEXT NOT NULL,
                version         INTEGER NOT NULL,
                file_path       TEXT NOT NULL,
                size_bytes      INTEGER NOT NULL DEFAULT 0,
                metrics         TEXT,
                parent_version  INTEGER,
                created_at      TEXT NOT NULL,
                created_by      TEXT NOT NULL DEFAULT 'system',
                FOREIGN KEY (model_id) REFERENCES models(id) ON DELETE CASCADE
            );

            -- Indexes for V2 tables
            CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON audit_log(timestamp);
            CREATE INDEX IF NOT EXISTS idx_audit_entity ON audit_log(entity_type, entity_id);
            CREATE INDEX IF NOT EXISTS idx_audit_action ON audit_log(action);
            CREATE INDEX IF NOT EXISTS idx_engine_log_key ON engine_process_log(engine_key);
            CREATE INDEX IF NOT EXISTS idx_engine_log_time ON engine_process_log(timestamp);
            CREATE INDEX IF NOT EXISTS idx_gw_request_engine ON gateway_request_log(engine_key);
            CREATE INDEX IF NOT EXISTS idx_gw_request_time ON gateway_request_log(timestamp);
            CREATE INDEX IF NOT EXISTS idx_metric_history_exp ON experiment_metric_history(experiment_id);
            CREATE INDEX IF NOT EXISTS idx_metric_history_name ON experiment_metric_history(metric_name);
            CREATE INDEX IF NOT EXISTS idx_model_version_model ON model_version_history(model_id);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// V3: Authentication — Users, security audit logs, and session tracking.
    /// </summary>
    private static void ApplyV3(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            -- Users
            CREATE TABLE IF NOT EXISTS users (
                user_id         TEXT PRIMARY KEY,
                username        TEXT NOT NULL UNIQUE,
                password_hash   TEXT NOT NULL,
                email           TEXT,
                role            TEXT NOT NULL DEFAULT 'Viewer',
                is_locked       INTEGER NOT NULL DEFAULT 0,
                failed_login_attempts INTEGER NOT NULL DEFAULT 0,
                locked_until    TEXT,
                created_at      TEXT NOT NULL,
                last_login_at   TEXT
            );

            -- Security audit logs
            CREATE TABLE IF NOT EXISTS security_audit_logs (
                id              TEXT PRIMARY KEY,
                user_id         TEXT,
                action          TEXT NOT NULL,
                detail          TEXT,
                timestamp       TEXT NOT NULL,
                ip_address      TEXT,
                FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL
            );

            -- Indexes for V3 tables
            CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username ON users(username);
            CREATE INDEX IF NOT EXISTS idx_security_audit_user ON security_audit_logs(user_id);
            CREATE INDEX IF NOT EXISTS idx_security_audit_time ON security_audit_logs(timestamp);
            CREATE INDEX IF NOT EXISTS idx_security_audit_action ON security_audit_logs(action);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// V4: Session tracking, execution policy logs, and user permissions.
    /// </summary>
    private static void ApplyV4(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            -- Active user sessions
            CREATE TABLE IF NOT EXISTS user_sessions (
                session_id      TEXT PRIMARY KEY,
                user_id         TEXT NOT NULL,
                started_at      TEXT NOT NULL,
                last_activity   TEXT NOT NULL,
                expired_at      TEXT,
                is_active       INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
            );

            -- Execution sandbox policy enforcement log
            CREATE TABLE IF NOT EXISTS execution_policy_log (
                id              TEXT PRIMARY KEY,
                user_id         TEXT,
                engine          TEXT NOT NULL,
                action          TEXT NOT NULL,
                policy_name     TEXT,
                violations      TEXT,
                duration_sec    REAL,
                exit_code       INTEGER,
                timestamp       TEXT NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL
            );

            -- Indexes for V4 tables
            CREATE INDEX IF NOT EXISTS idx_sessions_user ON user_sessions(user_id);
            CREATE INDEX IF NOT EXISTS idx_sessions_active ON user_sessions(is_active);
            CREATE INDEX IF NOT EXISTS idx_exec_policy_user ON execution_policy_log(user_id);
            CREATE INDEX IF NOT EXISTS idx_exec_policy_time ON execution_policy_log(timestamp);
            CREATE INDEX IF NOT EXISTS idx_exec_policy_engine ON execution_policy_log(engine);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// V5: Administration — storage quotas and system settings.
    /// </summary>
    private static void ApplyV5(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            -- Storage quotas per user
            CREATE TABLE IF NOT EXISTS storage_quotas (
                user_id             TEXT PRIMARY KEY,
                max_storage_bytes   INTEGER NOT NULL DEFAULT 107374182400,
                used_storage_bytes  INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
            );

            -- Global system settings (key-value)
            CREATE TABLE IF NOT EXISTS system_settings (
                key         TEXT PRIMARY KEY,
                value       TEXT NOT NULL,
                description TEXT,
                updated_at  TEXT NOT NULL
            );

            -- Indexes for V5 tables
            CREATE INDEX IF NOT EXISTS idx_settings_key ON system_settings(key);
            """;
        cmd.ExecuteNonQuery();
    }
}
