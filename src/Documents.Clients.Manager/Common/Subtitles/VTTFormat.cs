namespace Documents.Clients.Manager.Common.Subtitles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class VTTFormat
    {
        public async Task<List<SubtitleSegmentModel>> ParseStreamAsync(Stream stream)
        {
            var segments = new List<SubtitleSegmentModel>();

            string fullText = null;
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                fullText = await reader.ReadToEndAsync();

            var lines = fullText.Split('\n');

            if (lines.Length > 1)
            {
                if (lines[0] == "WEBVTT")
                {
                    int position = 2;

                    while (position < lines.Length)
                    {
                        var indexLine = lines[position++];

                        if (string.IsNullOrEmpty(indexLine))
                            break;

                        if (int.TryParse(indexLine, out int result))
                        {
                            var segment = new SubtitleSegmentModel
                            {
                                SegmentIndex = result,
                                Lines = new List<string>()
                            };
                            segments.Add(segment);

                            var timeline = lines[position++];
                            if (timeline.Length == 29)
                            {
                                segment.StartTime = TimeSpan.Parse(timeline.Substring(0, 12));
                                segment.EndTime = TimeSpan.Parse(timeline.Substring(17, 12));
                            }
                            else
                                throw new Exception("Found non-timeline on line: " + (position));

                            while (position < lines.Length)
                            {
                                var sample = lines[position++];
                                if (string.IsNullOrEmpty(sample))
                                    break;

                                segment.Lines.Add(sample);
                            }
                        }
                        else
                            throw new Exception("Found non-segment index on line: " + (position));
                    }
                }
                else
                    throw new Exception("Did not find expected WEBVTT header");
            }

            return segments;
        }

        public string CreateVTT(List<SubtitleSegmentModel> segments)
        {
            var sb = new StringBuilder();
            sb.Append("WEBVTT");
            sb.Append("\n\n");

            foreach (var segment in segments)
            {
                sb.Append($"{segment.SegmentIndex}\n");
                sb.Append($"{segment.StartTime:hh\\:mm\\:ss\\.fff} --> {segment.EndTime:hh\\:mm\\:ss\\.fff}\n");
                foreach (var line in segment.Lines)
                {
                    sb.Append(line);
                    sb.Append('\n');
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}