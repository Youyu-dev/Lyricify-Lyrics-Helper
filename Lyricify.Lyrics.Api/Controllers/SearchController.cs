using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers;
using Microsoft.AspNetCore.Mvc;

namespace Lyricify.Lyrics.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        [HttpGet("search/{provider}")]
        public async Task<IActionResult> Search(string provider, string title, string? artist = null, string? album = null)
        {
            Searchers.Searchers searcherType;
            try
            {
                searcherType = provider.ToLower() switch
                {
                    "netease" => Searchers.Searchers.Netease,
                    "qq" => Searchers.Searchers.QQMusic,
                    _ => throw new ArgumentException("Invalid provider")
                };
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid provider. Supported providers: netease, qq");
            }

            var track = new TrackMultiArtistMetadata
            {
                Title = title,
                Artists = artist != null ? new List<string> { artist } : new List<string>(),
                Album = album,
            };


            
            var searcher = searcherType.GetSearcher();
            var results = await searcher.SearchForResults(track);

            return Ok(results);
        }

        [HttpGet("lyrics/{provider}/{id}")]
        public async Task<IActionResult> GetLyrics(string provider, string id)
        {
            try
            {
                switch (provider.ToLower())
                {
                    case "qq":
                        var qqApi = Lyricify.Lyrics.Providers.Web.Providers.QQMusicApi;
                        var qqLyrics = await qqApi.GetLyricsAsync(id);
                        if (qqLyrics == null) return NotFound();
                        return Ok(qqLyrics);
                    case "netease":
                        var neteaseApi = Lyricify.Lyrics.Providers.Web.Providers.NeteaseApi;
                        var neteaseLyrics = await neteaseApi.GetLyric(id);
                         if (neteaseLyrics == null) return NotFound();
                        return Ok(neteaseLyrics);
                    default:
                        return BadRequest("Invalid provider. Supported providers: netease, qq");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
