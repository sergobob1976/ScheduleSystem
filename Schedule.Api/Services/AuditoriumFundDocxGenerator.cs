using System.IO.Compression;
using System.Security;
using System.Text;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;
using Schedule.Core.Models;

namespace Schedule.Api.Services;

public class AuditoriumFundDocxGenerator
{
    private const int RoomsPerTable = 12;
    private const int TableWidth = 15560;
    private const int PositionColumnWidth = 1200;

    public byte[] Generate(
        DayScheduleReportResponse report,
        IReadOnlyCollection<ClassRoom> classRooms)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml);
            WriteEntry(archive, "_rels/.rels", PackageRelationshipsXml);
            WriteEntry(archive, "word/_rels/document.xml.rels", DocumentRelationshipsXml);
            WriteEntry(archive, "word/styles.xml", StylesXml);
            WriteEntry(archive, "word/document.xml", CreateDocumentXml(report, classRooms));
        }

        return output.ToArray();
    }

    private static string CreateDocumentXml(
        DayScheduleReportResponse report,
        IReadOnlyCollection<ClassRoom> classRooms)
    {
        var rooms = classRooms
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var roomTables = rooms.Count == 0
            ? new List<List<ClassRoom>> { new() }
            : rooms.Chunk(RoomsPerTable).Select(chunk => chunk.ToList()).ToList();
        var lastLessonPosition = GetLastLessonPosition(report);
        var tablesPerPage = lastLessonPosition <= 6 ? 2 : 1;
        var pages = roomTables
            .Chunk(tablesPerPage)
            .Select(chunk => chunk.ToList())
            .ToList();

        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xml.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");

        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            if (pageIndex > 0)
            {
                xml.Append("<w:p><w:r><w:br w:type=\"page\"/></w:r></w:p>");
            }

            xml.Append(CreateParagraph(
                pageIndex == 0 ? "АУДИТОРНИЙ ФОНД" : "АУДИТОРНИЙ ФОНД — ПРОДОВЖЕННЯ",
                true,
                32,
                "1F4D78",
                40));
            xml.Append(CreateParagraph(
                $"{report.DayName}, {report.ScheduleDate:dd.MM.yyyy} · {report.SemesterName}",
                false,
                22,
                "44546A",
                80));

            var pageTables = pages[pageIndex];
            if (pageTables.Count == 1 && pageTables[0].Count == 0)
            {
                xml.Append(CreateParagraph("У довіднику немає аудиторій.", false, 22, "7F6000", 0));
                continue;
            }

            for (var tableIndex = 0; tableIndex < pageTables.Count; tableIndex++)
            {
                if (tableIndex > 0)
                {
                    xml.Append(CreateParagraph("Продовження", true, 18, "1F4D78", 20));
                }

                xml.Append(CreateTable(report, pageTables[tableIndex], lastLessonPosition));
            }

            xml.Append(CreateParagraph(
                "Скасовані заняття не враховуються.",
                false,
                18,
                "666666",
                0,
                true));
        }

        xml.Append("<w:sectPr>");
        xml.Append("<w:pgSz w:w=\"16838\" w:h=\"11906\" w:orient=\"landscape\"/>");
        xml.Append("<w:pgMar w:top=\"567\" w:right=\"567\" w:bottom=\"567\" w:left=\"567\" w:header=\"400\" w:footer=\"400\" w:gutter=\"0\"/>");
        xml.Append("</w:sectPr></w:body></w:document>");
        return xml.ToString();
    }

    private static string CreateTable(
        DayScheduleReportResponse report,
        IReadOnlyList<ClassRoom> rooms,
        int lastLessonPosition)
    {
        var roomColumnWidth = (TableWidth - PositionColumnWidth) / rooms.Count;
        var actualTableWidth = PositionColumnWidth + roomColumnWidth * rooms.Count;
        var xml = new StringBuilder();

        xml.Append($"<w:tbl><w:tblPr><w:tblW w:w=\"{actualTableWidth}\" w:type=\"dxa\"/><w:tblInd w:w=\"120\" w:type=\"dxa\"/><w:tblLayout w:type=\"fixed\"/><w:tblBorders>");
        xml.Append("<w:top w:val=\"single\" w:sz=\"6\" w:color=\"7F8C9A\"/><w:left w:val=\"single\" w:sz=\"6\" w:color=\"7F8C9A\"/><w:bottom w:val=\"single\" w:sz=\"6\" w:color=\"7F8C9A\"/><w:right w:val=\"single\" w:sz=\"6\" w:color=\"7F8C9A\"/><w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"AAB3BC\"/><w:insideV w:val=\"single\" w:sz=\"4\" w:color=\"AAB3BC\"/>");
        xml.Append("</w:tblBorders><w:tblCellMar><w:top w:w=\"80\" w:type=\"dxa\"/><w:left w:w=\"120\" w:type=\"dxa\"/><w:bottom w:w=\"80\" w:type=\"dxa\"/><w:right w:w=\"120\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr><w:tblGrid>");
        xml.Append($"<w:gridCol w:w=\"{PositionColumnWidth}\"/>");
        foreach (var _ in rooms)
        {
            xml.Append($"<w:gridCol w:w=\"{roomColumnWidth}\"/>");
        }
        xml.Append("</w:tblGrid>");

        xml.Append("<w:tr><w:trPr><w:tblHeader/><w:cantSplit/></w:trPr>");
        xml.Append(CreateCell("Пара", PositionColumnWidth, true, "E8EEF5", 18));
        foreach (var room in rooms)
        {
            xml.Append(CreateCell(room.Name, roomColumnWidth, true, "E8EEF5", 18));
        }
        xml.Append("</w:tr>");

        for (var position = 1; position <= lastLessonPosition; position++)
        {
            xml.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
            xml.Append(CreateCell($"{position}\n{GetPositionName(position)}", PositionColumnWidth, true, "F2F4F7", 18));
            foreach (var room in rooms)
            {
                var groups = GetGroups(report, room.Name, position);
                xml.Append(CreateCell(
                    groups.Count == 0 ? "—" : string.Join("\n", groups),
                    roomColumnWidth,
                    groups.Count > 0,
                    groups.Count > 0 ? "E2F0D9" : null,
                    18));
            }
            xml.Append("</w:tr>");
        }

        xml.Append("</w:tbl>");
        return xml.ToString();
    }

    private static int GetLastLessonPosition(DayScheduleReportResponse report)
    {
        var lastPosition = report.Groups
            .SelectMany(group => group.Lessons)
            .Where(lesson =>
                lesson.Status != RealLessonStatus.Cancelled &&
                !string.IsNullOrWhiteSpace(lesson.ClassRoomName))
            .Select(lesson => lesson.LessonPosition)
            .DefaultIfEmpty(1)
            .Max();

        return Math.Clamp(lastPosition, 1, 8);
    }

    private static List<string> GetGroups(
        DayScheduleReportResponse report,
        string classRoomName,
        int lessonPosition) =>
        report.Groups
            .Where(group => group.Lessons.Any(lesson =>
                lesson.LessonPosition == lessonPosition &&
                lesson.Status != RealLessonStatus.Cancelled &&
                string.Equals(lesson.ClassRoomName, classRoomName, StringComparison.CurrentCultureIgnoreCase)))
            .Select(group => group.GroupName)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(groupName => groupName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private static string CreateParagraph(
        string text,
        bool bold,
        int fontSize,
        string color,
        int spaceAfter,
        bool italic = false)
    {
        var paragraphProperties = $"<w:pPr><w:jc w:val=\"center\"/><w:spacing w:before=\"0\" w:after=\"{spaceAfter}\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr>";
        return $"<w:p>{paragraphProperties}{CreateRun(text, bold, fontSize, color, italic)}</w:p>";
    }

    private static string CreateCell(
        string text,
        int width,
        bool bold,
        string? fill,
        int fontSize)
    {
        var properties = new StringBuilder($"<w:tcPr><w:tcW w:w=\"{width}\" w:type=\"dxa\"/>");
        if (fill is not null)
        {
            properties.Append($"<w:shd w:val=\"clear\" w:fill=\"{fill}\"/>");
        }
        properties.Append("<w:vAlign w:val=\"center\"/><w:tcMar><w:top w:w=\"80\" w:type=\"dxa\"/><w:left w:w=\"120\" w:type=\"dxa\"/><w:bottom w:w=\"80\" w:type=\"dxa\"/><w:right w:w=\"120\" w:type=\"dxa\"/></w:tcMar></w:tcPr>");
        var paragraphProperties = "<w:pPr><w:jc w:val=\"center\"/><w:spacing w:before=\"0\" w:after=\"0\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr>";
        var run = CreateRun(text, bold, fontSize, "1F2937");
        return $"<w:tc>{properties}<w:p>{paragraphProperties}{run}</w:p></w:tc>";
    }

    private static string CreateRun(
        string text,
        bool bold,
        int fontSize,
        string color,
        bool italic = false)
    {
        var parts = text.Replace("\r", string.Empty).Split('\n');
        var xml = new StringBuilder();
        for (var index = 0; index < parts.Length; index++)
        {
            if (index > 0)
            {
                xml.Append("<w:r><w:br/></w:r>");
            }

            xml.Append("<w:r><w:rPr><w:rFonts w:ascii=\"Calibri\" w:hAnsi=\"Calibri\" w:cs=\"Calibri\"/>");
            if (bold) xml.Append("<w:b/>");
            if (italic) xml.Append("<w:i/>");
            xml.Append($"<w:color w:val=\"{color}\"/><w:sz w:val=\"{fontSize}\"/><w:szCs w:val=\"{fontSize}\"/></w:rPr><w:t xml:space=\"preserve\">{SecurityElement.Escape(parts[index])}</w:t></w:r>");
        }
        return xml.ToString();
    }

    private static string GetPositionName(int position) => position switch
    {
        1 => "перша",
        2 => "друга",
        3 => "третя",
        4 => "четверта",
        5 => "п’ята",
        6 => "шоста",
        7 => "сьома",
        8 => "восьма",
        _ => string.Empty
    };

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private const string ContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
          <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
        </Types>
        """;

    private const string PackageRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
        </Relationships>
        """;

    private const string DocumentRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string StylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
          <w:docDefaults>
            <w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri" w:cs="Calibri"/><w:sz w:val="22"/><w:szCs w:val="22"/></w:rPr></w:rPrDefault>
            <w:pPrDefault><w:pPr><w:spacing w:before="0" w:after="120" w:line="300" w:lineRule="auto"/></w:pPr></w:pPrDefault>
          </w:docDefaults>
          <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>
        </w:styles>
        """;
}
