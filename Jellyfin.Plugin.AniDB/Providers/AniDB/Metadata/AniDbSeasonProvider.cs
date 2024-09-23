using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AniDB.Providers.AniDB.Metadata
{
    public class AniDbSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
    {
        private readonly AniDbSeriesProvider _seriesProvider;

        private static readonly Regex AniDbIdRegex = new Regex(@"\[anidb(id)?-(?<anidb_id>[0-9]+)\]", RegexOptions.Compiled);

        public AniDbSeasonProvider(IApplicationPaths appPaths)
        {
            _seriesProvider = new AniDbSeriesProvider(appPaths);
        }

        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>
            {
                HasMetadata = true,
                Item = new Season
                {
                    Name = info.Name,
                    IndexNumber = info.IndexNumber
                }
            };

            var seriesId = info.ProviderIds.GetOrDefault(ProviderNames.AniDb);

            bool seriesIdFound = false;

            if (string.IsNullOrEmpty(seriesId))
            {
                seriesId = GetAniDbIdFromPath(info.Path);
                seriesIdFound = !string.IsNullOrEmpty(seriesId);
            }
            else
            {
                seriesIdFound = true;
            }

            if (string.IsNullOrEmpty(seriesId))
            {
                return result;
            }

            var seriesInfo = new SeriesInfo();
            seriesInfo.ProviderIds.Add(ProviderNames.AniDb, seriesId);

            var seriesResult = await _seriesProvider.GetMetadata(seriesInfo, cancellationToken);
            if (seriesResult.HasMetadata)
            {
                if (!seriesIdFound)
                {
                    result.Item.Name = seriesResult.Item.Name;
                    result.Item.Overview = seriesResult.Item.Overview;
                }
                result.Item.ProductionYear = seriesResult.Item.ProductionYear;
                result.Item.PremiereDate = seriesResult.Item.PremiereDate;
                result.Item.EndDate = seriesResult.Item.EndDate;
                result.Item.CommunityRating = seriesResult.Item.CommunityRating;
                result.Item.Studios = seriesResult.Item.Studios;
                result.Item.Genres = seriesResult.Item.Genres;
            }

            return result;
        }

        private static string GetAniDbIdFromPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var match = AniDbIdRegex.Match(path);
                if (match.Success)
                {
                    return match.Groups["anidb_id"].Value;
                }
            }
            return string.Empty;
        }

        public string Name => "AniDB";

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            var metadata = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

            var list = new List<RemoteSearchResult>();

            if (metadata.HasMetadata)
            {
                var res = new RemoteSearchResult
                {
                    Name = metadata.Item.Name,
                    PremiereDate = metadata.Item.PremiereDate,
                    ProductionYear = metadata.Item.ProductionYear,
                    ProviderIds = metadata.Item.ProviderIds,
                    SearchProviderName = Name
                };

                list.Add(res);
            }

            return list;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
