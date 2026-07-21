using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Schichtplaner.Models.ViewModels;

namespace Schichtplaner.Services;

public class MonatsplanPdfService : IMonatsplanPdfService
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public byte[] Create(MonatsplanViewModel model, string standortName)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var monthName = new DateTime(model.Jahr, model.Monat, 1).ToString("MMMM yyyy", German);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(8).FontFamily(Fonts.Arial));

                page.Header().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("SCHICHTPLAN").FontSize(9).Bold().FontColor("5B5BD6");
                        column.Item().Text(monthName).FontSize(22).Bold().FontColor("172033");
                        column.Item().Text(standortName).FontSize(10).FontColor("65708A");
                    });
                    row.ConstantItem(180).AlignRight().AlignBottom()
                        .Text($"Erstellt am {DateTime.Now:dd.MM.yyyy HH:mm}")
                        .FontColor("65708A");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(34);
                        for (var i = 0; i < 7; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, "KW");
                        foreach (var weekday in new[] { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" })
                            HeaderCell(header, weekday);
                    });

                    foreach (var week in model.Wochen)
                    {
                        table.Cell().Background("EEF0F5").Border(0.5f).BorderColor("DDE2EC")
                            .AlignCenter().AlignMiddle().Padding(3).Text(week.KalenderWoche.ToString()).Bold();

                        foreach (var day in week.Tage)
                        {
                            var isCurrentMonth = day.Datum.Month == model.Monat;
                            var background = !isCurrentMonth ? "F6F7F9" : day.IstFeiertag ? "FFF0F1" : day.IstSonntag ? "FFF7F7" : "FFFFFF";

                            table.Cell().MinHeight(74).Background(background).Border(0.5f).BorderColor("DDE2EC")
                                .Padding(4).Column(column =>
                                {
                                    column.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(day.Datum.ToString("dd.MM.")).Bold()
                                            .FontColor(isCurrentMonth ? "172033" : "A0A8B8");
                                        if (day.IstFeiertag)
                                            row.AutoItem().Text("Feiertag").FontSize(6).FontColor("B42334");
                                    });

                                    if (!string.IsNullOrWhiteSpace(day.FeiertagName))
                                        column.Item().PaddingBottom(2).Text(day.FeiertagName).FontSize(6).FontColor("B42334");

                                    foreach (var slot in day.Slots.Where(slot => !string.IsNullOrWhiteSpace(slot.MitarbeiterName)))
                                    {
                                        column.Item().PaddingTop(2).BorderLeft(2).BorderColor(NormalizeColor(slot.Farbe))
                                            .PaddingLeft(3).Column(slotColumn =>
                                            {
                                                slotColumn.Item().Text(slot.MitarbeiterName!).SemiBold().FontSize(7);
                                                slotColumn.Item().Text($"{slot.SlotName}  {slot.Beginn}-{slot.Ende}")
                                                    .FontSize(6).FontColor("65708A");
                                            });
                                    }
                                });
                        }
                    }
                });

                page.Footer().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Text($"Schichtplaner - {standortName}").FontSize(7).FontColor("65708A");
                    row.AutoItem().DefaultTextStyle(style => style.FontSize(7).FontColor("65708A")).Text(text =>
                    {
                        text.Span("Seite ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void HeaderCell(TableCellDescriptor header, string text) =>
        header.Cell().Background("252744").PaddingVertical(5).PaddingHorizontal(3)
            .AlignCenter().Text(text).Bold().FontColor(Colors.White);

    private static string NormalizeColor(string? color) =>
        string.IsNullOrWhiteSpace(color) || !color.StartsWith('#') ? "5B5BD6" : color.TrimStart('#');
}
