using System.IO.Compression;
using System.Security;
using System.Text;
using Schedule.Core.DTOs;
using Schedule.Core.Enums;

namespace Schedule.Api.Services;

public class DayScheduleDocxGenerator
{
    public byte[] Generate(DayScheduleReportResponse report)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml);
            WriteEntry(archive, "_rels/.rels", PackageRelationshipsXml);
            WriteEntry(archive, "word/_rels/document.xml.rels", DocumentRelationshipsXml);
            WriteEntry(archive, "word/styles.xml", StylesXml);
            WriteEntry(archive, "word/document.xml", CreateDocumentXml(report));
        }
        return output.ToArray();
    }

    private static string CreateDocumentXml(DayScheduleReportResponse report)
    {
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xml.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
        xml.Append(CreateParagraph($"Розклад на {report.ScheduleDate:dd.MM.yyyy}", true, true));
        xml.Append(CreateParagraph(report.DayName.ToUpperInvariant(), true, true));
        xml.Append("<w:tbl><w:tblPr><w:tblW w:w=\"10995\" w:type=\"dxa\"/><w:tblLayout w:type=\"fixed\"/><w:tblBorders>");
        xml.Append("<w:top w:val=\"single\" w:sz=\"8\"/><w:left w:val=\"single\" w:sz=\"8\"/><w:bottom w:val=\"single\" w:sz=\"8\"/><w:right w:val=\"single\" w:sz=\"8\"/><w:insideH w:val=\"single\" w:sz=\"8\"/><w:insideV w:val=\"single\" w:sz=\"8\"/>");
        xml.Append("</w:tblBorders></w:tblPr><w:tblGrid><w:gridCol w:w=\"450\"/><w:gridCol w:w=\"6450\"/><w:gridCol w:w=\"4095\"/></w:tblGrid>");

        foreach (var group in report.Groups)
        {
            xml.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
            xml.Append(CreateCell($"Група {group.GroupName}", 10995, true, true, 3, "D9EAF7"));
            xml.Append("</w:tr>");

            if (group.Lessons.Count == 0)
            {
                xml.Append("<w:tr>");
                xml.Append(CreateCell("", 450, true));
                xml.Append(CreateCell("Занять немає", 6450));
                xml.Append(CreateCell("", 4095));
                xml.Append("</w:tr>");
                continue;
            }

            foreach (var lesson in group.Lessons)
            {
                var discipline = lesson.Status == RealLessonStatus.Cancelled
                    ? $"{lesson.DisciplineName} (скасовано)"
                    : lesson.DisciplineName;
                if (!string.IsNullOrWhiteSpace(lesson.ResourceLink))
                    discipline += $"\nМатеріали: {lesson.ResourceLink}";

                var teacher = lesson.TeacherName;
                if (!string.IsNullOrWhiteSpace(lesson.ClassRoomName))
                    teacher += $"\nАудиторія: {lesson.ClassRoomName}";
                if (!string.IsNullOrWhiteSpace(lesson.ConferenceLink))
                    teacher += $"\nКонференція: {lesson.ConferenceLink}";

                xml.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
                xml.Append(CreateCell(lesson.LessonPosition.ToString(), 450, true, true));
                xml.Append(CreateCell(discipline, 6450));
                xml.Append(CreateCell(teacher, 4095));
                xml.Append("</w:tr>");
            }
        }

        xml.Append("</w:tbl><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"567\" w:right=\"567\" w:bottom=\"567\" w:left=\"567\" w:header=\"0\" w:footer=\"0\" w:gutter=\"0\"/></w:sectPr></w:body></w:document>");
        return xml.ToString();
    }

    private static string CreateParagraph(string text, bool bold = false, bool centered = false)
    {
        var properties = centered ? "<w:pPr><w:jc w:val=\"center\"/></w:pPr>" : "";
        return $"<w:p>{properties}{CreateRuns(text, bold)}</w:p>";
    }

    private static string CreateCell(string text, int width, bool bold = false, bool centered = false, int gridSpan = 1, string? fill = null)
    {
        var cellProperties = new StringBuilder($"<w:tcPr><w:tcW w:w=\"{width}\" w:type=\"dxa\"/>");
        if (gridSpan > 1) cellProperties.Append($"<w:gridSpan w:val=\"{gridSpan}\"/>");
        if (fill is not null) cellProperties.Append($"<w:shd w:val=\"clear\" w:fill=\"{fill}\"/>");
        cellProperties.Append("<w:vAlign w:val=\"center\"/><w:tcMar><w:top w:w=\"70\" w:type=\"dxa\"/><w:left w:w=\"90\" w:type=\"dxa\"/><w:bottom w:w=\"70\" w:type=\"dxa\"/><w:right w:w=\"90\" w:type=\"dxa\"/></w:tcMar></w:tcPr>");
        var paragraphProperties = centered ? "<w:pPr><w:jc w:val=\"center\"/></w:pPr>" : "";
        return $"<w:tc>{cellProperties}<w:p>{paragraphProperties}{CreateRuns(text, bold)}</w:p></w:tc>";
    }

    private static string CreateRuns(string text, bool bold)
    {
        var parts = text.Replace("\r", "").Split('\n');
        var xml = new StringBuilder();
        for (var index = 0; index < parts.Length; index++)
        {
            if (index > 0) xml.Append("<w:r><w:br/></w:r>");
            xml.Append("<w:r><w:rPr>");
            if (bold) xml.Append("<w:b/>");
            xml.Append("<w:sz w:val=\"30\"/><w:szCs w:val=\"30\"/></w:rPr><w:t xml:space=\"preserve\">");
            xml.Append(SecurityElement.Escape(parts[index]));
            xml.Append("</w:t></w:r>");
        }
        return xml.ToString();
    }

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

    private const string StylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
          <w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="30"/><w:szCs w:val="30"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after="0" w:line="240" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults>
          <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>
        </w:styles>
        """;

    private const string DocumentRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;
}
