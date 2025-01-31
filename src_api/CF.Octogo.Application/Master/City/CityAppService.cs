﻿using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Runtime.Caching;
using Abp.UI;
using CF.Octogo.Authorization;
using CF.Octogo.Data;
using CF.Octogo.Dto;
using CF.Octogo.Master.City.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CF.Octogo.Master
{
    public class CityAppService : OctogoAppServiceBase, ICityAppService
    {
        private const string masterCacheKey = OctogoCacheKeyConst.MasterDataCacheKey;
        private readonly ICacheManager _cacheManager;

        public CityAppService(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }
        public async Task<PagedResultDto<CityListDto>> GetCityList(PagedAndSortedInputDto input, string Filter)
        {

            SqlParameter[] parameters = new SqlParameter[4];
            parameters[0] = new SqlParameter("Sorting", input.Sorting);
            parameters[1] = new SqlParameter("PageSize", input.MaxResultCount);
            parameters[2] = new SqlParameter("PageNo", (input.SkipCount / input.MaxResultCount) + 1);
            parameters[3] = new SqlParameter("Filter", Filter);

            var ds = await SqlHelper.ExecuteDatasetAsync(
            Connection.GetSqlConnection("DefaultOctoGo"),
            System.Data.CommandType.StoredProcedure,
            "USP_GetCityList", parameters
            );
            var totalCount = 0;
            var cityList = new List<CityListDto>();
            if (ds.Tables.Count > 0)
            {
                cityList = SqlHelper.ConvertDataTable<CityListDto>(ds.Tables[0]);
                if (cityList != null && cityList.Count > 0)
                {
                    totalCount = Convert.ToInt32(ds.Tables[0].Rows[0]["TotalCount"]);
                }
            }
            return new PagedResultDto<CityListDto>(
                    totalCount,
                    cityList
                );

        }

        [AbpAuthorize(AppPermissions.Pages_Administration_City_Create, AppPermissions.Pages_Administration_City_Edit)]

        public async Task<int> CreateOrUpdateCity(CreateOrUpdateCityInput input)
        {
            SqlParameter[] parameters = new SqlParameter[18];
            parameters[0] = new SqlParameter("SNo", input.SNo);
            parameters[1] = new SqlParameter("CityCode", input.CityCode);
            parameters[2] = new SqlParameter("CityName", input.CityName);
            //parameters[3] = new SqlParameter("ZoneName", input.ZoneName);
            parameters[3] = new SqlParameter("StateSNo", input.StateSNo);
            parameters[4] = new SqlParameter("CountryName", input.CountryName);
            parameters[5] = new SqlParameter("PriorApproval", input.PriorApproval);
            parameters[6] = new SqlParameter("IsDayLightSaving", input.IsDayLightSaving);
            parameters[7] = new SqlParameter("IsActive", input.IsActive);
            parameters[8] = new SqlParameter("TimeZoneSNo", input.TimeZoneSNo);
            parameters[9] = new SqlParameter("ZoneSNo", input.ZoneSNo);
            parameters[10] = new SqlParameter("IataAreaCode", input.IataAreaCode);
            parameters[11] = new SqlParameter("ShcSNo", input.ShcSNo);
            parameters[12] = new SqlParameter("DgClassSNo", input.DgClassSNo);
            parameters[13] = new SqlParameter("UserId", AbpSession.UserId);
            parameters[14] = new SqlParameter("StateName", input.StateName);
            parameters[15] = new SqlParameter("ZoneName", input.ZoneName);
            parameters[16] = new SqlParameter("CountryCode", input.CountryCode);
            parameters[17] = new SqlParameter("CountrySNo", input.CountrySNo);

            var ds = await SqlHelper.ExecuteDatasetAsync(Connection.GetSqlConnection("DefaultOctoGo"),
            System.Data.CommandType.StoredProcedure,
            "USP_CreateOrUpdateOrDeleteCity", parameters);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && (int)ds.Tables[0].Rows[0]["Id"] == 0)
            {
                throw new UserFriendlyException(L((string)ds.Tables[0].Rows[0]["Message"]));
            }
            else if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && (string)ds.Tables[0].Rows[0]["Message"] == "Success" && (int)ds.Tables[0].Rows[0]["Id"] > 0)
            {
                await ClearCache();
                return (int)ds.Tables[0].Rows[0]["Id"];
            }
            else
            {
                return 0;
            }
        }

       
        public async Task<DataSet> GetCityById(GetEditCityInput input)
        {

            SqlParameter[] parameters = new SqlParameter[1];
            parameters[0] = new SqlParameter("SNo", input.SNo);
            var ds = await SqlHelper.ExecuteDatasetAsync(
            Connection.GetSqlConnection("DefaultOctoGo"),
            System.Data.CommandType.StoredProcedure,
            "USP_GetCityList", parameters
            );
            if (ds.Tables.Count > 0)
            {
                return ds;
            }
            else
            {
                return null;
            }


        }
        [AbpAuthorize(AppPermissions.Pages_Administration_City_Delete)]
        public async Task DeleteCity(EntityDto input)
        {
            SqlParameter[] parameters = new SqlParameter[3];
            parameters[0] = new SqlParameter("SNo", input.Id);
            parameters[1] = new SqlParameter("UserId", AbpSession.UserId);
            parameters[2] = new SqlParameter("IsDelete", true);
            await SqlHelper.ExecuteDatasetAsync(Connection.GetSqlConnection("DefaultOctoGo"),
           System.Data.CommandType.StoredProcedure,
           "USP_CreateOrUpdateOrDeleteCity", parameters);
        }
        public async Task ClearCache()
        {
            var allMasterCache = _cacheManager.GetCache(masterCacheKey);
            await allMasterCache.ClearAsync();
        }
    }
}
