using combit.Reporting;
using combit.Reporting.DataProviders;
using combit.Reporting.Dom;

namespace EventReg.Api;

public record Participant(int No, string Name, string Email, string Company, string RegisteredAt);

public class ParticipantReport(IConfiguration configuration, IWebHostEnvironment environment)
{
    private const string AccentColor = "RGB(47, 93, 80)";

    private string ProjectPath => Path.Combine(environment.ContentRootPath, "Reports", "participants.json");

    public byte[] CreatePdf(Event ev)
    {
        var participants = ev.Registrations
            .OrderBy(r => r.RegisteredAt)
            .Select((r, i) => new Participant(i + 1, r.Name, r.Email, r.Company ?? "-", r.RegisteredAt.ToString("dd.MM.yyyy HH:mm")))
            .ToList();

        using var listLabel = new ListLabel { DataSource = new ObjectDataProvider(participants) };

        var licenseKey = configuration["ListLabel:LicenseKey"];
        if (!string.IsNullOrWhiteSpace(licenseKey))
            listLabel.LicensingInfo = licenseKey;

        listLabel.Variables.Add("EventTitle", ev.Title);
        listLabel.Variables.Add("EventDate", ev.StartsAt.ToString("dd.MM.yyyy, HH:mm"));
        listLabel.Variables.Add("EventLocation", ev.Location);
        listLabel.Variables.Add("Occupancy", $"{participants.Count} / {ev.Capacity}");

        if (!File.Exists(ProjectPath))
            BuildProject(listLabel);

        var pdfPath = Path.Combine(Path.GetTempPath(), $"eventreg-{Guid.NewGuid():N}.pdf");
        try
        {
            listLabel.Export(new ExportConfiguration(LlExportTarget.Pdf, pdfPath, ProjectPath));
            return File.ReadAllBytes(pdfPath);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }

    private void BuildProject(ListLabel listLabel)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ProjectPath)!);

        using var project = (ProjectList)listLabel.OpenProject(ProjectPath, LlDomFileMode.Create, LlDomAccessMode.ReadWrite, LlProject.List)!;

        AddText(project, 15000, 15000, 180000, 12000, "EventTitle", size: 20, bold: true, color: AccentColor);
        AddText(project, 15000, 28000, 180000, 7000, "\"Teilnehmerliste · \" + EventDate + \" · \" + EventLocation", size: 10);
        AddText(project, 15000, 35000, 180000, 7000, "\"Plätze belegt: \" + Occupancy", size: 10);

        var container = project.Objects.AddNewReportContainer();
        container.Position.Left = "15000";
        container.Position.Top = "48000";
        container.Position.Width = "180000";
        container.Position.Height = "225000";

        var table = container.SubItems.AddNewTable();
        table.TableId = nameof(Participant);

        (string Header, string Field, int Width)[] columns =
        [
            ("Nr.", "Participant.No", 10000),
            ("Name", "Participant.Name", 40000),
            ("E-Mail", "Participant.Email", 55000),
            ("Firma", "Participant.Company", 40000),
            ("Angemeldet", "Participant.RegisteredAt", 35000),
        ];

        var header = table.Lines.Header.AddNew();
        var data = new TableLineData(table.Lines.Data);
        foreach (var column in columns)
        {
            var title = header.Fields.AddNewText();
            title.Contents = $"\"{column.Header}\"";
            title.Width = column.Width.ToString();
            title.Font.Bold = "True";
            title.Font.Color = "RGB(255, 255, 255)";
            title.Filling.Style = "1";
            title.Filling.Color = AccentColor;

            var value = data.Fields.AddNewText();
            value.Contents = column.Field == "Participant.No" ? "Str$(Participant.No, 0, 0)" : column.Field;
            value.Width = column.Width.ToString();
        }

        project.Save();
    }

    private static void AddText(ProjectList project, int left, int top, int width, int height, string contents,
        int size, bool bold = false, string? color = null)
    {
        var text = project.Objects.AddNewText();
        text.Position.Left = left.ToString();
        text.Position.Top = top.ToString();
        text.Position.Width = width.ToString();
        text.Position.Height = height.ToString();

        var paragraph = text.Paragraphs.AddNew();
        paragraph.Contents = contents;
        paragraph.Font.Size = size.ToString();
        paragraph.Font.Bold = bold ? "True" : "False";
        if (color != null)
            paragraph.Font.Color = color;
    }
}
