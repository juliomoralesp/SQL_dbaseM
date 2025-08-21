-- Script for database ClientManager
-- Generated on 2025-08-20 08:25:31

-- CREATE TABLE script for dbo.spt_fallback_db
CREATE TABLE [dbo].[spt_fallback_db] (
    [xserver_name] VARCHAR(30) NOT NULL,
    [xdttm_ins] DATETIME NOT NULL,
    [xdttm_last_ins_upd] DATETIME NOT NULL,
    [xfallback_dbid] SMALLINT(5),
    [name] VARCHAR(30) NOT NULL,
    [dbid] SMALLINT(5) NOT NULL,
    [status] SMALLINT(5) NOT NULL,
    [version] SMALLINT(5) NOT NULL
);


-- INSERT statements for dbo.spt_fallback_db


-- CREATE TABLE script for dbo.spt_fallback_dev
CREATE TABLE [dbo].[spt_fallback_dev] (
    [xserver_name] VARCHAR(30) NOT NULL,
    [xdttm_ins] DATETIME NOT NULL,
    [xdttm_last_ins_upd] DATETIME NOT NULL,
    [xfallback_low] INT(10),
    [xfallback_drive] CHAR(2),
    [low] INT(10) NOT NULL,
    [high] INT(10) NOT NULL,
    [status] SMALLINT(5) NOT NULL,
    [name] VARCHAR(30) NOT NULL,
    [phyname] VARCHAR(127) NOT NULL
);


-- INSERT statements for dbo.spt_fallback_dev


-- CREATE TABLE script for dbo.spt_fallback_usg
CREATE TABLE [dbo].[spt_fallback_usg] (
    [xserver_name] VARCHAR(30) NOT NULL,
    [xdttm_ins] DATETIME NOT NULL,
    [xdttm_last_ins_upd] DATETIME NOT NULL,
    [xfallback_vstart] INT(10),
    [dbid] SMALLINT(5) NOT NULL,
    [segmap] INT(10) NOT NULL,
    [lstart] INT(10) NOT NULL,
    [sizepg] INT(10) NOT NULL,
    [vstart] INT(10) NOT NULL
);


-- INSERT statements for dbo.spt_fallback_usg


-- CREATE TABLE script for dbo.spt_monitor
CREATE TABLE [dbo].[spt_monitor] (
    [lastrun] DATETIME NOT NULL,
    [cpu_busy] INT(10) NOT NULL,
    [io_busy] INT(10) NOT NULL,
    [idle] INT(10) NOT NULL,
    [pack_received] INT(10) NOT NULL,
    [pack_sent] INT(10) NOT NULL,
    [connections] INT(10) NOT NULL,
    [pack_errors] INT(10) NOT NULL,
    [total_read] INT(10) NOT NULL,
    [total_write] INT(10) NOT NULL,
    [total_errors] INT(10) NOT NULL
);


-- INSERT statements for dbo.spt_monitor
INSERT INTO [dbo].[spt_monitor]
([lastrun], [cpu_busy], [io_busy], [idle], [pack_received], [pack_sent], [connections], [pack_errors], [total_read], [total_write], [total_errors])
VALUES
('8/12/2025 5:14:46 PM', 20, 8, 220, 0, 0, 31, 0, 0, 0, 0)
;


-- CREATE TABLE script for dbo.MSreplication_options
CREATE TABLE [dbo].[MSreplication_options] (
    [optname] NVARCHAR(128) NOT NULL,
    [value] BIT NOT NULL,
    [major_version] INT(10) NOT NULL,
    [minor_version] INT(10) NOT NULL,
    [revision] INT(10) NOT NULL,
    [install_failures] INT(10) NOT NULL
);


-- INSERT statements for dbo.MSreplication_options
INSERT INTO [dbo].[MSreplication_options]
([optname], [value], [major_version], [minor_version], [revision], [install_failures])
VALUES
('transactional', 1, 90, 0, 0, 0),
('merge', 1, 90, 0, 0, 0),
('security_model', 1, 90, 0, 0, 0)
;


