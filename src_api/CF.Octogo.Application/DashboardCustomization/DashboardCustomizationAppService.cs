﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.MultiTenancy;
using Abp.Runtime.Session;
using Abp.UI;
using CF.Octogo.Authorization.Users;
using CF.Octogo.Configuration;
using CF.Octogo.DashboardCustomization.Definitions;
using CF.Octogo.DashboardCustomization.Dto;
using CF.Octogo.Data;
using CF.Octogo.Editions.Dto;
using CF.Octogo.MultiTenancy.HostDashboard.Dto;
using CF.Octogo.Tenants.Dto;
using Newtonsoft.Json;

namespace CF.Octogo.DashboardCustomization
{
    [AbpAuthorize]
    public class DashboardCustomizationAppService : OctogoAppServiceBase, IDashboardCustomizationAppService
    {
        private readonly DashboardConfiguration _dashboardConfiguration;
        private readonly IRepository<User, long> _userRepository;
        public DashboardCustomizationAppService(DashboardConfiguration dashboardConfiguration, IRepository<User, long> userRepository)
        {
            _dashboardConfiguration = dashboardConfiguration;
            _userRepository = userRepository;
        }

        public async Task<Dashboard> GetUserDashboard(GetDashboardInput input)
        {
            return GetDashboard(await GetDashboardFromSettings(input.Application), input.DashboardName);
        }

        public async Task SavePage(SavePageInput input)
        {
            var dashboards = await GetDashboardFromSettings(input.Application);
            var dashboard = GetDashboard(dashboards, input.DashboardName);

            foreach (var inputPage in input.Pages)
            {
                var page = dashboard.Pages.FirstOrDefault(p => p.Id == inputPage.Id);
                var pageIndex = dashboard.Pages.IndexOf(page);

                dashboard.Pages.RemoveAt(pageIndex);

                if (page != null)
                {
                    inputPage.Name = page.Name;
                    dashboard.Pages.Insert(pageIndex, inputPage);
                }
            }

            await SaveSetting(input.Application, dashboards);
        }

        public async Task RenamePage(RenamePageInput input)
        {
            var dashboards = await GetDashboardFromSettings(input.Application);
            var dashboard = GetDashboard(dashboards, input.DashboardName);

            var page = dashboard.Pages.FirstOrDefault(p => p.Id == input.Id);
            if (page == null)
            {
                return;
            }

            page.Name = input.Name;

            await SaveSetting(input.Application, dashboards);
        }

        public async Task<AddNewPageOutput> AddNewPage(AddNewPageInput input)
        {
            var dashboards = await GetDashboardFromSettings(input.Application);
            var dashboard = GetDashboard(dashboards, input.DashboardName);

            var page = new Page
            {
                Name = input.Name,
                Widgets = new List<Widget>(),
            };

            dashboard.Pages.Add(page);
            await SaveSetting(input.Application, dashboards);

            return new AddNewPageOutput { PageId = page.Id };
        }

        public async Task DeletePage(DeletePageInput input)
        {
            var dashboards = await GetDashboardFromSettings(input.Application);
            var dashboard = GetDashboard(dashboards, input.DashboardName);

            dashboard.Pages.RemoveAll(p => p.Id == input.Id);

            if (dashboard.Pages.Count == 0) // return to default
            {
                var defaultDashboard = (await GetDefaultDashboardValue(input.Application)).FirstOrDefault(d => d.DashboardName == input.DashboardName);

                dashboards.Remove(dashboard);
                dashboards.Add(defaultDashboard);
            }

            await SaveSetting(input.Application, dashboards);
        }

        public async Task<Widget> AddWidget(AddWidgetInput input)
        {
            var dashboards = await GetDashboardFromSettings(input.Application);
            var dashboard = GetDashboard(dashboards, input.DashboardName);

            var page = dashboard.Pages.Single(p => p.Id == input.PageId);

            var widget = new Widget
            {
                WidgetId = input.WidgetId,
                Height = input.Height,
                Width = input.Width,
                PositionX = 0,
                PositionY = CalculatePositionY(page.Widgets)
            };

            page.Widgets.Add(widget);

            await SaveSetting(input.Application, dashboards);
            return widget;
        }

        public DashboardOutput GetDashboardDefinition(GetDashboardInput input)
        {
            var dashboardDefinition = _dashboardConfiguration.DashboardDefinitions.FirstOrDefault(d => d.Name == input.DashboardName);
            if (dashboardDefinition == null)
            {
                throw new UserFriendlyException(L("UnknownDashboard", input.DashboardName));
            }

            //widgets which used in that dashboard
            var usedWidgetDefinitions = GetFilteredWidgets(dashboardDefinition);

            return new DashboardOutput(
                dashboardDefinition.Name,
                usedWidgetDefinitions
                    .Select(widget => new WidgetOutput(
                    widget.Id,
                    widget.Name,
                    widget.Description,
                    filters: GetNeededWidgetFiltersOutput(widget))
                    ).ToList()
            );
        }
        
        public List<WidgetOutput> GetAllWidgetDefinitions(GetDashboardInput input)
        {
            var dashboardDefinition = _dashboardConfiguration.DashboardDefinitions.FirstOrDefault(d => d.Name == input.DashboardName);
            if (dashboardDefinition == null)
            {
                throw new UserFriendlyException(L("UnknownDashboard", input.DashboardName));
            }

            return GetFilteredWidgets(dashboardDefinition)
                .Select(widget => new WidgetOutput(widget.Id, widget.Name, widget.Description)).ToList();
        }

        public string GetSettingName(string application)
        {
            return AppSettings.DashboardCustomization.Configuration + "." + application;
        }

        private Dashboard GetDashboard(List<Dashboard> dashboards, string dashboardName)
        {
            var dashboard = dashboards.FirstOrDefault(d => d.DashboardName == dashboardName);
            if (dashboard == null)
            {
                throw new UserFriendlyException(L("UnknownDashboard", dashboardName));
            }

            return dashboard;
        }

        private async Task<List<Dashboard>> GetDashboardFromSettings(string application)
        {
            var dashboardConfigAsJsonString = await SettingManager.GetSettingValueAsync(GetSettingName(application));

            if (string.IsNullOrWhiteSpace(dashboardConfigAsJsonString))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<List<Dashboard>>(dashboardConfigAsJsonString);
        }

        private async Task SaveSetting(string application, List<Dashboard> dashboards)
        {
            var value = JsonConvert.SerializeObject(dashboards);

            var currentUser = await GetCurrentUserAsync();
            await SettingManager.ChangeSettingForUserAsync(currentUser.ToUserIdentifier(), GetSettingName(application), value);
        }

        private byte CalculatePositionY(List<Widget> widgets)
        {
            if (widgets == null || !widgets.Any())
            {
                return 0;
            }

            return (byte)widgets.Max(w => w.PositionY + w.Height);
        }

        private async Task<List<Dashboard>> GetDefaultDashboardValue(string application)
        {
            string dashboardConfigAsJsonString;

            if (AbpSession.MultiTenancySide == MultiTenancySides.Host)
            {
                dashboardConfigAsJsonString = await SettingManager.GetSettingValueForApplicationAsync(GetSettingName(application));
            }
            else
            {
                dashboardConfigAsJsonString = await SettingManager.GetSettingValueForTenantAsync(GetSettingName(application), AbpSession.GetTenantId());
            }

            return string.IsNullOrWhiteSpace(dashboardConfigAsJsonString)
                ? null
                : JsonConvert.DeserializeObject<List<Dashboard>>(dashboardConfigAsJsonString);
        }

        private List<WidgetDefinition> GetFilteredWidgets(DashboardDefinition dashboardDefinition)
        {
            var dashboardWidgets = dashboardDefinition.AvailableWidgets ?? new List<string>();

            var widgetDefinitions = _dashboardConfiguration.WidgetDefinitions
                .Where(wd => dashboardWidgets.Contains(wd.Id)).ToList();

            return FilterWidgetsByPermissionAndMultiTenancySide(FilterWidgetsByMultiTenancySide(widgetDefinitions));
        }

        private List<WidgetDefinition> FilterWidgetsByPermissionAndMultiTenancySide(List<WidgetDefinition> widgets)
        {
            return FilterWidgetsByPermission(FilterWidgetsByMultiTenancySide(widgets));
        }

        private List<WidgetDefinition> FilterWidgetsByPermission(List<WidgetDefinition> widgets)
        {
            var filteredWidgets = new List<WidgetDefinition>();

            foreach (var widget in widgets)
            {
                if (widget.Permissions.All(p => PermissionChecker.IsGranted(p)))
                {
                    filteredWidgets.Add(widget);
                }
            }

            return filteredWidgets;
        }

        private List<WidgetDefinition> FilterWidgetsByMultiTenancySide(List<WidgetDefinition> widgets)
        {
            return widgets.Where(w => w.Side.HasFlag(AbpSession.MultiTenancySide)).ToList();
        }

        private List<WidgetFilterOutput> GetNeededWidgetFiltersOutput(WidgetDefinition widget)
        {
            if (widget.UsedWidgetFilters == null || !widget.UsedWidgetFilters.Any())
            {
                return new List<WidgetFilterOutput>();
            }

            var allNeededFilters = widget.UsedWidgetFilters.Distinct().ToList();

            return _dashboardConfiguration.WidgetFilterDefinitions
                .Where(definition => allNeededFilters.Contains(definition.Id))
                .Select(x => new WidgetFilterOutput(x.Id, x.Name))
                .ToList();
        }
        /// <summary>
        /// Description:get produclist and editionlist by userId
        /// Created by:Merajuddin khan
        /// Created on:20-12-2021
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>

        public async Task<ListResultDto<EditionAndProductListDto>> GetProductAndEditionDetailByUserId(int userId)
        {
            var user = _userRepository.Get(userId);
            SqlParameter[] parameters = new SqlParameter[1];
            parameters[0] = new SqlParameter("UserId", userId);
            var ds = await SqlHelper.ExecuteDatasetAsync(
            Connection.GetSqlConnection("DefaultOctoGo"),
            System.Data.CommandType.StoredProcedure,
            "USP_EditionNameAndProductByUserId", parameters
               );
            var EditionProductData = new List<EditionAndProductListDto>();
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                EditionProductData = SqlHelper.ConvertDataTable<EditionAndProductListDto>(ds.Tables[0]);
                if(EditionProductData != null && EditionProductData.Count > 0)
                {
                    for(int i = 0; i< EditionProductData.Count; i++)
                    {
                        EditionProductData[i].ProductUrl = PrepareProductUrl(EditionProductData[i].ProductUrl, user);
                    }
                }
                return new ListResultDto<EditionAndProductListDto>(EditionProductData);

            }
            else
            {
                //if user does not have any active product then throw an userfriendly exception
                // throw new UserFriendlyException(L("NotActiveProduct"));
                return null;
            }
        }
        private string PrepareProductUrl(string productUrl, User user)
        {
            if (productUrl != null)
            {
                if (productUrl.IndexOf("?") == -1)
                {
                    productUrl = productUrl + "?t=";
                }
                if (productUrl.IndexOf("http") == -1)
                {
                    productUrl = "https://" + productUrl;
                }
                if (user != null)
                {
                    string url = productUrl + Convert.ToBase64String(Encoding.UTF8.GetBytes(user.UserName.ToString() + ':' + user.Password.ToString()));
                    productUrl = url;
                }
            }
            return productUrl;
        }
        /// <summary>
        /// Get Subscribed Product details
        /// </summary>
        /// <param name="TenantId"></param>
        /// <returns></returns>
        public async Task<TenantSubscriotionsDto> GetTenantEditionAddonDetailsByTenantId(int tenantId)
        {
            long? userId = AbpSession.UserId;
            var user = userId != null ? _userRepository.Get((long)userId) : null;
            SqlParameter[] parameters = new SqlParameter[1];
            parameters[0] = new SqlParameter("TenantId", tenantId);
            var ds = await SqlHelper.ExecuteDatasetAsync(
            Connection.GetSqlConnection("DefaultOctoGo"),
            System.Data.CommandType.StoredProcedure,
            "USP_GetTenantEditionAddonDetails", parameters);

            if (ds.Tables.Count > 0)
            {
                var res = SqlHelper.ConvertDataTable<TenantEditionAddonRet>(ds.Tables[0]);
                var addonRes = SqlHelper.ConvertDataTable<SubscribedStandAloneAddonRet>(ds.Tables[1]);  // StandAlone Addons
                var result = res.Select(rw => new TenantEditionAddonDto
                {
                    ProductName = rw.ProductName,
                    ProductId = rw.ProductId,
                    EditionId = rw.EditionId,
                    EditionName = rw.EditionName,
                    Price = rw.Price,
                    AppURL = PrepareProductUrl(rw.AppURL, user),
                    StartDate = rw.StartDate,
                    EndDate = rw.EndDate,
                    RemainingDays = rw.RemainingDays,
                    IsSetupProcessComplete = rw.IsSetupProcessComplete,
                    ExpiryNotificationDays = rw.ExpiryNotificationDays,// added by: merajuddin 
                    Addon = rw.Addon != null ? JsonConvert.DeserializeObject<List<SubscribedAddonDto>>(rw.Addon.ToString()) : null

                }).ToList();
                var addonResult = addonRes.Select(rw => new SubscribedStandAloneAddonDto
                {
                    AddonName = rw.AddonName,
                    StartDate = rw.StartDate,
                    EndDate = rw.EndDate,
                    AddonPrice = rw.AddonPrice,
                    RemainingDays = rw.RemainingDays,// Added by:merajudin
                    ModuleList = rw.ModuleList != null ? PrepareFeaturesList(JsonConvert.DeserializeObject<List<StandAloneAddonModulesDto>>(rw.ModuleList.ToString())) : null
                }).ToList();
                return new TenantSubscriotionsDto
                {
                    TenantEditionAddon = result,
                    StandAloneAddon = addonResult
                };
            }
            else
            {
                return null;
            }
        }
        private List<StandAloneAddonModulesDto> PrepareFeaturesList(List<StandAloneAddonModulesDto> modules)
        {
            List<StandAloneAddonModulesDto> result = new List<StandAloneAddonModulesDto>();
            var allFeatures = FeatureManager.GetAll().Where(f => f.Scope.HasFlag(FeatureScopes.All));
            var featureDtos = ObjectMapper.Map<List<FlatFeatureDto>>(allFeatures).OrderBy(f => f.ParentName).ToList();
            featureDtos.ForEach(feature =>
            {
                var featureIndex = modules.FindIndex(x => x.ModuleName == feature.Name);
                if(featureIndex != -1)
                {
                    if(feature.ParentName == null)
                    {
                        result.Add(new StandAloneAddonModulesDto { ModuleName = feature.DisplayName, SubModule = GetSubFeatures(modules, featureDtos, feature.Name) });
                    }
                }
            });
            return result;
        }
        private List<StandAloneAddonModulesDto> GetSubFeatures(List<StandAloneAddonModulesDto> modules, List<FlatFeatureDto> features, string parentName)
        {
            List<StandAloneAddonModulesDto> result = new List<StandAloneAddonModulesDto>();
            var childFeatures = features.Where(f => f.ParentName == parentName).ToList();
            if(childFeatures != null && childFeatures.Count > 0)
            {
                childFeatures.ForEach(feature =>
                {
                    var featureIndex = modules.FindIndex(x => x.ModuleName == feature.Name);
                    if (featureIndex != -1)
                    {
                        result.Add(new StandAloneAddonModulesDto { ModuleName = feature.DisplayName });
                    }
                });
            }
            return result;
        }
        public async Task<List<TenantEditionAddonModulesDto>> GetTenantEditionAddonModuleDetails(int EditionId)
        {
                SqlParameter[] parameters = new SqlParameter[2];
                parameters[0] = new SqlParameter("TenantId", AbpSession.TenantId);
                parameters[1] = new SqlParameter("EditionId", EditionId);
                var ds = await SqlHelper.ExecuteDatasetAsync(
                    Connection.GetSqlConnection("DefaultOctoGo"),
                    System.Data.CommandType.StoredProcedure,
                    "USP_GetTenantEditionAddonModulesDetails", parameters);
                if (ds.Tables.Count > 0)
                {
                    var res = SqlHelper.ConvertDataTable<TenantEditionAddonModulesRet>(ds.Tables[0]);
                    var result = res.Select(rw => new TenantEditionAddonModulesDto
                    {
                        ProductName = rw.ProductName,
                        EditionId = rw.EditionId,
                        EditionName = rw.EditionName,
                        Price = rw.Price,
                        AppURL = rw.AppURL,
                        StartDate = rw.StartDate,
                        EndDate = rw.EndDate,
                        RemainingDays = rw.RemainingDays,
                        IsSetupProcessComplete = rw.IsSetupProcessComplete,
                        ExpiryNotificationDays = rw.ExpiryNotificationDays, // added by: merajuddin
                        Addon = rw.Addon != null ? JsonConvert.DeserializeObject<List<TenantAddonModulesDto>>(rw.Addon.ToString()) : null,
                        Module = rw.Module != null ? JsonConvert.DeserializeObject<List<EditionAddonModules>>(rw.Module.ToString()) : null,

                    }).ToList();
                    return result;
                }
                else
                {
                    return null;
                }
        }
        /// <summary>
        /// Created by:Deepak
        /// Created On: 06-may-2022
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="tenantId"></param>
        /// <param name="Dateinput"></param>
        /// <returns></returns>
        public async Task<List<TotalOctoCostDto>> GetTotalOctoCostWidget(List<string> filters, int tenantId, DashboardInputBase Dateinput)
        {
                    string filterData = String.Join<string>(",", filters);
                    SqlParameter[] parameters = new SqlParameter[4];
                    parameters[0] = new SqlParameter("TenantId", tenantId);
                    parameters[1] = new SqlParameter("Filter", filterData);
                    parameters[2] = new SqlParameter("StartDate", Dateinput.StartDate);
                    parameters[3] = new SqlParameter("EndDate", Dateinput.EndDate);

                    var ds = await SqlHelper.ExecuteDatasetAsync(
                    Connection.GetSqlConnection("DefaultOctoGo"),
                    System.Data.CommandType.StoredProcedure,
                    "USP_GetTotalOctoCostForWidget", parameters);
                    var i = new TotalOctoCostDto();
                    if (ds.Tables.Count > 0)
                    {
                        var res = SqlHelper.ConvertDataTable<TotalOctoCostRet>(ds.Tables[0]);
                        var result = res.Select(rw => new TotalOctoCostDto
                        {
                            Name = rw.FilterName,
                            Series = rw.Data != null ? JsonConvert.DeserializeObject<List<TotalOctoCostSeries>>(rw.Data.ToString()) : null



                        }).ToList();
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                return null;           
        }
        public async Task<List<AWBCountsResultDto>> GetAWBCountsByTenantId(int tenantId, DashboardInputBase Dateinput, int? productId = null)
        {
            List<AWBCountsDto> AwbCounts = new List<AWBCountsDto>()
            {
                new AWBCountsDto() { AwbTypeId=0, Name="Cargo", Value = 0 },
                new AWBCountsDto() { AwbTypeId=1, Name="Courier", Value = 0 },
                new AWBCountsDto() { AwbTypeId=2, Name="CBV", Value = 0 },      // Baggage Voucher
                new AWBCountsDto() { AwbTypeId=3, Name="PO Mail", Value = 0 }
            };
            SqlParameter[] parameters = new SqlParameter[2];
            parameters[0] = new SqlParameter("TenantId", tenantId);
            parameters[1] = new SqlParameter("AdminCreationCompleted", true);
            var ds = await SqlHelper.ExecuteDatasetAsync(
                    Connection.GetSqlConnection("DefaultOctoGo"),
                    System.Data.CommandType.StoredProcedure,
                    "USP_GetTenantDataBaseDetails", parameters
                    );
            if (ds.Tables.Count > 0)
            {
                    var result = SqlHelper.ConvertDataTable<TenantDBDetailsDto>(ds.Tables[0]);
                    if (result.Count > 0)
                    {
                        foreach (var tenantDetails in result)
                        {
                            if (!(productId > 0) || tenantDetails.ProductId == productId)
                            {
                                var awbRet = await SqlHelper.ExecuteDatasetAsync(tenantDetails.ConnectionString,
                                        System.Data.CommandType.Text,
                                        "SELECT AWBType, COUNT(AWBNO) AS AWBCount FROM AWB WHERE AWBDate >= '"+ Dateinput.StartDate.ToString() + "' AND AWBDate <= '"+ Dateinput.StartDate.ToString() + "' GROUP BY AWBType"
                                        );
                                if (awbRet.Tables.Count > 0)
                                {
                                    if(awbRet.Tables[0].Rows.Count > 0)
                                    {
                                        for(int i = 0; i < awbRet.Tables[0].Rows.Count; i++)
                                        {
                                            var row = awbRet.Tables[0].Rows[i];
                                            var awbTypeId = row["AWBType"];
                                            var awbCounts = row["AWBCount"];
                                            // AwbCounts
                                            var index = AwbCounts.FindIndex(x => x.AwbTypeId == Convert.ToInt32(awbTypeId));
                                            if(index >= 0)
                                            {
                                                AwbCounts[index].Value = AwbCounts[index].Value + Convert.ToInt32(awbCounts);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
            }
            return AwbCounts.Select(f => new AWBCountsResultDto { 
                Name = f.Name,
                Value = f.Value
            }).ToList();
        }
    }
}
