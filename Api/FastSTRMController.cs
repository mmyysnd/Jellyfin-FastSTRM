using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FastSTRM.Api
{
    [ApiController]
    [Route("[controller]")]
    public class FastSTRMController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<FastSTRMController> _logger;
        private readonly IMediaEncoder _mediaEncoder;

        public FastSTRMController(ILibraryManager libraryManager, ILogger<FastSTRMController> logger, IMediaEncoder mediaEncoder)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _mediaEncoder = mediaEncoder;
        }

        [HttpGet("app.js")]
        public IActionResult GetAppJs()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "FastSTRM.Web.app.js";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return NotFound();
                    }
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string result = reader.ReadToEnd();
                        return Content(result, "application/javascript");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load app.js");
                return StatusCode(500);
            }
        }

        [HttpGet("GetMockedPlaybackInfo")]
        [HttpPost("GetMockedPlaybackInfo")]
        public ActionResult GetMockedPlaybackInfo([FromQuery] string itemId, [FromQuery] string originalUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(itemId))
                    return BadRequest("Missing itemId");

                if (!Guid.TryParse(itemId, out Guid id))
                    return BadRequest("Invalid itemId");

                var item = _libraryManager.GetItemById(id);
                if (item == null || !(item is Video video))
                {
                    return RedirectPreserveMethod(originalUrl);
                }

                if (string.IsNullOrEmpty(video.Path) || !video.Path.EndsWith(".strm", StringComparison.OrdinalIgnoreCase))
                {
                    // Not a strm file, tell frontend to fallback
                    return RedirectPreserveMethod(originalUrl);
                }

                string strmUrl = "";
                try
                {
                    var lines = System.IO.File.ReadAllLines(video.Path);
                    strmUrl = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? "";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading strm file: {Path}", video.Path);
                    return StatusCode(500, "Error reading strm file");
                }

                if (string.IsNullOrEmpty(strmUrl))
                {
                    return NotFound("STRM file is empty");
                }

                // Construct Mocked MediaSourceInfo
                var mediaSource = new MediaSourceInfo
                {
                    Id = video.Id.ToString("N"),
                    Path = strmUrl,
                    Protocol = strmUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? MediaProtocol.Http : MediaProtocol.File,
                    Type = MediaSourceType.Default,
                    Container = "mp4", // Fake container for direct play
                    IsRemote = true,
                    RunTimeTicks = video.RunTimeTicks,
                    SupportsTranscoding = false,
                    SupportsDirectStream = true,
                    SupportsDirectPlay = true,
                    RequiresOpening = false,
                    RequiresClosing = false,
                    SupportsProbing = true,
                    VideoType = VideoType.VideoFile
                };

                var customStreams = new List<MediaStream>();

                // Add fake video and audio streams to ensure player doesn't complain
                customStreams.Add(new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Index = 0,
                    Codec = "h264",
                    IsDefault = true,
                    Width = video.Width,
                    Height = video.Height,
                    IsExternal = false
                });

                customStreams.Add(new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Index = 1,
                    Codec = "aac",
                    IsDefault = true,
                    Channels = 2,
                    IsExternal = false
                });

                // Fetch real subtitles from DB
                var streams = _libraryManager.GetItemById(id)?.GetMediaStreams() ?? new List<MediaStream>();
                int streamIndex = 2; // video is 0, audio is 1

                foreach (var stream in streams)
                {
                    if (stream.Type == MediaStreamType.Subtitle && stream.IsExternal)
                    {
                        var clonedStream = new MediaStream
                        {
                            Type = stream.Type,
                            Index = streamIndex++,
                            Codec = stream.Codec,
                            Language = stream.Language,
                            IsExternal = true,
                            Path = stream.Path,
                            DeliveryMethod = SubtitleDeliveryMethod.External,
                            DeliveryUrl = $"/Videos/{video.Id}/Subtitles/{stream.Index}/0/Stream.{stream.Codec}?ApiKey={Request.Query["ApiKey"]}"
                        };
                        customStreams.Add(clonedStream);
                    }
                }

                mediaSource.MediaStreams = customStreams;

                var response = new PlaybackInfoResponse
                {
                    MediaSources = new[] { mediaSource },
                    PlaySessionId = Guid.NewGuid().ToString("N")
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMockedPlaybackInfo");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
