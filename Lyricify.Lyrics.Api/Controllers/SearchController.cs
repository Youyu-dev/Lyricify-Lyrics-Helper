using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers;
using Lyricify.Lyrics.Searchers.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Lyricify.Lyrics.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        /// <summary>
        /// 搜索结果数据传输对象
        /// </summary>
        public class SearchResultDto
        {
            public string Title { get; set; } = string.Empty;
            public string[] Artists { get; set; } = Array.Empty<string>();
            public string Album { get; set; } = string.Empty;
            public string[]? AlbumArtists { get; set; }
            public int? DurationMs { get; set; }
            public string? Id { get; set; }
            public string? Mid { get; set; }
            public CompareHelper.MatchType? MatchType { get; set; }
            public string SearcherName { get; set; } = string.Empty;

            public SearchResultDto(ISearchResult result)
            {
                Title = result.Title;
                Artists = result.Artists;
                Album = result.Album;
                AlbumArtists = result.AlbumArtists;
                DurationMs = result.DurationMs;
                MatchType = result.MatchType;
                SearcherName = result.Searcher?.Name ?? string.Empty;

                // 尝试提取 ID
                if (result is QQMusicSearchResult qqResult)
                {
                    Id = qqResult.Id;
                    Mid = qqResult.Mid;
                }
                else if (result is NeteaseSearchResult neteaseResult)
                {
                    Id = neteaseResult.Id;
                }
                else if (result is KugouSearchResult kugouResult)
                {
                    Id = kugouResult.Hash;
                }
                else if (result is MusixmatchSearchResult mxResult)
                {
                    Id = mxResult.Id.ToString();
                }
                else if (result is SodaMusicSearchResult sodaResult)
                {
                    Id = sodaResult.Id;
                }
            }
        }

        [HttpGet("search/{provider}")]
        [ProducesResponseType(typeof(List<SearchResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<SearchResultDto>>> Search(string provider, string title,
            string? artist = null, string? album = null)
        {
            Searchers.Searchers searcherType;
            try
            {
                searcherType = provider.ToLower() switch
                {
                    "netease" => Searchers.Searchers.Netease,
                    "qq" => Searchers.Searchers.QQMusic,
                    "kugou" => Searchers.Searchers.Kugou,
                    "musixmatch" => Searchers.Searchers.Musixmatch,
                    "sodamusic" => Searchers.Searchers.SodaMusic,
                    _ => throw new ArgumentException("Invalid provider")
                };
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid provider. Supported providers: netease, qq, kugou, musixmatch, sodamusic");
            }

            var track = new TrackMultiArtistMetadata
            {
                Title = title,
                Artists = artist != null ? new List<string> { artist } : new List<string>(),
                Album = album,
            };

            var searcher = searcherType.GetSearcher();
            var results = await searcher.SearchForResults(track);

            var dtos = results.Select(r => new SearchResultDto(r)).ToList();

            return Ok(dtos);
        }

        [HttpGet("lyrics/{provider}/{id}")]
        [ProducesResponseType(typeof(LyricsData), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLyrics(string provider, string id, [FromQuery] bool parse = false)
        {
            try
            {
                object? lyricsResult = null;
                string? rawLyricsToParse = null;
                LyricsRawTypes rawType = LyricsRawTypes.Unknown;

                switch (provider.ToLower())
                {
                    case "qq":
                        var qqApi = Lyricify.Lyrics.Providers.Web.Providers.QQMusicApi;
                        var qqLyrics = await qqApi.GetLyricsAsync(id);
                        if (qqLyrics != null)
                        {
                            lyricsResult = qqLyrics;
                            rawLyricsToParse = qqLyrics.Lyrics;
                            rawType = LyricsRawTypes.Qrc;
                        }

                        break;
                    case "netease":
                        var neteaseApi = Lyricify.Lyrics.Providers.Web.Providers.NeteaseApi;
                        // 尝试获取逐字歌词
                        var neteaseLyrics = await neteaseApi.GetLyricNew(id);
                        if (neteaseLyrics != null)
                        {
                            lyricsResult = neteaseLyrics;
                            if (neteaseLyrics.Yrc?.Lyric != null)
                            {
                                rawLyricsToParse = neteaseLyrics.Yrc.Lyric;
                                rawType = LyricsRawTypes.Yrc;
                            }
                            else if (neteaseLyrics.Qfy &&
                                     neteaseLyrics.Yrc == null) // 如果没有 YRC 但标记有逐字，可能需要其他方式？暂时回退检查其他字段
                            {
                                // 也许在 Klyric 中？
                                if (neteaseLyrics.Klyric?.Lyric != null)
                                {
                                    rawLyricsToParse = neteaseLyrics.Klyric.Lyric;
                                    rawType = LyricsRawTypes.Krc;
                                }
                                // 实在没有，回退到 LRC
                                else if (neteaseLyrics.Lrc?.Lyric != null)
                                {
                                    rawLyricsToParse = neteaseLyrics.Lrc.Lyric;
                                    rawType = LyricsRawTypes.Lrc;
                                }
                            }
                            else if (neteaseLyrics.Lrc?.Lyric != null)
                            {
                                // 没有逐字，回退 LRC
                                rawLyricsToParse = neteaseLyrics.Lrc.Lyric;
                                rawType = LyricsRawTypes.Lrc;
                            }
                        }
                        break;
                    case "kugou":
                        var kugouApi = Lyricify.Lyrics.Providers.Web.Providers.KugouApi;
                        var kugouResp = await kugouApi.GetSearchLyrics(hash: id);
                        if (kugouResp != null)
                        {
                            lyricsResult = kugouResp;
                        }

                        break;
                    case "musixmatch":
                        var mxApi = Lyricify.Lyrics.Providers.Web.Providers.MusixmatchApi;
                        var mxLyrics = await mxApi.GetFullLyricsRaw(id);
                        if (mxLyrics != null)
                        {
                            lyricsResult = mxLyrics;
                            rawLyricsToParse = mxLyrics;
                            rawType = LyricsRawTypes.Musixmatch;
                        }

                        break;
                    case "sodamusic":
                        var sodaApi = Lyricify.Lyrics.Providers.Web.Providers.SodaMusicApi;
                        var sodaDetail = await sodaApi.GetDetail(id);
                        if (sodaDetail != null)
                        {
                            lyricsResult = sodaDetail;
                            if (sodaDetail.Lyric != null)
                            {
                                rawLyricsToParse = sodaDetail.Lyric.Content;
                                Enum.TryParse(sodaDetail.Lyric.Type, true, out rawType);
                            }
                        }

                        break;
                    default:
                        return BadRequest(
                            "Invalid provider. Supported providers: netease, qq, kugou, musixmatch, sodamusic");
                }

                if (lyricsResult == null) return NotFound();

                if (parse && rawLyricsToParse != null)
                {
                    var lyricsData = ParseHelper.ParseLyrics(rawLyricsToParse, rawType);
                    if (lyricsData != null) return Ok(lyricsData);
                }

                return Ok(lyricsResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}