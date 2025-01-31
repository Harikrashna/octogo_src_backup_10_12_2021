﻿CREATE TABLE [dbo].[TblMst_CityOld] (
    [SNo]                        INT             IDENTITY (1, 1) NOT NULL,
    [ZoneSNo]                    INT             NULL,
    [ZoneName]                   VARCHAR (50)    NULL,
    [CityCode]                   CHAR (3)        NOT NULL,
    [CityName]                   NVARCHAR (100)  NOT NULL,
    [CountrySNo]                 INT             NOT NULL,
    [CountryCode]                CHAR (2)        NOT NULL,
    [CountryName]                VARCHAR (100)   NOT NULL,
    [DaylightSaving]             NCHAR (10)      NULL,
    [TimeDifference]             INT             NULL,
    [TimeZoneSNo]                INT             NULL,
    [IsDayLightSaving]           BIT             NULL,
    [IATAArea]                   TINYINT         NULL,
    [IsActive]                   BIT             NOT NULL,
    [CreatedOn]                  DATETIME        NOT NULL,
    [CreatedBy]                  INT             NOT NULL,
    [UpdatedOn]                  DATETIME        NOT NULL,
    [UpdatedBy]                  INT             NOT NULL,
    [CurrentTime]                VARCHAR (20)    NULL,
    [TimeZoneMasterSno]          INT             NULL,
    [PriorApproval]              BIT             NULL,
    [SHCSNo]                     NVARCHAR (MAX)  NULL,
    [DGClassSNo]                 NVARCHAR (MAX)  NULL,
    [VolumeConversionInch]       DECIMAL (8, 3)  NULL,
    [VolumeConversionCM]         DECIMAL (18, 3) NULL,
    [DomesticBookingPeriod]      INT             NULL,
    [InternationalBookingPeriod] INT             NULL,
    [IsHouse]                    BIT             NULL,
    [EventType]                  TINYINT         NULL,
    [IsCraMasterTransfer]        TINYINT         NULL,
    [IsDimensioMandatoryAtCity]  BIT             NULL,
    [StateSNo]                   INT             NULL,
    [IsSync]                     BIT             NULL,
    [LastChangeDateTime]         DATETIME        NULL,
    [StateCode]                  NVARCHAR (30)   NULL,
    [StateName]                  VARCHAR (100)   NULL,
    CONSTRAINT [PK_TblMst_CityOld] PRIMARY KEY CLUSTERED ([SNo] ASC)
);

