using System.IO.Compression;
using System.Security;
using System.Text;
using Schedule.Core.DTOs;

namespace Schedule.Api.Services;

public class TeacherReadingDocxGenerator
{
    public byte[] Generate(TeacherReadingReportResponse report)
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

    private static string CreateDocumentXml(TeacherReadingReportResponse report)
    {
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xml.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
        xml.Append(CreateParagraph("Вичитка викладача", true, true, 30));
        xml.Append(CreateParagraph(report.TeacherName, true, true, 28));
        xml.Append(CreateParagraph($"Період: {report.PeriodStart:dd.MM.yyyy}–{report.PeriodEnd:dd.MM.yyyy}", false, true, 24));
        xml.Append(CreateParagraph($"Проведено пар: {report.TotalLessons}. Академічних годин: {report.TotalAcademicHours}.", false, true, 24));
        xml.Append("<w:tbl><w:tblPr><w:tblW w:w=\"10995\" w:type=\"dxa\"/><w:tblLayout w:type=\"fixed\"/><w:tblBorders>");
        xml.Append("<w:top w:val=\"single\" w:sz=\"8\"/><w:left w:val=\"single\" w:sz=\"8\"/><w:bottom w:val=\"single\" w:sz=\"8\"/><w:right w:val=\"single\" w:sz=\"8\"/><w:insideH w:val=\"single\" w:sz=\"8\"/><w:insideV w:val=\"single\" w:sz=\"8\"/>");
        xml.Append("</w:tblBorders></w:tblPr><w:tblGrid><w:gridCol w:w=\"650\"/><w:gridCol w:w=\"1500\"/><w:gridCol w:w=\"3200\"/><w:gridCol w:w=\"1800\"/><w:gridCol w:w=\"2400\"/><w:gridCol w:w=\"1445\"/></w:tblGrid>");
        xml.Append("<w:tr><w:trPr><w:tblHeader/><w:cantSplit/></w:trPr>");
        xml.Append(CreateCell("Пара", 650, true, true, fill: "D9E2F3"));
        xml.Append(CreateCell("Група", 1500, true, true, fill: "D9E2F3"));
        xml.Append(CreateCell("Дисципліна", 3200, true, true, fill: "D9E2F3"));
        xml.Append(CreateCell("Тип", 1800, true, true, fill: "D9E2F3"));
        xml.Append(CreateCell("Аудиторія", 2400, true, true, fill: "D9E2F3"));
        xml.Append(CreateCell("Акад. годин", 1445, true, true, fill: "D9E2F3"));
        xml.Append("</w:tr>");

        for (var dayIndex = 0; dayIndex < report.Days.Count; dayIndex++)
        {
            var day = report.Days[dayIndex];
            var dayFill = dayIndex % 2 == 0 ? "BDD7EE" : "C6E0B4";
            xml.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
            xml.Append(CreateCell($"{day.LessonDate:dd.MM.yyyy} — {day.DayName}", 10995, true, true, 6, dayFill, 26));
            xml.Append("</w:tr>");

            foreach (var lesson in day.Lessons)
            {
                xml.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
                xml.Append(CreateCell(lesson.LessonPosition.ToString(), 650, true, true));
                xml.Append(CreateCell(lesson.GroupName, 1500));
                xml.Append(CreateCell(lesson.DisciplineName, 3200));
                xml.Append(CreateCell(GetLessonTypeName(lesson.LessonType), 1800));
                xml.Append(CreateCell(lesson.ClassRoomName ?? "—", 2400));
                xml.Append(CreateCell(report.AcademicHoursPerLesson.ToString(), 1445, false, true));
                xml.Append("</w:tr>");
            }
        }

        if (report.Days.Count == 0)
        {
            xml.Append("<w:tr>");
            xml.Append(CreateCell("За вибраний період проведених занять немає.", 10995, false, true, 6, "F2F2F2"));
            xml.Append("</w:tr>");
        }

        xml.Append("</w:tbl><w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"567\" w:right=\"567\" w:bottom=\"567\" w:left=\"567\" w:header=\"0\" w:footer=\"0\" w:gutter=\"0\"/></w:sectPr></w:body></w:document>");
        return xml.ToString();
    }

    private static string CreateParagraph(string text, bool bold, bool centered, int fontSize)
    {
        var properties = centered ? "<w:pPr><w:jc w:val=\"center\"/></w:pPr>" : "";
        return $"<w:p>{properties}{CreateRun(text, bold, fontSize)}</w:p>";
    }

    private static string CreateCell(string text, int width, bool bold = false, bool centered = false, int gridSpan = 1, string? fill = null, int fontSize = 24)
    {
        var properties = new StringBuilder($"<w:tcPr><w:tcW w:w=\"{width}\" w:type=\"dxa\"/>");
        if (gridSpan > 1) properties.Append($"<w:gridSpan w:val=\"{gridSpan}\"/>");
        if (fill is not null) properties.Append($"<w:shd w:val=\"clear\" w:fill=\"{fill}\"/>");
        properties.Append("<w:vAlign w:val=\"center\"/><w:tcMar><w:top w:w=\"70\" w:type=\"dxa\"/><w:left w:w=\"90\" w:type=\"dxa\"/><w:bottom w:w=\"70\" w:type=\"dxa\"/><w:right w:w=\"90\" w:type=\"dxa\"/></w:tcMar></w:tcPr>");
        var paragraphProperties = centered ? "<w:pPr><w:jc w:val=\"center\"/></w:pPr>" : "";
        return $"<w:tc>{properties}<w:p>{paragraphProperties}{CreateRun(text, bold, fontSize)}</w:p></w:tc>";
    }

    private static string CreateRun(string text, bool bold, int fontSize)
    {
        var escaped = SecurityElement.Escape(text) ?? string.Empty;
        return $"<w:r><w:rPr>{(bold ? "<w:b/>" : "")}<w:sz w:val=\"{fontSize}\"/><w:szCs w:val=\"{fontSize}\"/></w:rPr><w:t xml:space=\"preserve\">{escaped}</w:t></w:r>";
    }

    private static string GetLessonTypeName(Schedule.Core.Enums.LessonType value) => value switch
    {
        Schedule.Core.Enums.LessonType.Lecture => "Лекція",
        Schedule.Core.Enums.LessonType.Practical => "Практичне",
        Schedule.Core.Enums.LessonType.Laboratory => "Лабораторне",
        Schedule.Core.Enums.LessonType.Seminar => "Семінар",
        _ => "Інше"
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
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
        </Types>
        """;
    private const string PackageRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>
        """;
    private const string DocumentRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
        """;
    private const string StylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="24"/><w:szCs w:val="24"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after="0" w:line="240" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults><w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style></w:styles>
        """;
}
