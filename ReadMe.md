SQL Server CDC to ClickHouse via Kafka & Debezium
This project implements a Change Data Capture (CDC) pipeline that streams real-time database changes from SQL Server into ClickHouse for analytics, using Apache Kafka and Debezium.

🏗 Architecture
SQL Server: Source database with CDC enabled on specific tables.

Debezium (Kafka Connect): Monitors SQL Server transaction logs and produces JSON events to Kafka.

Apache Kafka: The event streaming platform acting as the message broker.

ClickHouse: The OLAP database that consumes Kafka topics via a Materialized View.

🚀 Docker Stack Configuration
The environment is defined in docker-compose.yml using the following services:

mssql-2022: SQL Server Developer Edition.

kafka: Bitnami Legacy Kafka (KRaft mode).

connect: Debezium Connect with SQL Server plugins.

clickhouse: ClickHouse Server for analytical storage.

🛠 Setup & Configuration
1. SQL Server Preparation
CDC must be enabled on the database and the target tables:

SQL
USE DebeziumTest;
EXEC sys.sp_cdc_enable_db;

EXEC sys.sp_cdc_enable_table 
    @source_schema = N'dbo', 
    @source_name = N'Employee', 
    @role_name = NULL, 
    @supports_net_changes = 1;
2. Debezium Connector (REST API)
Endpoint: POST http://localhost:8083/connectors
Payload:

JSON
{
  "name": "debeziumtest-connector",
  "config": {
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "database.hostname": "mssql",
    "database.names": "DebeziumTest",
    "table.include.list": "dbo.Employee",
    "topic.prefix": "testenv"
  }
}
3. ClickHouse Consumer Logic
ClickHouse consumes the data using a 3-tier table approach:

Target Table: AnalyticsDB.Employee (Using ReplacingMergeTree for upserts).

Queue Table: AnalyticsDB.Employee_KafkaQueue (Using ENGINE = Kafka).

Materialized View: AnalyticsDB.Employee_MV (Parses JSON payload.after).

📊 Data Schema
The Employee table in both systems follows this schema:
| Column | Type | Description |
| :--- | :--- | :--- |
| Id | Int32 | Primary Key (SQL Server Identity) |
| Name | String | Employee Full Name |
| Department | String | Department Name |

📝 Usage Notes
Viewing Raw Data: SELECT * FROM AnalyticsDB.Employee_KafkaQueue LIMIT 10;

Verifying Connector Status: GET http://localhost:8083/connectors/debeziumtest-connector/status