﻿CREATE TABLE [dbo].[TblMst_Airline] (
    [SNo]                    INT             IDENTITY (1, 1) NOT NULL,
    [AirlineCode]            VARCHAR (3)     NOT NULL,
    [CarrierCode]            VARCHAR (3)     NOT NULL,
    [AirlineName]            NVARCHAR (100)  NOT NULL,
    [AirportCodeSNo]         INT             NULL,
    [AirportCode]            VARCHAR (3)     NULL,
    [ICAOCode]               VARCHAR (4)     NULL,
    [Address]                NVARCHAR (250)  NULL,
    [CountrySNo]             INT             NULL,
    [CountryName]            VARCHAR (50)    NULL,
    [SenderEmailId]          VARCHAR (100)   NULL,
    [AirlineEmailId]         VARCHAR (100)   NULL,
    [AirlineLogo]            VARCHAR (200)   NULL,
    [AwbLogo]                VARCHAR (200)   NULL,
    [AirlineWebsite]         VARCHAR (100)   NULL,
    [MobileCountryCode]      INT             NULL,
    [MobileNo]               VARCHAR (20)    NULL,
    [PhoneCountryCode]       INT             NULL,
    [CityPrefixCode]         INT             NULL,
    [PhoneNo]                VARCHAR (20)    NULL,
    [IsActive]               BIT             NULL,
    [CurrencySNo]            INT             NULL,
    [CurrencyCode]           VARCHAR (3)     NULL,
    [CreatedOn]              DATETIME        NULL,
    [CreatedBy]              INT             NULL,
    [UpdatedOn]              DATETIME        CONSTRAINT [DF__TblMst_Airline__Updated__15502E78] DEFAULT (getdate()) NULL,
    [UpdatedBy]              INT             NULL,
    [ContactPerson]          NVARCHAR (100)  NULL,
    [IsCCAllowed]            BIT             CONSTRAINT [DF_TblMst_Airline_IsCCAllowed] DEFAULT ((0)) NULL,
    [IsPartAllowed]          BIT             CONSTRAINT [DF_TblMst_Airline_IsPartAllowed] DEFAULT ((0)) NULL,
    [IsCheckModulus7]        BIT             CONSTRAINT [DF_TblMst_Airline_IsCheckModulus7] DEFAULT ((0)) NULL,
    [AWBDuplicacy]           INT             NULL,
    [HandlingInformation]    NVARCHAR (250)  NULL,
    [IsInterline]            BIT             NULL,
    [InvoicingCycle]         TINYINT         NULL,
    [EmailAddress]           VARCHAR (200)   NULL,
    [SitaAddress]            VARCHAR (50)    NULL,
    [FaxNo]                  VARCHAR (50)    NULL,
    [BillDate]               DATETIME        NULL,
    [CSDCode]                VARCHAR (25)    NULL,
    [SMS]                    BIT             NULL,
    [Message]                BIT             NULL,
    [Mobile]                 VARCHAR (25)    NULL,
    [Email]                  VARCHAR (100)   NULL,
    [BillingAddress]         VARCHAR (250)   NULL,
    [IsAllowedCL]            BIT             NULL,
    [IsEDIEnable]            BIT             CONSTRAINT [DF__TblMst_Airline__IsEDIEn__686A3DB1] DEFAULT ((0)) NULL,
    [IsCashAirline]          BIT             NULL,
    [Commission]             DECIMAL (18, 3) NULL,
    [OverBookingCapacity]    INT             NULL,
    [FreeSaleCapacity]       INT             NULL,
    [OverBookingCapacityVol] INT             NULL,
    [FreeSaleCapacityVol]    INT             NULL,
    [RateMarkup]             INT             NULL,
    [AirlineMember]          TINYINT         NULL,
    [AirlineSignatory]       BIT             NULL,
    [ISCPercentage]          DECIMAL (5, 2)  NULL,
    [SIS]                    VARCHAR (20)    NULL,
    [MaxStackContainer]      INT             NULL,
    [MaxStackPallets]        INT             NULL,
    [UCMOutAlert]            INT             NULL,
    [UCMINAlert]             INT             NULL,
    [EventType]              INT             NULL,
    [IsCraMasterTransfer]    TINYINT         NULL,
    [FOH_FWB]                INT             NULL,
    [IsHandling]             BIT             NULL,
    [IsUpdated]              BIT             NULL,
    [CountryRegistration]    VARCHAR (50)    NULL,
    [VATRegistrationNumber]  VARCHAR (50)    NULL,
    [SAPCustomeCode]         VARCHAR (50)    NULL,
    [IsDefaultAirline]       BIT             CONSTRAINT [DF__TblMst_Airline__IsDefau__4DB619DA] DEFAULT ((0)) NULL,
    [DimensionMandatoryOn]   TINYINT         NULL,
    [AirlinelineType]        TINYINT         NULL,
    [IsRFSHandling]          BIT             NULL,
    [IsFullTransitTonnage]   INT             NULL,
    [IsSAS]                  INT             NULL,
    [IsSASVendor]            INT             NULL,
    [IsAirlineVendor]        INT             NULL,
    [NoDomesticFlights]      INT             CONSTRAINT [DF__TblMst_Airline__NoDomes__52D16B60] DEFAULT ((0)) NOT NULL,
    [NoInternationalFlights] INT             CONSTRAINT [DF__TblMst_Airline__NoInter__53C58F99] DEFAULT ((0)) NOT NULL,
    [DefaultGSTNumber]       VARCHAR (20)    NULL,
    [DefaultAddress]         VARCHAR (500)   NULL,
    CONSTRAINT [PK_TblMst_Airline] PRIMARY KEY CLUSTERED ([SNo] ASC),
    CONSTRAINT [FK_TblMst_Airline_AirportCodeSno_TblMst_Airport_SNo] FOREIGN KEY ([AirportCodeSNo]) REFERENCES [dbo].[TblMst_Airport] ([SNo]),
    CONSTRAINT [FK_TblMst_Airline_CountrySno_TblMst_Country_SNo] FOREIGN KEY ([CountrySNo]) REFERENCES [dbo].[TblMst_Country] ([SNo])
);


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'1- 7 Days, 2- 10 Days,  3- 15 Days, 4- 30 Days', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'TblMst_Airline', @level2type = N'COLUMN', @level2name = N'InvoicingCycle';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'1=None,2=ICH,3=ACH', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'TblMst_Airline', @level2type = N'COLUMN', @level2name = N'AirlineMember';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'1-Domestic, 2-International, 3-Both, 4-None', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'TblMst_Airline', @level2type = N'COLUMN', @level2name = N'DimensionMandatoryOn';

