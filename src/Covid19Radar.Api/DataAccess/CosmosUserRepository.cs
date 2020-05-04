﻿using Covid19Radar.DataStore;
using Covid19Radar.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Covid19Radar.DataAccess
{
    public class CosmosUserRepository : IUserRepository
    {
        private readonly ICosmos _db;
        private readonly ILogger<CosmosUserRepository> _logger;

        public CosmosUserRepository(ICosmos db, ILogger<CosmosUserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<UserResultModel?> GetById(string id)
        {
            var itemResult = await _db.User.ReadItemAsync<UserResultModel>(id, PartitionKey.None);
            if (itemResult.StatusCode == HttpStatusCode.OK)
            {
                return itemResult.Resource;
            }

            return null;
        }

        public Task Create(UserModel user)
        {
            return _db.User.CreateItemAsync(user);
        }

        public async Task<bool> Exists(string id)
        {
            bool userFound = false;
            try
            {
                var userResult = await _db.User.ReadItemAsync<UserResultModel>(id, PartitionKey.None);
                if (userResult.StatusCode == HttpStatusCode.OK)
                {
                    userFound = true;
                }
            }
            catch (CosmosException cosmosException)
            {
                if (cosmosException.StatusCode == HttpStatusCode.NotFound)
                {
                    userFound = false;
                }
            }

            return userFound;
        }

        public async Task<SequenceDataModel?> NextSequenceNumber()
        {
            var id = SequenceDataModel._id.ToString();
            for (var i = 0; i < 100; i++)
            {
                var result = await _db.Sequence.ReadItemAsync<SequenceDataModel>(id, PartitionKey.None);
                var model = result.Resource;
                model.Increment();
                var option = new ItemRequestOptions();
                option.IfMatchEtag = model._etag;
                try
                {
                    var resultReplace = await _db.Sequence.ReplaceItemAsync(model, id, null, option);
                    return resultReplace.Resource;
                }
                catch (CosmosException ex)
                {
                    _logger.LogInformation(ex, $"GetNumber Retry {i}");
                    continue;
                }
            }
            _logger.LogWarning("GetNumber is over retry count.");
            return null;
        }
    }
}
